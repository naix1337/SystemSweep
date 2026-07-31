using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        AppSettings.Instance.Load();
        chkAutoAnalyze.IsChecked = AppSettings.Instance.AutoAnalyze;
        chkAutoClean.IsChecked = AppSettings.Instance.AutoClean;
        chkNotifications.IsChecked = AppSettings.Instance.ShowNotifications;
        chkSafetyBackup.IsChecked = AppSettings.Instance.SafetyBackup;

        switch (AppSettings.Instance.Theme)
        {
            case "Light": rdoLight.IsChecked = true; break;
            case "System": rdoSystem.IsChecked = true; break;
            default: rdoDark.IsChecked = true; break;
        }
        HighlightAccent(AppSettings.Instance.Accent);
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        string theme = rdoDark.IsChecked == true ? "Dark" : rdoLight.IsChecked == true ? "Light" : "System";
        AppSettings.Instance.Theme = theme;
        ThemeService.SetTheme(theme);
    }

    private void Accent_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is string hex)
        {
            AppSettings.Instance.Accent = hex;
            ThemeService.SetAccent(hex);
            HighlightAccent(hex);
        }
    }

    private void HighlightAccent(string hex)
    {
        foreach (var child in AccentPanel.Children)
        {
            if (child is Border b)
                b.BorderThickness = new Thickness((b.Tag as string) == hex ? 3 : 1);
        }
    }

    private void SaveSettings()
    {
        AppSettings.Instance.AutoAnalyze = chkAutoAnalyze.IsChecked ?? false;
        AppSettings.Instance.AutoClean = chkAutoClean.IsChecked ?? false;
        AppSettings.Instance.ShowNotifications = chkNotifications.IsChecked ?? false;
        AppSettings.Instance.SafetyBackup = chkSafetyBackup.IsChecked ?? false;
        // Theme + Accent were already written to AppSettings on selection
        AppSettings.Instance.Save();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        MessageBox.Show("✅ Settings saved successfully!", "Settings",
                        MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
