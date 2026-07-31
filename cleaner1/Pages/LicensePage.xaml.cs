using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class LicensePage
{
    public LicensePage()
    {
        InitializeComponent();
        LoadLicenseInfo();
    }

    private void LoadLicenseInfo()
    {
        txtHwid.Text = KeyAuthService.GetHwid();

        if (AppLicense.IsFullAccess)
            ShowActivated();
        else if (AppLicense.Mode == LicenseMode.Demo)
            ShowDemo();
        else
            ShowNoLicense();
    }

    private void ShowActivated()
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
        txtLicenseDetail.Text = "License is valid via KeyAuth";

        txtLicStatus.Text = "✅ Active";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));

        var savedKey = LicenseStorage.Load() ?? "";
        txtLicKey.Text = savedKey.Length > 24 ? savedKey[..8] + "..." + savedKey[^8..] : (savedKey.Length > 0 ? savedKey : "—");
        txtLicUser.Text = AppLicense.Username ?? "Licensed User";
        txtLicExpiry.Text = AppLicense.ExpiryUtc.HasValue
            ? AppLicense.ExpiryUtc.Value.ToLocalTime().ToString("yyyy-MM-dd")
            : "Perpetual";
        txtLicExpiry.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));
        txtSubscription.Text = AppLicense.Subscription ?? "Default";

        ActivationCard.Visibility = Visibility.Collapsed;
        btnContinue.Content = "⏭  Go to Dashboard";
    }

    private void ShowDemo()
    {
        txtIcon.Text = "🔒";
        txtTitle.Text = "Demo Mode";
        txtSubtitle.Text = "Browse only — actions are disabled";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0xFF, 0xAA, 0x00));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xAA, 0x00));
        txtLicenseStatus.Text = "🔒 Demo Mode";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicenseDetail.Text = "Enter a license key to unlock all features";

        txtLicStatus.Text = "🔒 Demo";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicKey.Text = "—";
        txtLicUser.Text = Environment.UserName;
        txtLicExpiry.Text = "—";
        txtSubscription.Text = "—";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Continue in Demo";
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
        txtLicUser.Text = Environment.UserName;
        txtLicExpiry.Text = "—";
        txtSubscription.Text = "—";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Start Demo";
    }

    private async void ActivateKeyAuth_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtLicenseDetail.Text = "Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtLicenseStatus.Text = "🔍 Validating...";

        var init = await App.License.InitAsync();
        if (!init.Success)
        {
            txtLicenseDetail.Text = $"❌ {init.Message}";
            btnActivate.IsEnabled = true;
            return;
        }

        var login = await App.License.LoginWithKeyAsync(key);
        if (login.Success)
        {
            AppLicense.SetFull(App.License.Username, App.License.Subscription, App.License.ExpiryUtc);
            LicenseStorage.Save(key);
            MessageBox.Show($"✅ License activated successfully!\n\nWelcome, {App.License.Username ?? "User"}!",
                "Activation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadLicenseInfo();
        }
        else
        {
            txtLicenseDetail.Text = $"❌ {login.Message}";
            btnActivate.IsEnabled = true;
        }
    }

    private void BuyLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://keyauth.cc/app/") { UseShellExecute = true });
        }
        catch { }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
