<div align="center">
  <img src="https://img.shields.io/badge/version-2.0.0-blue?style=for-the-badge" alt="Version"/>
  <img src="https://img.shields.io/badge/Windows-10%20|%2011-00adef?style=for-the-badge&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET"/>
  <img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="License"/>
  <br/>
  <img src="https://img.shields.io/github/downloads/naix1337/SystemSweep/v2.0.0/total?style=social" alt="Downloads"/>
  <img src="https://img.shields.io/github/stars/naix1337/SystemSweep?style=social" alt="Stars"/>
</div>

<br/>

<h1 align="center">🧹 System Sweep Professional</h1>
<p align="center">
  <b>The Ultimate Windows Optimization Toolkit</b><br/>
  Clean · Tweak · Monitor · Supercharge
</p>

<p align="center">
  <i>System Sweep is a professional Windows system cleaning and optimization tool.<br/>
  It cleans temporary files, manages startup programs, finds duplicates, applies 30+ performance tweaks for gaming, and monitors system health — all in a modern Fluent Design interface.</i>
</p>

---

## ✨ Features

### 📊 Dashboard
| Feature | Description |
|---------|-------------|
| Live System Monitor | Real-time CPU, RAM, Disk, and Uptime tracking |
| Health Score | Weighted algorithm (Disk 40% + RAM 30% + CPU 30%) |
| Quick Actions | One-click Quick Clean, Empty Recycle Bin, Refresh |

### 🧹 System Cleaner
| Category | Safety | Description |
|----------|--------|-------------|
| 🗑️ Temporary Files | ✅ Safe | Windows & user temp files |
| ♻️ Recycle Bin | ✅ Safe | Deleted files in Recycle Bin |
| 📥 Download Cache | ✅ Safe | Downloads folder contents |
| 🖼️ Thumbnail Cache | ✅ Safe | Explorer thumbnail cache |
| ⚠️ Error Reports | ⚠️ Caution | WER report files |
| 📦 Installer Temp | ⚠️ Caution | MSI installer leftovers |
| 🏪 Store Cache | ✅ Safe | Microsoft Store cache |
| 📋 Windows Logs | ⚠️ Caution | System log files |
| 🪟 Windows.old | 🔴 Dangerous | Previous Windows installation |
| 💾 Memory Dumps | 🔴 Dangerous | Crash dump files |

### ⚡ Performance Tweaks (30+)

<details>
<summary>🎮 Gaming & FPS (9 tweaks)</summary>

| Tweak | Impact | Recommended |
|-------|--------|:-----------:|
| Hardware-Accelerated GPU Scheduling | Reduces input lag, improves FPS | ✅ |
| Enable Game Mode | Prioritizes game processes | ✅ |
| Disable Xbox Game Bar & DVR | Recovers 5-15% FPS | ✅ |
| Disable HPET Timer | Reduces input lag | |
| Disable CPU Core Parking | Keeps all cores active for max perf | ✅ |
| Disable Mouse Acceleration | Raw aiming for FPS games | |
| Disable USB Selective Suspend | Prevents controller disconnects | |
| GPU Maximum Performance Mode | Forces max GPU clock | ✅ |
| Enable Gaming Focus Assist | Auto-disable notifications | ✅ |
</details>

<details>
<summary>⚡ System Boost (12 tweaks)</summary>

| Tweak | Impact | Recommended |
|-------|--------|:-----------:|
| High Performance Power Plan | Max CPU speed always | ✅ |
| Disable All Visual Effects | Snappiest UI possible | ✅ |
| Disable SysMain (Superfetch) | Less disk/CPU on SSD | ✅ |
| Disable Windows Search Indexing | Less disk usage | |
| Disable Background Apps | Frees RAM & CPU | ✅ |
| Disable Startup Delay | Faster boot | ✅ |
| Disable Notification Tray | Cleaner systray | |
| Disable Windows Error Reporting | Less background CPU | |
| Disable Transparency Effects | Frees GPU resources | ✅ |
| Disable Auto Disk Defrag | Not needed on SSDs | ✅ |
| Disable Tips & Suggestions | No ads in Windows | |
| Disable Cortana | Frees 200-500MB RAM | |
</details>

<details>
<summary>💾 Disk & Memory (7 tweaks)</summary>

| Tweak | Impact | Recommended |
|-------|--------|:-----------:|
| Disable Hibernation | Frees RAM-sized disk space | ✅ |
| Disable NTFS Last Access Time | Less disk writes | ✅ |
| Disable 8.3 Filename Creation | Faster NTFS | |
| Disable Large System Cache | Frees RAM | |
| Disable Memory Compression | Less CPU overhead (16GB+ RAM) | |
| Clear Page File on Shutdown | Security/privacy | |
| Disable Thumbnail Cache | Saves disk space | |
</details>

<details>
<summary>🌐 Network (8 tweaks)</summary>

| Tweak | Impact | Recommended |
|-------|--------|:-----------:|
| Flush DNS Cache | Fixes stale DNS | |
| Disable Nagle's Algorithm | Less network lag | ✅ |
| Enable TCP Auto-Tuning | Faster downloads | ✅ |
| Disable QoS Bandwidth Limit | Full network speed | ✅ |
| Disable IPv6 | Less overhead (IPv4-only) | |
| Enable RSS (multi-core networking) | Better throughput | |
| Disable TCP Chimney Offload | Fixes game stutters | |
| Set Optimal MTU (1492) | Less fragmentation | |
</details>

<details>
<summary>🧹 Cleanup (4 tweaks)</summary>

| Tweak | Impact | Recommended |
|-------|--------|:-----------:|
| Clear Prefetch Files | Removes old traces | |
| Clear Windows Update Cache | Frees 2-10GB | ✅ |
| Clear Font Cache | Fixes font issues | |
| Clear Recent Files List | Privacy | |
</details>

### 🧩 Additional Tools
| Tool | Description |
|------|-------------|
| 🌐 Browser Cache Cleaner | Chrome, Edge, Firefox, Brave cache |
| 🗂️ Duplicate File Finder | SHA256-based duplicate detection |
| 🚀 Startup Manager | Registry + Folder autostart control |
| 📄 Cleaning Reports | Export history as HTML |

### 🖥️ UI & Experience
- **Fluent Design** with Mica backdrop
- **Dark / Light** theme toggle
- **Smooth page transitions** and hover effects
- **Real-time Dashboard** with system health monitoring
- **Cleaning history** with statistics

---

## 🚀 Installation

### Option 1: Download Release (Recommended)
```bash
# 1. Download from GitHub Releases
https://github.com/naix1337/SystemSweep/releases

# 2. Run SystemSweep.exe (as Administrator)
```

### Option 2: Build from Source
```bash
git clone https://github.com/naix1337/SystemSweep.git
cd SystemSweep
dotnet build cleaner1/cleaner1.csproj
dotnet run --project cleaner1/cleaner1.csproj
```

### Option 3: Publish Single-File
```powershell
.\publish.ps1
# Output: ./publish/SystemSweep.exe (7 MB)
```

> ⚠️ **Run as Administrator** for full functionality (Windows.old, Memory Dumps, Performance Tweaks)

---

## 🎮 Usage

```
┌──────────────────────────────────────────────────────────┐
│ 🧹 System Sweep                                         │
│                                                          │
│ 📊 Dashboard    → Live system health + quick actions     │
│ 🧹 Clean        → Category cards + presets              │
│ 🌐 Browser Cache→ Chrome/Edge/Firefox/Brave             │
│ 🗂️ Duplicates   → Find and remove duplicate files       │
│ 🚀 Startup      → Manage autostart programs             │
│ ⚡ Tweaks        → 30+ performance optimizations        │
│ 📈 Statistics   → History + HTML reports                │
│ ⚙️ Settings      → App configuration                    │
│ ℹ️ About        → Version, license, updates             │
│ 🔐 License      → Activation & trial status             │
└──────────────────────────────────────────────────────────┘
```

### Command Line
```bash
SystemSweep.exe --silent --clean   # Clean without UI
SystemSweep.exe --analyze           # Analysis only
```

---

## 🔑 License System

System Sweep uses RSA 2048-bit signed license keys:

```
Format:  Base64( RSA_Signature | MachineFP | User | Expiry )
```

### Generate License Keys
```bash
cd tools/KeyGenerator
dotnet run -- gen-keys                    # Generate key pair
dotnet run -- create <FP> <User> <Date>   # Single license
dotnet run -- batch licenses.csv           # Bulk generation
```

---

## 🏗️ Architecture

```
cleaner1/
├── Pages/              # UI pages (8 pages)
│   ├── DashboardPage   # System health monitor
│   ├── CleanPage       # File cleaning
│   ├── TweaksPage      # Performance tweaks
│   ├── BrowserCachePage
│   ├── DuplicatesPage
│   ├── StartupPage
│   ├── SettingsPage
│   └── AboutPage       # Version, updates, license
├── Services/           # Business logic
│   ├── CleaningService # File cleaning operations
│   ├── CalculationService
│   ├── HistoryService  # Cleaning history persistence
│   ├── SystemMonitorService
│   ├── TweaksService   # 30+ registry/power tweaks
│   ├── BrowserCacheService
│   ├── DuplicateFinderService
│   ├── StartupService
│   ├── ReportService   # HTML export
│   ├── ThemeService    # Dark/Light switching
│   ├── UpdateService   # Auto-update check
│   └── LicenseService  # RSA-based activation
├── Models/             # Data models
├── Controls/           # Reusable UI controls
└── Styles/             # Custom styles & themes

tools/
└── KeyGenerator/       # License key generation tool
```

---

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| **.NET 8.0** | Runtime framework |
| **WPF** | Desktop UI framework |
| **WPF-UI 3.x** | Fluent Design controls |
| **RSA 2048-bit** | License key signing |
| **DPAPI** | Secure local storage |
| **System.Management** | Hardware monitoring |
| **PerformanceCounter** | CPU/RAM tracking |

---

## 🤝 Contributing

Contributions welcome! Open an issue or PR for:

- 🐛 Bug fixes
- ✨ New features
- ⚡ Additional tweaks
- 🌐 More browser support

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

<div align="center">
  <p>Made with ❤️ by <a href="https://github.com/naix1337">naix</a></p>
  <p>
    <a href="https://github.com/naix1337/SystemSweep/issues">Report Bug</a> ·
    <a href="https://github.com/naix1337/SystemSweep/discussions">Feature Request</a> ·
    <a href="https://github.com/naix1337/SystemSweep/releases">Download</a>
  </p>
</div>
