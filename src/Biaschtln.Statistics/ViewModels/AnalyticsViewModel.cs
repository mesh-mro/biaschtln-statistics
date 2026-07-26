using System.Collections.ObjectModel;
using System.Globalization;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// ViewModel für "Weitere Auswertungen" (WP8): Umsatz/Positionen über Zeit (Stunde/15 Min/
/// Minute), Verteilung der Zahlungsmethoden, je Tisch und die Stornoquote. Umsatz vs.
/// Positionen ist per <see cref="Metric"/> umschaltbar. Reagiert live auf Datei- und
/// Filteränderungen. Die Stornoquote wird bewusst inklusive stornierter Positionen berechnet.
/// </summary>
public sealed partial class AnalyticsViewModel : FilteredChartViewModel
{
    private const int TopTables = 15;
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private const string NoPayment = "(ohne)";

    private readonly IStatisticsService _statistics;
    private readonly IPickupSettings _pickup;
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
        ICsvExporter csvExporter,
        IPickupSettings pickup)
        : base(data, filterService, filter, csvExporter)
    {
        _statistics = statistics;
        _pickup = pickup;
        _pickup.Changed += (_, _) => Refresh();

        // Lineare Zeitachse (numerisch über Ticks) — konfiguriert in ConfigureTimeAxis().
        _timeXAxis = new Axis
        {
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

    /// <summary>Balken: Umsatz/Positionen je Zeitintervall.</summary>
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
    private TimeBucket _timeBucket = TimeBucket.QuarterHour;

    /// <summary>Kennzahl (Umsatz oder Positionen) für die quantitativen Diagramme; Standard: Positionen.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MetricLabel))]
    [NotifyPropertyChangedFor(nameof(TimeChartTitle))]
    [NotifyPropertyChangedFor(nameof(TableChartTitle))]
    private ChartMetric _metric = ChartMetric.Count;

    /// <summary>Aktueller Kennzahlname ("Umsatz" oder "Positionen").</summary>
    public string MetricLabel => Metric == ChartMetric.Revenue ? "Umsatz" : "Positionen";

    /// <summary>Titel der Über-Zeit-Kurve (kennzahlabhängig).</summary>
    public string TimeChartTitle => $"{MetricLabel} über Zeit";

    /// <summary>Titel des Tisch-Diagramms (kennzahlabhängig).</summary>
    public string TableChartTitle => $"{MetricLabel} je Tisch (Top 15, ohne Abholung)";

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

    partial void OnMetricChanged(ChartMetric value) => Refresh();

    private bool IsRevenue => Metric == ChartMetric.Revenue;

    /// <summary>Exportiert die Umsatz-über-Zeit-Tabelle (aktuelles Intervall) als CSV.</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        var rows = _statistics.RevenueOverTime(FilteredOrders(), TimeBucket);
        var suffix = TimeBucket switch
        {
            TimeBucket.Minute => "minute",
            TimeBucket.QuarterHour => "viertelstunde",
            _ => "stunde",
        };
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
        var isRevenue = IsRevenue;
        var buckets = _statistics.RevenueOverTime(orders, TimeBucket);

        RevenueOverTimeSeries.Clear();
        RevenueOverTimeSeries.Add(new ColumnSeries<DateTimePoint>
        {
            // DateTimePoint → X = Zeit (Ticks); je Bucket ein Balken auf linearer Zeitachse.
            Values = buckets
                .Select(b => new DateTimePoint(b.BucketStart, isRevenue ? (double)b.Revenue : b.Count))
                .ToArray(),
            Name = MetricLabel,
            Fill = new SolidColorPaint(ChartPalette.SeriesBlue),
            Stroke = null,
            Padding = 0,
            YToolTipLabelFormatter = point =>
            {
                var when = TicksToDate(point.Coordinate.SecondaryValue);
                var value = isRevenue
                    ? ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German)
                    : $"{point.Coordinate.PrimaryValue.ToString("N0", German)} Pos.";
                return $"{FormatBucket(when)}: {value}";
            },
        });

        _timeYAxis.Labeler = AmountLabeler(isRevenue);
        ConfigureTimeAxis(orders);
    }

    private void BuildPaymentDonut(IReadOnlyList<OrderLine> orders)
    {
        var isRevenue = IsRevenue;
        PaymentSeries.Clear();
        foreach (var payment in _statistics.RevenueByPaymentMethod(orders))
        {
            var summary = payment;
            PaymentSeries.Add(new PieSeries<double>
            {
                Values = [isRevenue ? (double)summary.Revenue : summary.Count],
                Name = summary.PaymentMethod,
                InnerRadius = 60,
                Fill = new SolidColorPaint(PaymentColor(summary.PaymentMethod)),
                Stroke = null,
                // Keine On-Slice-Beschriftung (überlappt bei kleinen Segmenten) — Legende + Tooltip genügen.
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
        var isRevenue = IsRevenue;

        // Abhol-Bestellungen gehören keinem Tisch an und fließen daher nicht ein.
        var atTable = orders.Where(o => !_pickup.IsPickup(o)).ToList();
        var summaries = _statistics.RevenueByTable(atTable);
        // RevenueByTable ist nach Umsatz sortiert; bei Positionen-Ansicht nach Anzahl ordnen.
        var ordered = isRevenue ? summaries : summaries.OrderByDescending(t => t.Count).ToList();
        var tables = ordered.Take(TopTables).ToList();

        TableSeries.Clear();
        TableSeries.Add(new ColumnSeries<double>
        {
            Values = tables.Select(t => isRevenue ? (double)t.Revenue : t.Count).ToArray(),
            Name = MetricLabel,
            Fill = new SolidColorPaint(ChartPalette.SeriesAqua),
            Stroke = null,
            Rx = 4,
            Ry = 4,
            DataLabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            DataLabelsSize = 11,
            DataLabelsPosition = DataLabelsPosition.Top,
            DataLabelsFormatter = point => isRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C0", German)
                : point.Coordinate.PrimaryValue.ToString("N0", German),
            YToolTipLabelFormatter = point => isRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German)
                : $"{point.Coordinate.PrimaryValue.ToString("N0", German)} Pos.",
        });

        _tableYAxis.Labeler = AmountLabeler(isRevenue);
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

    /// <summary>Achsen-/Label-Formatierung je Kennzahl: Währung bzw. ganze Zahl.</summary>
    private static Func<double, string> AmountLabeler(bool isRevenue) => isRevenue
        ? value => value.ToString("C0", German)
        : value => value.ToString("N0", German);

    private string FormatBucket(DateTime bucketStart) => TimeBucket == TimeBucket.Hour
        ? bucketStart.ToString("dd.MM. HH'h'", German)
        : bucketStart.ToString("dd.MM. HH:mm", German);

    /// <summary>
    /// Konfiguriert die lineare Zeitachse: Ticks-basierte Beschriftung, Schrittweite je
    /// Intervall und die Spannweite über die <paramref name="orders"/> der aktuellen Auswahl
    /// (erste..letzte Bestellung). So zeigt die Achse bei ausgewählter Datei nur deren Zeitraum.
    /// </summary>
    private void ConfigureTimeAxis(IReadOnlyList<OrderLine> orders)
    {
        var unit = UnitTicks(TimeBucket);
        _timeXAxis.Labeler = value => FormatBucket(TicksToDate(value));
        _timeXAxis.UnitWidth = unit;
        _timeXAxis.MinStep = unit;

        if (orders.Count > 0)
        {
            _timeXAxis.MinLimit = orders.Min(o => o.OrderedAt).Ticks;
            _timeXAxis.MaxLimit = orders.Max(o => o.OrderedAt).Ticks;
        }
        else
        {
            _timeXAxis.MinLimit = null;
            _timeXAxis.MaxLimit = null;
        }
    }

    private static double UnitTicks(TimeBucket bucket) => bucket switch
    {
        TimeBucket.Minute => TimeSpan.FromMinutes(1).Ticks,
        TimeBucket.QuarterHour => TimeSpan.FromMinutes(15).Ticks,
        _ => TimeSpan.FromHours(1).Ticks,
    };

    /// <summary>
    /// Wandelt einen Achsen-Tickwert sicher in ein Datum um. LiveCharts wertet den Labeler
    /// beim Auto-Ranging (z. B. ohne Daten) auch an Positionen außerhalb des gültigen
    /// <see cref="DateTime"/>-Bereichs aus — ohne Clamping würde das eine
    /// <see cref="ArgumentOutOfRangeException"/> werfen (die der Debugger anzeigt).
    /// </summary>
    private static DateTime TicksToDate(double value)
    {
        var ticks = (long)Math.Clamp(value, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);
        return new DateTime(ticks);
    }
}
