using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <summary>
/// Hält die aktuell geladenen Bestellpositionen im Speicher und meldet Änderungen.
/// Zentrale Datenquelle für ViewModels (Filter, Diagramme).
/// </summary>
public interface IOrderDataService
{
    /// <summary>Die aktuell geladenen Positionen (über alle geladenen Dateien).</summary>
    IReadOnlyList<OrderLine> Orders { get; }

    /// <summary>Wird ausgelöst, wenn sich <see cref="Orders"/> geändert hat.</summary>
    event EventHandler? OrdersChanged;

    /// <summary>
    /// Lädt die angegebenen CSV-Dateien und ersetzt den aktuellen Datenbestand durch
    /// deren konkatenierte Positionen. Gibt den Import-Status (inkl. Pro-Datei-Fehler) zurück.
    /// </summary>
    ImportResult LoadFiles(IEnumerable<string> paths);

    /// <summary>Verwirft alle geladenen Positionen.</summary>
    void Clear();
}
