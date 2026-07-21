using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModernFileCleaner.Models;

public enum SafetyLevel
{
    Safe,
    Caution,
    Dangerous
}

public class CleaningCategory : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private long _sizeInBytes;
    private bool _isSelected = true;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public long SizeInBytes
    {
        get => _sizeInBytes;
        set
        {
            if (_sizeInBytes != value)
            {
                _sizeInBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeFormatted));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public SafetyLevel Safety { get; set; } = SafetyLevel.Safe;
    public string Description { get; set; } = string.Empty;

    public string SizeFormatted
    {
        get
        {
            if (_sizeInBytes < 1024) return $"{_sizeInBytes} B";
            if (_sizeInBytes < 1024 * 1024) return $"{_sizeInBytes / 1024.0:F1} KB";
            if (_sizeInBytes < 1024 * 1024 * 1024) return $"{_sizeInBytes / (1024.0 * 1024.0):F1} MB";
            return $"{_sizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    public string SafetyName => Safety.ToString();

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
