using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class RestoreDialog : Window
{
    public bool RestorePointCreated { get; private set; }
    public bool Skipped { get; private set; }

    public RestoreDialog()
    {
        InitializeComponent();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        btnCreate.IsEnabled = false;
        btnSkip.IsEnabled = false;
        StatusBox.Visibility = Visibility.Visible;
        txtStatus.Text = "⏳ Creating restore point...";

        bool success = await Task.Run(() => RestorePointService.CreateRestorePoint());

        if (success)
        {
            txtStatus.Text = "✅ Restore point created successfully!";
            txtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            StatusBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
            await Task.Delay(1000);
            RestorePointCreated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            txtStatus.Text = "⚠️ Could not create restore point (System Restore may be disabled). Continue anyway?";
            btnSkip.IsEnabled = true;
            btnSkip.Content = "Continue Without Restore Point";
            btnCreate.IsEnabled = false;
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        Skipped = true;
        DialogResult = true;
        Close();
    }
}
