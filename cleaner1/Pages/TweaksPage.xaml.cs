using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernFileCleaner;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class TweaksPage
{
    private readonly TweaksService _tweaksService = new();
    private readonly List<TweakItem> _allTweaks = new();

    /// <summary>Staged toggle changes: tweak id -> desired enabled state. Applied via "Apply Changes".</summary>
    private readonly Dictionary<string, bool> _pending = new();

    public TweaksPage()
    {
        InitializeComponent();
        btnApplyRecommended.IsEnabled = btnRevertAll.IsEnabled = AppLicense.IsFullAccess;
        LoadTweaks();
        IsVisibleChanged += (_, _) => { if (IsVisible) RebuildCards(); };
        UpdatePendingState();
    }

    private void LoadTweaks()
    {
        _allTweaks.Clear();
        _allTweaks.AddRange(_tweaksService.GetAllTweaks());

        RebuildCards();
        UpdatePendingState();
    }

    private Border CreateTweakCard(TweakItem tweak)
    {
        var toggle = new Wpf.Ui.Controls.ToggleSwitch
        {
            IsChecked = tweak.IsEnabled,
            Tag = tweak.Id,
            VerticalAlignment = VerticalAlignment.Center
        };
        toggle.Click += TweakToggle_Click;
        toggle.IsEnabled = AppLicense.IsFullAccess;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconText = new TextBlock
        {
            Text = tweak.Icon,
            FontSize = 28,
            Margin = new Thickness(0, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(iconText, 0);

        var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text = tweak.Name,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });

        if (tweak.IsRecommended)
        {
            nameRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = "Recommended",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50)),
                    FontWeight = FontWeights.Bold
                }
            });
        }
        nameStack.Children.Add(nameRow);

        nameStack.Children.Add(new TextBlock
        {
            Text = tweak.Description,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrEmpty(tweak.WarningMessage))
        {
            nameStack.Children.Add(new TextBlock
            {
                Text = $"⚠️ {tweak.WarningMessage}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xAA, 0x00)),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        Grid.SetColumn(nameStack, 1);
        Grid.SetColumn(toggle, 2);

        grid.Children.Add(iconText);
        grid.Children.Add(nameStack);
        grid.Children.Add(toggle);

        return new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid
        };
    }

    private void TweakToggle_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
        if (sender is not Wpf.Ui.Controls.ToggleSwitch toggle) return;
        var id = toggle.Tag?.ToString();
        var tweak = _allTweaks.FirstOrDefault(t => t.Id == id);
        if (tweak == null) return;

        // Stage the change - nothing is written until "Apply Changes".
        _pending[id!] = toggle.IsChecked ?? false;
        UpdatePendingState();
    }

    private void ApplyRecommended_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
        var recommended = _allTweaks.Where(t => t.IsRecommended && !t.IsEnabled).ToList();
        if (recommended.Count == 0)
        {
            txtStatus.Text = "All recommended tweaks are already enabled.";
            return;
        }
        foreach (var tweak in recommended)
            _pending[tweak.Id] = true;
        UpdatePendingState();
        txtStatus.Text = $"{recommended.Count} recommended tweaks staged. Press Apply Changes.";
    }

    private void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
        var active = _allTweaks.Where(t => t.IsEnabled).ToList();
        if (active.Count == 0)
        {
            txtStatus.Text = "No enabled tweaks to revert.";
            return;
        }
        foreach (var tweak in active)
            _pending[tweak.Id] = false;
        UpdatePendingState();
        txtStatus.Text = $"{active.Count} tweaks staged for revert. Press Apply Changes.";
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!AppLicense.IsFullAccess || _pending.Count == 0) return;

        // Best-effort restore point before touching the system.
        if (!RestorePointService.EnsureRestorePoint())
            txtStatus.Text = "⚠️ Could not create a restore point — applying anyway (best-effort).";
        else
            txtStatus.Text = "🛡️ Restore point created.";

        int applied = 0;
        foreach (var kvp in _pending)
        {
            var tweak = _allTweaks.FirstOrDefault(t => t.Id == kvp.Key);
            if (tweak == null) continue;
            if (kvp.Value == tweak.IsEnabled) continue; // already in desired state

            try
            {
                if (kvp.Value)
                    await _tweaksService.ApplyTweakAsync(tweak);
                else
                    await _tweaksService.RevertTweakAsync(tweak);
                applied++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Tweaks] {ex.Message}");
            }
        }

        _pending.Clear();
        RebuildCards();
        UpdatePendingState();
        txtStatus.Text = applied > 0 ? $"✅ Applied {applied} change(s). Each tweak can be reverted individually." : "No changes to apply.";
    }

    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        _pending.Clear();
        RebuildCards();
        UpdatePendingState();
        txtStatus.Text = "Pending changes discarded.";
    }

    private void RebuildCards()
    {
        GamingPanel.Children.Clear();
        SystemPanel.Children.Clear();
        NetworkPanel.Children.Clear();
        DiskPanel.Children.Clear();
        AdvancedPanel.Children.Clear();
        CleanupPanel.Children.Clear();
        foreach (var tweak in _allTweaks)
        {
            var card = CreateTweakCard(tweak);
            var panel = tweak.Category switch
            {
                "Gaming" => GamingPanel,
                "System" => SystemPanel,
                "Network" => NetworkPanel,
                "Disk" => DiskPanel,
                "Advanced" => AdvancedPanel,
                "Cleanup" => CleanupPanel,
                _ => SystemPanel
            };
            panel.Children.Add(card);
        }
    }

    private void UpdatePendingState()
    {
        if (btnApply == null) return;
        bool enabled = AppLicense.IsFullAccess && _pending.Count > 0;
        btnApply.IsEnabled = enabled;
        btnDiscard.IsEnabled = AppLicense.IsFullAccess && _pending.Count > 0;
        txtPending.Text = _pending.Count > 0 ? $"{_pending.Count} pending change(s)" : "";
    }
}
