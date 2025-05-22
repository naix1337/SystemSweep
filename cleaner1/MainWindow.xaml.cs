using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;

namespace ModernFileCleaner
{
    public partial class MainWindow : Window
    {
        private long totalSpaceToClean = 0;
        private DispatcherTimer progressTimer;
        private DateTime lastCleaned = DateTime.MinValue;
        private bool isAdmin = false;

        // Settings
        public bool AutoAnalyze { get; set; } = false;
        public bool AutoClean { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;

        public MainWindow()
        {
            // Admin check
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                var result = MessageBox.Show("This application requires administrator privileges to use all features.\n\nRestart as Administrator?",
                                           "Admin Rights Required",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Process.GetCurrentProcess().MainModule.FileName,
                            UseShellExecute = true,
                            Verb = "runas"
                        });
                    }
                    catch { }
                    Application.Current.Shutdown();
                    return;
                }
            }

            InitializeComponent();
            txtAdminStatus.Text = isAdmin ? "Yes" : "No";
            txtAdminStatus.Foreground = isAdmin ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;
            LoadSettings();
            SetupProgressTimer();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AutoAnalyze)
            {
                btnAnalyze_Click(null, null);
            }
        }

        private void SetupProgressTimer()
        {
            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(100);
            progressTimer.Tick += ProgressTimer_Tick;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (progressBar.Value < progressBar.Maximum)
            {
                progressBar.Value += 1;
            }
            else
            {
                progressTimer.Stop();
            }
        }

        private void LoadSettings()
        {
            if (lastCleaned != DateTime.MinValue)
            {
                txtLastCleaned.Text = lastCleaned.ToString("g");
            }
        }

        private void SaveSettings()
        {
            lastCleaned = DateTime.Now;
            txtLastCleaned.Text = lastCleaned.ToString("g");
        }

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            totalSpaceToClean = 0;

            if (chkTemporaryFiles.IsChecked == true)
            {
                totalSpaceToClean += CalculateTempFilesSize();
            }

            if (chkRecycleBin.IsChecked == true)
            {
                totalSpaceToClean += GetRecycleBinSize();
            }

            if (chkDownloadCache.IsChecked == true)
            {
                totalSpaceToClean += CalculateDownloadCacheSize();
            }

            if (chkThumbnailCache.IsChecked == true)
            {
                totalSpaceToClean += CalculateThumbnailCacheSize();
            }

            if (chkWindowsLogs.IsChecked == true)
            {
                totalSpaceToClean += CalculateWindowsLogFilesSize();
            }

            if (chkErrorReports.IsChecked == true)
            {
                totalSpaceToClean += CalculateErrorReportsSize();
            }

            if (chkInstallerTemp.IsChecked == true)
            {
                totalSpaceToClean += CalculateInstallerTempSize();
            }

            if (chkStoreCache.IsChecked == true)
            {
                totalSpaceToClean += 100 * 1024 * 1024; // Estimated size
            }

            if (chkWindowsOld.IsChecked == true && isAdmin)
            {
                totalSpaceToClean += CalculateWindowsOldSize();
            }

            if (chkMemoryDumps.IsChecked == true && isAdmin)
            {
                totalSpaceToClean += CalculateMemoryDumpsSize();
            }

            txtSpaceToClean.Text = $"{totalSpaceToClean / (1024 * 1024)} MB";
            txtProgressStatus.Text = "Analysis complete. Ready to clean.";
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            if (totalSpaceToClean == 0)
            {
                MessageBox.Show("Please analyze first or select cleaning options.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if ((chkWindowsOld.IsChecked == true || chkMemoryDumps.IsChecked == true) && !isAdmin)
            {
                MessageBox.Show("Admin rights required for selected operations.", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressTimer.Start();

            try
            {
                if (chkTemporaryFiles.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning temporary files...";
                    CleanTempFiles();
                }

                if (chkRecycleBin.IsChecked == true)
                {
                    txtProgressStatus.Text = "Emptying recycle bin...";
                    EmptyRecycleBin();
                }

                if (chkDownloadCache.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning download cache...";
                    CleanDownloadCache();
                }

                if (chkThumbnailCache.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning thumbnail cache...";
                    CleanThumbnailCache();
                }

                if (chkWindowsLogs.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning Windows log files...";
                    CleanWindowsLogFiles();
                }

                if (chkErrorReports.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning error reports...";
                    CleanErrorReports();
                }

                if (chkInstallerTemp.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning installer temp files...";
                    CleanInstallerTempFiles();
                }

                if (chkStoreCache.IsChecked == true)
                {
                    txtProgressStatus.Text = "Cleaning Microsoft Store cache...";
                    CleanStoreCache();
                }

                if (chkWindowsOld.IsChecked == true && isAdmin)
                {
                    txtProgressStatus.Text = "Cleaning Windows.old folder...";
                    CleanWindowsOld();
                }

                if (chkMemoryDumps.IsChecked == true && isAdmin)
                {
                    txtProgressStatus.Text = "Cleaning memory dumps...";
                    CleanMemoryDumps();
                }

                SaveSettings();
                txtProgressStatus.Text = "Cleaning completed successfully!";
                totalSpaceToClean = 0;
                txtSpaceToClean.Text = "0 MB";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during cleaning: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtProgressStatus.Text = "Cleaning failed.";
            }
            finally
            {
                progressTimer.Stop();
                progressBar.Value = progressBar.Maximum;
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        #region Cleaning Methods

        private long CalculateTempFilesSize()
        {
            long size = 0;

            string tempPath = Path.GetTempPath();
            size += GetDirectorySize(tempPath);

            string userTempPath = Environment.GetEnvironmentVariable("TEMP");
            if (!string.IsNullOrEmpty(userTempPath) && Directory.Exists(userTempPath))
            {
                size += GetDirectorySize(userTempPath);
            }

            return size;
        }

        private void CleanTempFiles()
        {
            string tempPath = Path.GetTempPath();
            CleanDirectory(tempPath);

            string userTempPath = Environment.GetEnvironmentVariable("TEMP");
            if (!string.IsNullOrEmpty(userTempPath)) CleanDirectory(userTempPath);
        }

        private long GetRecycleBinSize()
        {
            return 100 * 1024 * 1024; // Approximation
        }

        private void EmptyRecycleBin()
        {
            try
            {
                // Alternative implementation
                string recyclePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "RecycleBin");

                if (Directory.Exists(recyclePath))
                {
                    CleanDirectory(recyclePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error emptying recycle bin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long CalculateDownloadCacheSize()
        {
            string downloadCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            return Directory.Exists(downloadCachePath) ? GetDirectorySize(downloadCachePath) : 0;
        }

        private void CleanDownloadCache()
        {
            string downloadCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            if (Directory.Exists(downloadCachePath)) CleanDirectory(downloadCachePath);
        }

        private long CalculateThumbnailCacheSize()
        {
            string thumbnailCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Explorer");
            return Directory.Exists(thumbnailCachePath) ?
                GetDirectorySize(thumbnailCachePath, "thumbcache_*.db") : 0;
        }

        private void CleanThumbnailCache()
        {
            string thumbnailCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Explorer");
            if (Directory.Exists(thumbnailCachePath))
            {
                foreach (var file in Directory.GetFiles(thumbnailCachePath, "thumbcache_*.db"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private long CalculateWindowsLogFilesSize()
        {
            string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            long size = 0;
            if (Directory.Exists(windowsPath))
            {
                foreach (var file in Directory.GetFiles(windowsPath, "*.log", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        size += fi.Length;
                    }
                    catch { }
                }
            }
            return size;
        }

        private void CleanWindowsLogFiles()
        {
            string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (Directory.Exists(windowsPath))
            {
                foreach (var file in Directory.GetFiles(windowsPath, "*.log", SearchOption.AllDirectories))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private long CalculateErrorReportsSize()
        {
            string werPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "WER");
            return Directory.Exists(werPath) ? GetDirectorySize(werPath) : 0;
        }

        private void CleanErrorReports()
        {
            string werPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "WER");
            if (Directory.Exists(werPath))
            {
                CleanDirectory(werPath);
            }
        }

        private long CalculateInstallerTempSize()
        {
            string installerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Installer");
            return Directory.Exists(installerPath) ? GetDirectorySize(installerPath, "*.tmp") : 0;
        }

        private void CleanInstallerTempFiles()
        {
            string installerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Installer");
            if (Directory.Exists(installerPath))
            {
                foreach (var file in Directory.GetFiles(installerPath, "*.tmp"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        private void CleanStoreCache()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wsreset.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cleaning Store cache: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long CalculateWindowsOldSize()
        {
            string windowsOld = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "..", "Windows.old");
            return Directory.Exists(windowsOld) ? GetDirectorySize(windowsOld) : 0;
        }

        private void CleanWindowsOld()
        {
            if (!isAdmin)
            {
                MessageBox.Show("Admin rights required to delete Windows.old folder.",
                              "Permission Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string windowsOld = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "..", "Windows.old");

            if (Directory.Exists(windowsOld))
            {
                try
                {
                    CleanDirectory(windowsOld);
                    Directory.Delete(windowsOld, true);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Admin rights required to delete Windows.old folder.",
                                  "Permission Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error cleaning Windows.old: {ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private long CalculateMemoryDumpsSize()
        {
            long size = 0;
            string dumpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "MEMORY.DMP");

            if (File.Exists(dumpPath))
            {
                try
                {
                    var fi = new FileInfo(dumpPath);
                    size += fi.Length;
                }
                catch { }
            }

            string minidumpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Minidump");

            if (Directory.Exists(minidumpPath))
            {
                size += GetDirectorySize(minidumpPath);
            }

            return size;
        }

        private void CleanMemoryDumps()
        {
            if (!isAdmin)
            {
                MessageBox.Show("Admin rights required to delete memory dump files.",
                              "Permission Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Main memory dump
            string dumpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "MEMORY.DMP");

            if (File.Exists(dumpPath))
            {
                try { File.Delete(dumpPath); }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Admin rights required to delete memory dump files.",
                                  "Permission Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch { }
            }

            // Minidumps
            string minidumpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Minidump");

            if (Directory.Exists(minidumpPath))
            {
                try
                {
                    CleanDirectory(minidumpPath);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Admin rights required to delete minidump files.",
                                  "Permission Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch { }
            }
        }

        #endregion

        #region Helper Methods

        private long GetDirectorySize(string path, string searchPattern = "*.*")
        {
            long size = 0;

            try
            {
                foreach (string file in Directory.GetFiles(path, searchPattern))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        size += fi.Length;
                    }
                    catch { }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        size += GetDirectorySize(dir, searchPattern);
                    }
                    catch { }
                }
            }
            catch { }

            return size;
        }

        private void CleanDirectory(string path, string searchPattern = "*.*")
        {
            try
            {
                foreach (string file in Directory.GetFiles(path, searchPattern))
                {
                    try { File.Delete(file); } catch { }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        #endregion
    }
}