namespace Biaschtln.Statistics.Models;

/// <summary>Sortierkriterium für die Top-Artikel-Auswertung.</summary>
public enum ArticleRanking
{
    /// <summary>Nach Umsatz (Summe Preis).</summary>
    Revenue,

    /// <summary>Nach verkaufter Stückzahl (Anzahl Positionen).</summary>
    Quantity,
}

/// <summary>Zeitliche Gruppierung für Umsatz-über-Zeit-Auswertungen.</summary>
public enum TimeBucket
{
    Hour,
    Day,
}

/// <summary>Umsatz und Anzahl je Kategorie (Alk/Anti/Essen).</summary>
public sealed record CategorySummary(string Category, decimal Revenue, int Count);

/// <summary>Umsatz und Stückzahl je Artikel.</summary>
public sealed record ArticleSummary(string Article, decimal Revenue, int Quantity);

/// <summary>Zubereitungsdauer-Statistik je Artikel (nur Positionen mit gesetzter Dauer).</summary>
public sealed record PreparationSummary(
    string Article,
    double AverageSeconds,
    double MedianSeconds,
    int MaxSeconds,
    int Count);

/// <summary>Umsatz und Anzahl je Benutzer (Kellner/Abholstation).</summary>
public sealed record UserSummary(string User, decimal Revenue, int Count);

/// <summary>Umsatz und Anzahl je Zahlungsmethode.</summary>
public sealed record PaymentMethodSummary(string PaymentMethod, decimal Revenue, int Count);

/// <summary>Umsatz und Anzahl je Tisch.</summary>
public sealed record TableSummary(string Table, decimal Revenue, int Count);

/// <summary>Umsatz und Anzahl je Zeitintervall.</summary>
public sealed record TimeBucketSummary(DateTime BucketStart, decimal Revenue, int Count);
