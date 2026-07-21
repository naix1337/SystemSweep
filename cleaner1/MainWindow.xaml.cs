using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;
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

        // Page fade transition animation
        NavFrame.Navigated += (_, _) =>
        {
            if (NavFrame.Content is FrameworkElement element)
            {
                element.Opacity = 0;
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
        };

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
            case "theme":
                ThemeService.Toggle();
                AppSettings.Instance.Theme = ThemeService.CurrentTheme;
                AppSettings.Instance.Save();
                navTheme.Content = ThemeService.CurrentTheme == "Dark" ? "Dark Mode" : "Light Mode";
                navTheme.Icon = new Wpf.Ui.Controls.SymbolIcon(
                    ThemeService.CurrentTheme == "Dark"
                        ? Wpf.Ui.Controls.SymbolRegular.DarkTheme24
                        : Wpf.Ui.Controls.SymbolRegular.BrightnessHigh24);
                // Navigate to Clean page
                if (!_pages.ContainsKey("clean"))
                    _pages["clean"] = new CleanPage(_historyService);
                NavFrame.Navigate(_pages["clean"]);
                break;
        }
    }
}
