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
        Assert.Equal(6416, service.Orders.Count); // 6418 Rohzeilen minus 2 Admin-Testbestellungen
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

    [Fact]
    public void LoadFiles_Append_AddsToExistingInsteadOfReplacing()
    {
        var samples = AllSamples();
        var (first, second) = (samples[0], samples[1]);
        var firstCount = SingleFileRowCount(first);
        var secondCount = SingleFileRowCount(second);

        var service = CreateService();
        service.LoadFiles([first]); // Ersetzen (leerer Bestand)
        var raisedAfterFirst = 0;
        service.OrdersChanged += (_, _) => raisedAfterFirst++;

        service.LoadFiles([second], append: true);

        Assert.Equal(firstCount + secondCount, service.Orders.Count);
        Assert.Equal(2, service.LoadedFiles.Count);
        Assert.Equal(1, raisedAfterFirst);
        Assert.Contains(service.LoadedFiles, f => f.FileName == Path.GetFileName(first) && f.RowCount == firstCount);
        Assert.Contains(service.LoadedFiles, f => f.FileName == Path.GetFileName(second) && f.RowCount == secondCount);
    }

    [Fact]
    public void RemoveFile_RemovesOnlyThatFilesRows_AndRaisesEventOnce()
    {
        var samples = AllSamples();
        var (first, second) = (samples[0], samples[1]);
        var secondCount = SingleFileRowCount(second);

        var service = CreateService();
        service.LoadFiles([first, second]);

        var raised = 0;
        service.OrdersChanged += (_, _) => raised++;

        service.RemoveFile(first);

        Assert.Equal(secondCount, service.Orders.Count);
        Assert.Single(service.LoadedFiles);
        Assert.Equal(Path.GetFileName(second), service.LoadedFiles[0].FileName);
        Assert.Equal(1, raised);

        // Entfernen eines unbekannten Pfads löst kein Event aus.
        service.RemoveFile("gibt-es-nicht.csv");
        Assert.Equal(1, raised);
    }

    [Fact]
    public void LoadFiles_Append_SameFileTwice_ReplacesSegmentWithoutDuplicating()
    {
        var first = AllSamples()[0];
        var firstCount = SingleFileRowCount(first);

        var service = CreateService();
        service.LoadFiles([first]);
        service.LoadFiles([first], append: true);

        Assert.Equal(firstCount, service.Orders.Count);
        Assert.Single(service.LoadedFiles);
    }

    private static int SingleFileRowCount(string path) => CreateService().LoadFiles([path]).Orders.Count;
}
