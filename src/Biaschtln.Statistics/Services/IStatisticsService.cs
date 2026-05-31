using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <summary>
/// Berechnet Aggregat-Kennzahlen über eine (bereits gefilterte) Menge von
/// Bestellpositionen. Der Service ist zustandslos: jede Methode aggregiert genau die
/// übergebene Menge. Die Ein-/Ausgrenzung von Stornos ist Aufgabe des Filters (WP4) —
/// "Umsatz" ist hier schlicht die Summe der Preise der übergebenen Positionen.
/// </summary>
public interface IStatisticsService
{
    /// <summary>Gesamtumsatz (Summe Preis) der übergebenen Positionen.</summary>
    decimal TotalRevenue(IEnumerable<OrderLine> orders);

    /// <summary>Anzahl der übergebenen Positionen.</summary>
    int TotalCount(IEnumerable<OrderLine> orders);

    /// <summary>Anzahl eindeutiger Bestellungen (<see cref="OrderLine.OrderId"/>).</summary>
    int DistinctOrderCount(IEnumerable<OrderLine> orders);

    /// <summary>Umsatz/Anzahl je Kategorie, absteigend nach Umsatz.</summary>
    IReadOnlyList<CategorySummary> RevenueByCategory(IEnumerable<OrderLine> orders);

    /// <summary>Top-N Artikel nach Umsatz oder Stückzahl.</summary>
    IReadOnlyList<ArticleSummary> TopArticles(IEnumerable<OrderLine> orders, int topN, ArticleRanking ranking);

    /// <summary>Ø/Median/Max Zubereitungsdauer je Artikel (nur Positionen mit Dauer), absteigend nach Ø.</summary>
    IReadOnlyList<PreparationSummary> PreparationByArticle(IEnumerable<OrderLine> orders);

    /// <summary>Umsatz/Anzahl je Benutzer, absteigend nach Anzahl.</summary>
    IReadOnlyList<UserSummary> RevenueByUser(IEnumerable<OrderLine> orders);

    /// <summary>Umsatz/Anzahl je Zahlungsmethode (für WP8).</summary>
    IReadOnlyList<PaymentMethodSummary> RevenueByPaymentMethod(IEnumerable<OrderLine> orders);

    /// <summary>Umsatz/Anzahl je Tisch (für WP8).</summary>
    IReadOnlyList<TableSummary> RevenueByTable(IEnumerable<OrderLine> orders);

    /// <summary>Umsatz/Anzahl je Zeitintervall, chronologisch (für WP8).</summary>
    IReadOnlyList<TimeBucketSummary> RevenueOverTime(IEnumerable<OrderLine> orders, TimeBucket bucket);

    /// <summary>Stornoquote: Anteil stornierter Positionen (0..1). 0 bei leerer Menge.</summary>
    double CancellationRate(IEnumerable<OrderLine> orders);
}
