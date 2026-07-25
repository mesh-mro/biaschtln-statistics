using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

/// <summary>
/// Reproduziert auf echten Sample-Daten, dass Abhol-Bestellungen bei einem Tisch-Filter
/// nicht mitgezählt werden (gleicher Codepfad wie die App: Import → Filter → Statistik).
/// </summary>
public sealed class TablePickupSampleTests
{
    private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "samples");

    private static IReadOnlyList<OrderLine> LoadSamples() =>
        new CsvOrderImporter().LoadFiles(Directory.GetFiles(SamplesDir, "*.csv")).Orders;

    [Fact]
    public void TableFilter_ExcludesAbholstationRevenue()
    {
        var orders = LoadSamples();
        var filter = new OrderFilterService();
        var stats = new StatisticsService();

        // Tisch A1 ohne Abholstations-Regel (Admin-Testbestellungen sind bereits beim Import
        // entfernt): 907 Rohumsatz minus 9 € Admin = 898 €.
        var withPickup = filter
            .Apply(orders, new OrderFilter { Tables = { "A1" } })
            .ToList();
        Assert.Equal(898m, stats.TotalRevenue(withPickup));

        // Mit Abholstation als Abholung: deren 522,50 € zählen dem Tisch A1 nicht mehr.
        var withoutPickup = filter
            .Apply(orders, new OrderFilter { Tables = { "A1" }, PickupUser = "Abholstation" })
            .ToList();
        Assert.Equal(375.5m, stats.TotalRevenue(withoutPickup));
    }
}
