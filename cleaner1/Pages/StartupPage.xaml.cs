using System.Windows;
using ModernFileCleaner;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class StartupPage
{
    private readonly StartupService _startupService = new();
    private List<StartupItem> _items = new();

    /// <summary>Enabled state each item had when the page was loaded (to detect changes).</summary>
    private readonly Dictionary<string, bool> _originalState = new();

    public StartupPage()
    {
        InitializeComponent();
        LoadItems();
        IsVisibleChanged += (_, _) => { if (IsVisible) UpdateApplyState(); };
    }

    private void LoadItems()
    {
        _originalState.Clear();
        _items = _startupService.GetItems();
        foreach (var item in _items)
            _originalState[ItemKey(item)] = item.Enabled;

        StartupListView.ItemsSource = null;
        StartupListView.ItemsSource = _items;
        UpdateApplyState();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadItems();
    }

    /// <summary>Runs when a toggle is flipped (TwoWay binding already updated the item).</summary>
    private void ToggleSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess)
        {
            // Revert the model + visual so demo users cannot change anything.
            if (sender is Wpf.Ui.Controls.ToggleSwitch demo)
            {
                if (demo.DataContext is StartupItem demoItem)
                    demoItem.Enabled = _originalState.TryGetValue(ItemKey(demoItem), out var o) ? o : demoItem.Enabled;
                demo.IsChecked = !(demo.IsChecked ?? false);
            }
            MessageBox.Show("🔒 Demo — license key required", "Demo Mode",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        UpdateApplyState();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess) return;

        int applied = 0;
        foreach (var item in _items)
        {
            bool original = _originalState.TryGetValue(ItemKey(item), out var o) ? o : item.Enabled;
            if (item.Enabled != original)
            {
                _startupService.SetEnabled(item, item.Enabled); // write the desired state directly
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
        int pending = _items.Count(i =>
            _originalState.TryGetValue(ItemKey(i), out var o) && i.Enabled != o);
        btnApply.IsEnabled = AppLicense.IsFullAccess && pending > 0;
        txtPending.Text = pending > 0 ? $"{pending} pending change(s)" : "";
    }
}
