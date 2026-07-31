using System.Diagnostics;
using System.Windows;
using ModernFileCleaner;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Pages;

public partial class InstallerPage
{
    private readonly List<InstallableApp> _apps = new()
    {
        new() { Id = "Google.Chrome", Name = "Google Chrome", Icon = "🌐" },
        new() { Id = "Mozilla.Firefox", Name = "Firefox", Icon = "🦊" },
        new() { Id = "7zip.7zip", Name = "7-Zip", Icon = "🗜️" },
        new() { Id = "VideoLAN.VLC", Name = "VLC Media Player", Icon = "▶️" },
        new() { Id = "Discord.Discord", Name = "Discord", Icon = "💬" },
        new() { Id = "Valve.Steam", Name = "Steam", Icon = "🎮" },
        new() { Id = "Spotify.Spotify", Name = "Spotify", Icon = "🎵" },
        new() { Id = "Microsoft.VisualStudioCode", Name = "Visual Studio Code", Icon = "📝" },
        new() { Id = "Notepad++.Notepad++", Name = "Notepad++", Icon = "📄" },
        new() { Id = "RARLab.WinRAR", Name = "WinRAR", Icon = "🗂️" },
        new() { Id = "Zoom.Zoom", Name = "Zoom", Icon = "🎥" },
        new() { Id = "OBSProject.OBSStudio", Name = "OBS Studio", Icon = "📹" },
        new() { Id = "GIMP.GIMP", Name = "GIMP", Icon = "🎨" },
        new() { Id = "TheDocumentFoundation.LibreOffice", Name = "LibreOffice", Icon = "📊" },
        new() { Id = "SlackTechnologies.Slack", Name = "Slack", Icon = "💼" },
        new() { Id = "TeamViewer.TeamViewer", Name = "TeamViewer", Icon = "🖥️" },
        new() { Id = "Git.Git", Name = "Git", Icon = "🔀" },
        new() { Id = "Oracle.JavaRuntime", Name = "Java Runtime", Icon = "☕" }
    };

    public InstallerPage()
    {
        InitializeComponent();
        AppsList.ItemsSource = _apps;
        btnInstall.IsEnabled = AppLicense.IsFullAccess;
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
