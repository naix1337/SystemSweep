using System.Windows;

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
    }

    private void SaveSettings()
    {
        AppSettings.Instance.AutoAnalyze = chkAutoAnalyze.IsChecked ?? false;
        AppSettings.Instance.AutoClean = chkAutoClean.IsChecked ?? false;
        AppSettings.Instance.ShowNotifications = chkNotifications.IsChecked ?? false;
        AppSettings.Instance.SafetyBackup = chkSafetyBackup.IsChecked ?? false;
        AppSettings.Instance.Save();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        MessageBox.Show("✅ Settings saved successfully!", "Settings",
                        MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
