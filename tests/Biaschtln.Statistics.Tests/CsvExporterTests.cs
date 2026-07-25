using System.Text;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using Biaschtln.Statistics.ViewModels;

namespace Biaschtln.Statistics.Tests;

public sealed class CsvExporterTests
{
    private sealed record Row(string Name, decimal Value);

    [Fact]
    public void RenderCsv_UsesSemicolonAndGermanDecimals()
    {
        var exporter = new CsvExporter(new FakeFileDialog(null));

        var csv = exporter.RenderCsv([new Row("Bier 0,5", 4.5m), new Row("Cola", 3m)]);

        Assert.Contains("Name;Value", csv);
        Assert.Contains("Bier 0,5;4,5", csv);
        Assert.Contains("Cola;3", csv);
    }

    [Fact]
    public void ExportCsv_WritesFileWithBom_WhenPathChosen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biaschtln-test-{Guid.NewGuid():N}.csv");
        try
        {
            var exporter = new CsvExporter(new FakeFileDialog(path));

            exporter.ExportCsv([new Row("Bier 0,5", 4.5m)], "test");

            Assert.True(File.Exists(path));
            var bytes = File.ReadAllBytes(path);
            // UTF-8-BOM für Excel.
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
            Assert.Contains("Bier 0,5;4,5", Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ExportCsv_DoesNothing_WhenDialogCanceled()
    {
        var exporter = new CsvExporter(new FakeFileDialog(null));

        // Kein Pfad → kein Wurf, keine Datei.
        exporter.ExportCsv([new Row("Bier 0,5", 4.5m)], "test");
    }

    [Fact]
    public void CategorySales_ExportCsvCommand_InvokesExporterWithRankingName()
    {
        var data = new FakeOrderDataForExport();
        var filter = new FilterViewModel(data);
        var recorder = new NoopCsvExporter();
        var vm = new CategorySalesViewModel(
            data, new OrderFilterService(), new StatisticsService(), filter, recorder)
        {
            ArticleRanking = ArticleRanking.Quantity,
        };

        vm.ExportCsvCommand.Execute(null);

        Assert.Equal(1, recorder.ExportCalls);
        Assert.Equal("top-artikel-stueckzahl", recorder.LastSuggestedName);
    }

    /// <summary>Kleiner Datenservice nur für den Kommando-Test.</summary>
    private sealed class FakeOrderDataForExport : IOrderDataService
    {
        public IReadOnlyList<OrderLine> Orders { get; } =
        [
            new() { Category = "Alk", Article = "Bier", Price = 4.5m, Status = "ORDER: CREATED" },
        ];

        public event EventHandler? OrdersChanged { add { } remove { } }

        public ImportResult LoadFiles(IEnumerable<string> paths) => throw new NotSupportedException();

        public void Clear() { }
    }
}
