<div align="center">
  <img src="https://img.shields.io/github/v/release/naix1337/SystemSweep?style=for-the-badge&color=blue" alt="Version"/>
  <img src="https://img.shields.io/badge/Windows-10%20|%2011-00adef?style=for-the-badge&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET"/>
  <img src="https://github.com/naix1337/SystemSweep/actions/workflows/ci.yml/badge.svg?style=for-the-badge" alt="CI"/>
  <br/>
  <img src="https://img.shields.io/github/downloads/naix1337/SystemSweep/total?style=social" alt="Downloads"/>
  <img src="https://img.shields.io/github/stars/naix1337/SystemSweep?style=social" alt="Stars"/>
</div>

<br/>

<h1 align="center">System Sweep</h1>
<p align="center">
  <b>The Ultimate Windows Optimization Toolkit</b><br/>
  Clean · Tweak · Monitor · Supercharge
</p>

<p align="center">
  System Sweep is a Windows system cleaning and optimization tool built with WPF and Fluent Design.<br/>
  It cleans temporary files, manages startup programs, finds duplicates, applies 30+ performance tweaks for gaming, and monitors system health in real time.
</p>

<div align="center">

<!-- SCREENSHOT: Dashboard mit Live-CPU/RAM/Disk-Monitor hier einfügen -->
<img src="docs/screenshot-dashboard.png" alt="Dashboard Screenshot" width="800"/>

<!-- Optional: GIF, das Clean-Workflow zeigt -->
<img src="docs/demo.gif" alt="Demo" width="800"/>

</div>

---

## Inhaltsverzeichnis

- [Features](#features)
- [Installation](#installation)
- [Nutzung](#nutzung)
- [Architektur](#architektur)
- [Tech Stack](#tech-stack)
- [Contributing](#contributing)
- [License](#license)

---

## Features

### Dashboard
| Feature | Beschreibung |
|---------|-------------|
| Live System Monitor | Echtzeit CPU-, RAM-, Disk- und Uptime-Tracking |
| Health Score | Gewichteter Algorithmus (Disk 40% + RAM 30% + CPU 30%) |
| Quick Actions | One-Click Quick Clean, Papierkorb leeren, Refresh |

### System Cleaner
| Kategorie | Sicherheit | Beschreibung |
|----------|--------|-------------|
| Temporäre Dateien | ✅ Sicher | Windows- & User-Temp-Dateien |
| Papierkorb | ✅ Sicher | Gelöschte Dateien im Papierkorb |
| Download-Cache | ✅ Sicher | Inhalte des Downloads-Ordners |
| Thumbnail-Cache | ✅ Sicher | Explorer-Thumbnail-Cache |
| Fehlerberichte | ⚠️ Vorsicht | WER-Reportdateien |
| Installer-Temp | ⚠️ Vorsicht | MSI-Installer-Reste |
| Store-Cache | ✅ Sicher | Microsoft Store Cache |
| Windows-Logs | ⚠️ Vorsicht | Systemlogdateien |
| Windows.old | 🔴 Gefährlich | Vorherige Windows-Installation |
| Memory Dumps | 🔴 Gefährlich | Crash-Dump-Dateien |

### Performance Tweaks (30+)

<details>
<summary>Gaming & FPS (9 Tweaks)</summary>

| Tweak | Wirkung | Empfohlen |
|-------|--------|:-----------:|
| Hardware-Accelerated GPU Scheduling | Weniger Input-Lag, mehr FPS | ✅ |
| Game Mode aktivieren | Priorisiert Game-Prozesse | ✅ |
| Xbox Game Bar & DVR deaktivieren | 5-15% mehr FPS | ✅ |
| HPET-Timer deaktivieren | Weniger Input-Lag | |
| CPU Core Parking deaktivieren | Alle Kerne aktiv für max. Performance | ✅ |
| Mausbeschleunigung deaktivieren | Raw Aiming für FPS-Spiele | |
| USB Selective Suspend deaktivieren | Verhindert Controller-Disconnects | |
| GPU Maximum Performance Mode | Erzwingt max. GPU-Takt | ✅ |
| Gaming Focus Assist aktivieren | Auto-Deaktivierung von Benachrichtigungen | ✅ |
</details>

<details>
<summary>System Boost (12 Tweaks)</summary>

| Tweak | Wirkung | Empfohlen |
|-------|--------|:-----------:|
| High Performance Power Plan | Max. CPU-Speed dauerhaft | ✅ |
| Visuelle Effekte deaktivieren | Schnellste UI möglich | ✅ |
| SysMain (Superfetch) deaktivieren | Weniger Disk/CPU auf SSD | ✅ |
| Windows Search Indexing deaktivieren | Weniger Disk-Nutzung | |
| Hintergrund-Apps deaktivieren | Gibt RAM & CPU frei | ✅ |
| Startup-Delay deaktivieren | Schnellerer Boot | ✅ |
| Notification Tray deaktivieren | Aufgeräumter Systray | |
| Windows Error Reporting deaktivieren | Weniger Hintergrund-CPU | |
| Transparenz-Effekte deaktivieren | Gibt GPU-Ressourcen frei | ✅ |
| Auto Disk Defrag deaktivieren | Nicht nötig auf SSDs | ✅ |
| Tips & Suggestions deaktivieren | Keine Werbung in Windows | |
| Cortana deaktivieren | Gibt 200-500MB RAM frei | |
</details>

<details>
<summary>Disk & Memory (7 Tweaks)</summary>

| Tweak | Wirkung | Empfohlen |
|-------|--------|:-----------:|
| Ruhezustand deaktivieren | Gibt RAM-großen Speicherplatz frei | ✅ |
| NTFS Last Access Time deaktivieren | Weniger Disk-Writes | ✅ |
| 8.3 Filename Creation deaktivieren | Schnelleres NTFS | |
| Large System Cache deaktivieren | Gibt RAM frei | |
| Memory Compression deaktivieren | Weniger CPU-Overhead (16GB+ RAM) | |
| Page File beim Shutdown leeren | Security/Privacy | |
| Thumbnail-Cache deaktivieren | Spart Speicherplatz | |
</details>

<details>
<summary>Netzwerk (8 Tweaks)</summary>

| Tweak | Wirkung | Empfohlen |
|-------|--------|:-----------:|
| DNS-Cache leeren | Behebt veraltete DNS-Einträge | |
| Nagle's Algorithm deaktivieren | Weniger Netzwerk-Lag | ✅ |
| TCP Auto-Tuning aktivieren | Schnellere Downloads | ✅ |
| QoS-Bandbreitenlimit deaktivieren | Volle Netzwerkgeschwindigkeit | ✅ |
| IPv6 deaktivieren | Weniger Overhead (IPv4-only) | |
| RSS (Multi-Core-Networking) aktivieren | Besserer Durchsatz | |
| TCP Chimney Offload deaktivieren | Behebt Game-Stutters | |
| Optimales MTU (1492) setzen | Weniger Fragmentierung | |
</details>

<details>
<summary>Cleanup (4 Tweaks)</summary>

| Tweak | Wirkung | Empfohlen |
|-------|--------|:-----------:|
| Prefetch-Dateien löschen | Entfernt alte Spuren | |
| Windows-Update-Cache leeren | Gibt 2-10GB frei | ✅ |
| Font-Cache leeren | Behebt Font-Probleme | |
| Recent-Files-Liste löschen | Privacy | |
</details>

### Zusätzliche Tools
| Tool | Beschreibung |
|------|-------------|
| Browser Cache Cleaner | Chrome, Edge, Firefox, Brave Cache |
| Duplicate File Finder | SHA256-basierte Duplikaterkennung |
| Startup Manager | Registry + Ordner-Autostart-Kontrolle |
| Cleaning Reports | Export der Historie als HTML |

### UI & Experience
- **Fluent Design** mit Mica-Backdrop
- **Dark / Light** Theme-Umschaltung
- **Flüssige Seitenübergänge** und Hover-Effekte
- **Echtzeit-Dashboard** mit System-Health-Monitoring
- **Cleaning-Historie** mit Statistiken

---

## Installation

### Option 1: Release herunterladen (empfohlen)
```bash
# 1. Von GitHub Releases herunterladen
https://github.com/naix1337/SystemSweep/releases

# 2. SystemSweep.exe ausführen (als Administrator)
```

### Option 2: Aus Quellcode bauen
```bash
git clone https://github.com/naix1337/SystemSweep.git
cd SystemSweep
dotnet build cleaner1/cleaner1.csproj
dotnet run --project cleaner1/cleaner1.csproj
```

### Option 3: Single-File Publish
```powershell
.\publish.ps1
# Output: ./publish/SystemSweep.exe (7 MB)
```

> ⚠️ **Als Administrator ausführen** für volle Funktionalität (Windows.old, Memory Dumps, Performance Tweaks)

---

## Nutzung

```
┌──────────────────────────────────────────────────────────┐
│ System Sweep                                              │
│                                                          │
│ Dashboard    → Live System Health + Quick Actions     │
│ Clean        → Kategorie-Karten + Presets             │
│ Browser Cache→ Chrome/Edge/Firefox/Brave                 │
│ Duplicates   → Duplikate finden & entfernen              │
│ Startup      → Autostart-Programme verwalten             │
│ Tweaks       → 30+ Performance-Optimierungen             │
│ Statistics   → Historie + HTML-Reports                   │
│ Settings     → App-Konfiguration                         │
│ About        → Version, Updates, Lizenz                  │
└──────────────────────────────────────────────────────────┘
```

### Command Line
```bash
SystemSweep.exe --silent --clean   # Bereinigung ohne UI
SystemSweep.exe --analyze          # Nur Analyse
```

---

## Architektur

```
cleaner1/
├── Pages/              # UI-Seiten (8 Seiten)
│   ├── DashboardPage   # System-Health-Monitor
│   ├── CleanPage       # Dateibereinigung
│   ├── TweaksPage      # Performance-Tweaks
│   ├── BrowserCachePage
│   ├── DuplicatesPage
│   ├── StartupPage
│   ├── SettingsPage
│   └── AboutPage       # Version, Updates
├── Services/            # Business-Logik
│   ├── CleaningService
│   ├── CalculationService
│   ├── HistoryService
│   ├── SystemMonitorService
│   ├── TweaksService
│   ├── BrowserCacheService
│   ├── DuplicateFinderService
│   ├── StartupService
│   ├── ReportService
│   ├── ThemeService
│   └── UpdateService
├── Models/              # Datenmodelle
├── Controls/            # Wiederverwendbare UI-Controls
└── Styles/              # Custom Styles & Themes
```

---

## Tech Stack

| Technologie | Zweck |
|------------|---------|
| **.NET 8.0** | Runtime-Framework |
| **WPF** | Desktop-UI-Framework |
| **WPF-UI 3.x** | Fluent Design Controls |
| **System.Management** | Hardware-Monitoring |
| **PerformanceCounter** | CPU/RAM-Tracking |

---

## Contributing

Contributions willkommen! Öffne ein Issue oder PR für:

- Bugfixes
- Neue Features
- Zusätzliche Tweaks
- Mehr Browser-Support

---

## License

MIT License — siehe [LICENSE](LICENSE) für Details.

---

<div align="center">
  <p>Made by <a href="https://github.com/naix1337">naix</a></p>
  <p>
    <a href="https://github.com/naix1337/SystemSweep/issues">Report Bug</a> ·
    <a href="https://github.com/naix1337/SystemSweep/discussions">Feature Request</a> ·
    <a href="https://github.com/naix1337/SystemSweep/releases">Download</a>
  </p>
</div>
