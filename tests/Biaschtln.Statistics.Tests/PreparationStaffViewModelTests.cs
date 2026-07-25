using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Biaschtln.Statistics.Tests;

public sealed class PreparationStaffViewModelTests
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

    private static OrderLine Line(string category, string article, decimal price, string user, int? prep) =>
        new()
        {
            Category = category,
            Article = article,
            Price = price,
            User = user,
            PreparationSeconds = prep,
            Status = "ORDER: CREATED - Bestellung erfasst",
        };

    // K1: 3 Positionen (2× Schnitzel mit Dauer, Cola ohne) · rev 21
    // K2: 2 Positionen (Pommes mit Dauer, Bier ohne) · rev 8,5
    // K3: 1 Position (Steak mit Dauer) · rev 50
    private static List<OrderLine> Sample() =>
    [
        Line("Essen", "Schnitzel", 9m, "K1", 100),
        Line("Essen", "Schnitzel", 9m, "K1", 200),
        Line("Anti", "Cola", 3m, "K1", null),
        Line("Essen", "Pommes", 4m, "K2", 300),
        Line("Alk", "Bier 0,5", 4.5m, "K2", null),
        Line("Essen", "Steak", 50m, "K3", 400),
    ];

    private static (PreparationStaffViewModel Vm, FakeOrderData Data, FilterViewModel Filter) CreateVm()
    {
        var data = new FakeOrderData();
        var filter = new FilterViewModel(data, new PickupSettings());
        var vm = new PreparationStaffViewModel(
            data, new OrderFilterService(), new StatisticsService(), filter, new NoopCsvExporter());
        return (vm, data, filter);
    }

    // Erste Serie = die Werte-Balken (bei der Zubereitung folgt ein unsichtbares Max-Overlay).
    private static double[] ColumnValues(IEnumerable<ISeries> series) =>
        ((ColumnSeries<double>)series.First()).Values!.ToArray();

    private static IReadOnlyList<string> Labels(IReadOnlyList<LiveChartsCore.Kernel.Sketches.ICartesianAxis> axes) =>
        ((Axis)axes[0]).Labels!.ToList();

    [Fact]
    public void Preparation_OnlyIncludesLinesWithDuration_AverageMatchesManualLinq()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Sortiert nach Ø-Dauer absteigend: Steak 400, Pommes 300, Schnitzel 150.
        Assert.Equal([400d, 300d, 150d], ColumnValues(vm.PreparationSeries));
        Assert.Equal(["Steak", "Pommes", "Schnitzel"], Labels(vm.PreparationXAxes));

        // Positionen ohne Dauer (Cola, Bier) tauchen nicht auf.
        Assert.DoesNotContain("Cola", Labels(vm.PreparationXAxes));
        Assert.DoesNotContain("Bier 0,5", Labels(vm.PreparationXAxes));
    }

    [Fact]
    public void Staff_OrdersMetric_CountsAllPositionsPerUser()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Standard: Positionen je Benutzer, nach Anzahl absteigend.
        Assert.Equal([3d, 2d, 1d], ColumnValues(vm.StaffSeries));
        Assert.Equal(["K1", "K2", "K3"], Labels(vm.StaffXAxes));
    }

    [Fact]
    public void Staff_RevenueMetric_ReordersByRevenue()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        vm.Metric = ChartMetric.Revenue;

        // Nach Umsatz absteigend: K3 (50), K1 (21), K2 (8,5).
        Assert.Equal([50d, 21d, 8.5d], ColumnValues(vm.StaffSeries));
        Assert.Equal(["K3", "K1", "K2"], Labels(vm.StaffXAxes));
    }

    [Fact]
    public void Charts_ReactLiveToUserFilter()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());

        filter.Users.Single(o => o.Name == "K1").IsSelected = true;

        // Nur K1: Personal = eine Säule (3 Positionen); Zubereitung = nur Schnitzel (Ø 150).
        Assert.Equal([3d], ColumnValues(vm.StaffSeries));
        Assert.Equal(["K1"], Labels(vm.StaffXAxes));
        Assert.Equal([150d], ColumnValues(vm.PreparationSeries));
        Assert.Equal(["Schnitzel"], Labels(vm.PreparationXAxes));
    }

    [Fact]
    public void HasData_IsFalseWhenFilteredSetEmpty()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());
        Assert.True(vm.HasData);

        filter.From = new DateTime(2099, 1, 1, 0, 0, 0);

        Assert.False(vm.HasData);
        Assert.Empty(vm.PreparationSeries.SelectMany(s => ((ColumnSeries<double>)s).Values!));
    }
}
