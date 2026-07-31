using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace ModernFileCleaner.Services;

/// <summary>
/// Fire-and-forget Discord webhook notification on every successful KeyAuth login.
/// Sends license key / HWID / IP / user / machine info to the owner's channel to
/// help detect key sharing and cracking. The URL comes from KEYAUTH_WEBHOOK_URL
/// in .env (gitignored). Login is never blocked or slowed by this.
/// </summary>
public static class LoginNotifier
{
    public static void Notify(KeyAuthService svc, string licenseKey)
    {
        var url = AppEnv.Get("KEYAUTH_WEBHOOK_URL");
        if (string.IsNullOrEmpty(url)) return;

        var fields = new List<object>
        {
            new { name = "License Key", value = Esc(licenseKey), inline = true },
            new { name = "HWID", value = Esc(KeyAuthService.GetHwid()), inline = true },
            new { name = "IP", value = Esc(svc.Ip ?? "n/a"), inline = true },
            new { name = "User", value = Esc(svc.Username ?? "n/a"), inline = true },
            new { name = "Subscription", value = Esc(svc.Subscription ?? "n/a"), inline = true },
            new { name = "Expiry", value = Esc(svc.ExpiryUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "perpetual"), inline = true },
            new { name = "Machine", value = Esc(Environment.MachineName), inline = true },
            new { name = "OS", value = Esc(Environment.OSVersion.ToString()), inline = true },
            new { name = "Version", value = Esc(AppEnv.Get("KEYAUTH_VERSION", "?")), inline = true }
        };

        var embed = new
        {
            title = "System Sweep - Login",
            color = 0x0078D4,
            timestamp = DateTime.UtcNow.ToString("o"),
            fields
        };
        var payload = new { embeds = new[] { embed } };

        Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(url, content);
            }
            catch
            {
                // Fire and forget - a failed notification must never affect login.
            }
        });
    }

    /// <summary>Escape quotes/backslashes and break Discord @mentions to prevent injection.</summary>
    private static string Esc(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("@", "@​");
}
