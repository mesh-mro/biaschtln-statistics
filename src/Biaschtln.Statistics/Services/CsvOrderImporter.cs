using System.Globalization;
using System.IO;
using Biaschtln.Statistics.Models;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Biaschtln.Statistics.Services;

/// <summary>Ergebnis des Ladevorgangs einer einzelnen Datei.</summary>
public sealed record FileLoadResult(string FilePath, bool Success, int RowCount, string? Error);

/// <summary>Gesamtergebnis: alle geladenen Positionen plus Pro-Datei-Status.</summary>
public sealed record ImportResult(IReadOnlyList<OrderLine> Orders, IReadOnlyList<FileLoadResult> Files)
{
    public bool AllSucceeded => Files.All(f => f.Success);
}

/// <summary>
/// CSV-Import mit CsvHelper. Format: Semikolon-getrennt, UTF-8 (mit BOM),
/// Komma-Dezimal, Datum "yyyy-MM-dd HH:mm:ss", Bool "WAHR"/"FALSCH",
/// Null als "NULL" oder leer. Siehe docs/IMPLEMENTATION_PLAN.md §2.
/// </summary>
public sealed class CsvOrderImporter : ICsvOrderImporter
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Benutzer, dessen Zeilen reine Testbestellungen sind und beim Import entfallen.</summary>
    private const string IgnoredUser = "Admin";

    public ImportResult LoadFiles(IEnumerable<string> paths)
    {
        var orders = new List<OrderLine>();
        var files = new List<FileLoadResult>();

        foreach (var path in paths)
        {
            try
            {
                var rows = ReadFile(path);
                orders.AddRange(rows);
                files.Add(new FileLoadResult(path, Success: true, rows.Count, Error: null));
            }
            catch (Exception ex)
            {
                files.Add(new FileLoadResult(path, Success: false, RowCount: 0, ex.Message));
            }
        }

        return new ImportResult(orders, files);
    }

    private static List<OrderLine> ReadFile(string path)
    {
        var config = new CsvConfiguration(GermanCulture)
        {
            Delimiter = ";",
            DetectColumnCountChanges = false,
            HasHeaderRecord = true,
            // Header werden per Name gemappt; unbekannte/fehlende Felder tolerieren wir nicht,
            // damit Formatabweichungen auffallen.
            MissingFieldFound = null,
        };

        // StreamReader erkennt das UTF-8-BOM automatisch (detectEncodingFromByteOrderMarks).
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<OrderLineMap>();

        // Testbestellungen des Admin-Benutzers werden generell ignoriert (schon beim Import).
        // Jede Position wird mit ihrem Dateinamen gestempelt (für den Datei-Filter).
        var fileName = Path.GetFileName(path);
        return csv.GetRecords<OrderLine>()
            .Where(o => !string.Equals(o.User, IgnoredUser, StringComparison.OrdinalIgnoreCase))
            .Select(o =>
            {
                o.SourceFile = fileName;
                return o;
            })
            .ToList();
    }
}

/// <summary>CsvHelper-Mapping CSV-Spalte → <see cref="OrderLine"/>-Property.</summary>
internal sealed class OrderLineMap : ClassMap<OrderLine>
{
    // In der Datei als "NULL" oder leer dargestellte Werte gelten als null.
    private static readonly string[] NullTokens = ["NULL", ""];

    public OrderLineMap()
    {
        // Erste (unbenannte) Index-Spalte wird bewusst nicht gemappt.
        Map(m => m.OrderLineId).Name("Bestell-ID");
        Map(m => m.OrderId).Name("Bestellung");
        Map(m => m.Status).Name("Status");
        Map(m => m.Table).Name("Tisch");
        Map(m => m.Article).Name("Artikel");
        Map(m => m.ArticleColor).Name("Artikelfarbe");
        Map(m => m.Category).Name("Kategorie");
        Map(m => m.Price).Name("Preis");
        Map(m => m.VatRate).Name("Ust-Satz");
        Map(m => m.VatKey).Name("Ust-Schlüssel").TypeConverterOption.NullValues(NullTokens);
        Map(m => m.Comment).Name("Kommentar").TypeConverterOption.NullValues(NullTokens);
        Map(m => m.OrderedAt).Name("Bestellzeitpunkt")
            .TypeConverterOption.Format("yyyy-MM-dd HH:mm:ss")
            .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
        Map(m => m.User).Name("Benutzer");
        Map(m => m.IsPaid).Name("Bezahlt").TypeConverter<GermanBooleanConverter>();
        Map(m => m.PaidAmount).Name("Bezahlter Betrag").TypeConverterOption.NullValues(NullTokens);
        Map(m => m.PaymentMethod).Name("Zahlungsmethode").TypeConverterOption.NullValues(NullTokens);
        Map(m => m.PaymentReference).Name("Zahlungsreferenz").TypeConverterOption.NullValues(NullTokens);
        Map(m => m.PreparationSeconds).Name("Zubereitungsdauer in Sekunden")
            .TypeConverterOption.NullValues(NullTokens);
    }
}

/// <summary>Konvertiert die deutschen Wahrheitswerte "WAHR"/"FALSCH" nach <see cref="bool"/>.</summary>
internal sealed class GermanBooleanConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        => string.Equals(text?.Trim(), "WAHR", StringComparison.OrdinalIgnoreCase);
}
