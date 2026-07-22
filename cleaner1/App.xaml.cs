using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();
        public static bool ProtectionPassed { get; private set; } = true;

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

            // === SHOW RESTORE POINT DIALOG (every time) ===
            var restoreDialog = new RestoreDialog();
            bool? result = restoreDialog.ShowDialog();

            if (result == true && restoreDialog.RestorePointCreated)
            {
                // User created restore point - proceed
            }
            // else: user skipped or closed - still proceed

            // Handle command-line arguments
            if (e.Args.Length > 0)
            {
                bool silent = Array.Exists(e.Args, a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
                bool clean = Array.Exists(e.Args, a => a.Equals("--clean", StringComparison.OrdinalIgnoreCase));
            }

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
