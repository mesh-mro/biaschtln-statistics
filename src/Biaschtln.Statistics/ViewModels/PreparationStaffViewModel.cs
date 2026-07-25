using System.Collections.ObjectModel;
using System.Globalization;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>Kennzahl für die Personal-Auswertung.</summary>
public enum StaffMetric
{
    /// <summary>Anzahl bearbeiteter Positionen je Benutzer.</summary>
    Orders,

    /// <summary>Umsatz je Benutzer.</summary>
    Revenue,
}

/// <summary>
/// ViewModel für "Zubereitung &amp; Personal" (WP7): ein Balkendiagramm der Ø
/// Zubereitungsdauer je Gericht (nur Positionen mit gesetzter Dauer) und eines für
/// Positionen bzw. Umsatz je Benutzer. Reagiert live auf Datei- und Filteränderungen.
/// </summary>
public sealed partial class PreparationStaffViewModel : FilteredChartViewModel
{
    private const int TopDishes = 12;
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly IStatisticsService _statistics;
    private readonly Axis _prepXAxis;
    private readonly Axis _prepYAxis;
    private readonly Axis _staffXAxis;
    private readonly Axis _staffYAxis;

    public PreparationStaffViewModel(
        IOrderDataService data,
        IOrderFilterService filterService,
        IStatisticsService statistics,
        FilterViewModel filter,
        ICsvExporter csvExporter)
        : base(data, filterService, filter, csvExporter)
    {
        _statistics = statistics;

        _prepXAxis = CategoryAxis();
        _prepYAxis = new Axis
        {
            MinLimit = 0,
            Labeler = value => FormatDuration(value),
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(ChartPalette.Grid) { StrokeThickness = 1 },
        };
        _staffXAxis = CategoryAxis();
        _staffYAxis = new Axis
        {
            MinLimit = 0,
            Labeler = value => value.ToString("N0", German),
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(ChartPalette.Grid) { StrokeThickness = 1 },
        };

        PreparationXAxes = [_prepXAxis];
        PreparationYAxes = [_prepYAxis];
        StaffXAxes = [_staffXAxis];
        StaffYAxes = [_staffYAxis];

        Refresh();
    }

    /// <summary>Balken: Ø Zubereitungsdauer je Gericht.</summary>
    public ObservableCollection<ISeries> PreparationSeries { get; } = [];

    /// <summary>Balken: Positionen bzw. Umsatz je Benutzer.</summary>
    public ObservableCollection<ISeries> StaffSeries { get; } = [];

    public IReadOnlyList<ICartesianAxis> PreparationXAxes { get; }

    public IReadOnlyList<ICartesianAxis> PreparationYAxes { get; }

    public IReadOnlyList<ICartesianAxis> StaffXAxes { get; }

    public IReadOnlyList<ICartesianAxis> StaffYAxes { get; }

    /// <summary>Umschaltung Personal-Kennzahl (Positionen oder Umsatz).</summary>
    [ObservableProperty]
    private StaffMetric _staffMetric = StaffMetric.Orders;

    /// <summary>False, wenn die gefilterte Menge leer ist (steuert die Leer-Anzeige).</summary>
    [ObservableProperty]
    private bool _hasData;

    partial void OnStaffMetricChanged(StaffMetric value) => Refresh();

    /// <summary>Exportiert die Zubereitungsdauer-Tabelle (je Gericht) als CSV.</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        var rows = _statistics.PreparationByArticle(FilteredOrders());
        CsvExporter.ExportCsv(rows, "zubereitungsdauer-je-gericht");
    }

    protected override void Refresh()
    {
        var orders = FilteredOrders();
        HasData = orders.Count > 0;

        BuildPreparationBars(orders);
        BuildStaffBars(orders);
    }

    private void BuildPreparationBars(IReadOnlyList<OrderLine> orders)
    {
        // PreparationByArticle berücksichtigt nur Positionen mit gesetzter Dauer.
        var prep = _statistics.PreparationByArticle(orders).Take(TopDishes).ToList();

        PreparationSeries.Clear();
        PreparationSeries.Add(new ColumnSeries<double>
        {
            Values = prep.Select(p => p.AverageSeconds).ToArray(),
            Name = "Ø Zubereitungsdauer",
            Fill = new SolidColorPaint(ChartPalette.SeriesOrange),
            Stroke = null,
            Rx = 4,
            Ry = 4,
            DataLabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            DataLabelsSize = 11,
            DataLabelsPosition = DataLabelsPosition.Top,
            DataLabelsFormatter = point => FormatDuration(point.Coordinate.PrimaryValue),
            YToolTipLabelFormatter = point =>
            {
                var p = prep[(int)point.Coordinate.SecondaryValue];
                return $"Ø {FormatDuration(p.AverageSeconds)} · Median {FormatDuration(p.MedianSeconds)} · " +
                       $"Max {FormatDuration(p.MaxSeconds)} · n={p.Count}";
            },
        });

        _prepXAxis.Labels = prep.Select(p => p.Article).ToList();
    }

    private void BuildStaffBars(IReadOnlyList<OrderLine> orders)
    {
        var byRevenue = StaffMetric == StaffMetric.Revenue;
        var users = _statistics.RevenueByUser(orders);

        // RevenueByUser ist nach Positionsanzahl sortiert; bei Umsatz-Ansicht danach ordnen.
        if (byRevenue)
        {
            users = users.OrderByDescending(u => u.Revenue).ToList();
        }

        StaffSeries.Clear();
        StaffSeries.Add(new ColumnSeries<double>
        {
            Values = users.Select(u => byRevenue ? (double)u.Revenue : u.Count).ToArray(),
            Name = byRevenue ? "Umsatz" : "Positionen",
            Fill = new SolidColorPaint(ChartPalette.SeriesBlue),
            Stroke = null,
            Rx = 4,
            Ry = 4,
            DataLabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            DataLabelsSize = 11,
            DataLabelsPosition = DataLabelsPosition.Top,
            DataLabelsFormatter = point => byRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C0", German)
                : point.Coordinate.PrimaryValue.ToString("N0", German),
            YToolTipLabelFormatter = point => byRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German)
                : $"{point.Coordinate.PrimaryValue.ToString("N0", German)} Pos.",
        });

        _staffXAxis.Labels = users.Select(u => u.User).ToList();
        _staffYAxis.Labeler = byRevenue
            ? (value => value.ToString("C0", German))
            : (value => value.ToString("N0", German));
    }

    private static Axis CategoryAxis() => new()
    {
        Labels = [],
        LabelsRotation = 25,
        LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
        TextSize = 12,
        SeparatorsPaint = null,
    };

    /// <summary>Formatiert Sekunden als "m:ss" (bzw. "h:mm:ss" ab einer Stunde).</summary>
    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes}:{ts.Seconds:00}";
    }
}
