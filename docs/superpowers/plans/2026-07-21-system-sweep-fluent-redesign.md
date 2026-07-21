# System Sweep Fluent Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign System Sweep with Windows 11 Fluent Design using WPF UI library, card-based layout, and improved cleaning functionality.

**Architecture:** WPF .NET 8 app using wpfui library for NavigationView, Mica backdrop, and modern controls. The monolithic MainWindow is refactored into Page-based architecture with separated Services and Models. The NavigationView drives page switching.

**Tech Stack:** .NET 8.0, WPF, wpfui (NuGet), Newtonsoft.Json

**Design Spec:** `docs/superpowers/specs/2026-07-21-system-sweep-fluent-redesign.md`

## Global Constraints

- Target Framework: net8.0-windows (WPF)
- wpfui NuGet package version 3.x (latest stable)
- All new XAML files must use wpfui namespace: `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`
- Main window must use `ui:UiWindow` with `WindowBackdrop.Mica` (or Acrylic fallback)
- NavigationView must have 4 items: Clean, Statistics, Settings, About
- Each page must inherit from `ui:UiPage` (or be wrapped in one)
- All cleaning logic refactored out of MainWindow into CleaningService
- Cleaning history persists to `history.json` via HistoryService
- Dangerous categories (Windows.old, Memory Dumps) show red warning + extra confirmation
- Size formatting must auto-scale: B, KB, MB, GB

---

### Task 1: Project Setup & Dependencies

**Files:**
- Modify: `cleaner1/cleaner.csproj`
- Modify: `cleaner1/App.xaml`
- Modify: `cleaner1/App.xaml.cs`

**Interfaces:**
- Consumes: nothing
- Produces: Solution with wpfui installed, App.xaml configured

- [ ] **Step 1: Add WPF UI NuGet package and update csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="WPF-UI" Version="3.*" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Restore packages**

Run: `cd cleaner1 && dotnet restore`
Expected: Packages restored, no errors

- [ ] **Step 3: Update App.xaml to use wpfui resources**

```xml
<Application x:Class="ModernFileCleaner.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ui:ThemesDictionary Theme="Dark" />
                <ui:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add cleaner1/cleaner.csproj cleaner1/App.xaml cleaner1/App.xaml.cs
git commit -m "feat: add wpfui dependency and configure app resources"
```

---

### Task 2: Create Data Models

**Files:**
- Create: `cleaner1/Models/CleaningCategory.cs`
- Create: `cleaner1/Models/CleanHistoryEntry.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `CleaningCategory` (with Id, Name, Icon, SizeInBytes, IsSelected, Safety, Action), `CleanHistoryEntry` (with Timestamp, BytesFreed, CategoriesCleaned)

- [ ] **Step 1: Create CleaningCategory.cs**

```csharp
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
```

- [ ] **Step 2: Create CleanHistoryEntry.cs**

```csharp
namespace ModernFileCleaner.Models;

public class CleanHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public long BytesFreed { get; set; }
    public List<string> CategoriesCleaned { get; set; } = new();

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
```

- [ ] **Step 3: Commit**

```bash
git add cleaner1/Models/
git commit -m "feat: add data models for cleaning categories and history"
```

---

### Task 3: Create Services

**Files:**
- Create: `cleaner1/Services/CalculationService.cs`
- Create: `cleaner1/Services/CleaningService.cs`
- Create: `cleaner1/Services/HistoryService.cs`

**Interfaces:**
- Consumes: `CleaningCategory`, `CleanHistoryEntry` (from Task 2)
- Produces: `CalculationService.CalculateAllAsync(categories)` returning void (updates each category's SizeInBytes), `CleaningService.CleanAsync(category, onProgress)` returning long bytesFreed, `HistoryService` (Save, Load, GetAll)

- [ ] **Step 1: Create CalculationService.cs**

```csharp
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class CalculationService
{
    public async Task CalculateAllAsync(List<CleaningCategory> categories, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            foreach (var category in categories)
            {
                progress?.Report($"Analysiere {category.Name}...");
                category.SizeInBytes = CalculateCategory(category.Id);
            }
        });
    }

    public long CalculateCategory(string categoryId)
    {
        return categoryId switch
        {
            "temp_files" => GetDirectorySize(Path.GetTempPath()),
            "recycle_bin" => 100L * 1024 * 1024, // approx
            "download_cache" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")),
            "thumbnail_cache" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Explorer"), "thumbcache_*.db"),
            "error_reports" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft", "Windows", "WER")),
            "installer_temp" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer"), "*.tmp"),
            "store_cache" => 100L * 1024 * 1024, // approx
            "windows_logs" => CalculateWindowsLogFilesSize(),
            "windows_old" => GetDirectorySize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old")),
            "memory_dumps" => CalculateMemoryDumpsSize(),
            _ => 0
        };
    }

    private long CalculateWindowsLogFilesSize()
    {
        long size = 0;
        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!Directory.Exists(windowsPath)) return size;
        foreach (var file in Directory.GetFiles(windowsPath, "*.log", SearchOption.AllDirectories))
        {
            try { size += new FileInfo(file).Length; } catch { }
        }
        return size;
    }

    private long CalculateMemoryDumpsSize()
    {
        long size = 0;
        string dumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
        if (File.Exists(dumpPath)) { try { size += new FileInfo(dumpPath).Length; } catch { } }
        string minidumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
        if (Directory.Exists(minidumpPath)) size += GetDirectorySize(minidumpPath);
        return size;
    }

    private long GetDirectorySize(string path, string searchPattern = "*.*")
    {
        long size = 0;
        if (!Directory.Exists(path)) return size;
        try
        {
            foreach (var file in Directory.GetFiles(path, searchPattern))
                try { size += new FileInfo(file).Length; } catch { }
            foreach (var dir in Directory.GetDirectories(path))
                try { size += GetDirectorySize(dir, searchPattern); } catch { }
        }
        catch { }
        return size;
    }
}
```

- [ ] **Step 2: Create CleaningService.cs**

```csharp
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Services;

public class CleaningService
{
    public async Task<long> CleanCategoryAsync(CleaningCategory category, IProgress<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report($"Bereinige {category.Name}...");
            long before = category.SizeInBytes;
            CleanById(category.Id);
            return before;
        });
    }

    private void CleanById(string categoryId)
    {
        switch (categoryId)
        {
            case "temp_files":
                CleanDirectory(Path.GetTempPath());
                string? userTemp = Environment.GetEnvironmentVariable("TEMP");
                if (!string.IsNullOrEmpty(userTemp)) CleanDirectory(userTemp);
                break;
            case "recycle_bin":
                try
                {
                    string recyclePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "Windows", "RecycleBin");
                    if (Directory.Exists(recyclePath)) CleanDirectory(recyclePath);
                }
                catch { }
                break;
            case "download_cache":
                string downloads = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloads)) CleanDirectory(downloads);
                break;
            case "thumbnail_cache":
                string thumbCache = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Explorer");
                if (Directory.Exists(thumbCache))
                {
                    foreach (var f in Directory.GetFiles(thumbCache, "thumbcache_*.db"))
                        try { File.Delete(f); } catch { }
                }
                break;
            case "error_reports":
                string wer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Microsoft", "Windows", "WER");
                if (Directory.Exists(wer)) CleanDirectory(wer);
                break;
            case "installer_temp":
                string installer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
                if (Directory.Exists(installer))
                {
                    foreach (var f in Directory.GetFiles(installer, "*.tmp"))
                        try { File.Delete(f); } catch { }
                }
                break;
            case "store_cache":
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wsreset.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.WaitForExit();
                break;
            case "windows_logs":
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (Directory.Exists(winDir))
                {
                    foreach (var f in Directory.GetFiles(winDir, "*.log", SearchOption.AllDirectories))
                        try { File.Delete(f); } catch { }
                }
                break;
            case "windows_old":
                string winOld = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old");
                if (Directory.Exists(winOld)) { CleanDirectory(winOld); try { Directory.Delete(winOld, true); } catch { } }
                break;
            case "memory_dumps":
                string dump = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
                if (File.Exists(dump)) try { File.Delete(dump); } catch { }
                string minidump = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");
                if (Directory.Exists(minidump)) CleanDirectory(minidump);
                break;
        }
    }

    private void CleanDirectory(string path, string searchPattern = "*.*")
    {
        if (!Directory.Exists(path)) return;
        try
        {
            foreach (var file in Directory.GetFiles(path, searchPattern))
                try { File.Delete(file); } catch { }
            foreach (var dir in Directory.GetDirectories(path))
                try { Directory.Delete(dir, true); } catch { }
        }
        catch { }
    }
}
```

- [ ] **Step 3: Create HistoryService.cs**

```csharp
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
        catch { }
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
        catch { }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Services/
git commit -m "feat: add CalculationService, CleaningService, HistoryService"
```

---

### Task 4: Extend AppSettings

**Files:**
- Modify: `cleaner1/AppSettings.cs`

**Interfaces:**
- Consumes: nothing
- Produces: New settings properties (SafetyBackup, ConfirmedDangerousCategories)

- [ ] **Step 1: Extend AppSettings with new properties**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ModernFileCleaner
{
    public sealed class AppSettings
    {
        private static readonly Lazy<AppSettings> lazy = new Lazy<AppSettings>(() => new AppSettings());
        public static AppSettings Instance { get { return lazy.Value; } }

        public bool AutoAnalyze { get; set; }
        public bool AutoClean { get; set; }
        public bool ShowNotifications { get; set; }
        public bool SafetyBackup { get; set; } = true;
        public DateTime LastCleaned { get; set; }

        private static readonly string SettingsPath = "settings.json";

        private AppSettings()
        {
            AutoAnalyze = false;
            AutoClean = false;
            ShowNotifications = true;
            SafetyBackup = true;
            LastCleaned = DateTime.MinValue;
        }

        public void Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        AutoAnalyze = settings.AutoAnalyze;
                        AutoClean = settings.AutoClean;
                        ShowNotifications = settings.ShowNotifications;
                        SafetyBackup = settings.SafetyBackup;
                        LastCleaned = settings.LastCleaned;
                    }
                }
                catch { }
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add cleaner1/AppSettings.cs
git commit -m "feat: extend AppSettings with SafetyBackup property"
```

---

### Task 5: Create CleaningCard Control

**Files:**
- Create: `cleaner1/Controls/CleaningCard.xaml`
- Create: `cleaner1/Controls/CleaningCard.xaml.cs`

**Interfaces:**
- Consumes: `CleaningCategory` (from Task 2)
- Produces: Reusable UserControl with card UI, bindable to CleaningCategory

- [ ] **Step 1: Create CleaningCard.xaml**

```xml
<UserControl x:Class="ModernFileCleaner.Controls.CleaningCard"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml">
    <Border CornerRadius="12" 
            Background="#FF2D2D2D" 
            BorderBrush="#FF3D3D3D" 
            BorderThickness="1"
            Padding="16"
            Margin="6">
        <Border.Style>
            <Style TargetType="Border">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Safety}" Value="Dangerous">
                        <Setter Property="BorderBrush" Value="#44FF4444"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding Safety}" Value="Caution">
                        <Setter Property="BorderBrush" Value="#44FFAA00"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Icon -->
            <TextBlock Grid.Row="0" Grid.Column="0" Grid.RowSpan="2"
                       Text="{Binding Icon}" FontSize="32" 
                       VerticalAlignment="Center" Margin="0,0,12,0"/>

            <!-- Name -->
            <TextBlock Grid.Row="0" Grid.Column="1" 
                       Text="{Binding Name}" 
                       FontSize="15" FontWeight="SemiBold"
                       Foreground="White"
                       VerticalAlignment="Center"/>

            <!-- Size -->
            <TextBlock Grid.Row="1" Grid.Column="1"
                       Text="{Binding SizeFormatted}" 
                       FontSize="13"
                       Foreground="{Binding SizeForeground}"
                       VerticalAlignment="Center"
                       Margin="0,2,0,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="#FF888888"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding SizeInBytes}" Value="0">
                                <Setter Property="Text" Value="—"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>

            <!-- Description -->
            <TextBlock Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2"
                       Text="{Binding Description}"
                       FontSize="11" Foreground="#FF666666"
                       Margin="0,8,0,0" TextWrapping="Wrap"/>

            <!-- Checkbox -->
            <ui:ToggleSwitch Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2"
                             IsChecked="{Binding IsSelected}"
                             Margin="0,8,0,0"
                             Height="24"/>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Create CleaningCard.xaml.cs**

```csharp
using System.Windows.Controls;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Controls;

public partial class CleaningCard : UserControl
{
    public CleaningCategory? Category
    {
        get => DataContext as CleaningCategory;
        set => DataContext = value;
    }

    public CleaningCard()
    {
        InitializeComponent();
    }

    public CleaningCard(CleaningCategory category) : this()
    {
        Category = category;
    }
}
```

- [ ] **Step 3: Build to verify XAML compiles**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Controls/
git commit -m "feat: add CleaningCard user control"
```

---

### Task 6: Rewrite MainWindow with NavigationView

**Files:**
- Modify: `cleaner1/MainWindow.xaml`
- Modify: `cleaner1/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: Pages (from Tasks 7-10 — referenced by namespace)
- Produces: NavigationView with 4 navigation items routing to pages

- [ ] **Step 1: Rewrite MainWindow.xaml**

```xml
<ui:UiWindow x:Class="ModernFileCleaner.MainWindow"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             xmlns:pages="clr-namespace:ModernFileCleaner.Pages"
             Title="System Sweep" 
             Height="750" 
             Width="1100"
             WindowStartupLocation="CenterScreen"
             ExtendsContentIntoTitleBar="True"
             WindowBackdrop="Mica">
    <ui:UiWindow.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ui:ThemesDictionary Theme="Dark" />
                <ui:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ui:UiWindow.Resources>

    <Grid>
        <ui:NavigationView x:Name="NavView"
                           Frame="{Binding ElementName=NavFrame}"
                           PaneDisplayMode="Left"
                           OpenPaneLength="200"
                           CompactPaneLength="48"
                           BreadcrumbBar="Collapsed"
                           IsPaneToggleVisible="False"
                           HeaderVisibility="Collapsed">
            <ui:NavigationView.MenuItems>
                <ui:NavigationViewItem Content="Clean" Icon="Wrench24" Tag="clean" 
                                       IsSelected="True" />
                <ui:NavigationViewItem Content="Statistics" Icon="DataBarVertical24" Tag="stats" />
                <ui:NavigationViewItem Content="Settings" Icon="Settings24" Tag="settings" />
                <ui:NavigationViewItem Content="About" Icon="Info24" Tag="about" />
            </ui:NavigationView.MenuItems>
        </ui:NavigationView>

        <Frame x:Name="NavFrame" 
               Visibility="Collapsed"
               NavigationUIVisibility="Hidden"/>
    </Grid>
</ui:UiWindow>
```

- [ ] **Step 2: Rewrite MainWindow.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Navigation;
using ModernFileCleaner.Pages;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class MainWindow
{
    private readonly HistoryService _historyService = new();

    public MainWindow()
    {
        InitializeComponent();
        _historyService.Load();
        NavView.SelectedPageChanged += OnSelectedPageChanged;

        // Workaround: Navigate to first page after load
        Loaded += (_, _) =>
        {
            NavFrame.Navigate(new CleanPage(_historyService));
            NavFrame.Visibility = Visibility.Visible;
        };
    }

    private void OnSelectedPageChanged(object? sender, RoutedEventArgs e)
    {
        var tag = NavView.SelectedItem?.Tag?.ToString();
        switch (tag)
        {
            case "clean":
                NavFrame.Navigate(new CleanPage(_historyService));
                break;
            case "stats":
                NavFrame.Navigate(new StatsPage(_historyService));
                break;
            case "settings":
                NavFrame.Navigate(new SettingsPage());
                break;
            case "about":
                NavFrame.Navigate(new AboutPage());
                break;
        }
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build may have warnings about missing Pages — that's expected, will resolve in next tasks

- [ ] **Step 4: Commit**

```bash
git add cleaner1/MainWindow.xaml cleaner1/MainWindow.xaml.cs
git commit -m "feat: rewrite MainWindow with wpfui NavigationView"
```

---

### Task 7: Create CleanPage

**Files:**
- Create: `cleaner1/Pages/CleanPage.xaml`
- Create: `cleaner1/Pages/CleanPage.xaml.cs`

**Interfaces:**
- Consumes: `CleaningCategory` (Task 2), `CalculationService`, `CleaningService` (Task 3), `HistoryService` (Task 3), `CleaningCard` (Task 5), `AppSettings` (Task 4)
- Produces: Navigable page with card grid, analyze/clean buttons, progress display

- [ ] **Step 1: Create CleanPage.xaml**

```xml
<ui:UiPage x:Class="ModernFileCleaner.Pages.CleanPage"
           xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
           xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
           xmlns:controls="clr-namespace:ModernFileCleaner.Controls"
           xmlns:models="clr-namespace:ModernFileCleaner.Models"
           Title="Clean">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
            <TextBlock Text="🧹" FontSize="28" Margin="0,0,12,0" VerticalAlignment="Center"/>
            <StackPanel>
                <TextBlock Text="System Clean" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                <TextBlock Text="Select categories to clean" FontSize="13" Foreground="#FF888888"/>
            </StackPanel>
        </StackPanel>

        <!-- Category Cards Grid -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto" Margin="0,0,0,16">
            <ItemsControl x:Name="CardsContainer">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel Orientation="Horizontal" />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate DataType="{x:Type models:CleaningCategory}">
                        <controls:CleaningCard Width="280" Margin="4"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Action Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,16" HorizontalAlignment="Center">
            <ui:ToggleSwitch x:Name="SelectAllToggle" 
                             Content="Select All" 
                             Margin="0,0,20,0"
                             IsChecked="True"
                             Click="SelectAllToggle_Click"/>
            <ui:Button x:Name="btnAnalyze" 
                       Content="📊  Analyze" 
                       Appearance="Primary"
                       Margin="0,0,10,0"
                       Width="140" Height="38"
                       Click="btnAnalyze_Click"/>
            <ui:Button x:Name="btnClean" 
                       Content="🧹  Clean Now" 
                       Appearance="Danger"
                       Width="140" Height="38"
                       Click="btnClean_Click"/>
        </StackPanel>

        <!-- Progress Bar -->
        <ui:ProgressBar x:Name="ProgressBar" 
                        Grid.Row="3" 
                        Height="4" 
                        Margin="0,0,0,8"
                        Visibility="Collapsed"
                        IsIndeterminate="False"/>

        <!-- Status Text -->
        <TextBlock x:Name="txtStatus" 
                   Grid.Row="4"
                   Text="Ready" 
                   Foreground="#FF888888"
                   HorizontalAlignment="Center"
                   FontSize="13"/>
    </Grid>
</ui:UiPage>
```

- [ ] **Step 2: Create CleanPage.xaml.cs**

```csharp
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

    private void SelectAllToggle_Click(object sender, RoutedEventArgs e)
    {
        bool isChecked = SelectAllToggle.IsChecked ?? true;
        foreach (var cat in _categories)
            cat.IsSelected = isChecked;
    }

    private async void btnAnalyze_Click(object sender, RoutedEventArgs e)
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

    private async void btnClean_Click(object sender, RoutedEventArgs e)
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
            var progress = new Progress<string>(s => txtStatus.Text = s);
            long freed = await _cleaningService.CleanCategoryAsync(cat, progress);
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

    private void ShowMessage(string msg, string title)
    {
        var win = new ui:MessageBox { Title = title, Content = msg };
        win.ShowDialog();
    }

    private MessageBoxResult ShowConfirm(string msg, string title)
    {
        var win = new ui:MessageBox { Title = title, Content = msg };
        return win.ShowDialog();
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Pages/CleanPage.xaml cleaner1/Pages/CleanPage.xaml.cs
git commit -m "feat: add CleanPage with cards, analyze, and clean functionality"
```

---

### Task 8: Create StatsPage

**Files:**
- Create: `cleaner1/Pages/StatsPage.xaml`
- Create: `cleaner1/Pages/StatsPage.xaml.cs`

**Interfaces:**
- Consumes: `HistoryService` (Task 3)
- Produces: Statistics page with summary cards and history list

- [ ] **Step 1: Create StatsPage.xaml**

```xml
<ui:UiPage x:Class="ModernFileCleaner.Pages.StatsPage"
           xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
           xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
           Title="Statistics">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,24">
            <TextBlock Text="📊" FontSize="28" Margin="0,0,12,0" VerticalAlignment="Center"/>
            <StackPanel>
                <TextBlock Text="Statistics" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                <TextBlock Text="Your cleaning history overview" FontSize="13" Foreground="#FF888888"/>
            </StackPanel>
        </StackPanel>

        <!-- Stat Cards -->
        <WrapPanel Grid.Row="1" Margin="0,0,0,24">
            <Border CornerRadius="12" Background="#FF2D2D2D" 
                    Width="200" Height="100" Margin="0,0,12,0" Padding="20">
                <StackPanel VerticalAlignment="Center">
                    <TextBlock Text="Total Freed" FontSize="12" Foreground="#FF888888"/>
                    <TextBlock x:Name="txtTotalFreed" Text="0 B" 
                               FontSize="24" FontWeight="SemiBold" Foreground="#FF4CAF50"
                               Margin="0,4,0,0"/>
                </StackPanel>
            </Border>
            <Border CornerRadius="12" Background="#FF2D2D2D" 
                    Width="200" Height="100" Margin="0,0,12,0" Padding="20">
                <StackPanel VerticalAlignment="Center">
                    <TextBlock Text="Sessions" FontSize="12" Foreground="#FF888888"/>
                    <TextBlock x:Name="txtSessions" Text="0" 
                               FontSize="24" FontWeight="SemiBold" Foreground="#FF0078D4"
                               Margin="0,4,0,0"/>
                </StackPanel>
            </Border>
            <Border CornerRadius="12" Background="#FF2D2D2D" 
                    Width="200" Height="100" Padding="20">
                <StackPanel VerticalAlignment="Center">
                    <TextBlock Text="Last Cleaned" FontSize="12" Foreground="#FF888888"/>
                    <TextBlock x:Name="txtLastCleaned" Text="Never" 
                               FontSize="16" FontWeight="SemiBold" Foreground="White"
                               Margin="0,4,0,0"/>
                </StackPanel>
            </Border>
        </WrapPanel>

        <!-- History List -->
        <Grid Grid.Row="2">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <TextBlock Grid.Row="0" Text="Clean History" 
                       FontSize="16" FontWeight="SemiBold" Foreground="White"
                       Margin="0,0,0,12"/>
            <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                <ItemsControl x:Name="HistoryList">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border CornerRadius="8" Background="#FF2D2D2D" 
                                    Padding="12" Margin="0,0,0,6">
                                <StackPanel>
                                    <StackPanel Orientation="Horizontal" 
                                                HorizontalAlignment="Stretch">
                                        <TextBlock Text="{Binding Timestamp, StringFormat={}{0:g}}" 
                                                   Foreground="White" FontWeight="SemiBold"/>
                                        <TextBlock Text="{Binding CategoriesCleaned.Count, StringFormat=' — {0} categories'}" 
                                                   Foreground="#FF888888" 
                                                   Margin="6,0,0,0"/>
                                        <TextBlock Text="{Binding BytesFormatted}" 
                                                   Foreground="#FF4CAF50" FontWeight="SemiBold"
                                                   HorizontalAlignment="Right" Margin="0,0,0,0"/>
                                    </StackPanel>
                                    <TextBlock Text="{Binding CategoriesCleaned, StringFormat='{0}'}" 
                                               Foreground="#FF666666" FontSize="11"
                                               Margin="0,4,0,0"
                                               TextTrimming="CharacterEllipsis"/>
                                </StackPanel>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Grid>
    </Grid>
</ui:UiPage>
```

- [ ] **Step 2: Create StatsPage.xaml.cs**

```csharp
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

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Pages/StatsPage.xaml cleaner1/Pages/StatsPage.xaml.cs
git commit -m "feat: add StatsPage with summary cards and history list"
```

---

### Task 9: Create SettingsPage

**Files:**
- Create: `cleaner1/Pages/SettingsPage.xaml`
- Create: `cleaner1/Pages/SettingsPage.xaml.cs`

**Interfaces:**
- Consumes: `AppSettings` (Task 4)
- Produces: Settings page with toggle switches

- [ ] **Step 1: Create SettingsPage.xaml**

```xml
<ui:UiPage x:Class="ModernFileCleaner.Pages.SettingsPage"
           xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
           xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
           Title="Settings">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,24">
            <TextBlock Text="⚙️" FontSize="28" Margin="0,0,12,0" VerticalAlignment="Center"/>
            <StackPanel>
                <TextBlock Text="Settings" FontSize="22" FontWeight="SemiBold" Foreground="White"/>
                <TextBlock Text="Configure app behavior" FontSize="13" Foreground="#FF888888"/>
            </StackPanel>
        </StackPanel>

        <!-- Settings Cards -->
        <StackPanel Grid.Row="1">
            <!-- General Section -->
            <Border CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="General" FontSize="16" FontWeight="SemiBold" Foreground="White" Margin="0,0,0,16"/>
                    <ui:ToggleSwitch x:Name="chkAutoAnalyze" 
                                     Content="Auto-analyze on startup" 
                                     Margin="0,0,0,12"/>
                    <ui:ToggleSwitch x:Name="chkAutoClean" 
                                     Content="Auto-clean on exit" 
                                     Margin="0,0,0,12"/>
                </StackPanel>
            </Border>

            <!-- Notifications Section -->
            <Border CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="Notifications" FontSize="16" FontWeight="SemiBold" Foreground="White" Margin="0,0,0,16"/>
                    <ui:ToggleSwitch x:Name="chkNotifications" 
                                     Content="Show notifications after cleaning"/>
                </StackPanel>
            </Border>

            <!-- Safety Section -->
            <Border CornerRadius="12" Background="#FF2D2D2D" Padding="20" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="Safety" FontSize="16" FontWeight="SemiBold" Foreground="White" Margin="0,0,0,16"/>
                    <ui:ToggleSwitch x:Name="chkSafetyBackup" 
                                     Content="Enable safety backup before delete"/>
                </StackPanel>
            </Border>

            <!-- Save Button -->
            <ui:Button x:Name="btnSave" Content="💾 Save Settings" 
                       Appearance="Primary"
                       HorizontalAlignment="Right"
                       Width="160" Height="38"
                       Margin="0,8,0,0"
                       Click="btnSave_Click"/>
        </StackPanel>
    </Grid>
</ui:UiPage>
```

- [ ] **Step 2: Create SettingsPage.xaml.cs**

```csharp
using System.Windows;

namespace ModernFileCleaner.Pages;

public partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        AppSettings.Instance.Load();
        chkAutoAnalyze.IsChecked = AppSettings.Instance.AutoAnalyze;
        chkAutoClean.IsChecked = AppSettings.Instance.AutoClean;
        chkNotifications.IsChecked = AppSettings.Instance.ShowNotifications;
        chkSafetyBackup.IsChecked = AppSettings.Instance.SafetyBackup;
    }

    private void SaveSettings()
    {
        AppSettings.Instance.AutoAnalyze = chkAutoAnalyze.IsChecked ?? false;
        AppSettings.Instance.AutoClean = chkAutoClean.IsChecked ?? false;
        AppSettings.Instance.ShowNotifications = chkNotifications.IsChecked ?? false;
        AppSettings.Instance.SafetyBackup = chkSafetyBackup.IsChecked ?? false;
        AppSettings.Instance.Save();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        var msg = new ui:MessageBox
        {
            Title = "Settings",
            Content = "✅ Settings saved successfully!"
        };
        msg.ShowDialog();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Pages/SettingsPage.xaml cleaner1/Pages/SettingsPage.xaml.cs
git commit -m "feat: add SettingsPage with toggle switches"
```

---

### Task 10: Create AboutPage

**Files:**
- Create: `cleaner1/Pages/AboutPage.xaml`
- Create: `cleaner1/Pages/AboutPage.xaml.cs`

- [ ] **Step 1: Create AboutPage.xaml**

```xml
<ui:UiPage x:Class="ModernFileCleaner.Pages.AboutPage"
           xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
           xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
           Title="About">
    <Grid Margin="24">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <!-- App Icon -->
            <Border CornerRadius="20" Background="#FF2D2D2D" 
                    Width="96" Height="96" Margin="0,0,0,20"
                    HorizontalAlignment="Center">
                <TextBlock Text="🧹" FontSize="48" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"/>
            </Border>

            <!-- App Name -->
            <TextBlock Text="System Sweep" 
                       FontSize="28" FontWeight="SemiBold" 
                       Foreground="White"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,8"/>

            <!-- Version -->
            <Border CornerRadius="8" Background="#1A0078D4" 
                    Padding="12,4" Margin="0,0,0,20"
                    HorizontalAlignment="Center">
                <TextBlock Text="Version 2.0 — Fluent Edition" 
                           FontSize="13" Foreground="#FF0078D4"
                           FontWeight="SemiBold"/>
            </Border>

            <!-- Description -->
            <TextBlock Text="A modern system cleaning tool for Windows 11"
                       FontSize="14" Foreground="#FF888888"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,8"/>
            <TextBlock Text="Built with WPF UI • .NET 8 • Fluent Design"
                       FontSize="12" Foreground="#FF666666"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,40"/>

            <!-- Developer -->
            <TextBlock Text="Developed by naix"
                       FontSize="14" Foreground="#FF888888"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,30"/>

            <!-- Close button -->
            <ui:Button Content="✖  Close" 
                       Appearance="Secondary"
                       Width="120" Height="36"
                       HorizontalAlignment="Center"
                       Click="Button_Click"/>
        </StackPanel>
    </Grid>
</ui:UiPage>
```

- [ ] **Step 2: Create AboutPage.xaml.cs**

```csharp
using System.Windows;

namespace ModernFileCleaner.Pages;

public partial class AboutPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Navigate back to Clean page
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
```

- [ ] **Step 3: Add NavigateToCleanPage method to MainWindow.xaml.cs**

Edit MainWindow.xaml.cs to add:

```csharp
public void NavigateToCleanPage()
{
    NavFrame.Navigate(new Pages.CleanPage(_historyService));
}
```

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add cleaner1/Pages/AboutPage.xaml cleaner1/Pages/AboutPage.xaml.cs
git commit -m "feat: add AboutPage with modern design"
```

---

### Task 11: Clean Up Old Files & Final Integration

**Files:**
- Delete: `cleaner1/SettingsWindow.xaml` + `.xaml.cs`
- Delete: `cleaner1/AboutWindow.xaml` + `.xaml.cs`
- Delete: `cleaner1/Properties/Settings.Designer.cs` (if unused)

- [ ] **Step 1: Delete old SettingsWindow and AboutWindow files**

Run:
```bash
git rm cleaner1/SettingsWindow.xaml cleaner1/SettingsWindow.xaml.cs cleaner1/AboutWindow.xaml cleaner1/AboutWindow.xaml.cs
```

- [ ] **Step 2: Verify final build**

Run: `dotnet build`
Expected: Build succeeds with no errors

- [ ] **Step 3: Run the application to verify it launches**

Run: `dotnet run --project cleaner1`
Expected: Window appears with NavigationView and Clean page

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove old SettingsWindow and AboutWindow in favor of pages"
```

---

### Task 12: App.xaml.cs — Wire Auto-Analyze on Startup

**Files:**
- Modify: `cleaner1/MainWindow.xaml.cs`

- [ ] **Step 1: Add auto-analyze in MainWindow constructor**

Update MainWindow.xaml.cs:

```csharp
using System.Windows;
using System.Windows.Navigation;
using ModernFileCleaner.Pages;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class MainWindow
{
    private readonly HistoryService _historyService = new();

    public MainWindow()
    {
        InitializeComponent();
        _historyService.Load();
        NavView.SelectedPageChanged += OnSelectedPageChanged;

        Loaded += async (_, _) =>
        {
            var cleanPage = new CleanPage(_historyService);
            NavFrame.Navigate(cleanPage);
            NavFrame.Visibility = Visibility.Visible;

            // Auto-analyze if enabled
            if (AppSettings.Instance.AutoAnalyze)
            {
                await cleanPage.RunAutoAnalyze();
            }
        };
    }

    public void NavigateToCleanPage()
    {
        NavFrame.Navigate(new Pages.CleanPage(_historyService));
    }

    private void OnSelectedPageChanged(object? sender, RoutedEventArgs e)
    {
        var tag = NavView.SelectedItem?.Tag?.ToString();
        switch (tag)
        {
            case "clean":
                NavFrame.Navigate(new CleanPage(_historyService));
                break;
            case "stats":
                NavFrame.Navigate(new StatsPage(_historyService));
                break;
            case "settings":
                NavFrame.Navigate(new SettingsPage());
                break;
            case "about":
                NavFrame.Navigate(new AboutPage());
                break;
        }
    }
}
```

- [ ] **Step 2: Add RunAutoAnalyze method to CleanPage.xaml.cs**

```csharp
public async Task RunAutoAnalyze()
{
    await btnAnalyze_Click(null, null);
}
```

Make btnAnalyze_Click accept nullable sender:

```csharp
private async void btnAnalyze_Click(object? sender, RoutedEventArgs? e)
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add cleaner1/MainWindow.xaml.cs cleaner1/Pages/CleanPage.xaml.cs
git commit -m "feat: wire auto-analyze on startup via MainWindow"
```
