using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System.Text;

namespace Infinity.Toolkit.LogFormatter;

public static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddCodeThemeConsoleFormatter(this ILoggingBuilder builder) =>
        builder.AddCodeThemeConsoleFormatter(_ => { });

    public static ILoggingBuilder AddCodeThemeConsoleFormatter(
        this ILoggingBuilder builder,
        Action<CustomOptions> configure)
    {
        builder
            .AddConsole(options => options.FormatterName = "CodeThemeConsoleFormatter")
            .AddConsoleFormatter<CodeThemeConsoleFormatter, CustomOptions>(configure);

        return builder;
    }
}

public sealed class CustomOptions : ConsoleFormatterOptions
{
    public string? CustomPrefix { get; set; } = "\"";

    public string? CustomSuffix { get; set; } = "\"";

    public AnsiColorTheme? Theme { get; set; } = AnsiColorThemes.Code;
}

public sealed class CodeThemeConsoleFormatter : ConsoleFormatter, IDisposable
{
    private const string AnsiStyleReset = "\x1b[0m";
    private readonly IDisposable? optionsReloadToken;
    private CustomOptions formatterOptions;

    public CodeThemeConsoleFormatter(IOptionsMonitor<CustomOptions> options) : base("CodeThemeConsoleFormatter")
    {
        optionsReloadToken = options.OnChange(options => formatterOptions = options);
        formatterOptions = options.CurrentValue;
        formatterOptions.TimestampFormat ??= "HH:mm:ss";
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        Func<TextWriter, TState, Exception?, string?> formatter = (writer, state, exception) =>
        {
            if (state is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
            {
                var messageFormat = stateProperties.FirstOrDefault(k => k.Key == "{OriginalFormat}").Value.ToString();
                var sb = new StringBuilder($"{messageFormat}");

                foreach (var item in stateProperties)
                {
                    if (item.Key.Equals("{OriginalFormat}"))
                    {
                        continue;
                    }

                    var itemThemeStyle = GetItemThemeStyle(item, formatterOptions.Theme!);
                    var defaultForegroundColor = formatterOptions.Theme!.GetStyle(ThemeStyle.Text);
                    var formattedItem = $"{itemThemeStyle}{formatterOptions.CustomPrefix}{item.Value}{formatterOptions.CustomSuffix}{defaultForegroundColor}";
                    sb.Replace($"{{{item.Key}}}", formattedItem);
                }

                return sb.ToString();
            }

            return state!.ToString();
        };

        var message = formatter.Invoke(textWriter, logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        // [HH:mm:ss INF]
        WriteTagStart(textWriter);
        WriteTimeStamp(textWriter);
        WriteLogLevel(textWriter, logEntry.LogLevel);
        WriteTagEnd(textWriter);

        // [Category]
        textWriter.Write(" ");
        WriteTagStart(textWriter);
        WriteThemeStyle(textWriter, ThemeStyle.SecondaryText);
        textWriter.Write(logEntry.Category);
        WriteTagEnd(textWriter);

        // [Message]
        textWriter.Write(" ");
        textWriter.Write(message);
        textWriter.WriteLine();
    }

    private void WriteThemeStyle(TextWriter textWriter, ThemeStyle themeStyle)
    {
        var themeStyleString = formatterOptions.Theme!.GetStyle(themeStyle);
        textWriter.Write(themeStyleString);
    }

    public void Dispose() => optionsReloadToken?.Dispose();

    private void WriteTimeStamp(TextWriter textWriter)
    {
        var now = formatterOptions.UseUtcTimestamp
            ? DateTime.UtcNow
            : DateTime.Now;

        var theme = formatterOptions.Theme!.GetStyle(ThemeStyle.SecondaryText);
        textWriter.Write($"{theme}{now.ToString(formatterOptions.TimestampFormat).TrimEnd()}{AnsiStyleReset}");
    }

    private void WritePrefix(TextWriter textWriter)
    {
        var theme = formatterOptions.Theme!.GetStyle(ThemeStyle.SecondaryText);
        textWriter.Write($"{theme}{formatterOptions.CustomPrefix}{AnsiStyleReset}");
    }

    private void WriteSuffix(TextWriter textWriter)
    {
        var theme = formatterOptions.Theme!.GetStyle(ThemeStyle.SecondaryText);
        textWriter.Write($"{theme}{formatterOptions.CustomSuffix}{AnsiStyleReset}");
    }

    private void WriteTagStart(TextWriter textWriter)
    {
        var theme = formatterOptions.Theme!.GetStyle(ThemeStyle.SecondaryText);
        textWriter.Write($"{theme}[{AnsiStyleReset}");
    }

    private void WriteTagEnd(TextWriter textWriter)
    {
        var theme = formatterOptions.Theme!.GetStyle(ThemeStyle.SecondaryText);
        textWriter.Write($"{theme}]{AnsiStyleReset}");
    }

    public static void WriteLogLevel(TextWriter textWriter, LogLevel logLevel)
    {
        var logLevelMessage = GetLogLevelString(logLevel);
        var theme = GetLogLevelThemeStyle(logLevel);
        textWriter.Write($" {theme}{logLevelMessage}{AnsiStyleReset}");
    }

    public static string GetLogLevelThemeStyle(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelTrace),
            LogLevel.Debug => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelDebug),
            LogLevel.Information => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelInformation),
            LogLevel.Warning => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelWarning),
            LogLevel.Error => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelError),
            LogLevel.Critical => AnsiColorThemes.Code.GetStyle(ThemeStyle.LevelCritical),
            _ => AnsiColorThemes.Code.GetStyle(ThemeStyle.Text),
        };

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Information => "INF",
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRI",
            _ => throw new NotImplementedException()
        };
    }

    private static string GetForegroundColorEscapeCode(ConsoleColor color) =>
        color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",

            _ => AnsiStyleReset
        };

    private static ConsoleColor GetItemColor(KeyValuePair<string, object> item) =>
        item.Value switch
        {
            bool _ => ConsoleColor.Blue,
            int _ => ConsoleColor.DarkGreen,
            string _ => ConsoleColor.DarkYellow,
            double _ => ConsoleColor.DarkBlue,
            decimal _ => ConsoleColor.DarkMagenta,
            DateTime _ => ConsoleColor.DarkCyan,
            TimeSpan _ => ConsoleColor.Gray,
            Guid _ => ConsoleColor.Red,
            _ => ConsoleColor.White,
        };

    private static string GetItemThemeStyle(KeyValuePair<string, object> item, AnsiColorTheme theme) =>
        item.Value switch
        {
            bool _ => theme.GetStyle(ThemeStyle.Boolean),
            int _ => theme.GetStyle(ThemeStyle.Number),
            string _ => theme.GetStyle(ThemeStyle.String),
            double _ => theme.GetStyle(ThemeStyle.Number),
            decimal _ => theme.GetStyle(ThemeStyle.Number),
            DateTime _ => theme.GetStyle(ThemeStyle.Scalar),
            TimeSpan _ => theme.GetStyle(ThemeStyle.Scalar),
            Guid _ => theme.GetStyle(ThemeStyle.Scalar),
            null => theme.GetStyle(ThemeStyle.Null),
            _ => theme.GetStyle(ThemeStyle.Text),
        };
}
