using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class TweaksPage
{
    private readonly TweaksService _tweaksService = new();
    private readonly List<TweakItem> _allTweaks = new();

    public TweaksPage()
    {
        InitializeComponent();
        LoadTweaks();
    }

    private void LoadTweaks()
    {
        _allTweaks.Clear();
        _allTweaks.AddRange(_tweaksService.GetAllTweaks());

        GamingPanel.Children.Clear();
        SystemPanel.Children.Clear();
        NetworkPanel.Children.Clear();
        DiskPanel.Children.Clear();
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
                "Cleanup" => CleanupPanel,
                _ => SystemPanel
            };
            panel.Children.Add(card);
        }
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

    private async void TweakToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.ToggleSwitch toggle) return;
        var id = toggle.Tag?.ToString();
        var tweak = _allTweaks.FirstOrDefault(t => t.Id == id);
        if (tweak == null) return;

        try
        {
            txtStatus.Text = tweak.IsEnabled ? $"Reverting {tweak.Name}..." : $"Applying {tweak.Name}...";
            if (tweak.IsEnabled)
                await _tweaksService.RevertTweakAsync(tweak);
            else
                await _tweaksService.ApplyTweakAsync(tweak);
            txtStatus.Text = $"✅ {tweak.Name} completed";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tweaks] {ex.Message}");
            txtStatus.Text = $"Error: {ex.Message}";
        }
    }

    private async void ApplyRecommended_Click(object sender, RoutedEventArgs e)
    {
        var recommended = _allTweaks.Where(t => t.IsRecommended && !t.IsEnabled).ToList();
        if (recommended.Count == 0)
        {
            txtStatus.Text = "All recommended tweaks already applied!";
            return;
        }

        txtStatus.Text = $"Applying {recommended.Count} tweaks...";
        foreach (var tweak in recommended)
        {
            try { await _tweaksService.ApplyTweakAsync(tweak); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Tweaks] {ex.Message}"); }
        }
        txtStatus.Text = $"✅ Applied {recommended.Count} recommended tweaks";
        RebuildCards();
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        var active = _allTweaks.Where(t => t.IsEnabled).ToList();
        if (active.Count == 0)
        {
            txtStatus.Text = "No tweaks to revert";
            return;
        }

        txtStatus.Text = $"Reverting {active.Count} tweaks...";
        foreach (var tweak in active)
        {
            try { await _tweaksService.RevertTweakAsync(tweak); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Tweaks] {ex.Message}"); }
        }
        txtStatus.Text = $"↩️ Reverted {active.Count} tweaks";
        RebuildCards();
    }

    private void RebuildCards()
    {
        GamingPanel.Children.Clear();
        SystemPanel.Children.Clear();
        NetworkPanel.Children.Clear();
        DiskPanel.Children.Clear();
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
                "Cleanup" => CleanupPanel,
                _ => SystemPanel
            };
            panel.Children.Add(card);
        }
    }
}
