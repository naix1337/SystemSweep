using System.Windows;
using ModernFileCleaner;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class StartupPage
{
    private readonly StartupService _startupService = new();
    private List<StartupItem> _items = new();

    /// <summary>Pending toggle changes: item identity -> desired Enabled state. Applied on "Apply Changes".</summary>
    private readonly Dictionary<string, bool> _pending = new();

    public StartupPage()
    {
        InitializeComponent();
        LoadItems();
    }

    private void LoadItems()
    {
        _pending.Clear();
        _items = _startupService.GetItems();
        StartupListView.ItemsSource = null;
        StartupListView.ItemsSource = _items;
        UpdateApplyState();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadItems();
    }

    private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.ToggleSwitch toggle) return;

        if (!AppLicense.IsFullAccess)
        {
            toggle.IsChecked = !(toggle.IsChecked ?? false);
            MessageBox.Show("🔒 Demo — license key required", "Demo Mode",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (toggle.DataContext is StartupItem item)
        {
            // Do not apply yet - stage the change and wait for "Apply Changes".
            _pending[ItemKey(item)] = toggle.IsChecked ?? false;
            UpdateApplyState();
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess || _pending.Count == 0) return;

        int applied = 0;
        foreach (var kvp in _pending)
        {
            var item = _items.FirstOrDefault(i => ItemKey(i) == kvp.Key);
            if (item == null) continue;
            if (item.Enabled != kvp.Value)
            {
                _startupService.Toggle(item); // flips current state to the desired one
                applied++;
            }
        }

        LoadItems();
        txtPending.Text = applied > 0 ? $"✅ Applied {applied} change(s)" : "No changes to apply";
    }

    private static string ItemKey(StartupItem item) => $"{item.Name}|{item.Source}|{item.Command}";

    private void UpdateApplyState()
    {
        if (btnApply == null) return;
        btnApply.IsEnabled = AppLicense.IsFullAccess && _pending.Count > 0;
        txtPending.Text = _pending.Count > 0 ? $"{_pending.Count} pending change(s)" : "";
    }
}
