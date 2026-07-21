using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class BrowserCachePage
{
    private readonly BrowserCacheService _cacheService = new();
    private List<BrowserCacheInfo> _browsers = new();

    public BrowserCachePage()
    {
        InitializeComponent();
    }

    private async void Scan_Click(object? sender, RoutedEventArgs? e)
    {
        btnScan.IsEnabled = false;
        btnClean.IsEnabled = false;
        txtStatus.Text = "Scanning browser caches...";

        _browsers = await Task.Run(() => _cacheService.Scan());
        CardsContainer.ItemsSource = null;
        CardsContainer.ItemsSource = _browsers;
        txtStatus.Text = $"Found {_browsers.Sum(b => b.SizeBytes) / (1024 * 1024)} MB browser cache";

        btnScan.IsEnabled = true;
        btnClean.IsEnabled = true;
    }

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        var selected = _browsers.Where(b => b.IsSelected).ToList();
        if (selected.Count == 0)
        {
            txtStatus.Text = "No browsers selected for cleaning";
            return;
        }

        btnScan.IsEnabled = false;
        btnClean.IsEnabled = false;
        txtStatus.Text = "Cleaning selected browser caches...";

        var freed = await Task.Run(() => _cacheService.Clean(_browsers));
        txtStatus.Text = $"Freed {freed / (1024 * 1024)} MB";

        // Re-scan to update sizes
        Scan_Click(null, null);
    }
}
