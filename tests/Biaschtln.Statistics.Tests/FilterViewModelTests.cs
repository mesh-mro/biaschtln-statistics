using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;

namespace Biaschtln.Statistics.Tests;

public sealed class FilterViewModelTests
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

    private static OrderLine Line(string category, string article, string table, string user, string? payment) =>
        new()
        {
            Category = category,
            Article = article,
            Table = table,
            User = user,
            PaymentMethod = payment,
            Status = "ORDER: CREATED - Bestellung erfasst",
        };

    private static List<OrderLine> Sample() =>
    [
        Line("Alk", "Bier 0,5", "A1", "K2", "cash"),
        Line("Essen", "Schnitzel", "A1", "K1", "card"),
        Line("Alk", "Bier 0,5", "B5", "K1", null),
    ];

    [Fact]
    public void RebuildOptions_BuildsDistinctSortedOptionsFromData()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data);

        data.Set(Sample());

        Assert.Equal(["Alk", "Essen"], vm.Categories.Select(o => o.Name));
        Assert.Equal(["Bier 0,5", "Schnitzel"], vm.Articles.Select(o => o.Name));
        Assert.Equal(["A1", "B5"], vm.Tables.Select(o => o.Name));
        Assert.Equal(["K1", "K2"], vm.Users.Select(o => o.Name));
        // Null-Zahlungsmethoden tauchen nicht als Option auf.
        Assert.Equal(["card", "cash"], vm.PaymentMethods.Select(o => o.Name));
    }

    [Fact]
    public void BuildFilter_ReflectsSelectionsAndProperties()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data);
        data.Set(Sample());

        vm.Categories.First(o => o.Name == "Alk").IsSelected = true;
        vm.IncludeCanceled = true;
        vm.Paid = PaidFilter.OnlyPaid;
        vm.From = new DateTime(2026, 5, 9, 0, 0, 0);

        var filter = vm.BuildFilter();

        Assert.Equal(["Alk"], filter.Categories);
        Assert.True(filter.IncludeCanceled);
        Assert.Equal(PaidFilter.OnlyPaid, filter.Paid);
        Assert.Equal(new DateTime(2026, 5, 9, 0, 0, 0), filter.From);
    }

    [Fact]
    public void FilterChanged_RaisedOnSelectionAndPropertyChanges()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data);
        data.Set(Sample());

        var raised = 0;
        vm.FilterChanged += (_, _) => raised++;

        vm.Categories.First().IsSelected = true;
        vm.IncludeCanceled = true;

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Reset_ClearsSelectionsAndProperties_AndRaisesOnce()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data);
        data.Set(Sample());
        vm.Categories.First().IsSelected = true;
        vm.Paid = PaidFilter.OnlyUnpaid;

        var raised = 0;
        vm.FilterChanged += (_, _) => raised++;

        vm.Reset();

        Assert.All(vm.Categories, o => Assert.False(o.IsSelected));
        Assert.Equal(PaidFilter.All, vm.Paid);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Sync_PreservesSelectionAcrossDataReload()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data);
        data.Set(Sample());
        vm.Categories.First(o => o.Name == "Alk").IsSelected = true;

        // Erneutes Laden (gleiche Kategorien vorhanden) -> Auswahl bleibt erhalten.
        data.Set(Sample());

        Assert.True(vm.Categories.First(o => o.Name == "Alk").IsSelected);
    }
}
