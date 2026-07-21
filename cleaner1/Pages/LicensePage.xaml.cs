using System.Diagnostics;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class LicensePage
{
    private readonly LicenseService _license = new();

    public LicensePage()
    {
        InitializeComponent();
        LoadStatus();
        txtFingerprint.Text = $"Device ID: {_license.GetMachineFingerprint()[..16]}...";
    }

    private void LoadStatus()
    {
        var status = _license.Status;
        switch (status)
        {
            case LicenseService.LicenseStatus.Activated:
                txtLicenseStatus.Text = "✅ Fully Activated";
                txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                txtLicenseDetail.Text = $"Licensed to: {_license.LicensedTo ?? "User"}";
                StatusCard.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
                btnActivate.IsEnabled = false;
                txtLicenseKey.IsEnabled = false;
                break;

            case LicenseService.LicenseStatus.Trial:
                txtLicenseStatus.Text = $"🔓 Trial Mode ({_license.TrialDaysRemaining} days left)";
                txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
                txtLicenseDetail.Text = "Some advanced features may be limited";
                StatusCard.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0xFF, 0xAA, 0x00));
                break;

            case LicenseService.LicenseStatus.Expired:
                txtLicenseStatus.Text = "❌ Trial Expired";
                txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
                txtLicenseDetail.Text = "Please purchase a license to continue using all features";
                StatusCard.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
                break;
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtStatus.Text = "Validating license key...";

        if (_license.ValidateLicenseKey(key))
        {
            txtStatus.Text = "✅ License activated successfully!";
            LoadStatus();
            MessageBox.Show("✅ License activated!\n\nAll features are now unlocked. Thank you for your support!",
                          "Activation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            txtStatus.Text = "❌ Invalid license key. Please check and try again.";
            btnActivate.IsEnabled = true;
        }
    }

    private void BuyLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new ProcessStartInfo("https://github.com/nix1337/SystemSweep/releases")
            {
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }

    private void ContinueTrial_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
