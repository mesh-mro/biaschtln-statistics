namespace Biaschtln.Statistics.Services;

/// <summary>Exportiert Aggregat-Tabellen (Zeilen-Records) als CSV.</summary>
public interface ICsvExporter
{
    /// <summary>Serialisiert die Zeilen als CSV-Text (Semikolon, de-DE, Komma-Dezimal).</summary>
    string RenderCsv<T>(IEnumerable<T> rows);

    /// <summary>
    /// Fragt per Speichern-Dialog einen Pfad ab und schreibt die Zeilen als CSV (UTF-8 mit
    /// BOM, Excel-freundlich). Bei Abbruch passiert nichts.
    /// </summary>
    void ExportCsv<T>(IEnumerable<T> rows, string suggestedName);
}
