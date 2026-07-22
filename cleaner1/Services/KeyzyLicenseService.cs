using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModernFileCleaner.Services;

/// <summary>
/// Keyzy.io License Validation Service
/// GET https://api.keyzy.io/v2/licenses/validate?app_id=...&api_key=...&code=...&serial=...&version=1.0
/// HTTP 200 (empty body) = License is valid
/// HTTP 404 = Serial does not exist or not registered
/// </summary>
public class KeyzyLicenseService
{
    private readonly HttpClient _client;

    private static readonly string AppId;
    private static readonly string ApiKey;
    private static readonly string ProductCode;

    static KeyzyLicenseService()
    {
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
        AppId = ""; ApiKey = ""; ProductCode = "";
    }

    public string? LicenseKey { get; private set; }
    public bool IsValid { get; private set; }
    public string? LicensedTo { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool HasCredentials => !string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(ApiKey);

    public KeyzyLicenseService()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("SystemSweep/2.0");
    }

    public async Task<bool> ValidateKeyAsync(string serial, string? hostId = null)
    {
        if (string.IsNullOrWhiteSpace(serial)) { ErrorMessage = "License key is empty"; return false; }
        if (!HasCredentials) { ErrorMessage = "License system not configured"; return false; }

        LicenseKey = serial.Trim();

        try
        {
            // Build query parameters
            var parms = new Dictionary<string, string>
            {
                ["app_id"] = AppId,
                ["api_key"] = ApiKey,
                ["code"] = ProductCode,
                ["serial"] = LicenseKey,
                ["version"] = "1.0",
                ["host_id"] = hostId ?? GetHostId(),
                ["device_tag"] = $"Windows_{Environment.OSVersion.Version}__{(Environment.Is64BitOperatingSystem ? "64bits" : "32bits")}"
            };

            var query = string.Join("&", parms.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            var url = $"https://api.keyzy.io/v2/licenses/validate?{query}";
            // Security: never log full URL with credentials
            Debug.WriteLine($"[Keyzy] Validating key {LicenseKey[..Math.Min(8, LicenseKey.Length)]}...");

            var response = await _client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Keyzy] HTTP {(int)response.StatusCode}: '{body}'");

            // HTTP 200 with empty body = VALID
            if (response.IsSuccessStatusCode)
            {
                IsValid = true;
                LicensedTo = "Licensed User";
                return true;
            }

            // Parse error
            try
            {
                var errResult = JObject.Parse(body);
                var errMsg = errResult["error"]?["message"]?.ToString();
                if (!string.IsNullOrEmpty(errMsg))
                {
                    ErrorMessage = errMsg;
                    return false;
                }
            }
            catch { }

            ErrorMessage = $"License rejected ({(int)response.StatusCode})";
            return false;
        }
        catch (HttpRequestException ex) { ErrorMessage = $"Network: {ex.Message}"; return false; }
        catch (TaskCanceledException) { ErrorMessage = "Connection timed out"; return false; }
        catch (Exception ex) { ErrorMessage = $"Error: {ex.Message}"; return false; }
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

    public void Dispose() => _client?.Dispose();
}
