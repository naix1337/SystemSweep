using System.Windows;
using ModernFileCleaner;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class StartupPage
{
    private readonly StartupService _startupService = new();
    private List<StartupItem> _items = new();

    public StartupPage()
    {
        InitializeComponent();
        LoadItems();
    }

    private void LoadItems()
    {
        _items = _startupService.GetItems();
        StartupListView.ItemsSource = null;
        StartupListView.ItemsSource = _items;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadItems();
    }

    private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch demo) demo.IsChecked = !(demo.IsChecked ?? false);
            MessageBox.Show("🔒 Demo — license key required", "Demo Mode",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (sender is Wpf.Ui.Controls.ToggleSwitch toggle && toggle.DataContext is StartupItem item)
        {
            _startupService.Toggle(item);
        }
    }
}
