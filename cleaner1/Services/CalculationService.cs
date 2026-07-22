using System.Diagnostics;
using System.IO;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class CalculationService
{
    public async Task CalculateAllAsync(List<CleaningCategory> categories, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            foreach (var category in categories)
            {
                try
                {
                    progress?.Report($"Analysing {category.Name}...");
                    category.SizeInBytes = CalculateCategory(category.Id);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CalcService] Error calculating {category.Id}: {ex.Message}");
                    category.SizeInBytes = 0;
                }
            }
        });
    }

    public long CalculateCategory(string categoryId)
    {
        return categoryId switch
        {
            "temp_files" => GetDirectorySize(Path.GetTempPath()),
            "recycle_bin" => 100L * 1024 * 1024, // approx
            "download_cache" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")),
            "thumbnail_cache" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Explorer"), "thumbcache_*.db"),
            "error_reports" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft", "Windows", "WER")),
            "installer_temp" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer"), "*.tmp"),
            "store_cache" => 100L * 1024 * 1024, // approx
            "browser_chrome" => GetDirectorySize(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Default", "Cache")),
            "browser_edge" => GetDirectorySize(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default", "Cache")),
            "browser_firefox" => GetBrowserFirefoxCacheSize(),
            "windows_logs" => CalculateWindowsLogFilesSize(),
            "windows_old" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old")),
            "memory_dumps" => CalculateMemoryDumpsSize(),
            _ => 0
        };
    }

    private long CalculateWindowsLogFilesSize()
    {
        long size = 0;
        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!Directory.Exists(windowsPath)) return size;
        foreach (var file in Directory.GetFiles(windowsPath, "*.log", SearchOption.AllDirectories))
        {
            try { size += new FileInfo(file).Length; } catch (Exception ex) { Debug.WriteLine($"[CalculationService] {ex.Message}"); }
        }
        return size;
    }

    private long CalculateMemoryDumpsSize()
    {
        long size = 0;
        string dumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
        if (File.Exists(dumpPath)) { try { size += new FileInfo(dumpPath).Length; } catch (Exception ex) { Debug.WriteLine($"[CalculationService] {ex.Message}"); } }
        string minidumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
        if (Directory.Exists(minidumpPath)) size += GetDirectorySize(minidumpPath);
        return size;
    }

    private long GetBrowserFirefoxCacheSize()
    {
        long size = 0;
        string profilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(profilesPath)) return size;
        foreach (var dir in Directory.GetDirectories(profilesPath))
        {
            size += GetDirectorySize(Path.Combine(dir, "cache2"));
            size += GetDirectorySize(Path.Combine(dir, "offlinecache"));
        }
        return size;
    }

    private long GetDirectorySize(string path, string searchPattern = "*.*")
    {
        long size = 0;
        if (!Directory.Exists(path)) return size;
        try
        {
            foreach (var file in Directory.GetFiles(path, searchPattern))
                try { size += new FileInfo(file).Length; } catch (Exception ex) { Debug.WriteLine($"[CalculationService] {ex.Message}"); }
            foreach (var dir in Directory.GetDirectories(path))
                try { size += GetDirectorySize(dir, searchPattern); } catch (Exception ex) { Debug.WriteLine($"[CalculationService] {ex.Message}"); }
        }
        catch (Exception ex) { Debug.WriteLine($"[CalculationService] {ex.Message}"); }
        return size;
    }
}
