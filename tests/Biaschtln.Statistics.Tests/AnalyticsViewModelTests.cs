using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Biaschtln.Statistics.Tests;

public sealed class AnalyticsViewModelTests
{
    /// <summary>Minimaler In-Memory-Datenservice für VM-Tests (ohne Dateizugriff).</summary>
    private sealed class FakeOrderData : IOrderDataService
    {
        public IReadOnlyList<OrderLine> Orders { get; private set; } = [];

        public event EventHandler? OrdersChanged;

        public ImportResult LoadFiles(IEnumerable<string> paths) => throw new NotSupportedException();

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

    private static decimal PieValue(IEnumerable<ISeries> series, string name) =>
        series.Cast<PieSeries<decimal>>().Single(s => s.Name == name).Values!.Single();

    private static double[] ColumnValues(IEnumerable<ISeries> series) =>
        ((ColumnSeries<double>)series.Single()).Values!.ToArray();

    private static double[] LineValues(IEnumerable<ISeries> series) =>
        ((LineSeries<double>)series.Single()).Values!.ToArray();

    [Fact]
    public void PaymentDonut_SumsRevenuePerMethod_ExcludingCanceled()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // cash = Bier 4,5 + Cola 3 = 7,5 · card = Schnitzel 9. Storno zählt nicht.
        Assert.Equal(2, vm.PaymentSeries.Count);
        Assert.Equal(7.5m, PieValue(vm.PaymentSeries, "cash"));
        Assert.Equal(9m, PieValue(vm.PaymentSeries, "card"));
    }

    [Fact]
    public void TableBars_RevenuePerTable_DescendingByRevenue()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // A1 = 4,5 + 9 = 13,5 · B5 = 3 (Storno auf B5 ausgeschlossen).
        Assert.Equal([13.5d, 3d], ColumnValues(vm.TableSeries));
    }

    [Fact]
    public void RevenueOverTime_BucketsByDayAndHour()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Standard = Tag: 08.05. (13,5) und 09.05. (3) → 2 Punkte.
        Assert.Equal([13.5d, 3d], LineValues(vm.RevenueOverTimeSeries));

        vm.TimeBucket = TimeBucket.Hour;
        // Stunde: 18h, 19h, 12h (Storno um 12:30 ausgeschlossen) → 3 Punkte.
        Assert.Equal(3, LineValues(vm.RevenueOverTimeSeries).Length);
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
    public void TableBars_ExcludePickupUser_AndReactToReconfiguration()
    {
        var (vm, data, pickup) = CreateVm();
        data.Set(
        [
            LineU("Essen", 9m, "A1", "Kellner 1"),
            LineU("Essen", 5m, "A1", "Abholstation"), // Abholung → zählt nicht zu A1
            LineU("Alk", 4m, "B5", "Kellner 2"),
        ]);

        // Standard-Abholstation ausgeschlossen: A1 = 9 (nicht 14), B5 = 4.
        Assert.Equal([9d, 4d], ColumnValues(vm.TableSeries));

        // Anderen Benutzer als Abholstation definieren → Diagramm reagiert live.
        pickup.PickupUser = "Kellner 1";

        // Jetzt Kellner-1-Position (A1, 9) ausgeschlossen: A1 = 5, B5 = 4.
        Assert.Equal([5d, 4d], ColumnValues(vm.TableSeries));
    }
}
