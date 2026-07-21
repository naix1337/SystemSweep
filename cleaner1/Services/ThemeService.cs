using Wpf.Ui.Appearance;

namespace ModernFileCleaner.Services;

public static class ThemeService
{
    public static string CurrentTheme { get; private set; } = "Dark";

    public static void SetTheme(string theme)
    {
        CurrentTheme = theme;
        ApplicationTheme appTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            _ => ApplicationTheme.Dark
        };
        ApplicationThemeManager.Apply(appTheme);
    }

    public static void Toggle()
    {
        SetTheme(CurrentTheme == "Dark" ? "Light" : "Dark");
    }
}
