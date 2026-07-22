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

            // Show restore point dialog
            if (!AppSettings.Instance.RestorePointSkipped)
            {
                var restoreDialog = new RestoreDialog();
                bool? result = restoreDialog.ShowDialog();
                if (result == true && restoreDialog.RestorePointCreated)
                {
                    // Restore point was created - don't show again
                    AppSettings.Instance.RestorePointSkipped = true;
                    AppSettings.Instance.Save();
                }
                else if (result == true && restoreDialog.Skipped)
                {
                    // User skipped - ask next time too
                    // Don't save RestorePointSkipped = true
                }
            }

            // Handle command-line arguments
            if (e.Args.Length > 0)
            {
                HandleCommandLine(e.Args);
            }
        }

        private static void HandleCommandLine(string[] args)
        {
            bool silent = Array.Exists(args, a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
            bool clean = Array.Exists(args, a => a.Equals("--clean", StringComparison.OrdinalIgnoreCase));
            bool analyze = Array.Exists(args, a => a.Equals("--analyze", StringComparison.OrdinalIgnoreCase));
        }
    }
}
