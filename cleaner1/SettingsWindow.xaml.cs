using System.Windows;

namespace ModernFileCleaner
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
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
        }

        private void SaveSettings()
        {
            AppSettings.Instance.AutoAnalyze = chkAutoAnalyze.IsChecked ?? false;
            AppSettings.Instance.AutoClean = chkAutoClean.IsChecked ?? false;
            AppSettings.Instance.ShowNotifications = chkNotifications.IsChecked ?? false;
            AppSettings.Instance.Save();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}