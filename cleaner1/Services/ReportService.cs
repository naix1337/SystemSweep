using System.IO;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class ReportService
{
    public async Task<string> ExportHtmlAsync(CleanHistoryEntry entry, string outputPath)
    {
        var html = $@"<!DOCTYPE html>
<html><head><title>System Sweep Report</title>
<style>
body {{ font-family: 'Segoe UI', sans-serif; background: #1e1e1e; color: #fff; padding: 40px; }}
h1 {{ color: #0078D4; }}
.card {{ background: #2d2d2d; border-radius: 12px; padding: 20px; margin: 16px 0; }}
.stat {{ font-size: 24px; font-weight: bold; color: #4CAF50; }}
</style></head><body>
<h1>🧹 System Sweep Report</h1>
<div class='card'>
    <p>Date: {entry.Timestamp:g}</p>
    <p>Space Freed: <span class='stat'>{FormatBytes(entry.BytesFreed)}</span></p>
    <p>Categories: {string.Join(", ", entry.CategoriesCleaned)}</p>
</div>
</body></html>";
        await File.WriteAllTextAsync(outputPath, html);
        return outputPath;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}
