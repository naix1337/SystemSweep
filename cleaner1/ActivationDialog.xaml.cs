using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class ActivationDialog : Window
{
    public bool IsActivated { get; private set; }
    public bool StartedTrial { get; private set; }
    private DateTime _lastAttempt = DateTime.MinValue;
    private int _attemptCount = 0;
    private const int MaxAttempts = 5;

    public ActivationDialog()
    {
        InitializeComponent();
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        // Rate limiting: max 5 attempts, min 2s between attempts
        _attemptCount++;
        if (_attemptCount > MaxAttempts)
        {
            txtStatus.Text = "❌ Too many attempts. Restart the app to try again.";
            btnActivate.IsEnabled = false;
            return;
        }
        var elapsed = DateTime.Now - _lastAttempt;
        if (elapsed.TotalSeconds < 2)
        {
            txtStatus.Text = $"⏳ Please wait...";
            await Task.Delay(2000 - (int)elapsed.TotalMilliseconds);
        }
        _lastAttempt = DateTime.Now;

        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "⚠️ Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtStatus.Text = "🔍 Validating license...";
        StatusBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0, 0x78, 0xD4));

        var keyzy = new KeyzyLicenseService();
        bool valid = await keyzy.ValidateKeyAsync(key);

        if (valid)
        {
            txtStatus.Text = $"✅ Activated! Welcome, {keyzy.LicensedTo}!";
            StatusBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
            IsActivated = true;
            SaveLicense(key);
            await Task.Delay(800);
            DialogResult = true;
            Close();
        }
        else
        {
            txtStatus.Text = $"❌ {keyzy.ErrorMessage}";
            StatusBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
            btnActivate.IsEnabled = true;
        }

        keyzy.Dispose();
    }

    private void Trial_Click(object sender, RoutedEventArgs e)
    {
        // Check trial validity before allowing
        var licSvc = new LicenseService();
        if (licSvc.Status == LicenseService.LicenseStatus.Expired)
        {
            txtStatus.Text = "❌ Trial has already expired. Please purchase a license.";
            StatusBox.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
            return;
        }
        StartedTrial = true;
        DialogResult = true;
        Close();
    }

    private static void SaveLicense(string key)
    {
        try
        {
            // HWID-bound license: includes machine fingerprint
            var hwid = GetHardwareId();
            var licenseData = $"KEYZY:{key}:HWID:{hwid}";
            var encrypted = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.UTF8.GetBytes(licenseData),
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            System.IO.File.WriteAllText("license.key", Convert.ToBase64String(encrypted));
        }
        catch { }
    }

    private static string GetHardwareId()
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
