using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;

namespace StoreFiler;

public partial class MainWindow : Window
{
    private const int MaxRows = 2000;                 // Vis maks så mange rader i lista
    private readonly ObservableCollection<FileItem> _items = new();
    private CancellationTokenSource? _cts;
    private Safety? _filter;                           // null = vis alle

    public MainWindow()
    {
        InitializeComponent();
        Grid.ItemsSource = _items;
        LoadDrives();
    }

    // ---- Diskliste ----

    private record DriveEntry(string RootPath, string Label, double UsedPercent, string FreeText);

    private void LoadDrives()
    {
        var entries = new List<DriveEntry>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            try
            {
                long total = d.TotalSize, free = d.TotalFreeSpace;
                double used = total > 0 ? (total - free) * 100.0 / total : 0;
                string name = string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Lokal disk" : d.VolumeLabel;
                string label = $"{name} ({d.Name.TrimEnd('\\')})";
                string freeText = $"{SizeConverter.Format(free)} ledig av {SizeConverter.Format(total)}";
                entries.Add(new DriveEntry(d.RootDirectory.FullName, label, used, freeText));
            }
            catch { /* hopp over disker vi ikke får lest */ }
        }
        DriveList.ItemsSource = entries;
    }

    private void OnDriveSelected(object sender, SelectionChangedEventArgs e)
    {
        if (DriveList.SelectedItem is DriveEntry d)
            PathBox.Text = d.RootPath;
    }

    // ---- Bla gjennom ----

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Velg mappe eller disk som skal skannes",
            UseDescriptionForTitle = true,
        };
        if (!string.IsNullOrWhiteSpace(PathBox.Text) && Directory.Exists(PathBox.Text))
            dlg.SelectedPath = PathBox.Text;
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            PathBox.Text = dlg.SelectedPath;
    }

    // ---- Skanning ----

    private long SelectedMinBytes()
    {
        if (MinSizeBox.SelectedItem is ComboBoxItem item && long.TryParse(item.Tag?.ToString(), out long v))
            return v;
        return 0;
    }

    private async void OnScan(object sender, RoutedEventArgs e)
    {
        string root = PathBox.Text.Trim();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            MessageBox.Show(this, "Velg en gyldig mappe eller disk å skanne.", "Store Filer",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        long minBytes = SelectedMinBytes();
        _items.Clear();
        _cts = new CancellationTokenSource();
        SetScanning(true);

        var found = new List<FileItem>();
        long totalBytes = 0;
        var sw = Stopwatch.StartNew();

        var progress = new Progress<(int folders, int matches, long bytes, string current)>(p =>
        {
            ScanStats.Text = $"Skanner… {p.folders:N0} mapper gjennomsøkt · {p.matches:N0} store filer funnet · {SizeConverter.Format(p.bytes)}";
            ScanCurrent.Text = p.current;
        });
        var reporter = (IProgress<(int, int, long, string)>)progress;

        try
        {
            await Task.Run(() =>
            {
                int folders = 0, matches = 0;
                long scannedFiles = 0, lastReportAt = 0;
                var stack = new Stack<string>();
                stack.Push(root);

                while (stack.Count > 0)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    string dir = stack.Pop();
                    folders++;

                    try
                    {
                        foreach (var sub in Directory.EnumerateDirectories(dir))
                            stack.Push(sub);
                    }
                    catch { /* ingen tilgang — hopp over */ }

                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(dir))
                        {
                            scannedFiles++;
                            try
                            {
                                var fi = new FileInfo(file);
                                if (fi.Length >= minBytes)
                                {
                                    var item = FileItem.FromInfo(fi);
                                    item.Analysis = FileAnalyzer.Analyze(item);
                                    found.Add(item);
                                    totalBytes += fi.Length;
                                    matches++;
                                }
                            }
                            catch { /* fil forsvant eller ingen tilgang */ }
                        }
                    }
                    catch { /* ingen tilgang til mappa */ }

                    // Rapporter ofte så brukeren ser at det jobbes: hver 10. mappe eller 2000. fil
                    if (folders % 10 == 0 || scannedFiles - lastReportAt >= 2000)
                    {
                        lastReportAt = scannedFiles;
                        reporter.Report((folders, matches, totalBytes, dir));
                    }
                }
            }, _cts.Token);

            // Sorter største først og vis toppen
            found.Sort((a, b) => b.Size.CompareTo(a.Size));
            foreach (var f in found.Take(MaxRows))
                _items.Add(f);

            SortGridBySize();
            ApplyFilter();
            if (found.Count == 0)
            {
                string limit = SelectedMinBytes() > 0 ? $" over {SizeConverter.Format(SelectedMinBytes())}" : "";
                StatusText.Text = $"Ferdig på {sw.Elapsed.TotalSeconds:0.0} s — ingen filer{limit} her. Prøv en lavere «Minst»-verdi.";
            }
            else
            {
                string shown = found.Count > MaxRows ? $"Viser de {MaxRows} største av {found.Count:N0}" : $"{found.Count:N0} filer";
                StatusText.Text = $"Ferdig på {sw.Elapsed.TotalSeconds:0.0} s — {shown}, til sammen {SizeConverter.Format(totalBytes)}.";
            }
        }
        catch (OperationCanceledException)
        {
            found.Sort((a, b) => b.Size.CompareTo(a.Size));
            foreach (var f in found.Take(MaxRows))
                _items.Add(f);
            SortGridBySize();
            ApplyFilter();
            StatusText.Text = $"Stoppet — {_items.Count:N0} filer vist, til sammen {SizeConverter.Format(totalBytes)}.";
        }
        finally
        {
            SetScanning(false);
        }
    }

    private void SortGridBySize()
    {
        var view = CollectionViewSource.GetDefaultView(_items);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(FileItem.Size), ListSortDirection.Descending));
        // Speil sorteringspilen i kolonneoverskriften
        foreach (var col in Grid.Columns)
            col.SortDirection = col.SortMemberPath == nameof(FileItem.Size) ? ListSortDirection.Descending : null;
    }

    private void OnStop(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void SetScanning(bool on)
    {
        ScanButton.IsEnabled = !on;
        StopButton.IsEnabled = on;
        ScanBanner.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on)
        {
            ScanStats.Text = "Skanner… starter opp";
            ScanCurrent.Text = "";
        }
    }

    // ---- Handlinger på filer ----

    private List<FileItem> Selected() => Grid.SelectedItems.Cast<FileItem>().ToList();

    private void OnRowDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => RevealInExplorer(Grid.SelectedItem as FileItem);

    private void OnOpenInExplorer(object sender, RoutedEventArgs e)
        => RevealInExplorer(Grid.SelectedItem as FileItem);

    private void RevealInExplorer(FileItem? item)
    {
        if (item is null) return;
        try
        {
            if (File.Exists(item.FullPath))
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FullPath}\"") { UseShellExecute = true });
            else if (Directory.Exists(item.Directory))
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{item.Directory}\"") { UseShellExecute = true });
        }
        catch { /* ignorer */ }
    }

    // ---- Hurtigfilter ----

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;   // hopp over støy under oppstart
        _filter = (FilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Safe" => Safety.Safe,
            "Caution" => Safety.Caution,
            "Unsafe" => Safety.Unsafe,
            "Unknown" => Safety.Unknown,
            _ => null,
        };
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var view = CollectionViewSource.GetDefaultView(_items);
        if (view is null) return;
        view.Filter = _filter is null ? null : o => o is FileItem f && f.Analysis?.Safety == _filter;
        view.Refresh();
        UpdateCountLabel();
    }

    private void UpdateCountLabel()
    {
        if (_items.Count == 0) { CountLabel.Text = ""; return; }
        int shown = _filter is null ? _items.Count : _items.Count(f => f.Analysis?.Safety == _filter);
        CountLabel.Text = shown == _items.Count
            ? $"{_items.Count:N0} filer"
            : $"Viser {shown:N0} av {_items.Count:N0} filer";
    }

    // ---- Analyse / detaljpanel ----

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        => ShowAnalysis(Grid.SelectedItem as FileItem);

    private void OnAnalyze(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is FileItem f)
            ShowAnalysis(f);
        else
            MessageBox.Show(this, "Velg en fil i lista først, så analyserer jeg den.", "Analyser",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowAnalysis(FileItem? f)
    {
        if (f is null)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }

        var a = f.Analysis ?? FileAnalyzer.Analyze(f);
        EmptyHint.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        var brush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(a.Color)!;
        VerdictBadge.Background = brush;
        VerdictIcon.Text = a.Glyph;
        VerdictText.Text = a.Verdict;
        CategoryText.Text = a.Category;

        DName.Text = f.Name;
        DSize.Text = SizeConverter.Format(f.Size);
        DApp.Text = string.IsNullOrEmpty(a.App) ? "—" : a.App;
        DExplain.Text = a.Explanation;
        DPath.Text = f.FullPath;
    }

    private void OnDonate(object sender, RoutedEventArgs e) => OpenUrl(AppLinks.Donation);
    private void OnOpenWebsite(object sender, RoutedEventArgs e) => OpenUrl(AppLinks.Website);

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ingen nettleser tilgjengelig */ }
    }

    private void OnCopyPath(object sender, RoutedEventArgs e)
    {
        var sel = Selected();
        if (sel.Count == 0) return;
        try { Clipboard.SetText(string.Join(Environment.NewLine, sel.Select(f => f.FullPath))); }
        catch { /* utklippstavla kan være opptatt */ }
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        var sel = Selected();
        if (sel.Count == 0) return;

        long total = sel.Sum(f => f.Size);
        string msg = sel.Count == 1
            ? $"Slett denne fila til papirkurven?\n\n{sel[0].Name}\n{SizeConverter.Format(sel[0].Size)}"
            : $"Slett {sel.Count} filer til papirkurven?\n\nFrigjør {SizeConverter.Format(total)}.";

        if (MessageBox.Show(this, msg, "Bekreft sletting", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        bool ok = NativeMethods.DeleteToRecycleBin(sel.Select(f => f.FullPath));

        // Fjern de som faktisk ble borte fra lista
        long freed = 0;
        foreach (var f in sel)
        {
            if (!File.Exists(f.FullPath))
            {
                _items.Remove(f);
                freed += f.Size;
            }
        }

        LoadDrives();
        StatusText.Text = ok
            ? $"Slettet til papirkurv — frigjorde {SizeConverter.Format(freed)}."
            : "Sletting ble avbrutt eller delvis fullført.";
    }
}
