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
    private string _currentPage = "";

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

        // Navigate to Dashboard on load
        Loaded += (_, _) =>
        {
            var dashPage = new DashboardPage();
            _pages["dashboard"] = dashPage;
            _currentPage = "dashboard";
            NavFrame.Navigate(dashPage);
            NavFrame.Visibility = Visibility.Visible;
            dashPage.OnPageVisible();
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

        // Notify previous page that it's hidden
        if (_currentPage == "dashboard" && _pages.TryGetValue("dashboard", out var prevDash))
            ((DashboardPage)prevDash).OnPageHidden();

        switch (tag)
        {
            case "dashboard":
                if (!_pages.ContainsKey("dashboard"))
                    _pages["dashboard"] = new DashboardPage();
                _currentPage = "dashboard";
                NavFrame.Navigate(_pages["dashboard"]);
                ((DashboardPage)_pages["dashboard"]).OnPageVisible();
                break;
            case "clean":
                if (!_pages.ContainsKey("clean"))
                    _pages["clean"] = new CleanPage(_historyService);
                _currentPage = "clean";
                NavFrame.Navigate(_pages["clean"]);
                break;
            case "browsers":
                if (!_pages.ContainsKey("browsers"))
                    _pages["browsers"] = new BrowserCachePage();
                _currentPage = "browsers";
                NavFrame.Navigate(_pages["browsers"]);
                break;
            case "startup":
                if (!_pages.ContainsKey("startup"))
                    _pages["startup"] = new StartupPage();
                _currentPage = "startup";
                NavFrame.Navigate(_pages["startup"]);
                break;
            case "stats":
                if (!_pages.ContainsKey("stats"))
                    _pages["stats"] = new StatsPage(_historyService);
                _currentPage = "stats";
                NavFrame.Navigate(_pages["stats"]);
                break;
            case "settings":
                if (!_pages.ContainsKey("settings"))
                    _pages["settings"] = new SettingsPage();
                _currentPage = "settings";
                NavFrame.Navigate(_pages["settings"]);
                break;
            case "about":
                if (!_pages.ContainsKey("about"))
                    _pages["about"] = new AboutPage();
                _currentPage = "about";
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
                // Navigate to Dashboard
                if (!_pages.ContainsKey("dashboard"))
                    _pages["dashboard"] = new DashboardPage();
                _currentPage = "dashboard";
                NavFrame.Navigate(_pages["dashboard"]);
                ((DashboardPage)_pages["dashboard"]).OnPageVisible();
                break;
        }
    }
}
