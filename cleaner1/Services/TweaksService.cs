using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ModernFileCleaner.Services;

public class TweakItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool IsEnabled { get; set; }
    public bool IsRecommended { get; set; }
    public string? WarningMessage { get; set; }
}

public class TweaksService
{
    public List<TweakItem> GetAllTweaks() => new()
    {
        // ============== GAMING & FPS ==============
        new() { Id = "gpu_scheduling", Name = "Hardware-Accelerated GPU Scheduling", Description = "Reduces input lag and improves FPS by letting GPU manage its own memory", Category = "Gaming", Icon = "🎮", IsRecommended = true },
        new() { Id = "game_mode", Name = "Enable Game Mode", Description = "Prioritizes game performance by limiting background processes", Category = "Gaming", Icon = "🎮", IsRecommended = true },
        new() { Id = "xbox_dvr", Name = "Disable Xbox Game Bar & DVR", Description = "Stops background recording saving 5-15% FPS in games", Category = "Gaming", Icon = "🎮", IsRecommended = true, WarningMessage = "Disables Xbox overlay and recording" },
        new() { Id = "disable_hpet", Name = "Disable HPET Timer", Description = "Reduces input lag in games by using faster system timers", Category = "Gaming", Icon = "🎮", WarningMessage = "Requires admin + reboot" },
        new() { Id = "core_parking", Name = "Disable CPU Core Parking", Description = "Keeps all CPU cores active for max gaming performance", Category = "Gaming", Icon = "🎮", IsRecommended = true, WarningMessage = "Slightly increases power usage" },
        new() { Id = "mouse_accel", Name = "Disable Mouse Acceleration", Description = "Removes pointer smoothing for raw, precise aiming in FPS games", Category = "Gaming", Icon = "🎮" },
        new() { Id = "usb_suspend", Name = "Disable USB Selective Suspend", Description = "Prevents game controllers and USB devices from disconnecting", Category = "Gaming", Icon = "🎮" },
        new() { Id = "gpu_maxperf", Name = "GPU Maximum Performance Mode", Description = "Forces GPU to run at max clock speeds instead of power-saving", Category = "Gaming", Icon = "🎮", IsRecommended = true, WarningMessage = "Higher power consumption" },
        new() { Id = "focus_assist", Name = "Enable Gaming Focus Assist", Description = "Auto-disable notifications during games for zero distractions", Category = "Gaming", Icon = "🎮", IsRecommended = true },

        // ============== SYSTEM BOOST ==============
        new() { Id = "power_high", Name = "High Performance Power Plan", Description = "Forces CPU to run at max speed, no throttling", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "visual_effects", Name = "Disable All Visual Effects", Description = "Turns off animations, shadows, transparency for instant UI feel", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "sysmain", Name = "Disable SysMain (Superfetch)", Description = "Reduces background disk/CPU activity on SSDs", Category = "System", Icon = "⚡", IsRecommended = true, WarningMessage = "HDD users may see slower app loading" },
        new() { Id = "indexing", Name = "Disable Windows Search Indexing", Description = "Stops background file indexing eating disk and CPU", Category = "System", Icon = "⚡", WarningMessage = "Windows search becomes slower" },
        new() { Id = "background_apps", Name = "Disable All Background Apps", Description = "Stops Store apps from running in background and using resources", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "cortana", Name = "Disable Cortana", Description = "Disables Cortana assistant to free 200-500MB RAM", Category = "System", Icon = "⚡" },
        new() { Id = "startup_delay", Name = "Disable Startup Delay", Description = "Removes the 10s boot delay Windows adds to startup processes", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "notification_tray", Name = "Disable Notification Tray", Description = "Stops background apps from cluttering system tray", Category = "System", Icon = "⚡" },
        new() { Id = "error_reporting", Name = "Disable Windows Error Reporting", Description = "Prevents background error reporting that uses CPU and disk", Category = "System", Icon = "⚡" },
        new() { Id = "transparency", Name = "Disable Transparency Effects", Description = "Disables acrylic/blur effects freeing GPU resources", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "disk_defrag_schedule", Name = "Disable Auto Disk Defrag", Description = "Disables scheduled defragmentation on SSDs (not needed)", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "tips_suggestions", Name = "Disable Tips & Suggestions", Description = "Stops Windows from showing tips and ads in the OS", Category = "System", Icon = "⚡" },

        // ============== DISK & MEMORY ==============
        new() { Id = "hibernation", Name = "Disable Hibernation", Description = "Frees disk space equal to RAM size (e.g. 16GB+) and reduces disk writes", Category = "Disk", Icon = "💾", IsRecommended = true, WarningMessage = "Fast Startup will be disabled" },
        new() { Id = "ntfs_last_access", Name = "Disable NTFS Last Access Time", Description = "Reduces disk writes by stopping NTFS from updating file timestamps", Category = "Disk", Icon = "💾", IsRecommended = true },
        new() { Id = "ntfs_83names", Name = "Disable 8.3 Filename Creation", Description = "Speeds up NTFS by not generating legacy short filenames", Category = "Disk", Icon = "💾" },
        new() { Id = "large_cache", Name = "Disable Large System Cache", Description = "Frees RAM by not caching large file operations unnecessarily", Category = "Disk", Icon = "💾" },
        new() { Id = "memory_compression", Name = "Disable Memory Compression", Description = "Disables RAM compression to reduce CPU overhead (more RAM = snappier)", Category = "Disk", Icon = "💾", WarningMessage = "Only on systems with 16GB+ RAM" },
        new() { Id = "pagefile_cleanup", Name = "Clear Page File on Shutdown", Description = "Reduces page file size and clears it on each shutdown", Category = "Disk", Icon = "💾" },
        new() { Id = "thumbnail_cache_off", Name = "Disable Thumbnail Cache", Description = "Prevents Windows from caching thumbnails saving disk space", Category = "Disk", Icon = "💾" },

        // ============== NETWORK ==============
        new() { Id = "dns_flush", Name = "Flush DNS Cache", Description = "Clears outdated DNS entries for faster domain resolution", Category = "Network", Icon = "🌐" },
        new() { Id = "nagle", Name = "Disable Nagle's Algorithm", Description = "Reduces network lag in games and real-time apps by disabling TCP buffering", Category = "Network", Icon = "🌐", IsRecommended = true },
        new() { Id = "tcp_autotuning", Name = "Enable TCP Auto-Tuning", Description = "Optimizes receive window for faster downloads and streaming", Category = "Network", Icon = "🌐", IsRecommended = true },
        new() { Id = "qos_limit", Name = "Disable QoS Bandwidth Limit", Description = "Removes the 20% reserved bandwidth limit for full network speed", Category = "Network", Icon = "🌐", IsRecommended = true },
        new() { Id = "ipv6", Name = "Disable IPv6", Description = "Disables IPv6 to reduce network overhead (use if only IPv4 needed)", Category = "Network", Icon = "🌐", WarningMessage = "IPv6 will be unavailable" },
        new() { Id = "rss", Name = "Enable RSS (Receive Side Scaling)", Description = "Distributes network processing across multiple CPU cores", Category = "Network", Icon = "🌐" },
        new() { Id = "tcp_chimney", Name = "Disable TCP Chimney Offload", Description = "Fixes network stutters in some games by disabling hardware offloading", Category = "Network", Icon = "🌐" },
        new() { Id = "mtu_optimize", Name = "Set Optimal MTU (1492)", Description = "Sets MTU to optimal size for reduced packet fragmentation", Category = "Network", Icon = "🌐" },

        // ============== ADVANCED ==============
        new() { Id = "disable_windows_update", Name = "Disable Automatic Windows Updates", Description = "Stops Windows from automatically downloading and installing updates", Category = "Advanced", Icon = "🛡️", WarningMessage = "System won't receive security updates automatically" },
        new() { Id = "disable_telemetry", Name = "Disable Windows Telemetry", Description = "Blocks Microsoft from collecting usage data and diagnostics", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "disable_copilot", Name = "Disable CoPilot AI (Win11)", Description = "Removes Microsoft CoPilot AI from taskbar and Edge", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "disable_office_telemetry", Name = "Disable Office Telemetry", Description = "Stops Microsoft Office from sending usage data (Office 2016+)", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "disable_onedrive", Name = "Disable OneDrive", Description = "Stops OneDrive from auto-starting and syncing in background", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "enable_utc_time", Name = "Enable UTC Time Globally", Description = "Fixes dual-boot time conflicts between Windows and Linux", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "hosts_block_tracking", Name = "Block Tracking via HOSTS File", Description = "Adds known tracking/malware domains to HOSTS file", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "fix_registry_issues", Name = "Fix Common Registry Issues", Description = "Repairs broken file associations, COM errors, and shell issues", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "disable_uwp_apps", Name = "Uninstall UWP Bloatware Apps", Description = "Removes pre-installed Windows Store apps (Xbox, Bing, etc.)", Category = "Advanced", Icon = "🛡️", WarningMessage = "Removes built-in Windows apps" },
        new() { Id = "disable_unused_services", Name = "Disable Unnecessary Services", Description = "Stops 15+ background services: Xbox, Print, Bluetooth, Fax, etc.", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "dns_cloudflare", Name = "Set DNS to Cloudflare (1.1.1.1)", Description = "Changes network DNS to Cloudflare for faster, more private browsing", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "disable_error_sound", Name = "Disable Windows Error Sound", Description = "Stops the annoying error beep sound in Windows", Category = "Advanced", Icon = "🔊" },
        new() { Id = "enable_long_paths", Name = "Enable Long File Paths", Description = "Removes the 260-character path limit in Windows", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "disable_sticky_keys", Name = "Disable Sticky Keys Prompt", Description = "Stops the 'Press Shift 5 times' accessibility popup", Category = "Advanced", Icon = "🔊" },
        new() { Id = "disable_auto_restart", Name = "Disable Auto-Restart on Update", Description = "Prevents Windows from forcibly restarting after updates", Category = "Advanced", Icon = "🛡️", IsRecommended = true },
        new() { Id = "disable_timedate_sync", Name = "Disable Time Sync with Internet", Description = "Stops Windows from automatically syncing system time", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "disable_biometric", Name = "Disable Windows Hello/Biometrics", Description = "Disables fingerprint, face, and PIN login requirements", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "disable_maps", Name = "Disable Windows Maps Service", Description = "Stops the background maps service (saves data and battery)", Category = "Advanced", Icon = "🛡️" },
        new() { Id = "disable_xbox_services", Name = "Disable Xbox Services", Description = "Disables all Xbox-related background services", Category = "Advanced", Icon = "🎮", IsRecommended = true },
        new() { Id = "disable_autorun", Name = "Disable AutoRun for USB Drives", Description = "Prevents automatic execution of programs from USB drives", Category = "Advanced", Icon = "🛡️", IsRecommended = true },

        // ============== CLEANUP ==============
        new() { Id = "prefetch_clean", Name = "Clear Prefetch Files", Description = "Deletes Windows prefetch files that accumulate over time", Category = "Cleanup", Icon = "🧹" },
        new() { Id = "update_cache", Name = "Clear Windows Update Cache", Description = "Deletes old Windows Update installation files (can free 2-10GB)", Category = "Cleanup", Icon = "🧹", IsRecommended = true },
        new() { Id = "font_cache", Name = "Clear Font Cache", Description = "Deletes Windows font cache to fix font issues and free space", Category = "Cleanup", Icon = "🧹" },
        new() { Id = "recent_files", Name = "Clear Recent Files List", Description = "Clears the jump list and recent files history", Category = "Cleanup", Icon = "🧹" },
    };

    public async Task ApplyTweakAsync(TweakItem tweak, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            progress?.Report($"Applying {tweak.Name}...");
            switch (tweak.Id)
            {
                // == Gaming ==
                case "gpu_scheduling":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                    break;
                case "game_mode":
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
                    SetRegistry(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
                    break;
                case "xbox_dvr":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameBar", 0);
                    break;
                case "disable_hpet":
                    RunPowerShell("bcdedit /deletevalue useplatformclock");
                    RunPowerShell("bcdedit /set tscsyncpolicy Enhanced");
                    break;
                case "core_parking":
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100");
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMAXCORES 100");
                    break;
                case "mouse_accel":
                    SetRegistry(@"Control Panel\Mouse", "MouseSpeed", "0");
                    SetRegistry(@"Control Panel\Mouse", "MouseThreshold1", "0");
                    SetRegistry(@"Control Panel\Mouse", "MouseThreshold2", "0");
                    break;
                case "usb_suspend":
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_USB USBSELECTIVESUSPEND 0");
                    break;
                case "gpu_maxperf":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DmCacheSize", 0);
                    break;
                case "focus_assist":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_TOASTS_ENABLED", 0);
                    break;

                // == System ==
                case "power_high":
                    RunPowerShell("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                    break;
                case "visual_effects":
                    SetSystemPerformanceToMaximum();
                    break;
                case "sysmain":
                    RunPowerShell("Stop-Service SysMain -Force; Set-Service SysMain -StartupType Disabled");
                    break;
                case "indexing":
                    RunPowerShell("Stop-Service WSearch -Force; Set-Service WSearch -StartupType Disabled");
                    break;
                case "background_apps":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
                    break;
                case "cortana":
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name AllowCortana -Value 0 -Type DWord -Force");
                    break;
                case "startup_delay":
                    RunPowerShell("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize' -Name StartupDelayInMSec -Value 0 -Type DWord -Force");
                    break;
                case "notification_tray":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", 0);
                    break;
                case "error_reporting":
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting' -Name Disabled -Value 1 -Type DWord -Force");
                    break;
                case "transparency":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
                    break;
                case "disk_defrag_schedule":
                    RunPowerShell("Disable-MMAgent -AutomaticDefragmentation");
                    break;
                case "tips_suggestions":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0);
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0);
                    break;

                // == Disk & Memory ==
                case "hibernation":
                    RunPowerShell("powercfg /hibernate off");
                    break;
                case "ntfs_last_access":
                    RunPowerShell("fsutil behavior set disablelastaccess 1");
                    break;
                case "ntfs_83names":
                    RunPowerShell("fsutil behavior set disable8dot3 1");
                    break;
                case "large_cache":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
                    break;
                case "memory_compression":
                    RunPowerShell("Disable-MMAgent -MemoryCompression");
                    break;
                case "pagefile_cleanup":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "ClearPageFileAtShutdown", 1);
                    break;
                case "thumbnail_cache_off":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisableThumbnailCache", 1);
                    break;

                // == Network ==
                case "dns_flush":
                    RunPowerShell("ipconfig /flushdns");
                    break;
                case "nagle":
                    DisableNagle();
                    break;
                case "tcp_autotuning":
                    RunPowerShell("netsh int tcp set global autotuninglevel=normal");
                    break;
                case "qos_limit":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit", 0);
                    break;
                case "ipv6":
                    RunPowerShell("Get-NetAdapterBinding -ComponentID ms_tcpip6 | Disable-NetAdapterBinding -ComponentID ms_tcpip6");
                    break;
                case "rss":
                    RunPowerShell("netsh int tcp set global rss=enabled");
                    break;
                case "tcp_chimney":
                    RunPowerShell("netsh int tcp set global chimney=disabled");
                    break;
                case "mtu_optimize":
                    RunPowerShell("netsh interface ipv4 set subinterface \"Ethernet\" mtu=1492 store=persistent");
                    RunPowerShell("netsh interface ipv4 set subinterface \"Wi-Fi\" mtu=1492 store=persistent");
                    break;

                // == Cleanup ==
                // == Advanced ==
                case "disable_windows_update":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
                    RunPowerShell("Stop-Service wuauserv -Force; Set-Service wuauserv -StartupType Disabled");
                    break;
                case "disable_telemetry":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
                    SetRegistryLocal(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0);
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata' -Name PreventDeviceMetadataFromNetwork -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue");
                    break;
                case "disable_copilot":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0);
                    break;
                case "disable_office_telemetry":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\office\16.0\common", "sendtelemetry", 0);
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\office\16.0\common", "updatereliabilitydata", 0);
                    break;
                case "disable_onedrive":
                    RunPowerShell("Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue");
                    RunPowerShell("Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\OneDrive' -Name DisableFileSyncNGSC -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue");
                    break;
                case "enable_utc_time":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 1);
                    break;
                case "hosts_block_tracking":
                    BlockHostsTracking();
                    break;
                case "fix_registry_issues":
                    RunPowerShell("Get-ChildItem 'HKLM:\\SOFTWARE\\Classes\\CLSID' -ErrorAction SilentlyContinue | Where-Object { (Get-ItemProperty -Path $_.PsPath -Name '(default)' -ErrorAction SilentlyContinue).'(default)' -eq '' } | Remove-Item -Force -ErrorAction SilentlyContinue");
                    break;
                case "disable_uwp_apps":
                    RunPowerShell("Get-AppxPackage -AllUsers | Where-Object { $_.Name -match 'xbox|bing|news|sports|zune|officehub|people|skype|solitaire|mixedreality|WindowsCamera' } | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue");
                    break;
                case "disable_unused_services":
                    foreach (var svc in new[] { "XblAuthManager","XboxNetApiSvc","XboxGipSvc","XboxGip","PrintNotify","Spooler","Fax","MapsBroker","lfsvc","wisvc","WMPNetworkSvc","WSearch","stisvc","SharedRealitySvc","PcaSvc","WpnService","PushNotifications" })
                        RunPowerShell($"Stop-Service {svc} -Force -ErrorAction SilentlyContinue; Set-Service {svc} -StartupType Disabled -ErrorAction SilentlyContinue");
                    break;
                case "dns_cloudflare":
                    RunPowerShell("Set-DnsClientServerAddress -InterfaceIndex (Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}).InterfaceIndex -ServerAddresses ('1.1.1.1','1.0.0.1')");
                    break;
                case "disable_error_sound":
                    SetRegistry(@"AppEvents\Schemes\Apps\.Default\.Default\.Default", "(Default)", "");
                    break;
                case "enable_long_paths":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1);
                    break;
                case "disable_sticky_keys":
                    SetRegistry(@"Control Panel\Accessibility\StickyKeys", "Flags", "506");
                    break;
                case "disable_auto_restart":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1);
                    break;
                case "disable_timedate_sync":
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\W32Time\\TimeProviders\\NtpClient' -Name Enabled -Value 0 -Type DWord -Force");
                    break;
                case "disable_biometric":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 0);
                    break;
                case "disable_maps":
                    RunPowerShell("Stop-Service MapsBroker -Force -ErrorAction SilentlyContinue; Set-Service MapsBroker -StartupType Disabled -ErrorAction SilentlyContinue");
                    break;
                case "disable_xbox_services":
                    foreach (var svc in new[] { "XblAuthManager","XboxNetApiSvc","XboxGipSvc","XboxGip" })
                        RunPowerShell($"Stop-Service {svc} -Force; Set-Service {svc} -StartupType Disabled");
                    break;
                case "disable_autorun":
                    SetRegistryLocal(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 255);
                    break;
                case "prefetch_clean":
                    CleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\Prefetch");
                    break;
                case "update_cache":
                    RunPowerShell("Stop-Service wuauserv -Force");
                    CleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\SoftwareDistribution\\Download");
                    RunPowerShell("Start-Service wuauserv");
                    break;
                case "font_cache":
                    RunPowerShell("Stop-Service FontCache -Force");
                    CleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\FontCache");
                    break;
                case "recent_files":
                    CleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
                    break;
            }
            tweak.IsEnabled = true;
        });
    }

    public async Task RevertTweakAsync(TweakItem tweak, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            progress?.Report($"Reverting {tweak.Name}...");
            switch (tweak.Id)
            {
                case "gpu_scheduling":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 0);
                    break;
                case "game_mode":
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameMode", 0);
                    SetRegistry(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", 0);
                    break;
                case "xbox_dvr":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameBar", 1);
                    break;
                case "disable_hpet":
                    RunPowerShell("bcdedit /set useplatformclock Yes");
                    break;
                case "core_parking":
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 0");
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMAXCORES 0");
                    break;
                case "mouse_accel":
                    SetRegistry(@"Control Panel\Mouse", "MouseSpeed", "1");
                    break;
                case "usb_suspend":
                    RunPowerShell("powercfg -setacvalueindex SCHEME_CURRENT SUB_USB USBSELECTIVESUSPEND 1");
                    break;
                case "gpu_maxperf":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DmCacheSize", 256);
                    break;
                case "focus_assist":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_TOASTS_ENABLED", 1);
                    break;
                case "power_high":
                    RunPowerShell("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2f");
                    break;
                case "visual_effects":
                    SetSystemPerformanceToDefault();
                    break;
                case "sysmain":
                    RunPowerShell("Set-Service SysMain -StartupType Automatic; Start-Service SysMain");
                    break;
                case "indexing":
                    RunPowerShell("Set-Service WSearch -StartupType Automatic; Start-Service WSearch");
                    break;
                case "background_apps":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 0);
                    break;
                case "cortana":
                    RunPowerShell("Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name AllowCortana -Force -ErrorAction SilentlyContinue");
                    break;
                case "startup_delay":
                    RunPowerShell("Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize' -Name StartupDelayInMSec -Force -ErrorAction SilentlyContinue");
                    break;
                case "notification_tray":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", 1);
                    break;
                case "error_reporting":
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting' -Name Disabled -Value 0 -Type DWord -Force");
                    break;
                case "transparency":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);
                    break;
                case "disk_defrag_schedule":
                    RunPowerShell("Enable-MMAgent -AutomaticDefragmentation");
                    break;
                case "tips_suggestions":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 1);
                    break;
                case "hibernation":
                    RunPowerShell("powercfg /hibernate on");
                    break;
                case "ntfs_last_access":
                    RunPowerShell("fsutil behavior set disablelastaccess 0");
                    break;
                case "ntfs_83names":
                    RunPowerShell("fsutil behavior set disable8dot3 0");
                    break;
                case "large_cache":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
                    break;
                case "memory_compression":
                    RunPowerShell("Enable-MMAgent -MemoryCompression");
                    break;
                case "pagefile_cleanup":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "ClearPageFileAtShutdown", 0);
                    break;
                case "thumbnail_cache_off":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisableThumbnailCache", 0);
                    break;
                case "qos_limit":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit", 80);
                    break;
                case "ipv6":
                    RunPowerShell("Get-NetAdapterBinding -ComponentID ms_tcpip6 | Enable-NetAdapterBinding -ComponentID ms_tcpip6");
                    break;
                case "mtu_optimize":
                    RunPowerShell("netsh interface ipv4 set subinterface \"Ethernet\" mtu=1500 store=persistent");
                    break;
                // == Advanced Reverts ==
                case "disable_windows_update":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0);
                    RunPowerShell("Set-Service wuauserv -StartupType Automatic; Start-Service wuauserv");
                    break;
                case "disable_telemetry":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1);
                    SetRegistryLocal(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 1);
                    break;
                case "disable_copilot":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 0);
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 1);
                    break;
                case "disable_office_telemetry":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\office\16.0\common", "sendtelemetry", 1);
                    break;
                case "disable_onedrive":
                    RunPowerShell("Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\OneDrive' -Name DisableFileSyncNGSC -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue");
                    break;
                case "enable_utc_time":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 0);
                    break;
                case "hosts_block_tracking":
                    UnblockHostsTracking();
                    break;
                case "disable_unused_services":
                    foreach (var svc in new[] { "Spooler","WSearch","WpnService" })
                        RunPowerShell($"Set-Service {svc} -StartupType Automatic; Start-Service {svc} -ErrorAction SilentlyContinue");
                    break;
                case "dns_cloudflare":
                    RunPowerShell("Set-DnsClientServerAddress -InterfaceIndex (Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}).InterfaceIndex -ResetServerAddresses");
                    break;
                case "enable_long_paths":
                    SetRegistryLocal(@"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 0);
                    break;
                case "disable_auto_restart":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 0);
                    break;
                case "disable_timedate_sync":
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\W32Time\\TimeProviders\\NtpClient' -Name Enabled -Value 1 -Type DWord -Force");
                    break;
                case "disable_biometric":
                    SetRegistryLocal(@"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 1);
                    break;
                case "disable_xbox_services":
                    foreach (var svc in new[] { "XblAuthManager","XboxNetApiSvc" })
                        RunPowerShell($"Set-Service {svc} -StartupType Manual; Start-Service {svc} -ErrorAction SilentlyContinue");
                    break;
                case "disable_autorun":
                    SetRegistryLocal(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 145);
                    break;
                case "prefetch_clean":
                    break; // No revert for cleanup operations
                case "update_cache":
                    break;
                case "font_cache":
                    break;
                case "recent_files":
                    break;
            }
            tweak.IsEnabled = false;
        });
    }

    private static void CleanDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                    try { File.Delete(file); } catch { }
                foreach (var dir in Directory.GetDirectories(path))
                    try { Directory.Delete(dir, true); } catch { }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] CleanDir: {ex.Message}"); }
    }

    private static void SetRegistry(string path, string name, object value)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path, true) ?? Registry.CurrentUser.CreateSubKey(path);
            key?.SetValue(name, value);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] Reg: {ex.Message}"); }
    }

    private static void SetRegistryLocal(string path, string name, object value)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path, true) ?? Registry.LocalMachine.CreateSubKey(path);
            key?.SetValue(name, value);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] RegLM: {ex.Message}"); }
    }

    private static void RunPowerShell(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell", $"-Command \"{command}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(15000);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] PS: {ex.Message}"); }
    }

    private static void SetSystemPerformanceToMaximum()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true)
                      ?? Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            key.SetValue("VisualFXSetting", 2);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] VFX: {ex.Message}"); }
    }

    private static void SetSystemPerformanceToDefault()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            key?.SetValue("VisualFXSetting", 0);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] VFX: {ex.Message}"); }
    }

    private static void BlockHostsTracking()
    {
        try
        {
            var hostsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\System32\\drivers\\etc\\hosts";
            if (!File.Exists(hostsPath)) return;
            var hosts = File.ReadAllLines(hostsPath).ToList();
            var tracking = new[] {
                "0.0.0.0 doubleclick.net","0.0.0.0 googleadservices.com",
                "0.0.0.0 googlesyndication.com","0.0.0.0 facebook.com/tr",
                "0.0.0.0 ads.twitter.com","0.0.0.0 bat.bing.com",
                "0.0.0.0 pixel.quantserve.com","0.0.0.0 scorecardresearch.com"
            };
            bool changed = false;
            foreach (var entry in tracking)
            {
                if (!hosts.Any(l => l.Trim().Equals(entry, StringComparison.OrdinalIgnoreCase)))
                { hosts.Add(entry); changed = true; }
            }
            if (changed) File.WriteAllLines(hostsPath, hosts);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] HOSTS: {ex.Message}"); }
    }

    private static void UnblockHostsTracking()
    {
        try
        {
            var hostsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\System32\\drivers\\etc\\hosts";
            if (!File.Exists(hostsPath)) return;
            var hosts = File.ReadAllLines(hostsPath).Where(l =>
                !l.Contains("doubleclick") && !l.Contains("googlead") &&
                !l.Contains("googlesyndication") && !l.Contains("facebook.com/tr") &&
                !l.Contains("ads.twitter") && !l.Contains("bat.bing") &&
                !l.Contains("quantserve") && !l.Contains("scorecardresearch")).ToList();
            File.WriteAllLines(hostsPath, hosts);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] HOSTS: {ex.Message}"); }
    }

    private static void DisableNagle()
    {
        try
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var interfaceKey = key.OpenSubKey(subKeyName, true);
                    interfaceKey?.SetValue("TcpAckFrequency", 1);
                    interfaceKey?.SetValue("TCPNoDelay", 1);
                }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] Nagle: {ex.Message}"); }
    }
}
