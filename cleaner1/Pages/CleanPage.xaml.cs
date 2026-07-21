using System.Windows;
using ModernFileCleaner.Controls;
using ModernFileCleaner.Models;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class CleanPage
{
    private readonly HistoryService _historyService;
    private readonly CalculationService _calculationService = new();
    private readonly CleaningService _cleaningService = new();
    private readonly List<CleaningCategory> _categories = new()
    {
        new() { Id = "temp_files", Name = "Temporary Files", Icon = "🗑️", Safety = SafetyLevel.Safe, Description = "Windows temp & user temp files" },
        new() { Id = "recycle_bin", Name = "Recycle Bin", Icon = "♻️", Safety = SafetyLevel.Safe, Description = "Deleted files in Recycle Bin" },
        new() { Id = "download_cache", Name = "Download Cache", Icon = "📥", Safety = SafetyLevel.Safe, Description = "Downloads folder contents" },
        new() { Id = "thumbnail_cache", Name = "Thumbnail Cache", Icon = "🖼️", Safety = SafetyLevel.Safe, Description = "Explorer thumbnail cache DBs" },
        new() { Id = "error_reports", Name = "Error Reports", Icon = "⚠️", Safety = SafetyLevel.Caution, Description = "Windows Error Reporting data" },
        new() { Id = "installer_temp", Name = "Installer Temp", Icon = "📦", Safety = SafetyLevel.Caution, Description = "MSI installer temporary files" },
        new() { Id = "store_cache", Name = "Store Cache", Icon = "🏪", Safety = SafetyLevel.Safe, Description = "Microsoft Store app cache (wsreset)" },
        new() { Id = "windows_logs", Name = "Windows Logs", Icon = "📋", Safety = SafetyLevel.Caution, Description = "Windows .log files" },
        new() { Id = "windows_old", Name = "Windows.old", Icon = "🪟", Safety = SafetyLevel.Dangerous, Description = "Previous Windows installation" },
        new() { Id = "memory_dumps", Name = "Memory Dumps", Icon = "💾", Safety = SafetyLevel.Dangerous, Description = "System crash memory dumps" },
    };

    public CleanPage(HistoryService historyService)
    {
        InitializeComponent();
        _historyService = historyService;
        CardsContainer.ItemsSource = _categories;
    }

    public void RunAutoAnalyze()
    {
        btnAnalyze_Click(null, null);
    }

    private void SelectAllToggle_Click(object sender, RoutedEventArgs e)
    {
        bool isChecked = SelectAllToggle.IsChecked ?? true;
        foreach (var cat in _categories)
            cat.IsSelected = isChecked;
    }

    private async void btnAnalyze_Click(object? sender, RoutedEventArgs? e)
    {
        btnAnalyze.IsEnabled = false;
        btnClean.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = true;
        txtStatus.Text = "Analysing...";

        var progress = new Progress<string>(s => txtStatus.Text = s);
        await _calculationService.CalculateAllAsync(_categories, progress);

        ProgressBar.IsIndeterminate = false;
        ProgressBar.Visibility = Visibility.Collapsed;
        btnAnalyze.IsEnabled = true;
        btnClean.IsEnabled = true;
        txtStatus.Text = "Analysis complete. Ready to clean.";
    }

    private async void btnClean_Click(object? sender, RoutedEventArgs? e)
    {
        var selected = _categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ShowMessage("Please select at least one category to clean.", "Info");
            return;
        }

        // Confirm dangerous selections
        var dangerous = selected.Where(c => c.Safety == SafetyLevel.Dangerous).ToList();
        if (dangerous.Count > 0)
        {
            var names = string.Join(", ", dangerous.Select(d => d.Name));
            var result = ShowConfirm(
                $"⚠️ Dangerous items selected: {names}\n\nAre you sure you want to proceed? This cannot be undone.",
                "Confirm Cleaning");
            if (result != MessageBoxResult.Yes) return;
        }

        btnAnalyze.IsEnabled = false;
        btnClean.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Maximum = selected.Count;
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;

        long totalFreed = 0;
        var cleanedCategories = new List<string>();

        for (int i = 0; i < selected.Count; i++)
        {
            var cat = selected[i];
            txtStatus.Text = $"🧹 Cleaning {cat.Name}...";
            long freed = await _cleaningService.CleanCategoryAsync(cat);
            totalFreed += freed;
            cleanedCategories.Add(cat.Name);
            ProgressBar.Value = i + 1;
        }

        // Save history
        if (totalFreed > 0)
        {
            await _historyService.AddEntryAsync(new CleanHistoryEntry
            {
                Timestamp = DateTime.Now,
                BytesFreed = totalFreed,
                CategoriesCleaned = cleanedCategories
            });
        }

        AppSettings.Instance.LastCleaned = DateTime.Now;
        AppSettings.Instance.Save();

        ProgressBar.Visibility = Visibility.Collapsed;
        btnAnalyze.IsEnabled = true;
        btnClean.IsEnabled = true;
        txtStatus.Text = $"✅ Cleaning complete! Freed {FormatBytes(totalFreed)}.";
        ShowMessage($"✅ Cleaning complete!\n\nFreed: {FormatBytes(totalFreed)}\nCategories: {cleanedCategories.Count}", "Success");
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    private static void ShowMessage(string msg, string title)
    {
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static MessageBoxResult ShowConfirm(string msg, string title)
    {
        return MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
    }
}
