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
    private const string ApiUrl = "https://api.keyzy.io/v2/licenses/valid";
    private readonly HttpClient _client;

    // TODO: Replace with your Keyzy.io app credentials
    private const string AppId = "";      // Your Keyzy.io app_id
    private const string ApiKey = "";     // Your Keyzy.io api_key
    private const string ProductCode = ""; // Your product code from Keyzy

    public string? LicenseKey { get; private set; }
    public bool IsValid { get; private set; }
    public string? LicensedTo { get; private set; }
    public string? LicenseeEmail { get; private set; }
    public string? ProductName { get; private set; }
    public string? ErrorMessage { get; private set; }

    public bool HasCredentials => !string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(ApiKey);

    public KeyzyLicenseService()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
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
            // Allow offline validation if no API credentials configured
            ErrorMessage = "KEYZY_NOT_CONFIGURED";
            return false;
        }

        LicenseKey = serial.Trim();

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["app_id"] = AppId,
                ["api_key"] = ApiKey,
                ["code"] = ProductCode,
                ["serial"] = LicenseKey,
                ["version"] = "2.0",
                ["host_id"] = hostId ?? GetHostId(),
                ["device_tag"] = $"Windows_{Environment.OSVersion.Version}__{(Environment.Is64BitOperatingSystem ? "64bits" : "32bits")}"
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.WriteLine($"[Keyzy] Validating: {LicenseKey[..Math.Min(8, LicenseKey.Length)]}...");

            var response = await _client.PostAsync(ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Keyzy] Status: {response.StatusCode}, Body: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"API error: {response.StatusCode}";
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    ErrorMessage = "Invalid API credentials. Check app_id and api_key.";
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    ErrorMessage = "Product not found. Check product code.";
                return false;
            }

            var result = JsonConvert.DeserializeObject<KeyzyResponseV2>(responseBody);
            if (result?.Data == null)
            {
                ErrorMessage = "Invalid API response format";
                return false;
            }

            if (result.Data.Message?.ToLower() == "valid")
            {
                IsValid = true;
                LicensedTo = result.Data.LicenseeName ?? "Licensed User";
                LicenseeEmail = result.Data.LicenseeEmail;
                ProductName = result.Data.SkuNumber ?? result.Data.ProductCode;
                return true;
            }

            ErrorMessage = $"License status: {result.Data.Message}";
            return false;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Connection timed out. Check your internet connection.";
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
            {
                return item["ProcessorId"]?.ToString() ?? Environment.MachineName;
            }
        }
        catch { }
        return Environment.MachineName;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Keyzy.io v2 API response (wrapped in "data" object)
/// </summary>
public class KeyzyResponseV2
{
    [JsonProperty("data")]
    public KeyzyData? Data { get; set; }
}

public class KeyzyData
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("licensee_name")]
    public string? LicenseeName { get; set; }

    [JsonProperty("licensee_email")]
    public string? LicenseeEmail { get; set; }

    [JsonProperty("sku_number")]
    public string? SkuNumber { get; set; }

    [JsonProperty("product_code")]
    public string? ProductCode { get; set; }

    [JsonProperty("version_code")]
    public string? VersionCode { get; set; }
}
