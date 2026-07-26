using System.IO;
using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IOrderDataService" />
public sealed class OrderDataService : IOrderDataService
{
    private readonly ICsvOrderImporter _importer;

    // Ein Segment je erfolgreich geladener Datei — hält deren Positionen, damit einzelne
    // Dateien wieder entfernt werden können.
    private readonly List<Segment> _segments = [];
    private IReadOnlyList<OrderLine> _orders = [];

    public OrderDataService(ICsvOrderImporter importer)
    {
        _importer = importer;
    }

    public IReadOnlyList<OrderLine> Orders => _orders;

    public IReadOnlyList<LoadedFileInfo> LoadedFiles => _segments.Select(s => s.Info).ToList();

    public event EventHandler? OrdersChanged;

    public ImportResult LoadFiles(IEnumerable<string> paths, bool append = false)
    {
        if (!append)
        {
            _segments.Clear();
        }

        var fileResults = new List<FileLoadResult>();
        foreach (var path in paths)
        {
            // Pro Pfad einzeln importieren, um die Positionen dieser Datei separat zu erhalten.
            // Der Importer fängt Fehler bereits pro Datei ab und liefert genau ein FileLoadResult.
            var single = _importer.LoadFiles([path]);
            var file = single.Files[0];
            fileResults.Add(file);

            if (!file.Success)
            {
                continue;
            }

            // Bereits geladene Datei aktualisieren statt duplizieren.
            _segments.RemoveAll(s => PathEquals(s.Info.FilePath, path));
            _segments.Add(new Segment(
                new LoadedFileInfo(path, Path.GetFileName(path), single.Orders.Count),
                single.Orders));
        }

        Rebuild();
        OrdersChanged?.Invoke(this, EventArgs.Empty);
        return new ImportResult(_orders, fileResults);
    }

    public void RemoveFile(string filePath)
    {
        var removed = _segments.RemoveAll(s => PathEquals(s.Info.FilePath, filePath));
        if (removed == 0)
        {
            return;
        }

        Rebuild();
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_segments.Count == 0 && _orders.Count == 0)
        {
            return;
        }

        _segments.Clear();
        _orders = [];
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Rebuild() => _orders = _segments.SelectMany(s => s.Rows).ToList();

    private static bool PathEquals(string a, string b) =>
        string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    private sealed record Segment(LoadedFileInfo Info, IReadOnlyList<OrderLine> Rows);
}
