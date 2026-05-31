using System.Globalization;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

public sealed class CsvOrderImporterTests
{
    private static readonly string SamplesDir =
        Path.Combine(AppContext.BaseDirectory, "samples");

    // Verifizierte Datenzeilen-Anzahl je Beispieldatei (siehe docs/IMPLEMENTATION_PLAN.md §2).
    private const int Rows1 = 1926; // Export_2026-05-08-23-16-14.csv
    private const int Rows2 = 2475; // Export_2026-05-10-12-50-16.csv
    private const int Rows3 = 2017; // Export_2026-05-11-13-24-51.csv

    private static string Sample(string name) => Path.Combine(SamplesDir, name);

    private static string[] AllSamples() =>
        Directory.GetFiles(SamplesDir, "*.csv").OrderBy(p => p).ToArray();

    [Fact]
    public void LoadFiles_AllSamples_LoadSuccessfullyWithExpectedRowCount()
    {
        var importer = new CsvOrderImporter();

        var result = importer.LoadFiles(AllSamples());

        Assert.True(result.AllSucceeded, "Alle Dateien sollten fehlerfrei laden.");
        Assert.Equal(3, result.Files.Count);
        Assert.Equal(Rows1 + Rows2 + Rows3, result.Orders.Count);
    }

    [Fact]
    public void LoadFiles_PerFileRowCountsMatchExpected()
    {
        var importer = new CsvOrderImporter();

        var r1 = importer.LoadFiles([Sample("Export_2026-05-08-23-16-14.csv")]);
        var r2 = importer.LoadFiles([Sample("Export_2026-05-10-12-50-16.csv")]);
        var r3 = importer.LoadFiles([Sample("Export_2026-05-11-13-24-51.csv")]);

        Assert.Equal(Rows1, r1.Orders.Count);
        Assert.Equal(Rows2, r2.Orders.Count);
        Assert.Equal(Rows3, r3.Orders.Count);
    }

    [Fact]
    public void LoadFiles_ParsesCommaDecimalPrices()
    {
        var importer = new CsvOrderImporter();

        var orders = importer.LoadFiles(AllSamples()).Orders;

        // "Bier 0,5" kostet 4,5 -> 4.5m (Komma-Dezimal korrekt geparst, nicht 45).
        var bier = orders.First(o => o.Article == "Bier 0,5");
        Assert.Equal(4.5m, bier.Price);
        Assert.All(orders, o => Assert.InRange(o.Price, 0m, 1000m));
    }

    [Fact]
    public void LoadFiles_ParsesTimestampsBooleansAndNullables()
    {
        var importer = new CsvOrderImporter();

        var orders = importer.LoadFiles([Sample("Export_2026-05-10-12-50-16.csv")]).Orders;

        // Bekannte Zeile: Bestell-ID 100, Schnitzelsemmerl, Essen, mit Zubereitungsdauer 183.
        var food = orders.First(o => o.OrderLineId == 100);
        Assert.Equal("Essen", food.Category);
        Assert.True(food.IsPaid);
        Assert.Equal(183, food.PreparationSeconds);
        Assert.Equal(
            DateTime.ParseExact("2026-05-09 18:12:30", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            food.OrderedAt);

        // Getränke haben keine Zubereitungsdauer -> null.
        var drink = orders.First(o => o.Category == "Anti");
        Assert.Null(drink.PreparationSeconds);
    }

    [Fact]
    public void LoadFiles_RecognizesCanceledOrders()
    {
        var importer = new CsvOrderImporter();

        var orders = importer.LoadFiles([Sample("Export_2026-05-10-12-50-16.csv")]).Orders;

        // Bestell-ID 97 ist ein STORNO.
        var canceled = orders.First(o => o.OrderLineId == 97);
        Assert.True(canceled.IsCanceled);
        Assert.False(canceled.IsPaid);
        Assert.Null(canceled.PaymentMethod);

        // "NULL"/leer wird zu null gemappt.
        Assert.Null(canceled.VatKey);
    }

    [Fact]
    public void LoadFiles_MissingFile_ReportedAsFailureWithoutThrowing()
    {
        var importer = new CsvOrderImporter();

        var result = importer.LoadFiles([Sample("does-not-exist.csv")]);

        Assert.False(result.AllSucceeded);
        Assert.Single(result.Files);
        Assert.False(result.Files[0].Success);
        Assert.NotNull(result.Files[0].Error);
        Assert.Empty(result.Orders);
    }
}
