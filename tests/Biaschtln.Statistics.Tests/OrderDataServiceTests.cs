using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

public sealed class OrderDataServiceTests
{
    private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "samples");

    private static string[] AllSamples() =>
        Directory.GetFiles(SamplesDir, "*.csv").OrderBy(p => p).ToArray();

    private static OrderDataService CreateService() => new(new CsvOrderImporter());

    [Fact]
    public void LoadFiles_StoresOrders_AndRaisesChangedEvent()
    {
        var service = CreateService();
        var raised = 0;
        service.OrdersChanged += (_, _) => raised++;

        var result = service.LoadFiles(AllSamples());

        Assert.True(result.AllSucceeded);
        Assert.Equal(6418, service.Orders.Count);
        Assert.Equal(result.Orders.Count, service.Orders.Count);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clear_EmptiesStore_AndRaisesChangedEventOnceWhenNonEmpty()
    {
        var service = CreateService();
        service.LoadFiles(AllSamples());

        var raisedAfterLoad = 0;
        service.OrdersChanged += (_, _) => raisedAfterLoad++;

        service.Clear();
        Assert.Empty(service.Orders);
        Assert.Equal(1, raisedAfterLoad);

        // Erneutes Clear auf leerem Bestand löst kein Event aus.
        service.Clear();
        Assert.Equal(1, raisedAfterLoad);
    }
}
