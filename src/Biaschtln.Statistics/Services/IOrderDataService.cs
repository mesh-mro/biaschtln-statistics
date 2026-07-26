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

    /// <summary>Übersicht der aktuell geladenen Dateien (Name + Positionsanzahl).</summary>
    IReadOnlyList<LoadedFileInfo> LoadedFiles { get; }

    /// <summary>Wird ausgelöst, wenn sich <see cref="Orders"/> geändert hat.</summary>
    event EventHandler? OrdersChanged;

    /// <summary>
    /// Lädt die angegebenen CSV-Dateien und gibt den Import-Status (inkl. Pro-Datei-Fehler)
    /// zurück. Bei <paramref name="append"/> = <c>false</c> (Standard) wird der bisherige
    /// Datenbestand ersetzt; bei <c>true</c> werden die Dateien an den bestehenden Bestand
    /// angehängt. Eine bereits geladene Datei wird beim Anhängen aktualisiert statt dupliziert.
    /// </summary>
    ImportResult LoadFiles(IEnumerable<string> paths, bool append = false);

    /// <summary>Entfernt die Positionen der angegebenen Datei aus dem Bestand.</summary>
    void RemoveFile(string filePath);

    /// <summary>Verwirft alle geladenen Positionen.</summary>
    void Clear();
}
