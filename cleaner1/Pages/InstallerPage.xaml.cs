using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ModernFileCleaner;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Pages;

public partial class InstallerPage
{
    private readonly List<InstallableApp> _apps = new()
    {
        // Browsers
        new() { Id = "Google.Chrome", Name = "Google Chrome", Icon = "🌐", Category = "Browsers" },
        new() { Id = "Mozilla.Firefox", Name = "Firefox", Icon = "🦊", Category = "Browsers" },

        // Media
        new() { Id = "VideoLAN.VLC", Name = "VLC Media Player", Icon = "▶️", Category = "Media" },
        new() { Id = "Spotify.Spotify", Name = "Spotify", Icon = "🎵", Category = "Media" },
        new() { Id = "OBSProject.OBSStudio", Name = "OBS Studio", Icon = "📹", Category = "Media" },
        new() { Id = "GIMP.GIMP", Name = "GIMP", Icon = "🎨", Category = "Media" },
        new() { Id = "TheDocumentFoundation.LibreOffice", Name = "LibreOffice", Icon = "📊", Category = "Media" },

        // Messaging
        new() { Id = "Discord.Discord", Name = "Discord", Icon = "💬", Category = "Messaging" },
        new() { Id = "SlackTechnologies.Slack", Name = "Slack", Icon = "💼", Category = "Messaging" },
        new() { Id = "Zoom.Zoom", Name = "Zoom", Icon = "🎥", Category = "Messaging" },
        new() { Id = "TeamViewer.TeamViewer", Name = "TeamViewer", Icon = "🖥️", Category = "Messaging" },

        // Utilities
        new() { Id = "7zip.7zip", Name = "7-Zip", Icon = "🗜️", Category = "Utilities" },
        new() { Id = "RARLab.WinRAR", Name = "WinRAR", Icon = "🗂️", Category = "Utilities" },
        new() { Id = "Notepad++.Notepad++", Name = "Notepad++", Icon = "📄", Category = "Utilities" },

        // Development
        new() { Id = "Microsoft.VisualStudioCode", Name = "Visual Studio Code", Icon = "📝", Category = "Development" },
        new() { Id = "Git.Git", Name = "Git", Icon = "🔀", Category = "Development" },
        new() { Id = "Oracle.JavaRuntime", Name = "Java Runtime", Icon = "☕", Category = "Development" },

        // Gaming
        new() { Id = "Valve.Steam", Name = "Steam", Icon = "🎮", Category = "Gaming" }
    };

    public InstallerPage()
    {
        InitializeComponent();
        LoadApps();
        btnInstall.IsEnabled = AppLicense.IsFullAccess;
    }

    private void LoadApps()
    {
        foreach (var app in _apps)
        {
            var panel = app.Category switch
            {
                "Browsers" => BrowsersPanel,
                "Media" => MediaPanel,
                "Messaging" => MessagingPanel,
                "Utilities" => UtilitiesPanel,
                "Development" => DevPanel,
                "Gaming" => GamingPanel,
                _ => UtilitiesPanel
            };
            panel.Children.Add(CreateAppCard(app));
        }
    }

    private static Border CreateAppCard(InstallableApp app)
    {
        var toggle = new Wpf.Ui.Controls.ToggleSwitch
        {
            Content = app.Name,
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = AppLicense.IsFullAccess
        };
        toggle.DataContext = app;
        toggle.SetBinding(Wpf.Ui.Controls.ToggleSwitch.IsCheckedProperty,
            new Binding(nameof(InstallableApp.IsSelected)) { Mode = BindingMode.TwoWay });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconText = new TextBlock
        {
            Text = app.Icon,
            FontSize = 20,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(iconText, 0);
        Grid.SetColumn(toggle, 1);

        grid.Children.Add(iconText);
        grid.Children.Add(toggle);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 6),
            Child = grid
        };
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }

        var selected = _apps.Where(a => a.IsSelected).ToList();
        if (selected.Count == 0) { txtStatus.Text = "No apps selected."; return; }

        btnInstall.IsEnabled = false;
        int ok = 0;
        foreach (var app in selected)
        {
            txtStatus.Text = $"⬇ Installing {app.Name}...";
            bool success = await Task.Run(() => InstallWithWinget(app.Id));
            if (success) ok++;
        }

        btnInstall.IsEnabled = true;
        txtStatus.Text = ok > 0
            ? $"✅ Installed {ok} of {selected.Count} apps."
            : "⚠️ No apps could be installed (winget missing or installation failed).";
    }

    private static bool InstallWithWinget(string id)
    {
        try
        {
            var psi = new ProcessStartInfo("winget",
                $"install --id {id} --silent --accept-package-agreements --accept-source-agreements --disable-interactivity")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(180000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
