namespace ModernFileCleaner.Models;

public enum SafetyLevel
{
    Safe,
    Caution,
    Dangerous
}

public class CleaningCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public bool IsSelected { get; set; } = true;
    public SafetyLevel Safety { get; set; } = SafetyLevel.Safe;
    public string Description { get; set; } = string.Empty;

    public string SizeFormatted
    {
        get
        {
            if (SizeInBytes < 1024) return $"{SizeInBytes} B";
            if (SizeInBytes < 1024 * 1024) return $"{SizeInBytes / 1024.0:F1} KB";
            if (SizeInBytes < 1024 * 1024 * 1024) return $"{SizeInBytes / (1024.0 * 1024.0):F1} MB";
            return $"{SizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
