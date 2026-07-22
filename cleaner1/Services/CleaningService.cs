using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class CleaningService
{
    public async Task<long> CleanCategoryAsync(CleaningCategory category, IProgress<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report($"Bereinige {category.Name}...");
            long before = category.SizeInBytes;
            CleanById(category.Id);
            return before;
        });
    }

    private void CleanById(string categoryId)
    {
        switch (categoryId)
        {
            case "temp_files":
                CleanDirectory(Path.GetTempPath());
                string? userTemp = Environment.GetEnvironmentVariable("TEMP");
                if (!string.IsNullOrEmpty(userTemp)) CleanDirectory(userTemp);
                break;
            case "recycle_bin":
                try
                {
                    string recyclePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "Windows", "RecycleBin");
                    if (Directory.Exists(recyclePath)) CleanDirectory(recyclePath);
                }
                catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
                break;
            case "download_cache":
                string downloads = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloads)) CleanDirectory(downloads);
                break;
            case "thumbnail_cache":
                string thumbCache = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Explorer");
                if (Directory.Exists(thumbCache))
                {
                    foreach (var f in Directory.GetFiles(thumbCache, "thumbcache_*.db"))
                        try { File.Delete(f); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
                }
                break;
            case "error_reports":
                string wer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft", "Windows", "WER");
                if (Directory.Exists(wer)) CleanDirectory(wer);
                break;
            case "installer_temp":
                string installer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
                if (Directory.Exists(installer))
                {
                    foreach (var f in Directory.GetFiles(installer, "*.tmp"))
                        try { File.Delete(f); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
                }
                break;
            case "store_cache":
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wsreset.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.WaitForExit();
                break;
            case "browser_chrome":
                CleanDirectory(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data", "Default", "Cache"));
                CleanDirectory(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data", "Default", "Code Cache"));
                break;
            case "browser_edge":
                CleanDirectory(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Edge", "User Data", "Default", "Cache"));
                break;
            case "browser_firefox":
                var firefoxProfiles = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(firefoxProfiles))
                {
                    foreach (var dir in Directory.GetDirectories(firefoxProfiles))
                    {
                        CleanDirectory(Path.Combine(dir, "cache2"));
                        CleanDirectory(Path.Combine(dir, "offlinecache"));
                    }
                }
                break;
            case "windows_logs":
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (Directory.Exists(winDir))
                {
                    foreach (var f in Directory.GetFiles(winDir, "*.log", SearchOption.AllDirectories))
                        try { File.Delete(f); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
                }
                break;
            case "windows_old":
                if (!IsAdmin()) return;
                string winOld = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old");
                if (Directory.Exists(winOld)) { CleanDirectory(winOld); try { Directory.Delete(winOld, true); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); } }
                break;
            case "memory_dumps":
                if (!IsAdmin()) return;
                string dump = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
                if (File.Exists(dump)) try { File.Delete(dump); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
                string minidump = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
                if (Directory.Exists(minidump)) CleanDirectory(minidump);
                break;
        }
    }

    private void CleanDirectory(string path, string searchPattern = "*.*")
    {
        if (!Directory.Exists(path)) return;
        try
        {
            foreach (var file in Directory.GetFiles(path, searchPattern))
                try { File.Delete(file); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
            foreach (var dir in Directory.GetDirectories(path))
                try { Directory.Delete(dir, true); } catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
        }
        catch (Exception ex) { Debug.WriteLine($"[CleaningService] {ex.Message}"); }
    }

    private static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
