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
