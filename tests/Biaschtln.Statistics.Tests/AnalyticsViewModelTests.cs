using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace Biaschtln.Statistics.Tests;

public sealed class AnalyticsViewModelTests
{
    /// <summary>Minimaler In-Memory-Datenservice für VM-Tests (ohne Dateizugriff).</summary>
    private sealed class FakeOrderData : IOrderDataService
    {
        public IReadOnlyList<OrderLine> Orders { get; private set; } = [];

        public event EventHandler? OrdersChanged;

        public IReadOnlyList<LoadedFileInfo> LoadedFiles { get; } = [];

        public ImportResult LoadFiles(IEnumerable<string> paths, bool append = false) => throw new NotSupportedException();

        public void RemoveFile(string filePath) => throw new NotSupportedException();

        public void Clear()
        {
            Orders = [];
            OrdersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Set(IReadOnlyList<OrderLine> orders)
        {
            Orders = orders;
            OrdersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static OrderLine Line(
        string category, decimal price, string table, string? payment, DateTime orderedAt, bool canceled) =>
        new()
        {
            Category = category,
            Article = category + "-" + table,
            Price = price,
            Table = table,
            PaymentMethod = payment,
            OrderedAt = orderedAt,
            Status = canceled
                ? "ORDER: CANCELED - Bestellung STORNO"
                : "ORDER: CREATED - Bestellung erfasst",
        };

    // 3 gültige Positionen + 1 Storno. Storno hat keine Zahlungsmethode.
    private static List<OrderLine> Sample() =>
    [
        Line("Alk", 4.5m, "A1", "cash", new DateTime(2026, 5, 8, 18, 0, 0), false),
        Line("Essen", 9m, "A1", "card", new DateTime(2026, 5, 8, 19, 0, 0), false),
        Line("Anti", 3m, "B5", "cash", new DateTime(2026, 5, 9, 12, 0, 0), false),
        Line("Alk", 4.5m, "B5", null, new DateTime(2026, 5, 9, 12, 30, 0), true),
    ];

    private static OrderLine LineU(string category, decimal price, string table, string user) =>
        new()
        {
            Category = category,
            Article = category + "-" + table,
            Price = price,
            Table = table,
            User = user,
            Status = "ORDER: CREATED - Bestellung erfasst",
        };

    // Ein gemeinsames PickupSettings für Filter und VM — wie in der App (Singleton).
    private static (AnalyticsViewModel Vm, FakeOrderData Data, PickupSettings Pickup) CreateVm()
    {
        var data = new FakeOrderData();
        var pickup = new PickupSettings();
        var filter = new FilterViewModel(data, pickup);
        var vm = new AnalyticsViewModel(
            data, new OrderFilterService(), new StatisticsService(), filter, new NoopCsvExporter(), pickup);
        return (vm, data, pickup);
    }

    private static double PieValue(IEnumerable<ISeries> series, string name) =>
        series.Cast<PieSeries<double>>().Single(s => s.Name == name).Values!.Single();

    private static double[] ColumnValues(IEnumerable<ISeries> series) =>
        ((ColumnSeries<double>)series.Single()).Values!.ToArray();

    private static double[] TimeSeriesValues(IEnumerable<ISeries> series) =>
        ((ColumnSeries<DateTimePoint>)series.Single()).Values!.Select(p => p.Value ?? 0d).ToArray();

    [Fact]
    public void PaymentDonut_SumsRevenuePerMethod_ExcludingCanceled()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());
        vm.Metric = ChartMetric.Revenue; // Standard ist jetzt Positionen.

        // cash = Bier 4,5 + Cola 3 = 7,5 · card = Schnitzel 9. Storno zählt nicht.
        Assert.Equal(2, vm.PaymentSeries.Count);
        Assert.Equal(7.5d, PieValue(vm.PaymentSeries, "cash"));
        Assert.Equal(9d, PieValue(vm.PaymentSeries, "card"));
    }

    [Fact]
    public void TableBars_RevenuePerTable_DescendingByRevenue()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());
        vm.Metric = ChartMetric.Revenue; // Standard ist jetzt Positionen.

        // A1 = 4,5 + 9 = 13,5 · B5 = 3 (Storno auf B5 ausgeschlossen).
        Assert.Equal([13.5d, 3d], ColumnValues(vm.TableSeries));
    }

    [Fact]
    public void RevenueOverTime_DefaultsToQuarterHour_AndReactsToInterval()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Kennzahl Umsatz setzen (Standard ist jetzt Positionen).
        vm.Metric = ChartMetric.Revenue;

        // Standard-Intervall = 15 Minuten: 18:00 (4,5), 19:00 (9), 12:00 (3) → 3 chronologische
        // Punkte (Storno um 12:30 ausgeschlossen).
        Assert.Equal(TimeBucket.QuarterHour, vm.TimeBucket);
        Assert.Equal([4.5d, 9d, 3d], TimeSeriesValues(vm.RevenueOverTimeSeries));

        // Umschalten auf Minute: hier ebenfalls 3 Punkte (Zeitpunkte in verschiedenen Minuten).
        vm.TimeBucket = TimeBucket.Minute;
        Assert.Equal(3, TimeSeriesValues(vm.RevenueOverTimeSeries).Length);
    }

    [Fact]
    public void CancellationRate_IncludesCanceled_DespiteDefaultFilterExcludingThem()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // 1 Storno von 4 Positionen → 25 %. Wichtig: trotz Standardfilter (ohne Stornos)
        // wird die Quote inkl. Stornos berechnet, sonst wäre sie immer 0.
        Assert.Equal("1 von 4 Positionen storniert", vm.CancellationDetailText);
        Assert.StartsWith("25", vm.CancellationRateText);
    }

    [Fact]
    public void Metric_Count_SwitchesQuantitativeChartsToPositionCounts()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        vm.Metric = ChartMetric.Count;

        // Tisch nach Anzahl: A1 2 Positionen, B5 1 (Storno ausgeschlossen).
        Assert.Equal([2d, 1d], ColumnValues(vm.TableSeries));
        // Zahlungsmethoden nach Anzahl: cash 2, card 1.
        Assert.Equal(2d, PieValue(vm.PaymentSeries, "cash"));
        Assert.Equal(1d, PieValue(vm.PaymentSeries, "card"));
        // Über Zeit (Stunde): drei Buckets mit je 1 Position.
        Assert.Equal([1d, 1d, 1d], TimeSeriesValues(vm.RevenueOverTimeSeries));
        // Titel spiegeln die Kennzahl.
        Assert.Equal("Positionen je Tisch (Top 15, ohne Abholung)", vm.TableChartTitle);
        Assert.Equal("Positionen über Zeit", vm.TimeChartTitle);
    }

    [Fact]
    public void TableBars_ExcludePickupUser_AndReactToReconfiguration()
    {
        var (vm, data, pickup) = CreateVm();
        data.Set(
        [
            LineU("Essen", 9m, "A1", "Kellner 1"),
            LineU("Essen", 5m, "A1", "Abholstation"), // Abholung → zählt nicht zu A1
            LineU("Alk", 4m, "B5", "Kellner 2"),
        ]);
        vm.Metric = ChartMetric.Revenue; // Standard ist jetzt Positionen.

        // Standard-Abholstation ausgeschlossen: A1 = 9 (nicht 14), B5 = 4.
        Assert.Equal([9d, 4d], ColumnValues(vm.TableSeries));

        // Anderen Benutzer als Abholstation definieren → Diagramm reagiert live.
        pickup.PickupUser = "Kellner 1";

        // Jetzt Kellner-1-Position (A1, 9) ausgeschlossen: A1 = 5, B5 = 4.
        Assert.Equal([5d, 4d], ColumnValues(vm.TableSeries));
    }
}
