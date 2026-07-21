using System.Windows;
using ModernFileCleaner.Models;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class DashboardPage
{
    private readonly SystemMonitorService _monitor;
    private readonly CleaningService _cleaningService = new();
    private bool _isVisible;

    public DashboardPage()
    {
        InitializeComponent();
        _monitor = new SystemMonitorService();
        _monitor.StatsUpdated += OnStatsUpdated;
    }

    public void OnPageVisible()
    {
        if (!_isVisible)
        {
            _isVisible = true;
            _monitor.Start();
            _monitor.Refresh(); // immediate first refresh
        }
    }

    public void OnPageHidden()
    {
        if (_isVisible)
        {
            _isVisible = false;
            _monitor.Stop();
        }
    }

    private void OnStatsUpdated(SystemStats stats)
    {
        Dispatcher.Invoke(() =>
        {
            txtCpu.Text = stats.CpuFormatted;
            txtRam.Text = stats.RamFormatted;
            txtDisk.Text = stats.DiskFormatted;
            txtUptime.Text = stats.UptimeFormatted;

            txtHealthScore.Text = $"{stats.HealthScore}/100";
            HealthBar.Value = stats.HealthScore;

            txtHealthScore.Foreground = stats.HealthScore switch
            {
                >= 80 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),
                >= 50 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54))
            };
            HealthBar.Foreground = txtHealthScore.Foreground;

            txtHealthLabel.Text = stats.HealthScore switch
            {
                >= 80 => "Your system is in great shape!",
                >= 50 => "Some attention needed",
                _ => "Your system needs a cleanup!"
            };
        });
    }

    private async void QuickClean_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var quickCategories = new[] { "temp_files", "recycle_bin", "thumbnail_cache" };
            foreach (var id in quickCategories)
            {
                var cat = new CleaningCategory { Id = id };
                await _cleaningService.CleanCategoryAsync(cat);
            }
            _monitor.Refresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard.QuickClean] {ex.Message}");
        }
    }

    private async void EmptyRecycle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cat = new CleaningCategory { Id = "recycle_bin" };
            await _cleaningService.CleanCategoryAsync(cat);
            _monitor.Refresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard.EmptyRecycle] {ex.Message}");
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _monitor.Refresh();
    }
}
