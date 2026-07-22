using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class LicensePage
{
    private readonly LicenseService _localLicense = new();

    public LicensePage()
    {
        InitializeComponent();
        LoadLicenseInfo();
    }

    private void LoadLicenseInfo()
    {
        // Show HWID always
        txtHwid.Text = GetHwid();

        string? savedKey = LoadSavedLicenseKey();
        var status = _localLicense.Status;

        if (!string.IsNullOrEmpty(savedKey) || status == LicenseService.LicenseStatus.Activated)
        {
            // Activated!
            ShowActivated(savedKey);
        }
        else if (status == LicenseService.LicenseStatus.Trial)
        {
            // Trial mode
            ShowTrial();
        }
        else if (status == LicenseService.LicenseStatus.Expired)
        {
            // Expired
            ShowExpired();
        }
        else
        {
            // No license - show activation form
            ShowNoLicense();
        }
    }

    private void ShowActivated(string? savedKey)
    {
        txtIcon.Text = "✅";
        txtTitle.Text = "License Active";
        txtSubtitle.Text = "All features unlocked";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0x4C, 0xAF, 0x50));
        txtLicenseStatus.Text = "✅ Fully Activated";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));
        txtLicenseDetail.Text = "License is valid and hardware-bound";

        txtLicStatus.Text = "✅ Active";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));

        if (!string.IsNullOrEmpty(savedKey))
            txtLicKey.Text = savedKey.Length > 24
                ? savedKey[..8] + "..." + savedKey[^8..]
                : savedKey;

        txtLicUser.Text = "Licensed User";
        txtLicExpiry.Text = "Never (Perpetual)";
        txtLicExpiry.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));
        txtTrialDays.Text = "—";

        // Hide activation form
        ActivationCard.Visibility = Visibility.Collapsed;
        btnContinue.Content = "⏭  Go to Dashboard";
    }

    private void ShowTrial()
    {
        txtIcon.Text = "🔓";
        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0xFF, 0xAA, 0x00));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xAA, 0x00));
        txtLicenseStatus.Text = $"🔓 Trial Mode ({_localLicense.TrialDaysRemaining} days)";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicenseDetail.Text = "Enter a license key to unlock all features permanently";

        txtLicStatus.Text = "🔓 Trial";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicKey.Text = "—";
        txtLicUser.Text = Environment.UserName;
        txtLicExpiry.Text = $"In {_localLicense.TrialDaysRemaining} days";
        txtLicExpiry.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtTrialDays.Text = $"{_localLicense.TrialDaysRemaining} days";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Continue Trial";
    }

    private void ShowExpired()
    {
        txtIcon.Text = "❌";
        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0xF4, 0x43, 0x36));
        txtLicenseStatus.Text = "❌ Trial Expired";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(244, 67, 54));
        txtLicenseDetail.Text = "Please purchase a license to continue";

        txtLicStatus.Text = "❌ Expired";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(244, 67, 54));
        txtTrialDays.Text = "0 days";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.IsEnabled = false;
    }

    private void ShowNoLicense()
    {
        txtIcon.Text = "🔐";
        txtTitle.Text = "Activate System Sweep";
        txtSubtitle.Text = "Enter your license key below";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0x00, 0x78, 0xD4));
        txtLicenseStatus.Text = "Not Activated";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4));
        txtLicenseDetail.Text = "Enter a license key to unlock all features";

        txtLicStatus.Text = "⏳ Not Activated";
        txtTrialDays.Text = "30 days";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Start Trial";
    }

    private async void ActivateKeyzy_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtLicenseDetail.Text = "Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtLicenseStatus.Text = "🔍 Validating...";

        var keyzy = new KeyzyLicenseService();
        bool valid = await keyzy.ValidateKeyAsync(key);

        if (valid)
        {
            SaveLicenseLocally(key);
            txtLicenseDetail.Text = "✅ License activated!";
            LoadLicenseInfo();
            MessageBox.Show($"✅ License activated successfully!\n\nYour device has been bound to this license.\nHWID: {GetHwid()[..16]}...",
                          "Activation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            txtLicenseDetail.Text = $"❌ {keyzy.ErrorMessage}";
            btnActivate.IsEnabled = true;
        }

        keyzy.Dispose();
    }

    private static void SaveLicenseLocally(string key)
    {
        try
        {
            var hwid = GetHwid();
            var data = $"KEYZY:{key}:HWID:{hwid}";
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
            File.WriteAllText("license.key", Convert.ToBase64String(encrypted));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[License] Save error: {ex.Message}");
        }
    }

    private static string? LoadSavedLicenseKey()
    {
        try
        {
            if (!File.Exists("license.key")) return null;
            var encrypted = Convert.FromBase64String(File.ReadAllText("license.key"));
            var decrypted = Encoding.UTF8.GetString(
                ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
            var parts = decrypted.Split(':');
            if (parts.Length >= 2 && parts[0] == "KEYZY")
                return parts[1]; // The license key
        }
        catch { }
        return null;
    }

    private static string GetHwid()
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

    private void BuyLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://keyzy.io") { UseShellExecute = true });
        }
        catch { }
    }

    private void ContinueTrial_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
