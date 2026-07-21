using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class StatsPage
{
    private readonly HistoryService _historyService;

    public StatsPage(HistoryService historyService)
    {
        InitializeComponent();
        _historyService = historyService;
        LoadStats();
    }

    private void LoadStats()
    {
        txtTotalFreed.Text = FormatBytes(_historyService.GetTotalBytesFreed());
        txtSessions.Text = _historyService.GetSessionCount().ToString();
        var last = _historyService.GetLastCleaned();
        txtLastCleaned.Text = last?.ToString("g") ?? "Never";
        HistoryList.ItemsSource = _historyService.GetAll().Reverse().ToList();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
