using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModernFileCleaner.Services;

/// <summary>
/// Keyzy.io v2 License Validation Service
/// API: POST https://api.keyzy.io/v2/licenses/valid
/// </summary>
public class KeyzyLicenseService
{
    private const string ApiUrl = "https://api.keyzy.io/v2/licenses/valid";
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
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
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
            // Build form-urlencoded data (Keyzy API expects this format)
            var formData = new Dictionary<string, string>
            {
                ["app_id"] = AppId,
                ["api_key"] = ApiKey,
                ["code"] = ProductCode,
                ["serial"] = LicenseKey,
                ["version"] = "2",
                ["host_id"] = hostId ?? GetHostId(),
                ["device_tag"] = $"Windows_{Environment.OSVersion.Version}__{(Environment.Is64BitOperatingSystem ? "64bits" : "32bits")}"
            };

            var content = new FormUrlEncodedContent(formData);
            Debug.WriteLine($"[Keyzy] Validating {LicenseKey[..Math.Min(8, LicenseKey.Length)]}...");

            var response = await _client.PostAsync(ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Keyzy] HTTP {(int)response.StatusCode}: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"License server error ({response.StatusCode})";
                if (responseBody.Contains("credentials", StringComparison.OrdinalIgnoreCase)
                    || responseBody.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                    ErrorMessage = "Invalid API credentials - check Keyzy.io config";
                else if ((int)response.StatusCode == 403)
                    ErrorMessage = "Access denied - verify API credentials in keyzy-config.json";
                return false;
            }

            // Parse response (could be JSON or XML)
            var result = JObject.Parse(responseBody);
            var message = result["data"]?["message"]?.ToString()?.ToLower()
                       ?? result["message"]?.ToString()?.ToLower()
                       ?? "";

            if (message == "valid")
            {
                IsValid = true;
                LicensedTo = result["data"]?["licensee_name"]?.ToString()
                           ?? result["licensee_name"]?.ToString()
                           ?? "Licensed User";
                LicenseeEmail = result["data"]?["licensee_email"]?.ToString();
                return true;
            }

            ErrorMessage = $"License status: {message}";
            return false;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Connection timed out";
            return false;
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Network error: {ex.Message}";
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
