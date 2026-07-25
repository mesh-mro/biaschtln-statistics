namespace Biaschtln.Statistics.Services;

/// <summary>
/// Abstrahiert den Datei-Öffnen-Dialog, damit ViewModels ohne direkte WPF-Abhängigkeit
/// (und damit testbar) bleiben.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Zeigt einen Öffnen-Dialog und liefert die gewählten Pfade, oder <c>null</c>, wenn
    /// der Benutzer abbricht.
    /// </summary>
    /// <param name="filter">Dateifilter im Win32-Format (z. B. "CSV|*.csv").</param>
    /// <param name="multiselect">Erlaubt Mehrfachauswahl.</param>
    IReadOnlyList<string>? OpenFiles(string filter, bool multiselect);

    /// <summary>
    /// Zeigt einen Speichern-Dialog und liefert den gewählten Pfad, oder <c>null</c> bei
    /// Abbruch.
    /// </summary>
    /// <param name="filter">Dateifilter im Win32-Format (z. B. "CSV|*.csv").</param>
    /// <param name="defaultFileName">Vorgeschlagener Dateiname.</param>
    string? SaveFile(string filter, string defaultFileName);
}
