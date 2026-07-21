using System.Diagnostics;
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
        // === Gaming ===
        new() { Id = "gpu_scheduling", Name = "Hardware-Accelerated GPU Scheduling", Description = "Reduces latency and improves FPS by letting GPU manage its memory directly", Category = "Gaming", Icon = "🎮", IsRecommended = true },
        new() { Id = "game_mode", Name = "Game Mode", Description = "Prioritizes游戏 performance by background process management", Category = "Gaming", Icon = "🎮", IsRecommended = true },
        new() { Id = "xbox_dvr", Name = "Disable Xbox Game Bar/DVR", Description = "Disables background recording that can cost 5-15% FPS in games", Category = "Gaming", Icon = "🎮", IsRecommended = true, WarningMessage = "Disables Xbox Game Bar recording" },
        new() { Id = "fullscreen_opt", Name = "Disable Fullscreen Optimizations", Description = "Prevents Windows from overriding your games' fullscreen mode", Category = "Gaming", Icon = "🎮" },

        // === System ===
        new() { Id = "power_high", Name = "High Performance Power Plan", Description = "Ensures CPU runs at max speed instead of throttling down", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "visual_effects", Name = "Disable Visual Effects", Description = "Turn off animations, shadows, and transparency for snappier UI", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "sysmain", Name = "Disable SysMain (Superfetch)", Description = "Can reduce background disk activity on SSDs", Category = "System", Icon = "⚡", WarningMessage = "May increase app load times on HDDs" },
        new() { Id = "indexing", Name = "Disable Windows Search Indexing", Description = "Disables background file indexing to reduce disk usage", Category = "System", Icon = "⚡", WarningMessage = "Windows search will be slower" },
        new() { Id = "background_apps", Name = "Disable Background Apps", Description = "Prevents apps from running in background and using resources", Category = "System", Icon = "⚡", IsRecommended = true },
        new() { Id = "cortana", Name = "Disable Cortana", Description = "Disables Cortana assistant to free up RAM and CPU", Category = "System", Icon = "⚡" },

        // === Network ===
        new() { Id = "dns_flush", Name = "Flush DNS Cache", Description = "Clears outdated DNS entries for faster internet", Category = "Network", Icon = "🌐" },
        new() { Id = "nagle", Name = "Disable Nagle's Algorithm", Description = "Reduces network latency for real-time applications (TCP)", Category = "Network", Icon = "🌐" },
        new() { Id = "tcp_autotuning", Name = "Optimize TCP Auto-Tuning", Description = "Improves network throughput for downloads and streaming", Category = "Network", Icon = "🌐" },
    };

    public async Task ApplyTweakAsync(TweakItem tweak, IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            progress?.Report($"Applying {tweak.Name}...");
            switch (tweak.Id)
            {
                case "gpu_scheduling":
                    SetRegistry(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                    break;
                case "game_mode":
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
                    SetRegistry(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
                    break;
                case "xbox_dvr":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameBar", 0);
                    break;
                case "fullscreen_opt":
                    // Per-application setting would need to be set per-game
                    // This sets the system-level behavior
                    break;
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
                    SetRegistry(@"Software\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
                    break;
                case "dns_flush":
                    RunPowerShell("ipconfig /flushdns");
                    break;
                case "nagle":
                    DisableNagle();
                    break;
                case "tcp_autotuning":
                    RunPowerShell("netsh int tcp set global autotuninglevel=normal");
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
                    SetRegistry(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 0);
                    break;
                case "game_mode":
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameMode", 0);
                    SetRegistry(@"Software\Microsoft\GameBar", "AutoGameModeEnabled", 0);
                    break;
                case "xbox_dvr":
                    SetRegistry(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
                    SetRegistry(@"Software\Microsoft\GameBar", "AllowAutoGameBar", 1);
                    break;
                case "power_high":
                    RunPowerShell("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2f"); // Balanced
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
                    SetRegistry(@"Software\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1);
                    break;
                case "nagle":
                    RunPowerShell("netsh int tcp set global autotuninglevel=normal");
                    break;
                case "tcp_autotuning":
                    RunPowerShell("netsh int tcp set global autotuninglevel=normal");
                    break;
            }
            tweak.IsEnabled = false;
        });
    }

    private static void SetRegistry(string path, string name, object value)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path, true) ?? Registry.CurrentUser.CreateSubKey(path);
            key?.SetValue(name, value);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] Registry: {ex.Message}"); }
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
            process?.WaitForExit(10000);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] PowerShell: {ex.Message}"); }
    }

    private static void SetSystemPerformanceToMaximum()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            }
            // Set VisualFX setting to "Adjust for best performance" (value 2)
            key.SetValue("VisualFXSetting", 2);

            // Disable specific visual effects
            var perfKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
            perfKey?.SetValue("TaskbarAnimations", 0);
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] VisualEffects: {ex.Message}"); }
    }

    private static void SetSystemPerformanceToDefault()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            key?.SetValue("VisualFXSetting", 0); // Let Windows choose
        }
        catch (Exception ex) { Debug.WriteLine($"[Tweaks] VisualEffects: {ex.Message}"); }
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
