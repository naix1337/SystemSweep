# Premium Edition – Plan 2: Dashboard & System Monitor

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans

**Goal:** Add live system monitoring Dashboard with Health Score, system stats cards, disk chart, and quick actions.

**Architecture:** DashboardPage as new start page with SystemMonitorService (PerformanceCounter + DriveInfo). Chart drawn with WPF Polyline.

**Tech Stack:** WPF UI 3.1.1, System.Diagnostics.PerformanceCounter, .NET 8.0-windows

## Global Constraints
- DashboardPage replaces CleanPage as first page in NavigationView
- SystemMonitorService refreshes every 5s via DispatcherTimer
- PerformanceCounter disposed on app exit
- Disk history stored in AppSettings.DiskHistory (List<long>, max 7 entries)

---

### Task 1: Add SystemStats Model + AppSettings Extensions

**Files:**
- Create: `cleaner1/Models/SystemStats.cs`
- Modify: `cleaner1/AppSettings.cs`

- [ ] **Step 1: Create SystemStats.cs**

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModernFileCleaner.Models;

public class SystemStats : INotifyPropertyChanged
{
    private float _cpuUsage;
    private float _ramUsage;
    private long _ramTotal;
    private long _ramAvailable;
    private long _diskFree;
    private long _diskTotal;
    private TimeSpan _uptime;

    public float CpuUsage { get => _cpuUsage; set { _cpuUsage = value; OnPropertyChanged(); } }
    public float RamUsage { get => _ramUsage; set { _ramUsage = value; OnPropertyChanged(); } }
    public long RamTotal { get => _ramTotal; set { _ramTotal = value; OnPropertyChanged(); } }
    public long RamAvailable { get => _ramAvailable; set { _ramAvailable = value; OnPropertyChanged(); } }
    public long DiskFree { get => _diskFree; set { _diskFree = value; OnPropertyChanged(); } }
    public long DiskTotal { get => _diskTotal; set { _diskTotal = value; OnPropertyChanged(); } }
    public TimeSpan Uptime { get => _uptime; set { _uptime = value; OnPropertyChanged(); } }
    
    public string RamFormatted => $"{(_ramTotal - _ramAvailable) / (1024.0 * 1024 * 1024):F1} / {_ramTotal / (1024.0 * 1024 * 1024):F0} GB";
    public string DiskFormatted => $"{_diskFree / (1024.0 * 1024 * 1024):F0} GB free";
    public string CpuFormatted => $"{_cpuUsage:F0}%";
    public string UptimeFormatted => _uptime.Days > 0 ? $"{_uptime.Days}d {_uptime.Hours}h" : $"{_uptime.Hours}h {_uptime.Minutes}m";
    
    public int HealthScore => CalculateHealth();
    
    private int CalculateHealth()
    {
        int diskScore = (int)Math.Min(100, _diskTotal > 0 ? (_diskFree * 100 / _diskTotal) * 5 : 0);
        int ramScore = (int)Math.Min(100, (1 - (_ramTotal - _ramAvailable) / (float)Math.Max(1, _ramTotal)) * 100);
        int cpuScore = (int)Math.Max(0, 100 - _cpuUsage * 1.5);
        return (diskScore * 40 + ramScore * 30 + cpuScore * 30) / 100;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

- [ ] **Step 2: Add DiskHistory to AppSettings.cs**

```csharp
// Add property:
public List<long> DiskHistory { get; set; } = new();

// In constructor:
DiskHistory = new List<long>();

// In Load():
DiskHistory = settings.DiskHistory ?? new List<long>();

// In Save(): automatic via JsonConvert
```

- [ ] **Step 3: Build & commit**

---

### Task 2: Create SystemMonitorService

**Files:**
- Create: `cleaner1/Services/SystemMonitorService.cs`

- [ ] **Step 1: Create SystemMonitorService.cs**

```csharp
using System.Diagnostics;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class SystemMonitorService : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private Timer? _timer;
    private bool _disposed;

    public event Action<SystemStats>? StatsUpdated;

    public SystemMonitorService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call returns 0, need to seed
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch { Debug.WriteLine("[SystemMonitor] Performance counters not available"); }
    }

    public void Start(int intervalMs = 5000)
    {
        if (_cpuCounter == null) return;
        _timer = new Timer(_ => Refresh(), null, 0, intervalMs);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public SystemStats Refresh()
    {
        var stats = new SystemStats();
        try
        {
            stats.CpuUsage = (float)Math.Round(_cpuCounter?.NextValue() ?? 0, 1);
            float ramAvailableMb = _ramCounter?.NextValue() ?? 0;
            stats.RamAvailable = (long)(ramAvailableMb * 1024 * 1024);
            stats.RamTotal = (long)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory);
        }
        catch { }

        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.Name.StartsWith("C:")).FirstOrDefault();
            if (drives != null)
            {
                stats.DiskFree = drives.TotalFreeSpace;
                stats.DiskTotal = drives.TotalSize;
            }
        }
        catch { }

        stats.Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        StatsUpdated?.Invoke(stats);
        return stats;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}
```

- [ ] **Step 2: Add Microsoft.VisualBasic reference to csproj**

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.VisualBasic" Version="10.3.0" />
</ItemGroup>
```

- [ ] **Step 3: Build & commit**

---

### Task 3: Create DashboardPage

**Files:**
- Create: `cleaner1/Pages/DashboardPage.xaml`
- Create: `cleaner1/Pages/DashboardPage.xaml.cs`

- [ ] **Step 1: Create DashboardPage.xaml**

```xml
<Page x:Class="ModernFileCleaner.Pages.DashboardPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      Title="Dashboard">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Margin="24">
        <StackPanel>
            <!-- Header -->
            <StackPanel Orientation="Horizontal" Margin="0,0,0,24">
                <TextBlock Text="&#xE933;" FontSize="28" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" Margin="0,0,12,0" VerticalAlignment="Center"/>
                <StackPanel>
                    <TextBlock Text="Dashboard" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                    <TextBlock Text="PC health at a glance" FontSize="13" Foreground="#FF888888"/>
                </StackPanel>
            </StackPanel>

            <!-- Health Score -->
            <Border x:Name="HealthCard" CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="System Health" FontSize="14" Foreground="#FF888888" Margin="0,0,0,8"/>
                    <TextBlock x:Name="txtHealthScore" Text="—" FontSize="36" FontWeight="Bold" Foreground="#FF4CAF50"/>
                    <TextBlock x:Name="txtHealthLabel" Text="Initializing..." FontSize="13" Foreground="#FF888888" Margin="0,4,0,0"/>
                    <ProgressBar x:Name="HealthBar" Height="6" Margin="0,12,0,0" CornerRadius="3" Background="#FF3D3D3D"/>
                </StackPanel>
            </Border>

            <!-- System Cards (2x2 Grid) -->
            <Grid Margin="0,0,0,16">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <!-- CPU -->
                <Border Grid.Row="0" Grid.Column="0" CornerRadius="12" Background="#FF2D2D2D" Padding="16" Margin="0,0,6,6">
                    <StackPanel>
                        <TextBlock Text="&#xE8B7;" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" FontSize="20" Foreground="#FF0078D4"/>
                        <TextBlock Text="CPU" FontSize="12" Foreground="#FF888888" Margin="0,8,0,2"/>
                        <TextBlock x:Name="txtCpu" Text="—" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                    </StackPanel>
                </Border>

                <!-- RAM -->
                <Border Grid.Row="0" Grid.Column="1" CornerRadius="12" Background="#FF2D2D2D" Padding="16" Margin="6,0,0,6">
                    <StackPanel>
                        <TextBlock Text="&#xE8B7;" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" FontSize="20" Foreground="#FF4CAF50"/>
                        <TextBlock Text="RAM" FontSize="12" Foreground="#FF888888" Margin="0,8,0,2"/>
                        <TextBlock x:Name="txtRam" Text="—" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                    </StackPanel>
                </Border>

                <!-- Disk -->
                <Border Grid.Row="1" Grid.Column="0" CornerRadius="12" Background="#FF2D2D2D" Padding="16" Margin="0,6,6,0">
                    <StackPanel>
                        <TextBlock Text="&#xE8B7;" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" FontSize="20" Foreground="#FFFFA500"/>
                        <TextBlock Text="Free Space" FontSize="12" Foreground="#FF888888" Margin="0,8,0,2"/>
                        <TextBlock x:Name="txtDisk" Text="—" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                    </StackPanel>
                </Border>

                <!-- Uptime -->
                <Border Grid.Row="1" Grid.Column="1" CornerRadius="12" Background="#FF2D2D2D" Padding="16" Margin="6,6,0,0">
                    <StackPanel>
                        <TextBlock Text="&#xE8B7;" FontFamily="/Wpf.Ui;Component/Resources/Font/#Segoe Fluent Icons" FontSize="20" Foreground="#FF8888FF"/>
                        <TextBlock Text="Uptime" FontSize="12" Foreground="#FF888888" Margin="0,8,0,2"/>
                        <TextBlock x:Name="txtUptime" Text="—" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- Disk Chart -->
            <Border CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Free Disk Space (7 days)" FontSize="14" Foreground="#FF888888" Margin="0,0,0,12"/>
                    <Border x:Name="ChartArea" Height="120" Background="#FF363636" CornerRadius="8"/>
                </StackPanel>
            </Border>

            <!-- Quick Actions -->
            <Border CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Quick Actions" FontSize="14" Foreground="#FF888888" Margin="0,0,0,12"/>
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                        <ui:Button Content="🧹 Quick Clean" Appearance="Primary" Width="140" Height="38" Margin="0,0,10,0" Click="QuickClean_Click"/>
                        <ui:Button Content="🗑️ Empty Recycle Bin" Appearance="Caution" Width="160" Height="38" Margin="0,0,10,0" Click="EmptyRecycle_Click"/>
                        <ui:Button Content="🔄 Refresh" Appearance="Secondary" Width="120" Height="38" Click="Refresh_Click"/>
                    </StackPanel>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</Page>
```

- [ ] **Step 2: Create DashboardPage.xaml.cs**

```csharp
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
                >= 80 => "✅ Your system is in great shape!",
                >= 50 => "⚠️ Some attention needed",
                _ => "🔴 Your system needs a cleanup!"
            };
        });
    }

    private async void QuickClean_Click(object sender, RoutedEventArgs e)
    {
        var quickCategories = new[] { "temp_files", "recycle_bin", "thumbnail_cache" };
        foreach (var id in quickCategories)
        {
            var cat = new CleaningCategory { Id = id };
            await _cleaningService.CleanCategoryAsync(cat);
        }
        _monitor.Refresh();
    }

    private async void EmptyRecycle_Click(object sender, RoutedEventArgs e)
    {
        var cat = new CleaningCategory { Id = "recycle_bin" };
        await _cleaningService.CleanCategoryAsync(cat);
        _monitor.Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _monitor.Refresh();
    }
}
```

- [ ] **Step 3: Build & commit**

---

### Task 4: Update NavigationView + MainWindow for Dashboard

**Files:**
- Modify: `cleaner1/MainWindow.xaml`
- Modify: `cleaner1/MainWindow.xaml.cs`

- [ ] **Step 1: Add Dashboard to NavigationView items (first position)**

```xml
<ui:NavigationViewItem Content="Dashboard" Icon="GridDots24" Tag="dashboard" IsActive="True" />
<ui:NavigationViewItem Content="Clean" Icon="Wrench24" Tag="clean" />
<!-- rest stays same -->
```

- [ ] **Step 2: Handle Dashboard in OnSelectionChanged + page caching**

```csharp
case "dashboard":
    if (!_pages.ContainsKey("dashboard"))
        _pages["dashboard"] = new DashboardPage();
    NavFrame.Navigate(_pages["dashboard"]);
    break;
```

Also handle page visibility:
```csharp
// After navigation, notify DashboardPage of visibility
if (NavFrame.Content is DashboardPage dbPage)
    dbPage.OnPageVisible();
else if (_previousPage is DashboardPage oldDb)
    oldDb.OnPageHidden();
```

- [ ] **Step 3: Change default page in Loaded event to Dashboard**

```csharp
Loaded += (_, _) =>
{
    var dashPage = new DashboardPage();
    _pages["dashboard"] = dashPage;
    NavFrame.Navigate(dashPage);
    NavFrame.Visibility = Visibility.Visible;
    dashPage.OnPageVisible();
};
```

- [ ] **Step 4: Build & commit**
