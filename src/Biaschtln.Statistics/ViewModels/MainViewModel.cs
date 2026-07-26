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
        AnalyticsViewModel analytics,
        OrdersTableViewModel ordersTable)
    {
        _data = data;
        _statistics = statistics;
        _filterService = filterService;
        _fileDialog = fileDialog;
        Filter = filter;
        CategorySales = categorySales;
        PreparationStaff = preparationStaff;
        Analytics = analytics;
        OrdersTable = ordersTable;

        Filter.FilterChanged += (_, _) => RecomputeKpis();
        _data.OrdersChanged += (_, _) => OnOrdersChanged();
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

    /// <summary>ViewModel der Datentabelle (alle gefilterten Positionen).</summary>
    public OrdersTableViewModel OrdersTable { get; }

    /// <summary>Aktuell geladene Dateien (Name + Positionsanzahl) für die Übersicht.</summary>
    public ObservableCollection<LoadedFileInfo> LoadedFiles { get; } = [];

    /// <summary>True, wenn mindestens eine Datei geladen ist (aktiviert Menüpunkte).</summary>
    [ObservableProperty]
    private bool _hasLoadedFiles;

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

    /// <summary>Öffnet Dateien und hängt sie an den bestehenden Bestand an.</summary>
    [RelayCommand]
    private void AddFiles() => OpenFiles(append: true);

    /// <summary>Öffnet Dateien und ersetzt den bestehenden Bestand.</summary>
    [RelayCommand]
    private void ReplaceFiles() => OpenFiles(append: false);

    /// <summary>Entfernt eine einzelne geladene Datei aus dem Bestand.</summary>
    [RelayCommand]
    private void RemoveFile(LoadedFileInfo? file)
    {
        if (file is null)
        {
            return;
        }

        _data.RemoveFile(file.FilePath);
        HasError = false;
        StatusMessage = _data.LoadedFiles.Count == 0
            ? "Noch keine Dateien geladen."
            : CurrentStoreStatus();
    }

    /// <summary>Verwirft alle geladenen Dateien.</summary>
    [RelayCommand]
    private void Clear()
    {
        _data.Clear();
        HasError = false;
        StatusMessage = "Noch keine Dateien geladen.";
    }

    private void OpenFiles(bool append)
    {
        var paths = _fileDialog.OpenFiles(CsvFilter, multiselect: true);
        if (paths is null || paths.Count == 0)
        {
            return;
        }

        LoadPaths(paths, append);
    }

    /// <summary>
    /// Lädt die angegebenen CSV-Pfade und aktualisiert Status, Dateiliste und (über das
    /// OrdersChanged-Event) die KPIs. Bei <paramref name="append"/> werden die Dateien an den
    /// bestehenden Bestand angehängt. Auch für das Vorladen per Startargument nutzbar.
    /// </summary>
    public void LoadPaths(IReadOnlyList<string> paths, bool append = false)
    {
        if (paths.Count == 0)
        {
            return;
        }

        var result = _data.LoadFiles(paths, append);

        HasError = !result.AllSucceeded;
        StatusMessage = BuildStatusMessage(result);
        // KPIs und Dateiliste werden über das OrdersChanged-Event aktualisiert.
    }

    private void OnOrdersChanged()
    {
        RecomputeKpis();
        RefreshLoadedFiles();
    }

    private void RefreshLoadedFiles()
    {
        LoadedFiles.Clear();
        foreach (var file in _data.LoadedFiles)
        {
            LoadedFiles.Add(file);
        }

        HasLoadedFiles = LoadedFiles.Count > 0;
    }

    private void RecomputeKpis()
    {
        var filter = Filter.BuildFilter();
        var filtered = _filterService.Apply(_data.Orders, filter).ToList();
        OrderCount = _statistics.DistinctOrderCount(filtered);
        LineCount = _statistics.TotalCount(filtered);
        RevenueText = FormatCurrency(_statistics.TotalRevenue(filtered));
    }

    private string CurrentStoreStatus() =>
        $"{_data.LoadedFiles.Count} Datei(en) · {_data.Orders.Count} Positionen.";

    private string BuildStatusMessage(ImportResult result)
    {
        if (result.AllSucceeded)
        {
            return CurrentStoreStatus();
        }

        var failed = result.Files
            .Where(f => !f.Success)
            .Select(f => $"{Path.GetFileName(f.FilePath)}: {f.Error}");
        return $"{CurrentStoreStatus()} Fehler bei: {string.Join("; ", failed)}";
    }

    private static string FormatCurrency(decimal value) => value.ToString("C2", GermanCulture);
}
