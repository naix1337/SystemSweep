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

            // Create main window (but don't show yet)
            AppMainWindow = new MainWindow();

            // === STEP 1: Activation Dialog (if not activated) ===
            if (!IsLicenseActivated())
            {
                var activationDialog = new ActivationDialog();
                bool? actResult = activationDialog.ShowDialog();

                if (actResult == true && activationDialog.IsActivated)
                {
                    // User activated - proceed
                }
                else if (actResult == true && activationDialog.StartedTrial)
                {
                    // User chose trial - proceed
                }
                // else: dialog closed unexpectedly - still proceed
            }

            // === STEP 2: Restore Point Dialog (always) ===
            var restoreDialog = new RestoreDialog();
            restoreDialog.ShowDialog();

            // Handle command-line arguments
            if (e.Args.Length > 0)
            {
                bool silent = Array.Exists(e.Args, a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
            }

            // Show main window
            AppMainWindow.Show();
            AppMainWindow.Activate();
            AppMainWindow.Focus();
        }

        private static bool IsLicenseActivated()
        {
            try
            {
                // Check Keyzy license with HWID binding
                if (System.IO.File.Exists("license.key"))
                {
                    var encrypted = System.Convert.FromBase64String(
                        System.IO.File.ReadAllText("license.key"));
                    var decrypted = System.Text.Encoding.UTF8.GetString(
                        System.Security.Cryptography.ProtectedData.Unprotect(
                            encrypted, null,
                            System.Security.Cryptography.DataProtectionScope.CurrentUser));

                    // Verify HWID match
                    var parts = decrypted.Split(':');
                    if (parts.Length >= 4 && parts[2] == "HWID")
                    {
                        var expectedHwid = parts[3];
                        var currentHwid = GetCurrentHwid();
                        if (expectedHwid == currentHwid)
                            return true;
                        // HWID mismatch - license is for another machine
                        System.IO.File.Delete("license.key");
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string GetCurrentHwid()
        {
            try
            {
                using var mc = new System.Management.ManagementClass("Win32_Processor");
                using var items = mc.GetInstances();
                foreach (var item in items)
                    return (item["ProcessorId"]?.ToString() ?? "") + Environment.MachineName;
            }
            catch { }
            return Environment.MachineName;
        }
    }
}
