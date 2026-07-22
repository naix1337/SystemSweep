using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();
        public static bool ProtectionPassed { get; private set; } = true;
        public static MainWindow? AppMainWindow { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupArgs = e.Args;

            // Run anti-tamper checks
            ProtectionPassed = ProtectionService.RunStartupChecks();

            // Load settings
            AppSettings.Instance.Load();

            // Apply theme
            ThemeService.SetTheme(AppSettings.Instance.Theme);

            // Create main window first
            AppMainWindow = new MainWindow();
            AppMainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Show restore point dialog
            var restoreDialog = new RestoreDialog();
            bool? result = restoreDialog.ShowDialog();

            // Handle command-line arguments
            if (e.Args.Length > 0)
            {
                bool silent = Array.Exists(e.Args, a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
                bool clean = Array.Exists(e.Args, a => a.Equals("--clean", StringComparison.OrdinalIgnoreCase));
            }

            // Show main window
            AppMainWindow.Show();
            AppMainWindow.Activate();
            AppMainWindow.Focus();
        }
    }
}
