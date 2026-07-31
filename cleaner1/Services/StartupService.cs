using System.IO;
using Microsoft.Win32;

namespace ModernFileCleaner.Services;

public class StartupItem : System.ComponentModel.INotifyPropertyChanged
{
    private bool _enabled;
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Source { get; set; } = "Registry";

    public bool Enabled
    {
        get => _enabled;
        set { if (_enabled != value) { _enabled = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Enabled))); } }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public class StartupService
{
    public List<StartupItem> GetItems()
    {
        var items = new List<StartupItem>();

        // Registry (HKCU)
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (key != null)
        {
            foreach (var name in key.GetValueNames())
            {
                items.Add(new StartupItem
                {
                    Name = name,
                    Command = key.GetValue(name)?.ToString() ?? "",
                    Enabled = true,
                    Source = "Registry"
                });
            }
        }

        // Startup folder
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (Directory.Exists(startupFolder))
        {
            foreach (var file in Directory.GetFiles(startupFolder))
            {
                items.Add(new StartupItem
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = file,
                    Enabled = !file.EndsWith(".disabled"),
                    Source = "Startup Folder"
                });
            }
        }

        return items;
    }

    public void Toggle(StartupItem item)
    {
        if (item.Source == "Registry")
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                if (item.Enabled)
                    key.DeleteValue(item.Name, false);
                else
                    key.SetValue(item.Name, item.Command);
            }
        }
        else
        {
            var file = item.Command;
            if (item.Enabled && file.EndsWith(".disabled"))
                File.Move(file, file.Replace(".disabled", ""));
            else if (!item.Enabled && !file.EndsWith(".disabled"))
                File.Move(file, file + ".disabled");
        }
        item.Enabled = !item.Enabled;
    }

    /// <summary>Set a startup item to the given enabled state (idempotent).</summary>
    public void SetEnabled(StartupItem item, bool enable)
    {
        if (item.Source == "Registry")
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
                key?.SetValue(item.Name, item.Command);
            else
                key?.DeleteValue(item.Name, false);
        }
        else
        {
            var file = item.Command;
            if (enable && file.EndsWith(".disabled"))
                File.Move(file, file.Replace(".disabled", ""));
            else if (!enable && !file.EndsWith(".disabled"))
                File.Move(file, file + ".disabled");
        }
        item.Enabled = enable;
    }
}
