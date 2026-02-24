
#:property TargetFramework=net10.0
#:property PublishAot=false
#:package System.CommandLine@2.0.0
#:package Spectre.Console@*

using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;

// ---------------------- Command line ----------------------
// Options
var startOption = new Option<string>("--start", "-s")
{
    Description = "Start directory for search (default: current directory)",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

var maxDepthOption = new Option<int>("--max-depth")
{
    Description = "How many levels up to search for a .git dir (default: 6)",
    DefaultValueFactory = _ => 6
};

var outOption = new Option<string>("--out")
{
    Description = "Output JSON file path (default: ./git-info.json in --start)",
    DefaultValueFactory = _ => Path.Combine(Directory.GetCurrentDirectory(), "git-info.json")
};

var verboseOption = new Option<bool>("--verbose")
{
    Description = "Print diagnostic details"
};

var failOnMissingOption = new Option<bool>("--fail-on-missing")
{
    Description = "Exit with code 2 if no repository is found"
};

var silentOption = new Option<bool>("--silent")
{
    Description = "Write JSON only, no console output"
};

// Root command
var root = new RootCommand("Writes Git branch/commit info to a JSON file (no worktree support).");
root.Options.Add(startOption);
root.Options.Add(maxDepthOption);
root.Options.Add(outOption);
root.Options.Add(verboseOption);
root.Options.Add(failOnMissingOption);
root.Options.Add(silentOption);

// Handler
// root.SetAction((string start, int maxDepth, string outputPath, bool verbose, bool failOnMissing, bool silent) =>
root.SetAction(async (parseResult, cancellationToken) =>
{
    var start = parseResult.GetValue(startOption);
    var maxDepth = parseResult.GetValue(maxDepthOption);
    var outputPath = parseResult.GetValue(outOption);
    var verbose = parseResult.GetValue(verboseOption);
    var failOnMissing = parseResult.GetValue(failOnMissingOption);
    var silent = parseResult.GetValue(silentOption);

    // Internal logger respecting --silent and --verbose
    void Log(string msg)
    {
        if (silent) return;
        if (verbose) AnsiConsole.MarkupLine($"[dim]{Escape(msg)}[/]");
    }

    try
    {
        var repo = FindRepositoryNoWorktrees(start, maxDepth);
        if (repo is null)
        {
            var message = $"No Git repository ('.git' directory) found within {maxDepth} level(s) above '{Path.GetFullPath(start)}'.";
            if (!silent) AnsiConsole.MarkupLine($"[red]{Escape(message)}[/]");

            if (failOnMissing)
                Environment.ExitCode = 2;
            else
                Environment.ExitCode = 0;

            return await Task.FromResult(0);
        }

        Log($"Repository root: {repo.Value.RepositoryRoot}");
        Log($".git dir      : {repo.Value.GitDirPath}");

        var head = ReadHead(repo.Value.GitDirPath);
        if (head is null)
            throw new InvalidOperationException($"Missing HEAD in '{repo.Value.GitDirPath}'.");

        Log($"HEAD raw: {head.Raw.Trim()}");

        string? commitSha = null;
        string resolvedFrom = "unknown";

        if (!head.Detached && head.RefPath is not null)
        {
            if (TryReadLooseRefSha(repo.Value.GitDirPath, head.RefPath, out commitSha))
                resolvedFrom = "loose";
            else if (TryReadPackedRefsSha(repo.Value.GitDirPath, head.RefPath, out commitSha))
                resolvedFrom = "packed";
            else
                Log($"Could not resolve ref '{head.RefPath}' via loose or packed refs.");
        }
        else
        {
            if (TryNormalizeSha(head.DetachedSha, out var sha))
            {
                commitSha = sha;
                resolvedFrom = "head";
            }
        }

        string? originUrl = TryReadOriginUrl(repo.Value.GitDirPath);

        var info = new
        {
            repositoryRoot = repo.Value.RepositoryRoot,
            gitDir = repo.Value.GitDirPath,
            headRef = head.RefPath,
            branch = head.BranchName,
            commit = commitSha,
            detached = head.Detached,
            originUrl = originUrl,
            resolvedFrom = commitSha is null ? null : resolvedFrom,
            timestampUtc = DateTime.UtcNow.ToString("o"),
            searchedStart = Path.GetFullPath(start),
            maxDepth = maxDepth,
            worktreesSupported = false
        };

        // Write JSON
        var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
        var outFull = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outFull)!);
        File.WriteAllText(outFull, json, new UTF8Encoding(false));

        if (!silent)
        {
            // Pretty output with Spectre.Console
            var table = new Table().Border(TableBorder.Rounded).AddColumn("[bold]Field[/]").AddColumn("[bold]Value[/]");
            table.AddRow("repositoryRoot", Escape(info.repositoryRoot));
            table.AddRow("gitDir", Escape(info.gitDir));
            table.AddRow("branch", info.branch ?? "(detached)");
            table.AddRow("headRef", info.headRef ?? "(none)");
            table.AddRow("commit", info.commit ?? "(unresolved)");
            table.AddRow("detached", info.detached.ToString());
            table.AddRow("originUrl", info.originUrl ?? "(none)");
            table.AddRow("resolvedFrom", info.resolvedFrom ?? "(n/a)");
            table.AddRow("output", outFull);

            AnsiConsole.Write(new Rule("[green]Build Info[/]").RuleStyle("green").LeftJustified());
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[green]Git Info[/]").RuleStyle("green").LeftJustified());
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]Wrote Git info to[/] [bold]{Escape(outFull)}[/]");
        }
    }
    catch (Exception ex)
    {
        if (!silent)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Escape(ex.Message)}");
            if (verbose) AnsiConsole.WriteException(ex);
        }
        Environment.ExitCode = 1;
    }

    return await Task.FromResult(0);
});

return await root.Parse(args).InvokeAsync();

// ---------------------- Helpers ----------------------

static string Escape(string text) => Markup.Escape(text);

static (string RepositoryRoot, string GitDirPath)? FindRepositoryNoWorktrees(string start, int maxDepth)
{
    string current = Path.GetFullPath(start);
    for (int depth = 0; depth <= maxDepth; depth++)
    {
        string dotGitDir = Path.Combine(current, ".git");
        if (Directory.Exists(dotGitDir))
        {
            return (current, dotGitDir);
        }

        // Explicitly ignore ".git" as a file (worktrees/submodules not supported)
        if (File.Exists(dotGitDir))
            return null;

        var parent = Directory.GetParent(current)?.FullName;
        if (string.IsNullOrEmpty(parent)) break;
        current = parent!;
    }
    return null;
}

HeadInfo? ReadHead(string gitDirPath)
{
    string headPath = Path.Combine(gitDirPath, "HEAD");
    if (!File.Exists(headPath)) return null;

    string raw = File.ReadAllText(headPath);
    string trimmed = raw.Trim();

    if (trimmed.StartsWith("ref:"))
    {
        string refPath = trimmed.Substring("ref:".Length).Trim();
        string branchName = refPath.Replace('\\', '/').Split('/').LastOrDefault() ?? "";
        return new HeadInfo(raw, Detached: false, RefPath: refPath, BranchName: branchName, DetachedSha: null);
    }
    else
    {
        string token = trimmed.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? trimmed;
        return new HeadInfo(raw, Detached: true, RefPath: null, BranchName: null, DetachedSha: token);
    }
}

static bool TryReadLooseRefSha(string gitDirPath, string refPath, out string? sha)
{
    string refFile = Path.Combine(gitDirPath, refPath.Replace('/', Path.DirectorySeparatorChar));
    if (File.Exists(refFile))
    {
        var content = File.ReadAllText(refFile).Trim();
        if (TryNormalizeSha(content, out sha)) return true;
    }
    sha = null;
    return false;
}

static bool TryReadPackedRefsSha(string gitDirPath, string refPath, out string? sha)
{
    string packed = Path.Combine(gitDirPath, "packed-refs");
    if (!File.Exists(packed))
    {
        sha = null;
        return false;
    }

    using var sr = new StreamReader(packed);
    string? line;
    while ((line = sr.ReadLine()) is not null)
    {
        line = line.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;
        if (line.StartsWith("^")) continue;
        int sp = line.IndexOf(' ');
        if (sp <= 0) continue;

        string candidateSha = line.Substring(0, sp).Trim();
        string candidateRef = line.Substring(sp + 1).Trim();

        if (string.Equals(candidateRef, refPath, StringComparison.Ordinal))
        {
            if (TryNormalizeSha(candidateSha, out sha))
                return true;
        }
    }
    sha = null;
    return false;
}

static bool TryNormalizeSha(string? raw, out string? sha)
{
    sha = null;
    if (string.IsNullOrWhiteSpace(raw)) return false;
    var token = raw.Trim();
    if (Regex.IsMatch(token, "^[0-9a-fA-F]{40}$"))
    {
        sha = token.ToLowerInvariant();
        return true;
    }
    return false;
}

static string? TryReadOriginUrl(string gitDirPath)
{
    string configPath = Path.Combine(gitDirPath, "config");
    if (!File.Exists(configPath)) return null;

    string? currentSection = null;
    foreach (var line in File.ReadLines(configPath))
    {
        var l = line.Trim();
        if (l.Length == 0 || l.StartsWith("#") || l.StartsWith(";")) continue;

        if (l.StartsWith("[") && l.EndsWith("]"))
        {
            currentSection = l.Trim('[', ']');
            continue;
        }

        int eq = l.IndexOf('=');
        if (eq > 0)
        {
            string key = l[..eq].Trim();
            string value = l[(eq + 1)..].Trim();

            if (string.Equals(currentSection, "remote \"origin\"", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(key, "url", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
    }
    return null;
}

record HeadInfo(string Raw, bool Detached, string? RefPath, string? BranchName, string? DetachedSha);