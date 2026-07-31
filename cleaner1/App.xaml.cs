using System;
using System.Threading;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();
        public static bool ProtectionPassed { get; private set; } = true;
        public static MainWindow? AppMainWindow { get; private set; }

        /// <summary>Shared KeyAuth session (one instance for the app lifetime).</summary>
        public static KeyAuthService License { get; private set; } = null!;

        private static Timer? _licenseTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupArgs = e.Args;

            ProtectionPassed = ProtectionService.RunStartupChecks();
            if (!ProtectionPassed)
            {
                MessageBox.Show("Security checks failed. The application may be tampered with.\n\nPlease download a fresh copy from the official source.",
                    "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            AppSettings.Instance.Load();
            ThemeService.SetTheme(AppSettings.Instance.Theme);
            AppMainWindow = new MainWindow();

            // === License: KeyAuth (online only, session-based) ===
            AppEnv.EnsureLoaded();
            License = new KeyAuthService();

            if (!ValidateSavedKey())
            {
                var activationDialog = new ActivationDialog();
                bool? actResult = activationDialog.ShowDialog();
                if (actResult != true)
                {
                    Current.Shutdown();
                    return;
                }
            }

            if (AppLicense.IsFullAccess)
            {
                EnsurePeriodicCheck();

                var restoreDialog = new RestoreDialog();
                restoreDialog.ShowDialog();
            }

            AppMainWindow.Show();
            AppMainWindow.Activate();
            AppMainWindow.Focus();
        }

        /// <summary>
        /// Re-authenticates a saved key against KeyAuth. Returns true on success.
        /// On any failure (invalid/expired/offline) the stored key is deleted.
        /// </summary>
        private static bool ValidateSavedKey()
        {
            string? savedKey = LicenseStorage.Load();
            if (string.IsNullOrEmpty(savedKey)) return false;

            bool deleteKey = false;
            bool ok = Task.Run(async () =>
            {
                var init = await License.InitAsync();
                if (!init.Success) return false;
                var login = await License.LoginWithKeyAsync(savedKey!);
                deleteKey = login.IsRejection;
                return login.Success;
            }).GetAwaiter().GetResult();

            if (ok)
            {
                AppLicense.SetFull(License.Username, License.Subscription, License.ExpiryUtc);
                LoginNotifier.Notify(License, savedKey!);
                return true;
            }
            if (deleteKey) LicenseStorage.Delete();
            return false;
        }

        /// <summary>
        /// Periodically re-validates the KeyAuth session. Network errors are retried
        /// on the next interval; a hard rejection shuts the app down.
        /// </summary>
        public static void EnsurePeriodicCheck()
        {
            if (!AppLicense.IsFullAccess || _licenseTimer != null) return;
            _licenseTimer = new Timer(
                async _ => await PeriodicLicenseCheck(),
                null,
                TimeSpan.FromMinutes(4),
                TimeSpan.FromMinutes(4));
        }

        private static async Task PeriodicLicenseCheck()
        {
            try
            {
                var check = await License.CheckAsync();
                if (check.Success || !check.IsRejection) return;

                _licenseTimer?.Dispose();
                await AppMainWindow!.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Your license has become invalid or expired.\n\nThe application will now close.",
                        "License Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[License] Periodic check error: {ex.Message}");
            }
        }
    }
}
