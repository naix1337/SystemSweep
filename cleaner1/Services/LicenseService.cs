using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ModernFileCleaner.Services;

public class LicenseService
{
    private const string LicenseFile = "license.key";
    private const string TrialFile = "trial.dat";
    private const int TrialDays = 30;

    public enum LicenseStatus
    {
        Activated,
        Trial,
        Expired,
        Invalid
    }

    public LicenseStatus Status { get; private set; } = LicenseStatus.Trial;
    public string? LicensedTo { get; private set; }
    public int TrialDaysRemaining { get; private set; } = TrialDays;

    public LicenseService()
    {
        LoadLicense();
    }

    public string GetMachineFingerprint()
    {
        try
        {
            var parts = new List<string>();

            // CPU ID
            using var mc = new System.Management.ManagementClass("Win32_Processor");
            using var items = mc.GetInstances();
            foreach (var item in items)
            {
                parts.Add(item["ProcessorId"]?.ToString() ?? "");
                break;
            }

            // Motherboard serial
            using var mb = new System.Management.ManagementClass("Win32_BaseBoard");
            using var mbItems = mb.GetInstances();
            foreach (var item in mbItems)
            {
                parts.Add(item["SerialNumber"]?.ToString() ?? "");
                break;
            }

            // MAC address
            using var net = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration");
            using var netItems = net.GetInstances();
            foreach (var item in netItems)
            {
                var mac = item["MacAddress"]?.ToString();
                if (!string.IsNullOrEmpty(mac) && mac != "00:00:00:00:00:00")
                {
                    parts.Add(mac);
                    break;
                }
            }

            var raw = string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
            return BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).Replace("-", "");
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    public bool ValidateLicenseKey(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Length < 20)
            return false;

        try
        {
            // Format: XXXXX-XXXXX-XXXXX-XXXXX
            var clean = licenseKey.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            if (clean.Length != 20)
                return false;

            // Simple checksum validation: Last 2 chars are a checksum of first 18
            var data = clean[..18];
            var checksum = clean[18..];

            int sum = 0;
            foreach (char c in data) sum += c;
            var expected = (sum % 100).ToString("D2");

            // Verify fingerprint binding (trial or full)
            var fp = GetMachineFingerprint();
            var fpPrefix = fp[..Math.Min(8, fp.Length)];

            // License contains fingerprint prefix validation
            var keyValid = data.StartsWith(fpPrefix, StringComparison.OrdinalIgnoreCase);

            if (checksum == expected && keyValid)
            {
                LicensedTo = Environment.UserName;
                SaveLicense(clean);
                Status = LicenseStatus.Activated;
                return true;
            }

            // Allow override for offline activation codes
            if (checksum == expected && licenseKey.Contains("SYSWEEP"))
            {
                LicensedTo = "Licensed User";
                SaveLicense(clean);
                Status = LicenseStatus.Activated;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void SaveLicense(string key)
    {
        try
        {
            var data = ProtectData($"LICENSE:{key}:{GetMachineFingerprint()}");
            File.WriteAllText(LicenseFile, data);
        }
        catch { }
    }

    private void LoadLicense()
    {
        try
        {
            // Check trial first
            if (File.Exists(TrialFile))
            {
                var trialData = UnprotectData(File.ReadAllText(TrialFile));
                if (DateTime.TryParse(trialData, out var firstRun))
                {
                    var daysUsed = (DateTime.Now - firstRun).Days;
                    TrialDaysRemaining = Math.Max(0, TrialDays - daysUsed);
                    if (TrialDaysRemaining <= 0)
                    {
                        Status = LicenseStatus.Expired;
                        return;
                    }
                    Status = LicenseStatus.Trial;
                }
            }
            else
            {
                // First run - start trial
                var data = ProtectData(DateTime.Now.ToString("O"));
                File.WriteAllText(TrialFile, data);
                TrialDaysRemaining = TrialDays;
                Status = LicenseStatus.Trial;
            }

            // Check for license key (overrides trial)
            if (File.Exists(LicenseFile))
            {
                var licData = UnprotectData(File.ReadAllText(LicenseFile));
                if (licData.StartsWith("LICENSE:"))
                {
                    var parts = licData.Split(':');
                    if (parts.Length >= 3 && parts[2] == GetMachineFingerprint())
                    {
                        Status = LicenseStatus.Activated;
                        LicensedTo = "Licensed User";
                    }
                }
            }
        }
        catch
        {
            // On error, allow trial access (fail open)
            Status = LicenseStatus.Trial;
            TrialDaysRemaining = TrialDays;
        }
    }

    private static string ProtectData(string data)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }
    }

    private static string UnprotectData(string data)
    {
        try
        {
            var encrypted = Convert.FromBase64String(data);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(data)); }
            catch { return ""; }
        }
    }

    public void ResetTrial()
    {
        try { if (File.Exists(TrialFile)) File.Delete(TrialFile); } catch { }
        try { if (File.Exists(LicenseFile)) File.Delete(LicenseFile); } catch { }
        Status = LicenseStatus.Trial;
        TrialDaysRemaining = TrialDays;
        LicensedTo = null;
    }
}
