using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class DuplicatesPage
{
    private readonly DuplicateFinderService _dupService = new();
    private CancellationTokenSource? _cts;
    private ObservableCollection<DuplicateGroup> _duplicates = new();
    private string _selectedPath = "";

    public DuplicatesPage()
    {
        InitializeComponent();
        resultsList.ItemsSource = _duplicates;
        cmbPath.SelectedIndex = 0;
    }

    private void cmbPath_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var tag = (cmbPath.SelectedItem as FrameworkElement)?.Tag?.ToString();
        txtCustomPath.Visibility = tag == "custom" ? Visibility.Visible : Visibility.Collapsed;

        _selectedPath = tag switch
        {
            "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "downloads" => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            _ => ""
        };

        if (tag != "custom")
            txtStatus.Text = _selectedPath;
    }

    private async void btnScan_Click(object? sender, RoutedEventArgs? e)
    {
        // Resolve path
        var path = _selectedPath;
        if ((cmbPath.SelectedItem as FrameworkElement)?.Tag?.ToString() == "custom")
            path = txtCustomPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
        {
            MessageBox.Show("Please select a valid directory to scan.", "Invalid Path",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Cancel previous scan if running
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        btnScan.IsEnabled = false;
        btnDelete.IsEnabled = false;
        _duplicates.Clear();
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = true;
        txtStatus.Text = "Scanning...";

        var progress = new Progress<string>(s => txtStatus.Text = s);

        try
        {
            var results = await _dupService.FindDuplicatesAsync(path, progress, ct);

            _duplicates.Clear();
            foreach (var group in results)
                _duplicates.Add(group);

            txtStatus.Text = $"Found {_duplicates.Count} duplicate groups ({_duplicates.Sum(g => g.Files.Count)} files).";
            btnDelete.IsEnabled = _duplicates.Count > 0;
        }
        catch (OperationCanceledException)
        {
            txtStatus.Text = "Scan cancelled.";
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            btnScan.IsEnabled = true;
        }
    }

    private async void btnDelete_Click(object? sender, RoutedEventArgs? e)
    {
        var selectedGroups = _duplicates.Where(g => g.IsSelected).ToList();
        if (selectedGroups.Count == 0)
        {
            MessageBox.Show("No duplicate groups selected.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Collect all files to delete (preserve one per group)
        var filesToDelete = new List<string>();
        long totalSize = 0;
        foreach (var group in selectedGroups)
        {
            // Keep the first file, delete the rest
            for (int i = 1; i < group.Files.Count; i++)
            {
                filesToDelete.Add(group.Files[i]);
                totalSize += group.SizeBytes;
            }
        }

        if (filesToDelete.Count == 0)
        {
            MessageBox.Show("No duplicate files to delete (each group needs at least 2 files).", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Delete {filesToDelete.Count} duplicate files?\n" +
            $"Estimated space to free: {FormatBytes(totalSize)}\n\n" +
            "One copy of each file will be preserved. This cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        btnScan.IsEnabled = false;
        btnDelete.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Maximum = filesToDelete.Count;
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;

        int deleted = 0;
        long freed = 0;
        foreach (var file in filesToDelete)
        {
            try
            {
                System.IO.File.Delete(file);
                deleted++;
                freed += new FileInfo(file).Length;
                txtStatus.Text = $"Deleted {deleted}/{filesToDelete.Count}...";
            }
            catch
            {
                // Skip files that can't be deleted
            }
            ProgressBar.Value = deleted;
        }

        // Remove empty groups from list
        var toRemove = _duplicates.Where(g => !g.Files.Any(f => System.IO.File.Exists(f))).ToList();
        foreach (var g in toRemove)
            _duplicates.Remove(g);

        ProgressBar.Visibility = Visibility.Collapsed;
        btnScan.IsEnabled = true;
        btnDelete.IsEnabled = _duplicates.Count > 0;
        txtStatus.Text = $"Deleted {deleted} files, freed {FormatBytes(freed)}.";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
