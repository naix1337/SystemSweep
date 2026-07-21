using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using ModernFileCleaner.Pages;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class MainWindow
{
    private readonly HistoryService _historyService = new();
    private readonly Dictionary<string, object> _pages = new();

    public MainWindow()
    {
        InitializeComponent();
        _historyService.Load();
        NavView.SelectionChanged += OnSelectionChanged;

        // Workaround: Navigate to first page after load
        Loaded += (_, _) =>
        {
            var cleanPage = new CleanPage(_historyService);
            _pages["clean"] = cleanPage;
            NavFrame.Navigate(cleanPage);
            NavFrame.Visibility = Visibility.Visible;

            // Auto-analyze if enabled
            if (AppSettings.Instance.AutoAnalyze)
            {
                cleanPage.RunAutoAnalyze();
            }
        };
    }

    public void NavigateToCleanPage()
    {
        if (!_pages.ContainsKey("clean"))
            _pages["clean"] = new CleanPage(_historyService);
        NavFrame.Navigate(_pages["clean"]);
    }

    private void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (NavView.SelectedItem is not Wpf.Ui.Controls.NavigationViewItem item) return;
        var tag = item.Tag?.ToString();
        switch (tag)
        {
            case "clean":
                if (!_pages.ContainsKey("clean"))
                    _pages["clean"] = new CleanPage(_historyService);
                NavFrame.Navigate(_pages["clean"]);
                break;
            case "stats":
                if (!_pages.ContainsKey("stats"))
                    _pages["stats"] = new StatsPage(_historyService);
                NavFrame.Navigate(_pages["stats"]);
                break;
            case "settings":
                if (!_pages.ContainsKey("settings"))
                    _pages["settings"] = new SettingsPage();
                NavFrame.Navigate(_pages["settings"]);
                break;
            case "about":
                if (!_pages.ContainsKey("about"))
                    _pages["about"] = new AboutPage();
                NavFrame.Navigate(_pages["about"]);
                break;
        }
    }
}
