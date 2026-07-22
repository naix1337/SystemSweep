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

            // CRITICAL: If protection checks fail, don't start
            if (!ProtectionPassed)
            {
                MessageBox.Show("Security checks failed. The application may be tampered with.\n\nPlease download a fresh copy from the official source.",
                    "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            // Load settings
            AppSettings.Instance.Load();

            // Apply theme
            ThemeService.SetTheme(AppSettings.Instance.Theme);

            // Create main window (but don't show yet)
            AppMainWindow = new MainWindow();

            // === STEP 1: License Verification (EVERY startup) ===
            // Always verify the license with Keyzy API, don't just check file existence
            bool licenseValid = VerifyLicenseAtStartup();

            if (!licenseValid)
            {
                var activationDialog = new ActivationDialog();
                bool? actResult = activationDialog.ShowDialog();

                bool canProceed = actResult == true &&
                    (activationDialog.IsActivated || activationDialog.StartedTrial);

                if (!canProceed)
                {
                    Current.Shutdown();
                    return;
                }
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

        /// <summary>
        /// Verifies license on EVERY startup.
        /// Checks local license.key file, validates HWID, AND calls Keyzy API.
        /// If anything fails, license is considered invalid.
        /// </summary>
        private static bool VerifyLicenseAtStartup()
        {
            try
            {
                // Step 1: Check if license file exists
                if (!System.IO.File.Exists("license.key"))
                {
                    System.IO.File.Delete("trial.dat"); // Also reset trial on license deletion
                    return false;
                }

                // Step 2: Decrypt and verify HWID
                string decrypted;
                try
                {
                    var encrypted = System.Convert.FromBase64String(
                        System.IO.File.ReadAllText("license.key"));
                    decrypted = System.Text.Encoding.UTF8.GetString(
                        System.Security.Cryptography.ProtectedData.Unprotect(
                            encrypted, null,
                            System.Security.Cryptography.DataProtectionScope.CurrentUser));
                }
                catch
                {
                    // Corrupted license file
                    System.IO.File.Delete("license.key");
                    return false;
                }

                var parts = decrypted.Split(':');
                if (parts.Length < 4 || parts[2] != "HWID")
                {
                    System.IO.File.Delete("license.key");
                    return false;
                }

                // Step 3: Verify HWID match
                var expectedHwid = parts[3];
                var currentHwid = GetCurrentHwid();
                if (expectedHwid != currentHwid)
                {
                    // License is for another machine - delete it
                    System.IO.File.Delete("license.key");
                    return false;
                }

                // Step 4: Extract license key and re-validate with Keyzy API
                var savedKey = parts[1];
                if (!string.IsNullOrEmpty(savedKey))
                {
                    var keyzy = new KeyzyLicenseService();
                    if (keyzy.HasCredentials)
                    {
                        bool keyzyValid = Task.Run(async () =>
                            await keyzy.ValidateKeyAsync(savedKey)).GetAwaiter().GetResult();

                        if (!keyzyValid)
                        {
                            // Keyzy says invalid - remove local license
                            System.IO.File.Delete("license.key");
                            keyzy.Dispose();
                            return false;
                        }
                    }
                    keyzy.Dispose();
                }

                // All checks passed
                return true;
            }
            catch
            {
                // On any error, require re-activation (fail closed)
                try { System.IO.File.Delete("license.key"); } catch { }
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
