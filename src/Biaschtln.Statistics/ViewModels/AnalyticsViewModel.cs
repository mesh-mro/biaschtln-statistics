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
using SkiaSharp;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// ViewModel für "Weitere Auswertungen" (WP8): Umsatz über Zeit (Stunde/Tag), Verteilung
/// der Zahlungsmethoden, Umsatz je Tisch und die Stornoquote. Reagiert live auf Datei- und
/// Filteränderungen. Die Stornoquote wird bewusst inklusive stornierter Positionen
/// berechnet (unabhängig vom Sidebar-Storno-Schalter).
/// </summary>
public sealed partial class AnalyticsViewModel : FilteredChartViewModel
{
    private const int TopTables = 15;
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private const string NoPayment = "(ohne)";

    private readonly IStatisticsService _statistics;
    private IReadOnlyDictionary<string, SKColor> _paymentColors = new Dictionary<string, SKColor>();
    private readonly Axis _timeXAxis;
    private readonly Axis _timeYAxis;
    private readonly Axis _tableXAxis;
    private readonly Axis _tableYAxis;

    public AnalyticsViewModel(
        IOrderDataService data,
        IOrderFilterService filterService,
        IStatisticsService statistics,
        FilterViewModel filter,
        ICsvExporter csvExporter)
        : base(data, filterService, filter, csvExporter)
    {
        _statistics = statistics;

        _timeXAxis = new Axis
        {
            Labels = [],
            LabelsRotation = 30,
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = null,
        };
        _timeYAxis = CurrencyAxis();
        _tableXAxis = new Axis
        {
            Labels = [],
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = null,
        };
        _tableYAxis = CurrencyAxis();

        RevenueOverTimeXAxes = [_timeXAxis];
        RevenueOverTimeYAxes = [_timeYAxis];
        TableXAxes = [_tableXAxis];
        TableYAxes = [_tableYAxis];

        Refresh();
    }

    /// <summary>Linie: Umsatz je Zeitintervall.</summary>
    public ObservableCollection<ISeries> RevenueOverTimeSeries { get; } = [];

    /// <summary>Donut: Umsatzanteil je Zahlungsmethode.</summary>
    public ObservableCollection<ISeries> PaymentSeries { get; } = [];

    /// <summary>Balken: Umsatz je Tisch (Top-N).</summary>
    public ObservableCollection<ISeries> TableSeries { get; } = [];

    public IReadOnlyList<ICartesianAxis> RevenueOverTimeXAxes { get; }

    public IReadOnlyList<ICartesianAxis> RevenueOverTimeYAxes { get; }

    public IReadOnlyList<ICartesianAxis> TableXAxes { get; }

    public IReadOnlyList<ICartesianAxis> TableYAxes { get; }

    /// <summary>Zeitliche Gruppierung der Umsatz-über-Zeit-Kurve.</summary>
    [ObservableProperty]
    private TimeBucket _timeBucket = TimeBucket.Day;

    /// <summary>Stornoquote als Prozenttext.</summary>
    [ObservableProperty]
    private string _cancellationRateText = "–";

    /// <summary>Erläuterung zur Stornoquote (Anzahl storniert / gesamt).</summary>
    [ObservableProperty]
    private string _cancellationDetailText = string.Empty;

    /// <summary>False, wenn die gefilterte Menge leer ist (steuert die Leer-Anzeige).</summary>
    [ObservableProperty]
    private bool _hasData;

    partial void OnTimeBucketChanged(TimeBucket value) => Refresh();

    /// <summary>Exportiert die Umsatz-über-Zeit-Tabelle (aktuelles Intervall) als CSV.</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        var rows = _statistics.RevenueOverTime(FilteredOrders(), TimeBucket);
        var suffix = TimeBucket == TimeBucket.Day ? "tag" : "stunde";
        CsvExporter.ExportCsv(rows, $"umsatz-ueber-zeit-{suffix}");
    }

    protected override void Refresh()
    {
        var orders = FilteredOrders();
        HasData = orders.Count > 0;

        RebuildPaymentColors();
        BuildRevenueOverTime(orders);
        BuildPaymentDonut(orders);
        BuildTableBars(orders);
        BuildCancellationRate();
    }

    private void BuildRevenueOverTime(IReadOnlyList<OrderLine> orders)
    {
        var buckets = _statistics.RevenueOverTime(orders, TimeBucket);

        RevenueOverTimeSeries.Clear();
        RevenueOverTimeSeries.Add(new LineSeries<double>
        {
            Values = buckets.Select(b => (double)b.Revenue).ToArray(),
            Name = "Umsatz",
            Stroke = new SolidColorPaint(ChartPalette.SeriesBlue) { StrokeThickness = 2 },
            Fill = null,
            GeometryFill = new SolidColorPaint(ChartPalette.SeriesBlue),
            GeometryStroke = new SolidColorPaint(ChartPalette.OnFill) { StrokeThickness = 2 },
            GeometrySize = buckets.Count > 40 ? 0 : 6,
            LineSmoothness = 0,
            YToolTipLabelFormatter = point =>
                ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German),
        });

        _timeXAxis.Labels = buckets.Select(b => FormatBucket(b.BucketStart)).ToList();
    }

    private void BuildPaymentDonut(IReadOnlyList<OrderLine> orders)
    {
        PaymentSeries.Clear();
        foreach (var payment in _statistics.RevenueByPaymentMethod(orders))
        {
            var summary = payment;
            PaymentSeries.Add(new PieSeries<decimal>
            {
                Values = [summary.Revenue],
                Name = summary.PaymentMethod,
                InnerRadius = 60,
                Fill = new SolidColorPaint(PaymentColor(summary.PaymentMethod)),
                Stroke = null,
                DataLabelsPaint = new SolidColorPaint(ChartPalette.Ink),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = _ => summary.PaymentMethod,
                ToolTipLabelFormatter = _ =>
                    $"{summary.PaymentMethod}: {summary.Revenue.ToString("C2", German)} ({summary.Count} Pos.)",
            });
        }
    }

    /// <summary>
    /// Baut eine stabile Farbzuordnung je Zahlungsmethode über das gesamte (ungefilterte)
    /// Methoden-Universum auf. Alphabetisch nach Slot — so bleibt die Farbe je Methode gleich,
    /// egal welche Methoden ein Filter gerade übrig lässt. "(ohne)" bleibt neutral grau.
    /// </summary>
    private void RebuildPaymentColors()
    {
        var methods = AllOrders
            .Select(o => string.IsNullOrWhiteSpace(o.PaymentMethod) ? NoPayment : o.PaymentMethod!)
            .Distinct()
            .OrderBy(m => m, StringComparer.CurrentCulture)
            .ToList();

        var map = new Dictionary<string, SKColor>();
        var slot = 0;
        foreach (var method in methods)
        {
            map[method] = method == NoPayment ? ChartPalette.Muted : ChartPalette.Slot(slot++);
        }

        _paymentColors = map;
    }

    private SKColor PaymentColor(string method) =>
        _paymentColors.TryGetValue(method, out var color) ? color : ChartPalette.Muted;

    private void BuildTableBars(IReadOnlyList<OrderLine> orders)
    {
        var tables = _statistics.RevenueByTable(orders).Take(TopTables).ToList();

        TableSeries.Clear();
        TableSeries.Add(new ColumnSeries<double>
        {
            Values = tables.Select(t => (double)t.Revenue).ToArray(),
            Name = "Umsatz",
            Fill = new SolidColorPaint(ChartPalette.SeriesAqua),
            Stroke = null,
            Rx = 4,
            Ry = 4,
            DataLabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            DataLabelsSize = 11,
            DataLabelsPosition = DataLabelsPosition.Top,
            DataLabelsFormatter = point => ((decimal)point.Coordinate.PrimaryValue).ToString("C0", German),
            YToolTipLabelFormatter = point => ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German),
        });

        _tableXAxis.Labels = tables.Select(t => t.Table).ToList();
    }

    private void BuildCancellationRate()
    {
        // Bewusst inkl. Stornos, sonst wäre die Quote (Standardfilter) immer 0.
        var withCanceled = FilteredOrders(f => f.IncludeCanceled = true);
        var total = withCanceled.Count;
        var canceled = withCanceled.Count(o => o.IsCanceled);
        var rate = _statistics.CancellationRate(withCanceled);

        CancellationRateText = total == 0 ? "–" : rate.ToString("P1", German);
        CancellationDetailText = total == 0
            ? "Keine Positionen"
            : $"{canceled:N0} von {total:N0} Positionen storniert";
    }

    private static Axis CurrencyAxis() => new()
    {
        MinLimit = 0,
        Labeler = value => value.ToString("C0", German),
        LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
        TextSize = 12,
        SeparatorsPaint = new SolidColorPaint(ChartPalette.Grid) { StrokeThickness = 1 },
    };

    private string FormatBucket(DateTime bucketStart) => TimeBucket == TimeBucket.Day
        ? bucketStart.ToString("ddd dd.MM.", German)
        : bucketStart.ToString("dd.MM. HH'h'", German);
}
