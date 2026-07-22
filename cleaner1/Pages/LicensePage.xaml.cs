using System.Diagnostics;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class LicensePage
{
    private readonly LicenseService _localLicense = new();

    public LicensePage()
    {
        InitializeComponent();
        LoadStatus();
    }

    private void LoadStatus()
    {
        var status = _localLicense.Status;
        switch (status)
        {
            case LicenseService.LicenseStatus.Activated:
                SetStatus("✅ Fully Activated", _localLicense.LicensedTo ?? "User",
                    System.Windows.Media.Color.FromRgb(76, 175, 80), "Activated");
                btnActivate.IsEnabled = false;
                txtLicenseKey.IsEnabled = false;
                break;

            case LicenseService.LicenseStatus.Trial:
                SetStatus($"🔓 Trial Mode ({_localLicense.TrialDaysRemaining} days left)",
                    "Some features may be limited",
                    System.Windows.Media.Color.FromRgb(255, 193, 7), "Trial");
                break;

            case LicenseService.LicenseStatus.Expired:
                SetStatus("❌ Trial Expired",
                    "Please purchase a license",
                    System.Windows.Media.Color.FromRgb(244, 67, 54), "Expired");
                break;
        }
    }

    private void SetStatus(string title, string detail, System.Windows.Media.Color color, string type)
    {
        txtLicenseStatus.Text = title;
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(color);
        txtLicenseDetail.Text = detail;
        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, color.R, color.G, color.B));
    }

    private async void ActivateKeyzy_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "Please enter a license key from Keyzy.io";
            return;
        }

        btnActivate.IsEnabled = false;
        txtStatus.Text = "🔍 Validating with Keyzy.io...";

        var keyzy = new KeyzyLicenseService();
        bool valid = await keyzy.ValidateKeyAsync(key);

        if (valid)
        {
            txtStatus.Text = $"✅ Activated! Licensed to: {keyzy.LicensedTo}";
            SetStatus("✅ Fully Activated", $"Licensed to: {keyzy.LicensedTo}",
                System.Windows.Media.Color.FromRgb(76, 175, 80), "Activated");
            btnActivate.IsEnabled = false;
            txtLicenseKey.IsEnabled = false;
            SaveLicenseLocally(key);
            MessageBox.Show($"✅ License activated via Keyzy.io!\n\nUser: {keyzy.LicensedTo}\nProduct: {keyzy.ProductName ?? "System Sweep"}",
                "Activation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            txtStatus.Text = $"❌ {keyzy.ErrorMessage}";
            btnActivate.IsEnabled = true;
        }

        keyzy.Dispose();
    }

    private void ActivateOffline_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "Please enter an offline license key";
            return;
        }

        if (_localLicense.ValidateLicenseKey(key))
        {
            txtStatus.Text = "✅ Offline license activated!";
            LoadStatus();
        }
        else
        {
            txtStatus.Text = "❌ Invalid offline license key";
        }
    }

    private static void SaveLicenseLocally(string key)
    {
        try
        {
            var data = Convert.ToBase64String(
                System.Security.Cryptography.ProtectedData.Protect(
                    System.Text.Encoding.UTF8.GetBytes($"KEYZY:{key}"),
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser));
            System.IO.File.WriteAllText("license.key", data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[License] Save error: {ex.Message}");
        }
    }

    private void BuyLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://keyzy.io")
            {
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ContinueTrial_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
