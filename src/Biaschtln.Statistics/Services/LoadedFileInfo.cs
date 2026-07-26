namespace Biaschtln.Statistics.Services;

/// <summary>
/// Beschreibt eine aktuell geladene Datei für die Datei-Übersicht: Pfad, Anzeigename
/// und Anzahl der daraus stammenden Positionen.
/// </summary>
public sealed record LoadedFileInfo(string FilePath, string FileName, int RowCount)
{
    /// <summary>Anzeigetext für Menü/Übersicht, z. B. "tag1.csv — 1924 Positionen".</summary>
    public string Display => $"{FileName} — {RowCount} Positionen";
}
