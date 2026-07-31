using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ModernFileCleaner.Services;

public class KeyAuthResponse
{
    public bool Success { get; set; }
    public bool IsNetworkError { get; set; }
    public bool IsRejection { get; set; }
    public string? Message { get; set; }
    public string? SessionId { get; set; }
    public KeyAuthUserInfo? Info { get; set; }
}

public class KeyAuthUserInfo
{
    public string? Username { get; set; }
    public string? Ip { get; set; }
    public string? Hwid { get; set; }
    public List<KeyAuthSubscription> Subscriptions { get; set; } = new();
}

public class KeyAuthSubscription
{
    public string? Subscription { get; set; }
    public string? Expiry { get; set; }
    public string? Timeleft { get; set; }
}

/// <summary>
/// KeyAuth API v1.3 client (https://keyauth.win/api/1.3/).
/// Session-based: InitAsync() first, then LoginWithKeyAsync(), then CheckAsync().
/// Every response is Ed25519-signed by KeyAuth; verification fails closed.
/// </summary>
public class KeyAuthService : IDisposable
{
    private const string ApiUrl = "https://keyauth.win/api/1.3/";

    // KeyAuth response-signing public key (from official C# SDK)
    private static readonly byte[] SignaturePublicKey =
        HexToBytes("5586b4bc69c7a4b487e4563a4cd96afd39140f919bd31cea7d1c6a1e8439422b");

    private readonly HttpClient _http;
    private readonly string _name;
    private readonly string _ownerid;
    private readonly string _version;

    public string? SessionId { get; private set; }
    public string? Username { get; private set; }
    public string? Subscription { get; private set; }
    public string? Ip { get; private set; }
    public DateTime? ExpiryUtc { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public KeyAuthService()
    {
        _name = AppEnv.Get("KEYAUTH_NAME", "System sweep");
        _ownerid = AppEnv.Get("KEYAUTH_OWNERID");
        _version = AppEnv.Get("KEYAUTH_VERSION", "1.0");
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SystemSweep/2.0");
    }

    public async Task<KeyAuthResponse> InitAsync()
    {
        var resp = await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "init",
            ["ver"] = _version,
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });
        if (resp.Success) SessionId = resp.SessionId;
        return resp;
    }

    public async Task<KeyAuthResponse> LoginWithKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new KeyAuthResponse { Success = false, Message = "License key is empty" };

        var resp = await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "license",
            ["key"] = key.Trim(),
            ["hwid"] = GetHwid(),
            ["sessionid"] = SessionId ?? "",
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });

        if (resp.Success && resp.Info != null)
        {
            Username = resp.Info.Username;
            Ip = resp.Info.Ip;
            var sub = resp.Info.Subscriptions.FirstOrDefault();
            Subscription = sub?.Subscription;
            if (sub != null && long.TryParse(sub.Expiry, out var unix))
                ExpiryUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            IsAuthenticated = true;
        }
        else if (resp.Success)
        {
            IsAuthenticated = true;
        }
        return resp;
    }

    public async Task<KeyAuthResponse> CheckAsync()
    {
        return await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "check",
            ["sessionid"] = SessionId ?? "",
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });
    }

    private async Task<KeyAuthResponse> PostAsync(Dictionary<string, string> data)
    {
        try
        {
            using var content = new FormUrlEncodedContent(data);
            using var response = await _http.PostAsync(ApiUrl, content);

            var rawBytes = await response.Content.ReadAsByteArrayAsync();
            string? sig = response.Headers.TryGetValues("x-signature-ed25519", out var sv) ? sv.FirstOrDefault() : null;
            string? ts = response.Headers.TryGetValues("x-signature-timestamp", out var tv) ? tv.FirstOrDefault() : null;

            if (string.IsNullOrEmpty(sig) || string.IsNullOrEmpty(ts) || !VerifySignature(sig, ts, rawBytes))
                return new KeyAuthResponse { Success = false, Message = "License server verification failed" };

            var body = Encoding.UTF8.GetString(rawBytes);
            var json = JObject.Parse(body);
            var result = new KeyAuthResponse
            {
                Success = json["success"]?.Value<bool>() ?? false,
                Message = json["message"]?.ToString(),
                SessionId = json["sessionid"]?.ToString()
            };

            var info = json["info"];
            if (info != null && info.Type != JTokenType.Null)
            {
                result.Info = new KeyAuthUserInfo
                {
                    Username = info["username"]?.ToString(),
                    Ip = info["ip"]?.ToString(),
                    Hwid = info["hwid"]?.ToString(),
                    Subscriptions = info["subscriptions"]?.ToObject<List<KeyAuthSubscription>>() ?? new List<KeyAuthSubscription>()
                };
            }
            result.IsRejection = !result.Success;
            return result;
        }
        catch (HttpRequestException ex)
        {
            return new KeyAuthResponse { Success = false, IsNetworkError = true, Message = $"Network error: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new KeyAuthResponse { Success = false, IsNetworkError = true, Message = "Connection timed out" };
        }
        catch (Exception ex)
        {
            return new KeyAuthResponse { Success = false, Message = ex.Message };
        }
    }

    private static bool VerifySignature(string signatureHex, string timestamp, byte[] rawResponse)
    {
        try
        {
            // Replay window: timestamp may be ~30s in the future, up to 60s old.
            if (!long.TryParse(timestamp, out var ts)) return false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (ts - now > 30 || now - ts > 60) return false;

            var sig = HexToBytes(signatureHex);
            var tsBytes = Encoding.UTF8.GetBytes(timestamp);
            var body = new byte[tsBytes.Length + rawResponse.Length];
            Buffer.BlockCopy(tsBytes, 0, body, 0, tsBytes.Length);
            Buffer.BlockCopy(rawResponse, 0, body, tsBytes.Length, rawResponse.Length);
            return Ed25519.CheckValid(sig, body, SignaturePublicKey);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    /// <summary>Stable machine fingerprint (CPU + mainboard serial), used as KeyAuth HWID.</summary>
    public static string GetHwid()
    {
        try
        {
            var parts = new List<string>();
            using (var mc = new System.Management.ManagementClass("Win32_Processor"))
            foreach (var item in mc.GetInstances()) { parts.Add(item["ProcessorId"]?.ToString() ?? ""); break; }

            using (var mb = new System.Management.ManagementClass("Win32_BaseBoard"))
            foreach (var item in mb.GetInstances()) { parts.Add(item["SerialNumber"]?.ToString() ?? ""); break; }

            var raw = string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
            if (string.IsNullOrEmpty(raw)) raw = Environment.MachineName;
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        }
        catch
        {
            return "hwid-" + Environment.MachineName;
        }
    }

    public void Dispose() => _http.Dispose();
}
