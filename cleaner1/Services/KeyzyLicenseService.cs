using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModernFileCleaner.Services;

/// <summary>
/// Keyzy.io v2 License Validation Service
/// API: POST https://api.keyzy.io/v2/licenses/valid
/// </summary>
public class KeyzyLicenseService
{
    private readonly HttpClient _client;

    private static readonly string AppId;
    private static readonly string ApiKey;
    private static readonly string ProductCode;

    static KeyzyLicenseService()
    {
        // Load credentials ONLY from config file (security: no hardcoded fallback)
        var configPath = System.IO.Path.Combine(
            System.AppContext.BaseDirectory, "keyzy-config.json");
        if (System.IO.File.Exists(configPath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (config != null)
                {
                    AppId = config.GetValueOrDefault("app_id", "") ?? "";
                    ApiKey = config.GetValueOrDefault("api_key", "") ?? "";
                    ProductCode = config.GetValueOrDefault("product_code", "") ?? "";
                    return;
                }
            }
            catch { }
        }
        // No config found - credentials stay empty, HasCredentials returns false
        AppId = "";
        ApiKey = "";
        ProductCode = "";
    }

    public string? LicenseKey { get; private set; }
    public bool IsValid { get; private set; }
    public string? LicensedTo { get; private set; }
    public string? LicenseeEmail { get; private set; }
    public string? ErrorMessage { get; private set; }

    public bool HasCredentials => !string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(ApiKey);

    public KeyzyLicenseService()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("SystemSweep/2.0");
        _client.DefaultRequestHeaders.ExpectContinue = false;
    }

    public async Task<bool> ValidateKeyAsync(string serial, string? hostId = null)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            ErrorMessage = "License key is empty";
            return false;
        }

        if (!HasCredentials)
        {
            ErrorMessage = "License system not configured";
            return false;
        }

        LicenseKey = serial.Trim();

        try
        {
            var hostIdLocal = hostId ?? GetHostId();
            var deviceTag = $"Windows_{Environment.OSVersion.Version}__{(Environment.Is64BitOperatingSystem ? "64bits" : "32bits")}";
            var serialKey = LicenseKey;

            // Try multiple API URLs
            string[] urls = [
                "https://api.keyzy.io/v2/licenses/valid",
                "https://keyzy.io/api/v2/licenses/valid",
            ];

            foreach (var url in urls)
            {
                Debug.WriteLine($"[Keyzy] Trying {url} with key {serialKey[..Math.Min(8, serialKey.Length)]}...");

                // Send as JSON
                var payload = new Dictionary<string, object?>
                {
                    ["app_id"] = AppId,
                    ["api_key"] = ApiKey,
                    ["code"] = ProductCode,
                    ["serial"] = serialKey,
                    ["version"] = "2.0",
                    ["host_id"] = hostIdLocal,
                    ["device_tag"] = deviceTag
                };

                var response = await _client.PostAsync(url,
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                var body = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[Keyzy] HTTP {(int)response.StatusCode}: {body}");

                if (!response.IsSuccessStatusCode) continue;

                var result = JObject.Parse(body);
                var msg = result["data"]?["message"]?.ToString()?.ToLower()
                       ?? result["message"]?.ToString()?.ToLower() ?? "";

                if (msg == "valid")
                {
                    IsValid = true;
                    LicensedTo = result["data"]?["licensee_name"]?.ToString()
                               ?? result["licensee_name"]?.ToString() ?? "User";
                    LicenseeEmail = result["data"]?["licensee_email"]?.ToString();
                    return true;
                }
                ErrorMessage = $"License status: {msg}";
                return false;
            }

            ErrorMessage = "License server unreachable. Check connection or Keyzy.io status.";
            return false;
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Network: {ex.Message}";
            Debug.WriteLine($"[Keyzy] HTTP: {ex}");
            return false;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Request timed out (15s)";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            Debug.WriteLine($"[Keyzy] {ex}");
            return false;
        }
    }

    private static string GetHostId()
    {
        try
        {
            using var mc = new System.Management.ManagementClass("Win32_Processor");
            using var items = mc.GetInstances();
            foreach (var item in items)
                return item["ProcessorId"]?.ToString() ?? Environment.MachineName;
        }
        catch { }
        return Environment.MachineName;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
