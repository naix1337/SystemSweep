using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class HistoryService
{
    private static readonly string HistoryPath = "history.json";
    private List<CleanHistoryEntry> _entries = new();

    public IReadOnlyList<CleanHistoryEntry> GetAll() => _entries.AsReadOnly();

    public long GetTotalBytesFreed() => _entries.Sum(e => e.BytesFreed);

    public int GetSessionCount() => _entries.Count;

    public DateTime? GetLastCleaned() => _entries.Count > 0 ? _entries.Max(e => e.Timestamp) : null;

    public void Load()
    {
        if (!File.Exists(HistoryPath)) return;
        try
        {
            string json = File.ReadAllText(HistoryPath);
            var entries = JsonConvert.DeserializeObject<List<CleanHistoryEntry>>(json);
            if (entries != null) _entries = entries;
        }
        catch (Exception ex) { Debug.WriteLine($"[HistoryService] {ex.Message}"); }
    }

    public async Task AddEntryAsync(CleanHistoryEntry entry)
    {
        _entries.Add(entry);
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
            await File.WriteAllTextAsync(HistoryPath, json);
        }
        catch (Exception ex) { Debug.WriteLine($"[HistoryService] {ex.Message}"); }
    }
}
