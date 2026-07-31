using System.Windows;
using System.Windows.Media;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class ActivationDialog : Window
{
    public bool IsActivated { get; private set; }
    public bool IsDemo { get; private set; }
    private DateTime _lastAttempt = DateTime.MinValue;
    private int _attemptCount = 0;
    private const int MaxAttempts = 5;

    public ActivationDialog()
    {
        InitializeComponent();
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "⚠️ Please enter a license key";
            return;
        }

        _attemptCount++;
        if (_attemptCount > MaxAttempts)
        {
            txtStatus.Text = "❌ Too many attempts. Restart the app to try again.";
            btnActivate.IsEnabled = false;
            return;
        }

        btnActivate.IsEnabled = false;

        var elapsed = DateTime.Now - _lastAttempt;
        if (elapsed.TotalSeconds < 2)
        {
            txtStatus.Text = "⏳ Please wait...";
            await Task.Delay(2000 - (int)elapsed.TotalMilliseconds);
        }
        _lastAttempt = DateTime.Now;

        txtStatus.Text = "🔍 Validating license...";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x78, 0xD4));

        var init = await App.License.InitAsync();
        if (!init.Success)
        {
            ShowError(init.Message ?? "Could not connect to license server");
            return;
        }

        var login = await App.License.LoginWithKeyAsync(key);
        if (!login.Success)
        {
            ShowError(login.Message ?? "Invalid license key");
            return;
        }

        AppLicense.SetFull(App.License.Username, App.License.Subscription, App.License.ExpiryUtc);
        LicenseStorage.Save(key);
        LoginNotifier.Notify(App.License, key);
        if (!IsVisible) return;
        txtStatus.Text = $"✅ Activated! Welcome, {App.License.Username ?? "User"}!";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
        IsActivated = true;
        await Task.Delay(800);
        if (IsVisible) { DialogResult = true; Close(); }
    }

    private void Demo_Click(object sender, RoutedEventArgs e)
    {
        AppLicense.SetDemo();
        IsDemo = true;
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        txtStatus.Text = $"❌ {message}";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
        btnActivate.IsEnabled = true;
    }
}
