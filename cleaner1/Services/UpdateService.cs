using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

namespace ModernFileCleaner.Services;

public class UpdateInfo
{
    public string Version { get; set; } = "2.0.0";
    public string DownloadUrl { get; set; } = "";
    public string Changelog { get; set; } = "";
    public string ReleaseDate { get; set; } = "";
}

public class UpdateService
{
    private const string VersionUrl = "https://raw.githubusercontent.com/nix1337/SystemSweep/main/version.json";

    public string CurrentVersion => "2.0.0";

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(VersionUrl);
            return JsonConvert.DeserializeObject<UpdateInfo>(json);
        }
        catch
        {
            return null; // Offline or unreachable
        }
    }

    public bool IsNewerVersion(string? remoteVersion)
    {
        if (string.IsNullOrEmpty(remoteVersion)) return false;
        return Version.TryParse(remoteVersion, out var remote)
            && Version.TryParse(CurrentVersion, out var current)
            && remote > current;
    }

    public void OpenDownloadPage(string url)
    {
        try
        {
            // Security: Only allow HTTPS URLs to prevent argument injection
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                return;
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
    }
}
