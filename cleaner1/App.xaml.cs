using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupArgs = e.Args;

            // Load settings
            AppSettings.Instance.Load();

            // Apply theme
            ThemeService.SetTheme(AppSettings.Instance.Theme);

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

            // CLI flags are handled by MainWindow when it loads
            // They're passed through StartupArgs
        }
    }
}
