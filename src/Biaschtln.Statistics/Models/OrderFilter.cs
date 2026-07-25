namespace Biaschtln.Statistics.Models;

/// <summary>Filtert nach Bezahlstatus.</summary>
public enum PaidFilter
{
    /// <summary>Bezahlte und unbezahlte Positionen.</summary>
    All,

    /// <summary>Nur bezahlte Positionen.</summary>
    OnlyPaid,

    /// <summary>Nur unbezahlte Positionen.</summary>
    OnlyUnpaid,
}

/// <summary>
/// Filterkriterien für Bestellpositionen. Eine leere Mengen-Auswahl (z. B. keine
/// Kategorie ausgewählt) bedeutet "kein Filter für dieses Kriterium" (alles erlaubt).
/// </summary>
public sealed class OrderFilter
{
    /// <summary>Untere Zeitgrenze (inklusive), oder null für unbegrenzt.</summary>
    public DateTime? From { get; set; }

    /// <summary>Obere Zeitgrenze (inklusive), oder null für unbegrenzt.</summary>
    public DateTime? To { get; set; }

    /// <summary>Erlaubte Kategorien (leer = alle).</summary>
    public ISet<string> Categories { get; } = new HashSet<string>();

    /// <summary>Erlaubte Artikel (leer = alle).</summary>
    public ISet<string> Articles { get; } = new HashSet<string>();

    /// <summary>Erlaubte Tische (leer = alle).</summary>
    public ISet<string> Tables { get; } = new HashSet<string>();

    /// <summary>Erlaubte Benutzer (leer = alle).</summary>
    public ISet<string> Users { get; } = new HashSet<string>();

    /// <summary>Erlaubte Zahlungsmethoden (leer = alle).</summary>
    public ISet<string> PaymentMethods { get; } = new HashSet<string>();

    /// <summary>
    /// Benutzer, dessen Bestellungen als Abholung gelten (kein Tischplatz). Solche Positionen
    /// erfüllen keinen Tisch-Filter. Null/leer = keine Abholstation.
    /// </summary>
    public string? PickupUser { get; set; }

    /// <summary>Wenn false (Standard), werden stornierte Positionen ausgeschlossen.</summary>
    public bool IncludeCanceled { get; set; }

    /// <summary>Filter nach Bezahlstatus.</summary>
    public PaidFilter Paid { get; set; } = PaidFilter.All;
}
