using System.Diagnostics;
using System.IO;

namespace ModernFileCleaner.Services;

public class BrowserCacheInfo
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Path { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public string SizeFormatted => FormatBytes(SizeBytes);
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}

public class BrowserCacheService
{
    private readonly List<BrowserCacheInfo> _browsers = new()
    {
        new() { Name = "Google Chrome", Icon = "🟢", Path = @"Google\Chrome\User Data\Default\Cache" },
        new() { Name = "Microsoft Edge", Icon = "🔵", Path = @"Microsoft\Edge\User Data\Default\Cache" },
        new() { Name = "Firefox", Icon = "🦊", Path = @"Mozilla\Firefox\Profiles" },
        new() { Name = "Brave", Icon = "🦁", Path = @"BraveSoftware\Brave-Browser\User Data\Default\Cache" },
    };

    public List<BrowserCacheInfo> Scan()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var browser in _browsers)
        {
            var fullPath = System.IO.Path.Combine(localAppData, browser.Path);
            browser.SizeBytes = DirSize(new DirectoryInfo(fullPath));
        }
        return _browsers;
    }

    public long Clean(List<BrowserCacheInfo> selected)
    {
        long freed = 0;
        foreach (var browser in selected.Where(b => b.IsSelected))
        {
            freed += browser.SizeBytes;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fullPath = System.IO.Path.Combine(localAppData, browser.Path);
            if (Directory.Exists(fullPath))
            {
                try { Directory.Delete(fullPath, true); } catch { }
            }
        }
        return freed;
    }

    private static long DirSize(DirectoryInfo? dir)
    {
        if (dir == null || !dir.Exists) return 0;
        long size = 0;
        try
        {
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                try { size += file.Length; } catch { }
        }
        catch { }
        return size;
    }
}
