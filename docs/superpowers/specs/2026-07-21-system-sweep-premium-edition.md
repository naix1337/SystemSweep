# System Sweep – Premium Edition (UI Overhaul + Dashboard + Power Tools)

**Date:** 2026-07-21
**Status:** Approved Design

## Overview
Three sub-projects to evolve System Sweep from a clean WPF-UI app into a premium system utility: UI/UX polish with Acrylic + animations, a live system Dashboard, and advanced Power Tools.

---

## Project 1: UI/UX Overhaul

### Acrylic Backdrop
- Change `WindowBackdropType` from `Mica` to `Acrylic`
- Acrylic works on both Windows 10 and 11 (Mica is Win11-only)

### Dark/Light Theme Toggle
- Add toggle button in NavigationView pane footer
- Switch applies instantly via `ui:ThemesDictionary Theme="Dark"|"Light"`
- Store preference in `AppSettings.Theme` (string: "Dark"|"Light")

### Segoe Fluent Icons
- Replace all emoji icons with Fluent UI icon glyphs
- Create `Services/IconService.cs` with mapping: `GetIcon(string id) → string glyph`
- Use font `Segoe Fluent Icons`, `Segoe MDL2 Assets`, or built-in wpfui `Icon` property
- wpfui `SymbolRegular` icons available directly via `Icon="Wrench24"` etc.

### Page Transitions
- Custom `SlideTransition` using `DoubleAnimation` on `Frame.Opacity` + `TranslateTransform`
- Use `Frame.NavigationService.Navigating` event to trigger animations
- Timing: 200ms with `QuadraticEase` easing

### Card Hover Effects
- Add `Style.Triggers` for `IsMouseOver` on CleaningCard
- Scale up 1.02x, background brighten (#FF2D2D2D → #FF353535)
- Smooth transition via `ColorAnimation` / `ScaleTransform`

### Smooth ProgressBar
- Replace indeterminate mode with continuous sliding animation
- Use custom style with repeating `DoubleAnimation` on the track

### Modern ScrollViewer
- Custom ScrollBar style: thin (6px), dark, rounded (4px)
- Auto-hide when not scrolling
- Smooth scrolling via `ScrollViewer.PanningMode="Both"`

---

## Project 2: Dashboard

### DashboardPage (new Start Page)
- Replaces CleanPage as the first page shown on app launch
- Added to NavigationView as first item: "Dashboard" icon "GridDots24"
- Contains: System cards, Health Score, Disk chart, Quick Actions

### System Info Cards (4 Cards)
**DataModel:** `Models/SystemStats.cs`
```csharp
public class SystemStats : INotifyPropertyChanged
{
    public float CpuUsage { get; set; }      // %
    public float RamUsage { get; set; }      // %
    public long RamTotal { get; set; }       // bytes
    public long RamAvailable { get; set; }   // bytes
    public long DiskFree { get; set; }       // bytes
    public long DiskTotal { get; set; }      // bytes
    public TimeSpan Uptime { get; set; }
    public int HealthScore { get; set; }     // 0-100
}
```

**Service:** `Services/SystemMonitorService.cs`
- Uses `PerformanceCounter` for CPU ("Processor", "% Processor Time", "_Total")  
- Uses `PerformanceCounter` for RAM ("Memory", "Available MBytes")
- Uses `DriveInfo.GetDrives()` for disk
- Uses `Environment.TickCount64` for uptime
- Timer-based refresh every 5 seconds when page is visible
- Implements `IDisposable` for PerformanceCounter cleanup

### Health Score
- Weighted formula: 40% Disk + 30% RAM + 30% CPU
- Disk: >20% free = 100pts, <5% free = 0pts (linear)
- RAM: <70% used = 100pts, >95% used = 0pts
- CPU: <50% = 100pts, >95% = 0pts
- Score displayed with color: green (≥80), amber (≥50), red (<50)

### Disk Chart (Simple Line Chart)
- 7 data points stored in `settings.json` under `DiskHistory: List<long>`
- One snapshot per day at first dashboard load
- Drawn using WPF `Polyline` + `Path` geometry
- Y-axis: bytes (auto-scaled), X-axis: last 7 days
- Area fill under line using `LinearGradientBrush`

### Quick Actions
- **Quick Clean:** Runs Temp Files + Recycle Bin + Thumbnail Cache (safe categories)
- **Empty Recycle Bin:** Single-action bin empty
- **Refresh Stats:** Forces immediate system stats refresh

### Navigation Update
- NavigationView order: Dashboard, Clean, Statistics, Settings, About
- DashboardPage has constructor injection for `HistoryService` and `SystemMonitorService`

---

## Project 3: Power Tools

### Browser Cache Cleaner (BrowserCachePage)
**Service:** `Services/BrowserCacheService.cs`

| Browser | Path | Strategy |
|---|---|---|
| Chrome | `%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache` | Delete Cache/Code Cache |
| Firefox | `%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*.default\cache2` | Delete entries |
| Edge | `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache` | Delete Cache/Code Cache |
| Brave | `%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\Default\Cache` | Delete Cache |

- Calculate cache size per browser
- Clean with file deletion + optional browser restart warning
- Safety level: Safe (browsers recreate cache automatically)

### Duplicate File Finder (DuplicatesPage)
**Service:** `Services/DuplicateFinderService.cs`
- Scan directory tree (user picks root: Downloads, Desktop, Documents, Custom)
- Group by file name + size first (fast pre-filter)
- Hash candidates with SHA256 for exact match
- UI: List with checkboxes, grouped by duplicate set
- "Select All Duplicates" / "Keep Newest" / "Keep in Folder" actions
- Preview before delete
- Progress bar with current file being hashed

### Startup Manager (StartupPage)
**Service:** `Services/StartupService.cs`
- Read from: `Registry.CurrentUser\Software\Microsoft\Windows\CurrentVersion\Run`
- Read from: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`
- Display: Name, Command, Enabled/Disabled, Source (Registry/Folder)
- Toggle enable/disable via registry rename + `.disabled` suffix for folder items
- Safety level: Safe (no deletion, only disable)

### Cleaning Presets
**Model:** `Models/CleaningPreset.cs`
- Quick Clean: Temp + Recycle Bin + Thumbnail Cache
- Standard Clean: Quick + Download Cache + Error Reports + Logs
- Deep Clean: Standard + Store Cache + Windows.old + Memory Dumps (requires admin)
- Custom: User selection (current behavior)
- UI: Dropdown/Radio buttons at top of CleanPage

### Cleaning Reports (export)
**Service:** `Services/ReportService.cs`
- Export last cleaning session as HTML or CSV
- HTML: Beautiful styled report with header, date, categories, bytes freed
- CSV: Machine-readable with timestamp, category, bytes
- Saved to `Documents/SystemSweep Reports/` folder
- "Open Report" button in StatsPage

---

## Architecture Changes

### New/Modified Files Summary

| Project | New Files | Modified Files |
|---|---|---|
| UI | `Services/ThemeService.cs`, `Services/IconService.cs`, `Styles/ModernScrollBar.xaml`, `Styles/Animations.xaml` | `MainWindow.xaml`, `App.xaml`, `AppSettings.cs`, `Controls/CleaningCard.xaml` |
| Dashboard | `Pages/DashboardPage.xaml/.cs`, `Models/SystemStats.cs`, `Services/SystemMonitorService.cs` | `MainWindow.xaml.cs` (nav order), `AppSettings.cs` (DiskHistory) |
| Power Tools | `Pages/BrowserCachePage.xaml/.cs`, `Pages/DuplicatesPage.xaml/.cs`, `Pages/StartupPage.xaml/.cs`, `Services/BrowserCacheService.cs`, `Services/DuplicateFinderService.cs`, `Services/StartupService.cs`, `Services/ReportService.cs`, `Models/CleaningPreset.cs` | `MainWindow.xaml.cs` (nav items), `Pages/CleanPage.xaml/.cs` (presets), `Pages/StatsPage.xaml/.cs` (reports), `AppSettings.cs` |

### NavigationView (Updated)
1. 📊 Dashboard (new)
2. 🧹 Clean (moved from 1st)
3. 🧩 Browser Cache (new)
4. 🗂️ Duplicates (new)
5. 🚀 Startup (new)
6. 📈 Statistics
7. ⚙️ Settings
8. ℹ️ About

---

## Error Handling
- SystemMonitorService: Silent failure on missing PerformanceCounter (different Windows versions), show "—" in UI
- DuplicateFinderService: Cancellation support via CancellationToken (large scans)
- BrowserCacheService: Graceful handling of running browsers (locked files)
- StartupService: Registry read-only operations never write without user action
- All new services follow existing pattern: `catch { Debug.WriteLine(...) }`

## Out of Scope
- Real-time file system watcher for duplicate finder
- Cloud storage cache cleaning (Dropbox, OneDrive, Google Drive)
- Driver update / registry cleaning
- System restore point creation (requires COM elevation)
