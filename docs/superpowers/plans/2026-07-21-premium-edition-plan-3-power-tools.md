# Premium Edition – Plan 3: Power Tools

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans

**Goal:** Add Browser Cache Cleaner, Duplicate File Finder, Startup Manager, Cleaning Presets, and Cleaning Reports.

**Architecture:** 3 new pages + 4 new services. Each tool is independent. Browser cache uses known paths, duplicate finder uses SHA256 hashing, startup manager reads registry.

**Tech Stack:** WPF UI 3.1.1, System.Security.Cryptography, Microsoft.Win32.Registry, .NET 8.0-windows

---

### Task 1: Cleaning Presets (CleanPage modification)

**Files:**
- Modify: `cleaner1/Pages/CleanPage.xaml`
- Modify: `cleaner1/Pages/CleanPage.xaml.cs`

- [ ] **Step 1: Add preset selector to CleanPage.xaml (above the card grid)**

```xml
<StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16" HorizontalAlignment="Center">
    <TextBlock Text="Preset:" Foreground="#FF888888" VerticalAlignment="Center" Margin="0,0,10,0"/>
    <ui:ComboBox x:Name="cmbPreset" Width="200" 
                 SelectionChanged="cmbPreset_SelectionChanged">
        <ui:ComboBoxItem Content="🧹 Quick Clean" Tag="quick"/>
        <ui:ComboBoxItem Content="🧹 Standard Clean" Tag="standard"/>
        <ui:ComboBoxItem Content="🧹 Deep Clean" Tag="deep"/>
        <ui:ComboBoxItem Content="✋ Custom" Tag="custom"/>
    </ui:ComboBox>
</StackPanel>
```

Add `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"` if not present. Adjust Grid.Row numbers (add 1 to all existing rows).

- [ ] **Step 2: Add preset logic to CleanPage.xaml.cs**

```csharp
private void cmbPreset_SelectionChanged(object? sender, SelectionChangedEventArgs e)
{
    var tag = (cmbPreset.SelectedItem as FrameworkElement)?.Tag?.ToString();
    ApplyPreset(tag);
}

private void ApplyPreset(string? preset)
{
    foreach (var cat in _categories)
        cat.IsSelected = preset switch
        {
            "quick" => cat.Id is "temp_files" or "recycle_bin" or "thumbnail_cache",
            "standard" => cat.Id is "temp_files" or "recycle_bin" or "thumbnail_cache" 
                or "download_cache" or "error_reports" or "windows_logs",
            "deep" => true, // all including dangerous
            _ => cat.IsSelected // custom = keep current
        };
    
    if (preset == "deep")
    {
        var result = ShowConfirm("Deep Clean will remove ALL items including Windows.old and Memory Dumps. Continue?", "Deep Clean");
        if (result != MessageBoxResult.Yes)
        {
            cmbPreset.SelectedIndex = _lastPresetIndex;
            return;
        }
    }
    _lastPresetIndex = cmbPreset.SelectedIndex;
}
```

Add `private int _lastPresetIndex = 0;` field.

- [ ] **Step 3: Build**

- [ ] **Step 4: Commit**

---

### Task 2: Browser Cache Cleaner

**Files:**
- Create: `cleaner1/Services/BrowserCacheService.cs`
- Create: `cleaner1/Pages/BrowserCachePage.xaml`
- Create: `cleaner1/Pages/BrowserCachePage.xaml.cs`

- [ ] **Step 1: Create BrowserCacheService.cs**

```csharp
using System.Diagnostics;

namespace ModernFileCleaner.Services;

public class BrowserCacheInfo
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Path { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public string SizeFormatted => FormatBytes(SizeBytes);
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}

public class BrowserCacheService
{
    private readonly List<BrowserCacheInfo> _browsers = new()
    {
        new() { Name = "Google Chrome", Icon = "", Path = @"Google\Chrome\User Data\Default\Cache" },
        new() { Name = "Microsoft Edge", Icon = "", Path = @"Microsoft\Edge\User Data\Default\Cache" },
        new() { Name = "Firefox", Icon = "", Path = @"Mozilla\Firefox\Profiles" },
        new() { Name = "Brave", Icon = "", Path = @"BraveSoftware\Brave-Browser\User Data\Default\Cache" },
    };

    public List<BrowserCacheInfo> Scan()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var browser in _browsers)
        {
            var fullPath = System.IO.Path.Combine(localAppData, browser.Path);
            browser.SizeBytes = DirSize(new DirectoryInfo(fullPath));
        }
        return _browsers;
    }

    public long Clean(List<BrowserCacheInfo> selected)
    {
        long freed = 0;
        foreach (var browser in selected.Where(b => b.IsSelected))
        {
            freed += browser.SizeBytes;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fullPath = System.IO.Path.Combine(localAppData, browser.Path);
            if (Directory.Exists(fullPath))
            {
                try { Directory.Delete(fullPath, true); } catch { }
            }
        }
        return freed;
    }

    private static long DirSize(DirectoryInfo? dir)
    {
        if (dir == null || !dir.Exists) return 0;
        long size = 0;
        try
        {
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                try { size += file.Length; } catch { }
        }
        catch { }
        return size;
    }
}
```

- [ ] **Step 2: Create BrowserCachePage.xaml** (similar card layout to CleanPage, 4 browser cards)

- [ ] **Step 3: Create BrowserCachePage.xaml.cs**

```csharp
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

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        _browsers = await Task.Run(() => _cacheService.Scan());
        CardsContainer.ItemsSource = null;
        CardsContainer.ItemsSource = _browsers;
        txtStatus.Text = $"Found {_browsers.Sum(b => b.SizeBytes) / (1024*1024)} MB browser cache";
    }

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        var freed = await Task.Run(() => _cacheService.Clean(_browsers));
        txtStatus.Text = $"✅ Freed {freed / (1024*1024)} MB";
        Scan_Click(null, null); // re-scan
    }
}
```

- [ ] **Step 4: Add BrowserCache to NavigationView** (after Clean)

```xml
<ui:NavigationViewItem Content="Browser Cache" Icon="Globe24" Tag="browsers" />
```

- [ ] **Step 5: Handle in MainWindow.xaml.cs**

```csharp
case "browsers":
    if (!_pages.ContainsKey("browsers"))
        _pages["browsers"] = new BrowserCachePage();
    NavFrame.Navigate(_pages["browsers"]);
    break;
```

- [ ] **Step 6: Build & commit**

---

### Task 3: Startup Manager

**Files:**
- Create: `cleaner1/Services/StartupService.cs`
- Create: `cleaner1/Pages/StartupPage.xaml`
- Create: `cleaner1/Pages/StartupPage.xaml.cs`

- [ ] **Step 1: Create StartupService.cs**

```csharp
using Microsoft.Win32;

namespace ModernFileCleaner.Services;

public class StartupItem
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public bool Enabled { get; set; }
    public string Source { get; set; } = "Registry";
}

public class StartupService
{
    public List<StartupItem> GetItems()
    {
        var items = new List<StartupItem>();
        
        // Registry (HKCU)
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (key != null)
        {
            foreach (var name in key.GetValueNames())
            {
                items.Add(new StartupItem
                {
                    Name = name,
                    Command = key.GetValue(name)?.ToString() ?? "",
                    Enabled = true,
                    Source = "Registry"
                });
            }
        }

        // Startup folder
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (Directory.Exists(startupFolder))
        {
            foreach (var file in Directory.GetFiles(startupFolder))
            {
                items.Add(new StartupItem
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = file,
                    Enabled = !file.EndsWith(".disabled"),
                    Source = "Startup Folder"
                });
            }
        }

        return items;
    }

    public void Toggle(StartupItem item)
    {
        if (item.Source == "Registry")
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                if (item.Enabled)
                    key.DeleteValue(item.Name, false);
                else
                    key.SetValue(item.Name, item.Command);
            }
        }
        else
        {
            var file = item.Command;
            if (item.Enabled && file.EndsWith(".disabled"))
                File.Move(file, file.Replace(".disabled", ""));
            else if (!item.Enabled && !file.EndsWith(".disabled"))
                File.Move(file, file + ".disabled");
        }
        item.Enabled = !item.Enabled;
    }
}
```

- [ ] **Step 2: Create StartupPage.xaml** (ListBox with items, toggle button)

- [ ] **Step 3: Create StartupPage.xaml.cs** (load on page shown, toggle on button)

- [ ] **Step 4: Add to NavigationView + MainWindow handler**

- [ ] **Step 5: Build & commit**

---

### Task 4: Cleaning Reports (StatsPage extension)

**Files:**
- Create: `cleaner1/Services/ReportService.cs`
- Modify: `cleaner1/Pages/StatsPage.xaml`
- Modify: `cleaner1/Pages/StatsPage.xaml.cs`

- [ ] **Step 1: Create ReportService.cs**

```csharp
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
```

- [ ] **Step 2: Add "Export Report" button to StatsPage.xaml history section**

```xml
<ui:Button Content="📄 Export Report" Appearance="Primary" 
           Width="140" Height="32" 
           HorizontalAlignment="Right"
           Click="ExportReport_Click"/>
```

- [ ] **Step 3: Add ExportReport_Click handler to StatsPage.xaml.cs**

```csharp
private async void ExportReport_Click(object sender, RoutedEventArgs e)
{
    var history = _historyService.GetAll().LastOrDefault();
    if (history == null) return;
    
    var reportsDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "SystemSweep Reports");
    Directory.CreateDirectory(reportsDir);
    
    var path = System.IO.Path.Combine(reportsDir, 
        $"clean-report-{history.Timestamp:yyyy-MM-dd-HHmmss}.html");
    
    var reportService = new ReportService();
    await reportService.ExportHtmlAsync(history, path);
    
    MessageBox.Show($"Report saved to:\n{path}", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
}
```

- [ ] **Step 4: Build & commit**

---

### Task 5: Duplicate File Finder (Simple Version)

**Files:**
- Create: `cleaner1/Services/DuplicateFinderService.cs`
- Create: `cleaner1/Pages/DuplicatesPage.xaml`
- Create: `cleaner1/Pages/DuplicatesPage.xaml.cs`

- [ ] **Step 1: Create DuplicateFinderService.cs**

```csharp
using System.Security.Cryptography;

namespace ModernFileCleaner.Services;

public class DuplicateGroup
{
    public string Hash { get; set; } = "";
    public long SizeBytes { get; set; }
    public List<string> Files { get; set; } = new();
    public bool IsSelected { get; set; } = true;
    public string SizeFormatted => FormatBytes(SizeBytes);
    private static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" :
        bytes < 1024 * 1024 ? $"{bytes / 1024.0:F1} KB" :
        bytes < 1024 * 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):F1} MB" :
        $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
}

public class DuplicateFinderService
{
    public async Task<List<DuplicateGroup>> FindDuplicatesAsync(string rootPath, IProgress<string>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var groups = new Dictionary<string, DuplicateGroup>();
            var allFiles = new List<string>();
            
            try
            {
                foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    allFiles.Add(file);
                }
            }
            catch (UnauthorizedAccessException) { }

            progress?.Report($"Scanning {allFiles.Count} files...");

            // Phase 1: Group by (name + size)
            var candidates = allFiles
                .GroupBy(f => $"{Path.GetFileName(f)}|{new FileInfo(f).Length}")
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            progress?.Report($"Hashing {candidates.Count} candidates...");

            // Phase 2: Hash candidates
            foreach (var file in candidates)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var stream = File.OpenRead(file);
                    var hash = BitConverter.ToString(SHA256.HashData(stream)).Replace("-", "");
                    
                    if (!groups.ContainsKey(hash))
                        groups[hash] = new DuplicateGroup { Hash = hash, SizeBytes = new FileInfo(file).Length };
                    groups[hash].Files.Add(file);
                }
                catch { }
            }

            return groups.Values.Where(g => g.Files.Count > 1).ToList();
        }, ct);
    }
}
```

- [ ] **Step 2: Create DuplicatesPage.xaml** (path picker, scan button, results list, delete button)

- [ ] **Step 3: Create DuplicatesPage.xaml.cs**

- [ ] **Step 4: Add to NavigationView + MainWindow handler**

- [ ] **Step 5: Build & commit**
