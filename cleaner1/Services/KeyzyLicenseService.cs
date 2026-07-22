using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace ModernFileCleaner.Services;

/// <summary>
/// Keyzy.io License Validation Service
/// API: https://keyzy.io/docs/developers/rest-api/licenses-validate/
/// </summary>
public class KeyzyLicenseService
{
    private const string ApiBaseUrl = "https://api.keyzy.io/v1";
    private readonly HttpClient _client;

    public string? LicenseKey { get; private set; }
    public bool IsValid { get; private set; }
    public string? LicensedTo { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? ProductName { get; private set; }
    public string? ErrorMessage { get; private set; }

    public KeyzyLicenseService()
    {
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("SystemSweep", "2.0"));
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Validates a license key against Keyzy.io API.
    /// </summary>
    public async Task<bool> ValidateKeyAsync(string licenseKey, string? machineCode = null)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            ErrorMessage = "License key is empty";
            return false;
        }

        LicenseKey = licenseKey.Trim();

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["license_key"] = LicenseKey,
                ["machine_code"] = machineCode ?? GetDefaultMachineCode(),
                ["product"] = "system-sweep"
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{ApiBaseUrl}/licenses/validate", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Keyzy] Response: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"API error: {response.StatusCode}";
                return false;
            }

            var result = JsonConvert.DeserializeObject<KeyzyResponse>(responseBody);
            if (result == null)
            {
                ErrorMessage = "Invalid API response";
                return false;
            }

            if (result.Status == "valid" || result.Valid == true)
            {
                IsValid = true;
                LicensedTo = result.Licensee ?? result.CustomerName ?? "Licensed User";
                ExpiresAt = result.ExpiresAt;
                ProductName = result.ProductName;
                return true;
            }

            ErrorMessage = result.Message ?? "License key is not valid";
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
            ErrorMessage = $"Validation error: {ex.Message}";
            Debug.WriteLine($"[Keyzy] {ex}");
            return false;
        }
    }

    /// <summary>
    /// Returns the default machine identifier for license binding.
    /// </summary>
    private static string GetDefaultMachineCode()
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
/// Keyzy.io API response model.
/// Adjust properties based on actual API response.
/// </summary>
public class KeyzyResponse
{
    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("valid")]
    public bool? Valid { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("licensee")]
    public string? Licensee { get; set; }

    [JsonProperty("customer_name")]
    public string? CustomerName { get; set; }

    [JsonProperty("product_name")]
    public string? ProductName { get; set; }

    [JsonProperty("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [JsonProperty("license_type")]
    public string? LicenseType { get; set; }

    [JsonProperty("max_machines")]
    public int MaxMachines { get; set; } = 1;
}
