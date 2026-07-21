using System.Diagnostics;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class AboutPage
{
    private readonly UpdateService _updateService = new();

    public AboutPage()
    {
        InitializeComponent();
        txtVersion.Text = _updateService.CurrentVersion;
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        btnCheckUpdate.IsEnabled = false;
        txtUpdateStatus.Text = "Checking for updates...";

        var update = await _updateService.CheckForUpdatesAsync();
        if (update == null)
        {
            txtUpdateStatus.Text = "Could not check (offline or server unreachable)";
            btnCheckUpdate.IsEnabled = true;
            return;
        }

        if (_updateService.IsNewerVersion(update.Version))
        {
            var result = MessageBox.Show(
                $"New version {update.Version} available!\n\n{update.Changelog}\n\nDownload now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                _updateService.OpenDownloadPage(update.DownloadUrl);
        }
        else
        {
            txtUpdateStatus.Text = $"✅ You have the latest version ({_updateService.CurrentVersion})";
        }

        btnCheckUpdate.IsEnabled = true;
    }

    private void Website_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/nix1337/SystemSweep") { UseShellExecute = true }); } catch { }
    }

    private void Rate_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/nix1337/SystemSweep") { UseShellExecute = true }); } catch { }
    }

    private void Support_Click(object sender, RoutedEventArgs e)
    {
        txtUpdateStatus.Text = "Write to: support@naix.dev";
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
