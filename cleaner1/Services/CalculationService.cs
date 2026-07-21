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
                progress?.Report($"Analysiere {category.Name}...");
                category.SizeInBytes = CalculateCategory(category.Id);
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
            try { size += new FileInfo(file).Length; } catch { }
        }
        return size;
    }

    private long CalculateMemoryDumpsSize()
    {
        long size = 0;
        string dumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
        if (File.Exists(dumpPath)) { try { size += new FileInfo(dumpPath).Length; } catch { } }
        string minidumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
        if (Directory.Exists(minidumpPath)) size += GetDirectorySize(minidumpPath);
        return size;
    }

    private long GetDirectorySize(string path, string searchPattern = "*.*")
    {
        long size = 0;
        if (!Directory.Exists(path)) return size;
        try
        {
            foreach (var file in Directory.GetFiles(path, searchPattern))
                try { size += new FileInfo(file).Length; } catch { }
            foreach (var dir in Directory.GetDirectories(path))
                try { size += GetDirectorySize(dir, searchPattern); } catch { }
        }
        catch { }
        return size;
    }
}
