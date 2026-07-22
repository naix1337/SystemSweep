using System.Net.Http;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();
        public static bool ProtectionPassed { get; private set; } = true;
        public static MainWindow? AppMainWindow { get; private set; }
        private static System.Threading.Timer? _licenseTimer;
        private static string? _savedLicenseKey;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupArgs = e.Args;

            // Run anti-tamper checks
            ProtectionPassed = ProtectionService.RunStartupChecks();
            if (!ProtectionPassed)
            {
                MessageBox.Show("Security checks failed. The application may be tampered with.\n\nPlease download a fresh copy from the official source.",
                    "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            // Load settings
            AppSettings.Instance.Load();
            ThemeService.SetTheme(AppSettings.Instance.Theme);

            // Create main window (but don't show yet)
            AppMainWindow = new MainWindow();

            // === STEP 1: License Verification (EVERY startup) ===
            // IMPORTANT: Use Task.Run to avoid deadlock on UI thread
            _savedLicenseKey = LoadLicenseKey();
            bool licenseValid = Task.Run(async () =>
                await VerifyLicenseWithKeyzy(_savedLicenseKey)).GetAwaiter().GetResult();

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
                // Reload key after activation
                _savedLicenseKey = LoadLicenseKey();
            }

            // === STEP 2: Start periodic license check (every 4 minutes) ===
            if (!string.IsNullOrEmpty(_savedLicenseKey))
            {
                _licenseTimer = new System.Threading.Timer(
                    async _ => await PeriodicLicenseCheck(),
                    null,
                    TimeSpan.FromMinutes(4),
                    TimeSpan.FromMinutes(4));
            }

            // === STEP 3: Restore Point Dialog ===
            var restoreDialog = new RestoreDialog();
            restoreDialog.ShowDialog();

            // Handle CLI args
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
        /// Verifies license against Keyzy API.
        /// Returns true only if Keyzy confirms validity.
        /// If Keyzy is not configured, license is DENIED.
        /// </summary>
        private static async Task<bool> VerifyLicenseWithKeyzy(string? savedKey)
        {
            // No key = no license
            if (string.IsNullOrEmpty(savedKey))
                return false;

            // Check HWID first
            var hwid = GetCurrentHwid();
            if (!VerifyHwidMatch(savedKey, hwid))
                return false;

            // MUST verify with Keyzy API
            var keyzy = new KeyzyLicenseService();
            if (!keyzy.HasCredentials)
            {
                keyzy.Dispose();
                return false; // DENIED: no Keyzy config = no validation possible
            }

            bool isValid = await keyzy.ValidateKeyAsync(savedKey);
            keyzy.Dispose();

            if (!isValid)
            {
                // Keyzy rejected the key - delete local license
                try { System.IO.File.Delete("license.key"); } catch { }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks HWID match without calling Keyzy (fast check).
        /// </summary>
        private static bool VerifyHwidMatch(string savedKey, string currentHwid)
        {
            try
            {
                if (!System.IO.File.Exists("license.key")) return false;

                var encrypted = System.Convert.FromBase64String(
                    System.IO.File.ReadAllText("license.key"));
                var decrypted = System.Text.Encoding.UTF8.GetString(
                    System.Security.Cryptography.ProtectedData.Unprotect(
                        encrypted, null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser));

                var parts = decrypted.Split(':');
                if (parts.Length < 4 || parts[2] != "HWID") return false;
                if (parts[3] != currentHwid) return false;

                return true;
            }
            catch
            {
                try { System.IO.File.Delete("license.key"); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Periodic license check (runs every 4 minutes).
        /// If license becomes invalid, forces app to shutdown.
        /// </summary>
        private static async Task PeriodicLicenseCheck()
        {
            try
            {
                if (string.IsNullOrEmpty(_savedLicenseKey)) return;

                bool stillValid = await VerifyLicenseWithKeyzy(_savedLicenseKey);

                if (!stillValid)
                {
                    // Check if Keyzy is reachable or if it's a network error
                    var keyzyTest = new KeyzyLicenseService();
                    bool keyzyReachable = keyzyTest.HasCredentials;
                    keyzyTest.Dispose();

                    if (!keyzyReachable)
                    {
                        // Keyzy not configured - don't shut down (bail out)
                        return;
                    }

                    // Keyzy says invalid - shut down
                    _savedLicenseKey = null;
                    _licenseTimer?.Dispose();

                    AppMainWindow?.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Your license has become invalid or expired.\n\nThe application will now close.",
                            "License Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Current.Shutdown();
                    });
                }
            }
            catch (HttpRequestException)
            {
                // Network error - don't shut down, retry on next interval
                System.Diagnostics.Trace.WriteLine("[License] Periodic check failed (network)");
            }
            catch (TaskCanceledException)
            {
                // Timeout - retry next interval
                System.Diagnostics.Trace.WriteLine("[License] Periodic check timed out");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[License] Periodic check error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts the license key from the encrypted license file
        /// </summary>
        private static string? LoadLicenseKey()
        {
            try
            {
                if (!System.IO.File.Exists("license.key")) return null;
                var encrypted = System.Convert.FromBase64String(
                    System.IO.File.ReadAllText("license.key"));
                var decrypted = System.Text.Encoding.UTF8.GetString(
                    System.Security.Cryptography.ProtectedData.Unprotect(
                        encrypted, null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser));
                var parts = decrypted.Split(':');
                return parts.Length >= 2 ? parts[1] : null;
            }
            catch { return null; }
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
