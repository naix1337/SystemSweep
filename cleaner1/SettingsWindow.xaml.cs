using System.Windows;

namespace ModernFileCleaner
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow mainWindow;

        public SettingsWindow(MainWindow owner)
        {
            InitializeComponent();
            mainWindow = owner;
            Owner = owner;
            LoadSettings();
        }

        private void LoadSettings()
        {
            chkAutoAnalyze.IsChecked = mainWindow.AutoAnalyze;
            chkAutoClean.IsChecked = mainWindow.AutoClean;
            chkShowNotifications.IsChecked = mainWindow.ShowNotifications;
        }

        private void SaveSettings()
        {
            mainWindow.AutoAnalyze = chkAutoAnalyze.IsChecked ?? false;
            mainWindow.AutoClean = chkAutoClean.IsChecked ?? false;
            mainWindow.ShowNotifications = chkShowNotifications.IsChecked ?? false;
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