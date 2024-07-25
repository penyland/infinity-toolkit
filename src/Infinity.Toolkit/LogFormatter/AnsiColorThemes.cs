namespace Infinity.Toolkit.LogFormatter;

public static class AnsiColorThemes
{
    public const string AnsiStyleReset = "\x1b[0m";

    public static AnsiColorTheme Code { get; } = new AnsiColorTheme(new Dictionary<ThemeStyle, string>
    {
        [ThemeStyle.Text] = "\x1b[38;5;0253m",
        [ThemeStyle.SecondaryText] = "\x1b[38;5;0246m",
        [ThemeStyle.Invalid] = "\x1b[38;5;0242m",
        [ThemeStyle.Null] = "\x1b[38;5;0038m",
        [ThemeStyle.Name] = "\x1b[38;5;0081m",
        [ThemeStyle.Number] = "\x1b[38;5;0151m",
        [ThemeStyle.String] = "\x1b[38;5;0216m",
        [ThemeStyle.Boolean] = "\x1b[38;5;0038m",
        [ThemeStyle.Scalar] = "\x1b[38;5;0079m",
        [ThemeStyle.LevelTrace] = "\x1b[37m",
        [ThemeStyle.LevelDebug] = "\x1b[37m",
        [ThemeStyle.LevelInformation] = "\x1b[37;1m",
        [ThemeStyle.LevelWarning] = "\x1b[38;5;0229m",
        [ThemeStyle.LevelError] = "\x1b[38;5;0197m\x1b[48;5;0238m",
        [ThemeStyle.LevelCritical] = "\x1b[38;5;0197m\x1b[48;5;0238m",
        [ThemeStyle.DefaultForegroundColor] = "\x1B[39m\x1B[22m",
        [ThemeStyle.DefaultBackgroundColor] = "\x1b[49m"
    });
}

public class AnsiColorTheme
{
    private readonly IReadOnlyDictionary<ThemeStyle, string> styles;

    public AnsiColorTheme(IReadOnlyDictionary<ThemeStyle, string> styles)
    {
        this.styles = styles;
    }

    public string GetStyle(ThemeStyle style)
    {
        return styles.TryGetValue(style, out var value) ? value : AnsiColorThemes.AnsiStyleReset;
    }
}

public enum ThemeStyle
{
    Text,
    SecondaryText,
    Invalid,
    Null,
    Name,
    Number,
    String,
    Boolean,
    Scalar,
    LevelTrace,
    LevelDebug,
    LevelInformation,
    LevelWarning,
    LevelError,
    LevelCritical,
    DefaultForegroundColor,
    DefaultBackgroundColor
}
