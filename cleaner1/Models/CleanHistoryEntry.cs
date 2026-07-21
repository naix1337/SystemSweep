namespace ModernFileCleaner.Models;

public class CleanHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public long BytesFreed { get; set; }
    public List<string> CategoriesCleaned { get; set; } = new();

    public string CategoriesDisplay => string.Join(", ", CategoriesCleaned);

    public string BytesFormatted
    {
        get
        {
            if (BytesFreed < 1024) return $"{BytesFreed} B";
            if (BytesFreed < 1024 * 1024) return $"{BytesFreed / 1024.0:F1} KB";
            if (BytesFreed < 1024 * 1024 * 1024) return $"{BytesFreed / (1024.0 * 1024.0):F1} MB";
            return $"{BytesFreed / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
