using System.Windows;
using System.Windows.Navigation;
using ModernFileCleaner.Pages;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class MainWindow
{
    private readonly HistoryService _historyService = new();

    public MainWindow()
    {
        InitializeComponent();
        _historyService.Load();
        NavView.SelectionChanged += OnSelectionChanged;

        // Workaround: Navigate to first page after load
        Loaded += (_, _) =>
        {
            NavFrame.Navigate(new CleanPage(_historyService));
            NavFrame.Visibility = Visibility.Visible;
        };
    }

    public void NavigateToCleanPage()
    {
        NavFrame.Navigate(new CleanPage(_historyService));
    }

    private void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (NavView.SelectedItem is not Wpf.Ui.Controls.NavigationViewItem item) return;
        var tag = item.Tag?.ToString();
        switch (tag)
        {
            case "clean":
                NavFrame.Navigate(new CleanPage(_historyService));
                break;
            case "stats":
                NavFrame.Navigate(new StatsPage(_historyService));
                break;
            case "settings":
                NavFrame.Navigate(new SettingsPage());
                break;
            case "about":
                NavFrame.Navigate(new AboutPage());
                break;
        }
    }
}
