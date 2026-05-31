namespace Biaschtln.Statistics.Services;

/// <summary>Liest Biaschtln-CSV-Exporte ein.</summary>
public interface ICsvOrderImporter
{
    /// <summary>
    /// Lädt mehrere CSV-Dateien und konkateniert deren Bestellpositionen. Fehler einer
    /// einzelnen Datei brechen den Vorgang nicht ab, sondern werden im Ergebnis gemeldet.
    /// </summary>
    ImportResult LoadFiles(IEnumerable<string> paths);
}
