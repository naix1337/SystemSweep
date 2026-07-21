using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
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

        navDashboard.Click += NavButton_Click;
        navClean.Click += NavButton_Click;
        navBrowser.Click += NavButton_Click;
        navDuplicates.Click += NavButton_Click;
        navStartup.Click += NavButton_Click;
        navTweaks.Click += NavButton_Click;
        navStats.Click += NavButton_Click;
        navSettings.Click += NavButton_Click;
        navAbout.Click += NavButton_Click;
        navLicense.Click += NavButton_Click;

        // Navigate to Dashboard on load
        Loaded += (_, _) =>
        {
            NavigateTo("dashboard");
        };
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Primitives.ToggleButton btn) return;
        if (btn.Tag is string tag)
            NavigateTo(tag);
    }

    public void NavigateToCleanPage()
    {
        NavigateTo("clean");
    }

    private void NavigateTo(string tag)
    {
        // Notify previous dashboard page it's hidden
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
            case "duplicates":
                if (!_pages.ContainsKey("duplicates"))
                    _pages["duplicates"] = new DuplicatesPage();
                _currentPage = "duplicates";
                NavFrame.Navigate(_pages["duplicates"]);
                break;
            case "startup":
                if (!_pages.ContainsKey("startup"))
                    _pages["startup"] = new StartupPage();
                _currentPage = "startup";
                NavFrame.Navigate(_pages["startup"]);
                break;
            case "tweaks":
                if (!_pages.ContainsKey("tweaks"))
                    _pages["tweaks"] = new TweaksPage();
                _currentPage = "tweaks";
                NavFrame.Navigate(_pages["tweaks"]);
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
            case "license":
                if (!_pages.ContainsKey("license"))
                    _pages["license"] = new LicensePage();
                _currentPage = "license";
                NavFrame.Navigate(_pages["license"]);
                break;
        }
    }

    private void btnTheme_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();
        AppSettings.Instance.Theme = ThemeService.CurrentTheme;
        AppSettings.Instance.Save();
        bool isDark = ThemeService.CurrentTheme == "Dark";
        txtThemeLabel.Text = isDark ? "Dark Mode" : "Light Mode";
        txtThemeIcon.Text = isDark ? "&#xE706;" : "&#xE707;";
        NavigateTo("dashboard");
    }
}
