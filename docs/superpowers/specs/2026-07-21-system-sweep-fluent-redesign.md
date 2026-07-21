# System Sweep - Modern Fluent Redesign

**Date:** 2026-07-21
**Status:** Approved Design

## Overview

Complete redesign of the System Sweep WPF application using the WPF UI library (wpfui) to achieve a modern Windows 11 Fluent Design look with Mica/Acrylic effects, NavigationView sidebar, card-based layout, and improved functionality.

## Architecture

### Technology Stack
- **Runtime:** .NET 8.0 WPF
- **UI Library:** [wpfui](https://github.com/lepoco/wpfui) v3.x
- **Json:** Newtonsoft.Json (existing, for settings)

### Project Structure
```
cleaner1/
├── MainWindow.xaml/.cs        → UiWindow with NavigationView
├── App.xaml/.cs               → Application entry (unchanged)
├── AppSettings.cs             → Existing settings class (extended)
├── Pages/
│   ├── CleanPage.xaml/.cs     → Main cleaning page with cards
│   ├── StatsPage.xaml/.cs     → Statistics & history dashboard
│   ├── SettingsPage.xaml/.cs  → Settings with modern toggles
│   └── AboutPage.xaml/.cs     → About dialog content
├── Services/
│   ├── CleaningService.cs     → Core cleaning logic (refactored from MainWindow)
│   ├── CalculationService.cs  → Size calculation logic
│   └── HistoryService.cs      → Clean history logging
├── Models/
│   ├── CleaningCategory.cs    → Data model for a cleanable item
│   └── CleanHistoryEntry.cs   → Data model for history log
└── Controls/
    └── CleaningCard.xaml/.cs  → Reusable card control for a category
```

## UI Design

### Main Window (UiWindow)
- **Style:** UiWindow with windowExt style (Mica backdrop)
- **Layout:** NavigationView as primary navigation element
- **Title Bar:** Hidden default title bar, custom header in NavigationView

### Navigation Structure
| Icon | Label | Page | Description |
|---|---|---|---|
| 🧹 | Clean | CleanPage | Main cleaning interface |
| 📊 | Statistics | StatsPage | Clean history & saved space |
| ⚙️ | Settings | SettingsPage | App configuration |
| ℹ️ | About | AboutPage | Version & credits |

### CleanPage (Main Interface)
- **Header:** "System Sweep" with icon
- **Card Grid:** 2-column WrapPanel of CleaningCard items
  - Each card shows: Icon, Category Name, Size (after analysis), Selected checkbox, Warning badge (for dangerous items)
  - Cards are color-coded: green (safe), amber (caution), red (dangerous)
- **Action Bar:** "Select All" toggle, "Analyze" button (accent), "Clean Now" button (danger/attention)
- **Progress Section:** Smooth progress bar with current status text ("Cleaning temporary files...", "245 MB freed")

### StatsPage
- **Summary Cards:** Total cleaned, last cleaned date, sessions count
- **Timeline:** Recent clean operations list
- **Chart Placeholder:** Simple bar chart showing cleaned space per session (using WPF shapes)

### SettingsPage
- **Modern Toggles:** Fluent-style toggle switches for each setting
- **Sections:**
  - General: Auto-analyze on startup, Auto-clean on exit
  - Notifications: Show notifications after clean
  - Safety: Enable backup before delete (new)

### AboutPage
- App icon + name
- Version number
- Developer info
- Modern-styled close button

## Data Models

### CleaningCategory
```csharp
public class CleaningCategory
{
    public string Id { get; set; }          // e.g. "temp_files"
    public string Name { get; set; }        // Display name
    public string Icon { get; set; }        // Icon glyph / emoji
    public long SizeInBytes { get; set; }   // Calculated size
    public bool IsSelected { get; set; }    // User toggle
    public SafetyLevel Safety { get; set; } // Safe / Caution / Dangerous
    public CleaningAction Action { get; set; } // Delegate to execute
}
```

### CleanHistoryEntry
```csharp
public class CleanHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public long BytesFreed { get; set; }
    public List<string> CategoriesCleaned { get; set; }
}
```

## Functional Improvements

1. **Per-category size display** - Each card shows its individual size after analysis (currently only total is shown)
2. **Select/Deselect All** - Toggle all categories at once
3. **Cleaning History** - Persistent log of clean operations stored in `history.json`
4. **Safety Levels** - Categories flagged as Caution or Dangerous get visual warning + confirmation
5. **Better Progress** - Shows what is currently being cleaned with real per-item updates
6. **Size Formatting** - Automatic KB/MB/GB formatting instead of always MB
7. **Risky Category Confirmation** - Dangerous items (Windows.old, Memory Dumps) require double-confirmation

## Error Handling
- All cleaning operations wrapped in try/catch with per-category granularity (not all-or-nothing)
- Admin elevation check per dangerous category
- If a category fails, others continue and the error is reported per-card
- History is always saved even on partial clean

## Non-Goals (Out of Scope for v1)
- Actual animated charts / graphs (use simple shape-based indicators)
- Scheduled / automated background cleaning
- Real-time file system monitoring
- Multi-language / localization support
- Portable / standalone mode
