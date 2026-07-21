using System.Diagnostics;
using System.IO;
using System.Management;
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
            stats.RamTotal = GetTotalRam();
            stats.RamUsage = stats.RamTotal > 0
                ? (float)Math.Round((stats.RamTotal - stats.RamAvailable) * 100.0f / stats.RamTotal, 1)
                : 0;
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

    private static long GetTotalRam()
    {
        try
        {
            using var mc = new ManagementClass("Win32_ComputerSystem");
            using var items = mc.GetInstances();
            foreach (var item in items)
                return Convert.ToInt64(item["TotalPhysicalMemory"]);
        }
        catch { }
        return 0;
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
