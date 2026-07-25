using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Biaschtln.Statistics.Tests;

public sealed class CategorySalesViewModelTests
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

    private static OrderLine Line(string category, string article, decimal price, bool canceled = false) =>
        new()
        {
            Category = category,
            Article = article,
            Price = price,
            Status = canceled
                ? "ORDER: CANCELED - Bestellung STORNO"
                : "ORDER: CREATED - Bestellung erfasst",
        };

    // Alk 3×4,5 = 13,5 · Essen 2×9 = 18 · Anti 1×3 = 3 · plus ein stornierter Alk (4,5), der
    // bei Standardfilter (IncludeCanceled=false) nicht zählt.
    private static List<OrderLine> Sample() =>
    [
        Line("Alk", "Bier 0,5", 4.5m),
        Line("Alk", "Bier 0,5", 4.5m),
        Line("Alk", "Bier 0,5", 4.5m),
        Line("Essen", "Schnitzel", 9m),
        Line("Essen", "Schnitzel", 9m),
        Line("Anti", "Cola", 3m),
        Line("Alk", "Bier 0,5", 4.5m, canceled: true),
    ];

    private static (CategorySalesViewModel Vm, FakeOrderData Data, FilterViewModel Filter) CreateVm()
    {
        var data = new FakeOrderData();
        var filter = new FilterViewModel(data, new PickupSettings());
        var vm = new CategorySalesViewModel(
            data, new OrderFilterService(), new StatisticsService(), filter, new NoopCsvExporter());
        return (vm, data, filter);
    }

    private static decimal PieValue(IEnumerable<ISeries> series, string name) =>
        series.Cast<PieSeries<decimal>>().Single(s => s.Name == name).Values!.Single();

    private static double[] ColumnValues(IEnumerable<ISeries> series) =>
        ((ColumnSeries<double>)series.Single()).Values!.ToArray();

    [Fact]
    public void CategoryDonut_ValuesMatchManualLinqSum_ExcludingCanceled()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Manuelle LINQ-Summe (Storno ausgeschlossen, wie im Standardfilter).
        var expected = Sample()
            .Where(o => !o.IsCanceled)
            .GroupBy(o => o.Category)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Price));

        Assert.Equal(3, vm.CategorySeries.Count);
        Assert.Equal(expected["Alk"], PieValue(vm.CategorySeries, "Alk"));
        Assert.Equal(expected["Essen"], PieValue(vm.CategorySeries, "Essen"));
        Assert.Equal(expected["Anti"], PieValue(vm.CategorySeries, "Anti"));
        // Storno darf den Alk-Umsatz nicht auf 18 anheben.
        Assert.Equal(13.5m, PieValue(vm.CategorySeries, "Alk"));
    }

    [Fact]
    public void TopArticles_RespectRankingToggle_AndReactLive()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Standard: nach Umsatz absteigend → Schnitzel (18), Bier (13,5), Cola (3).
        Assert.Equal([18d, 13.5d, 3d], ColumnValues(vm.ArticleSeries));

        // Umschalten auf Positionen → Bier (3), Schnitzel (2), Cola (1).
        vm.ArticleRanking = ArticleRanking.Quantity;
        Assert.Equal([3d, 2d, 1d], ColumnValues(vm.ArticleSeries));
    }

    [Fact]
    public void Charts_ReactLiveToFilterChanges()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());

        // Nur Kategorie "Essen" auswählen → Donut zeigt genau ein Segment mit dessen Umsatz.
        filter.Categories.Single(o => o.Name == "Essen").IsSelected = true;

        Assert.Single(vm.CategorySeries);
        Assert.Equal(18m, PieValue(vm.CategorySeries, "Essen"));
        Assert.Equal([18d], ColumnValues(vm.ArticleSeries));
    }

    [Fact]
    public void HasData_IsFalseWhenFilteredSetEmpty()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());
        Assert.True(vm.HasData);

        // Filter, der nichts durchlässt (nicht existierende Kategorie).
        filter.Categories.Single(o => o.Name == "Anti").IsSelected = true;
        filter.Users.Clear(); // keine Nutzer-Optionen; nur zur Sicherheit
        // Zeitfenster in der Zukunft → leere Menge.
        filter.From = new DateTime(2099, 1, 1, 0, 0, 0);

        Assert.False(vm.HasData);
        Assert.Empty(vm.CategorySeries);
    }
}
