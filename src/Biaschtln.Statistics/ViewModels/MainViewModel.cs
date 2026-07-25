using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Wurzel-ViewModel des Hauptfensters (WP5): lädt CSV-Dateien, hält die Filter-Sidebar
/// und berechnet den KPI-Kopf (Bestellungen, Positionen, Umsatz) auf der gefilterten
/// Menge. KPIs werden bei Datei- und Filteränderungen automatisch neu berechnet.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private const string CsvFilter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";

    private readonly IOrderDataService _data;
    private readonly IStatisticsService _statistics;
    private readonly IOrderFilterService _filterService;
    private readonly IFileDialogService _fileDialog;

    public MainViewModel(
        IOrderDataService data,
        IStatisticsService statistics,
        IOrderFilterService filterService,
        IFileDialogService fileDialog,
        FilterViewModel filter,
        CategorySalesViewModel categorySales,
        PreparationStaffViewModel preparationStaff,
        AnalyticsViewModel analytics)
    {
        _data = data;
        _statistics = statistics;
        _filterService = filterService;
        _fileDialog = fileDialog;
        Filter = filter;
        CategorySales = categorySales;
        PreparationStaff = preparationStaff;
        Analytics = analytics;

        Filter.FilterChanged += (_, _) => RecomputeKpis();
        _data.OrdersChanged += (_, _) => RecomputeKpis();
        RecomputeKpis();
    }

    public string Title => "Biaschtln-Statistik";

    /// <summary>Filter-Sidebar; ihre Optionen werden aus den geladenen Daten aufgebaut.</summary>
    public FilterViewModel Filter { get; }

    /// <summary>ViewModel der Seite "Umsatz nach Kategorie / Artikel" (WP6).</summary>
    public CategorySalesViewModel CategorySales { get; }

    /// <summary>ViewModel der Seite "Zubereitung &amp; Personal" (WP7).</summary>
    public PreparationStaffViewModel PreparationStaff { get; }

    /// <summary>ViewModel der Seite "Weitere Auswertungen" (WP8).</summary>
    public AnalyticsViewModel Analytics { get; }

    /// <summary>Namen der zuletzt geladenen Dateien.</summary>
    public ObservableCollection<string> LoadedFiles { get; } = [];

    [ObservableProperty]
    private string _statusMessage = "Noch keine Dateien geladen.";

    [ObservableProperty]
    private bool _hasError;

    /// <summary>Anzahl eindeutiger Bestellungen in der gefilterten Menge.</summary>
    [ObservableProperty]
    private int _orderCount;

    /// <summary>Anzahl Bestellpositionen in der gefilterten Menge.</summary>
    [ObservableProperty]
    private int _lineCount;

    /// <summary>Gesamtumsatz der gefilterten Menge, bereits als Währung formatiert.</summary>
    [ObservableProperty]
    private string _revenueText = FormatCurrency(0m);

    [RelayCommand]
    private void OpenFiles()
    {
        var paths = _fileDialog.OpenFiles(CsvFilter, multiselect: true);
        if (paths is null || paths.Count == 0)
        {
            return;
        }

        LoadPaths(paths);
    }

    /// <summary>
    /// Lädt die angegebenen CSV-Pfade und aktualisiert Status, Dateiliste und (über das
    /// OrdersChanged-Event) die KPIs. Auch für das Vorladen per Startargument nutzbar.
    /// </summary>
    public void LoadPaths(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        var result = _data.LoadFiles(paths);

        LoadedFiles.Clear();
        foreach (var file in result.Files)
        {
            LoadedFiles.Add(Path.GetFileName(file.FilePath));
        }

        HasError = !result.AllSucceeded;
        StatusMessage = BuildStatusMessage(result);
        // KPIs werden über das OrdersChanged-Event neu berechnet.
    }

    private void RecomputeKpis()
    {
        var filter = Filter.BuildFilter();
        var filtered = _filterService.Apply(_data.Orders, filter).ToList();
        OrderCount = _statistics.DistinctOrderCount(filtered);
        LineCount = _statistics.TotalCount(filtered);
        RevenueText = FormatCurrency(_statistics.TotalRevenue(filtered));
    }

    private static string BuildStatusMessage(ImportResult result)
    {
        var ok = result.Files.Count(f => f.Success);
        var rows = result.Files.Where(f => f.Success).Sum(f => f.RowCount);
        if (result.AllSucceeded)
        {
            return $"{ok} Datei(en) geladen · {rows} Positionen.";
        }

        var failed = result.Files
            .Where(f => !f.Success)
            .Select(f => $"{Path.GetFileName(f.FilePath)}: {f.Error}");
        return $"{ok} Datei(en) geladen · Fehler bei: {string.Join("; ", failed)}";
    }

    private static string FormatCurrency(decimal value) => value.ToString("C2", GermanCulture);
}
