namespace Biaschtln.Statistics.Models;

/// <summary>
/// Eine einzelne Bestellposition aus einem Biaschtln-CSV-Export. Mehrere Positionen
/// mit derselben <see cref="OrderId"/> bilden eine Bestellung.
/// </summary>
public sealed class OrderLine
{
    /// <summary>CSV: <c>Bestell-ID</c> — ID der Bestellposition.</summary>
    public int OrderLineId { get; set; }

    /// <summary>CSV: <c>Bestellung</c> — gruppiert mehrere Positionen zu einer Bestellung.</summary>
    public int OrderId { get; set; }

    /// <summary>CSV: <c>Status</c> — z. B. "ORDER: CREATED - Bestellung erfasst" oder "... STORNO".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>CSV: <c>Tisch</c> — Tischkennung, z. B. "H10".</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>CSV: <c>Artikel</c> — Artikelname, z. B. "Bier 0,5".</summary>
    public string Article { get; set; } = string.Empty;

    /// <summary>CSV: <c>Artikelfarbe</c> — Hex-Farbcode, z. B. "#fbc02d".</summary>
    public string ArticleColor { get; set; } = string.Empty;

    /// <summary>CSV: <c>Kategorie</c> — "Alk", "Anti" oder "Essen".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>CSV: <c>Preis</c> — Einzelpreis (Komma-Dezimal in der Datei).</summary>
    public decimal Price { get; set; }

    /// <summary>CSV: <c>Ust-Satz</c> — Umsatzsteuersatz in Prozent (meist 0).</summary>
    public decimal VatRate { get; set; }

    /// <summary>CSV: <c>Ust-Schlüssel</c> — Steuerschlüssel (oft NULL).</summary>
    public string? VatKey { get; set; }

    /// <summary>CSV: <c>Kommentar</c> — optionaler Kommentar.</summary>
    public string? Comment { get; set; }

    /// <summary>CSV: <c>Bestellzeitpunkt</c> — Zeitpunkt der Bestellung.</summary>
    public DateTime OrderedAt { get; set; }

    /// <summary>CSV: <c>Benutzer</c> — erfassender Benutzer, z. B. "Kellner 9", "Abholstation".</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>CSV: <c>Bezahlt</c> — WAHR/FALSCH.</summary>
    public bool IsPaid { get; set; }

    /// <summary>CSV: <c>Bezahlter Betrag</c> — gezahlter Betrag (kann fehlen/NULL).</summary>
    public decimal? PaidAmount { get; set; }

    /// <summary>CSV: <c>Zahlungsmethode</c> — cash, card, voucher, ec, other.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>CSV: <c>Zahlungsreferenz</c> — optionale Referenz.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>CSV: <c>Zubereitungsdauer in Sekunden</c> — nur bei Essen gesetzt.</summary>
    public int? PreparationSeconds { get; set; }

    /// <summary>
    /// Name der Quelldatei (z. B. "tag1.csv"). Kein CSV-Feld — wird beim Import gestempelt und
    /// dient dem Datei-Filter (eine Datei = typischerweise ein Veranstaltungstag).
    /// </summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// True, wenn die Position storniert wurde (Status enthält STORNO/CANCELED).
    /// Solche Positionen werden bei der Umsatzberechnung standardmäßig ausgeschlossen.
    /// </summary>
    public bool IsCanceled =>
        Status.Contains("STORNO", StringComparison.OrdinalIgnoreCase) ||
        Status.Contains("CANCELED", StringComparison.OrdinalIgnoreCase);
}
