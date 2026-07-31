using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace ModernFileCleaner.Services;

public static class ThemeService
{
    public static string CurrentTheme { get; private set; } = "Dark";
    public static string CurrentAccent { get; private set; } = "#0078D4";

    public static void SetTheme(string theme)
    {
        CurrentTheme = theme;
        ApplicationTheme appTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            "System" => ApplicationTheme.Unknown,
            _ => ApplicationTheme.Dark
        };
        ApplicationThemeManager.Apply(appTheme);
    }

    public static void SetAccent(string hexColor)
    {
        CurrentAccent = hexColor;
        try
        {
            if (ColorConverter.ConvertFromString(hexColor) is Color color)
            {
                ApplicationTheme theme = CurrentTheme switch
                {
                    "Light" => ApplicationTheme.Light,
                    "System" => ApplicationTheme.Unknown,
                    _ => ApplicationTheme.Dark
                };
                ApplicationAccentColorManager.Apply(color, theme, false);
            }
        }
        catch { }
    }

    /// <summary>Apply theme + accent from the persisted settings.</summary>
    public static void ApplyFromSettings()
    {
        SetTheme(AppSettings.Instance.Theme);
        SetAccent(AppSettings.Instance.Accent);
    }

    public static void Toggle() => SetTheme(CurrentTheme == "Dark" ? "Light" : "Dark");
}
