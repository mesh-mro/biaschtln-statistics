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

    private static OrderLine Line(string category, string article, string table, string user, string? payment,
        string? sourceFile = null) =>
        new()
        {
            Category = category,
            Article = article,
            Table = table,
            User = user,
            PaymentMethod = payment,
            SourceFile = sourceFile ?? string.Empty,
            Status = "ORDER: CREATED - Bestellung erfasst",
        };

    private static List<OrderLine> Sample() =>
    [
        Line("Alk", "Bier 0,5", "A1", "K2", "cash", sourceFile: "tag1.csv"),
        Line("Essen", "Schnitzel", "A1", "K1", "card", sourceFile: "tag1.csv"),
        Line("Alk", "Bier 0,5", "B5", "K1", null, sourceFile: "tag2.csv"),
    ];

    [Fact]
    public void RebuildOptions_BuildsDistinctSortedOptionsFromData()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data, new PickupSettings());

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
        var vm = new FilterViewModel(data, new PickupSettings());
        data.Set(Sample());

        vm.Categories.First(o => o.Name == "Alk").IsSelected = true;
        vm.IncludeCanceled = true;
        vm.Paid = PaidFilter.OnlyPaid;
        vm.SelectedFile = "tag1.csv";

        var filter = vm.BuildFilter();

        Assert.Equal(["Alk"], filter.Categories);
        Assert.True(filter.IncludeCanceled);
        Assert.Equal(PaidFilter.OnlyPaid, filter.Paid);
        Assert.Equal("tag1.csv", filter.File);
    }

    [Fact]
    public void SyncFiles_BuildsFileListWithSentinel_AndDefaultsToAll()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data, new PickupSettings());
        data.Set(Sample());

        Assert.Equal([FilterViewModel.AllFiles, "tag1.csv", "tag2.csv"], vm.Files);
        Assert.Equal(FilterViewModel.AllFiles, vm.SelectedFile);
        // "Alle Dateien" bedeutet kein Datei-Filter.
        Assert.Null(vm.BuildFilter().File);
    }

    [Fact]
    public void FilterChanged_RaisedOnSelectionAndPropertyChanges()
    {
        var data = new FakeOrderData();
        var vm = new FilterViewModel(data, new PickupSettings());
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
        var vm = new FilterViewModel(data, new PickupSettings());
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
        var vm = new FilterViewModel(data, new PickupSettings());
        data.Set(Sample());
        vm.Categories.First(o => o.Name == "Alk").IsSelected = true;

        // Erneutes Laden (gleiche Kategorien vorhanden) -> Auswahl bleibt erhalten.
        data.Set(Sample());

        Assert.True(vm.Categories.First(o => o.Name == "Alk").IsSelected);
    }
}
