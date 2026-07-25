using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="ICsvExporter" />
public sealed class CsvExporter : ICsvExporter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly IFileDialogService _dialog;

    public CsvExporter(IFileDialogService dialog)
    {
        _dialog = dialog;
    }

    public string RenderCsv<T>(IEnumerable<T> rows)
    {
        // Semikolon + de-DE (Komma-Dezimal) — konsistent zum Import-/Excel-Format der App.
        var config = new CsvConfiguration(German) { Delimiter = ";" };
        using var writer = new StringWriter();
        using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteRecords(rows);
        }

        return writer.ToString();
    }

    public void ExportCsv<T>(IEnumerable<T> rows, string suggestedName)
    {
        var path = _dialog.SaveFile("CSV-Datei (*.csv)|*.csv", EnsureExtension(suggestedName, ".csv"));
        if (path is null)
        {
            return;
        }

        // UTF-8 mit BOM, damit Excel Umlaute korrekt erkennt.
        File.WriteAllText(path, RenderCsv(rows), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string EnsureExtension(string name, string extension) =>
        name.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? name : name + extension;
}
