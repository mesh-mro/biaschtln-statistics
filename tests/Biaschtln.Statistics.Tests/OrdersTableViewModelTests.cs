using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;

namespace Biaschtln.Statistics.Tests;

public sealed class OrdersTableViewModelTests
{
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

    private static OrderLine Line(string category, string article, decimal price, int? prep = null, bool canceled = false) =>
        new()
        {
            Category = category,
            Article = article,
            Price = price,
            PreparationSeconds = prep,
            User = "K1",
            Status = canceled
                ? "ORDER: CANCELED - Bestellung STORNO"
                : "ORDER: CREATED - Bestellung erfasst",
        };

    private static List<OrderLine> Sample() =>
    [
        Line("Alk", "Bier 0,5", 4.5m),
        Line("Essen", "Schnitzel", 9m, prep: 183),
        Line("Alk", "Bier 0,5", 4.5m, canceled: true),
    ];

    private static (OrdersTableViewModel Vm, FakeOrderData Data, FilterViewModel Filter) CreateVm(
        ICsvExporter? exporter = null)
    {
        var data = new FakeOrderData();
        var filter = new FilterViewModel(data, new PickupSettings());
        var vm = new OrdersTableViewModel(data, new OrderFilterService(), filter, exporter ?? new NoopCsvExporter());
        return (vm, data, filter);
    }

    [Fact]
    public void Rows_ReflectFilteredSet_ExcludingCanceledByDefault()
    {
        var (vm, data, _) = CreateVm();
        data.Set(Sample());

        // Standardfilter schließt Stornos aus → 2 Zeilen.
        Assert.Equal(2, vm.RowCount);
        Assert.True(vm.HasData);
        Assert.All(vm.Rows, r => Assert.False(r.Storno));
        // Speise trägt die Zubereitungsdauer; Getränk hat keine.
        Assert.Contains(vm.Rows, r => r is { Artikel: "Schnitzel", Preis: 9m, Kategorie: "Essen", Zubereitung: 183 });
        Assert.Contains(vm.Rows, r => r is { Artikel: "Bier 0,5", Zubereitung: null });
    }

    [Fact]
    public void Rows_ReactLiveToFilter()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());

        filter.Categories.Single(o => o.Name == "Essen").IsSelected = true;

        var row = Assert.Single(vm.Rows);
        Assert.Equal("Schnitzel", row.Artikel);
    }

    [Fact]
    public void IncludeCanceled_ShowsCanceledRows()
    {
        var (vm, data, filter) = CreateVm();
        data.Set(Sample());

        filter.IncludeCanceled = true;

        Assert.Equal(3, vm.RowCount);
        Assert.Contains(vm.Rows, r => r.Storno);
    }

    [Fact]
    public void ExportCsvCommand_InvokesExporterWithPositionsName()
    {
        var recorder = new NoopCsvExporter();
        var (vm, data, _) = CreateVm(recorder);
        data.Set(Sample());

        vm.ExportCsvCommand.Execute(null);

        Assert.Equal(1, recorder.ExportCalls);
        Assert.Equal("positionen", recorder.LastSuggestedName);
    }
}
