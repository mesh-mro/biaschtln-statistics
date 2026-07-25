using SkiaSharp;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Zentrale Diagramm-Farben. Kategoriale Werte stammen aus einer auf Farbfehlsichtigkeit
/// geprüften Palette (Slots 1–3, all-pairs-sicher). Die Kategorie-Farbe folgt der
/// Kategorie (Entität), nicht dem Rang im Diagramm.
/// </summary>
internal static class ChartPalette
{
    /// <summary>Kategoriale Slot-1-Farbe (Blau) — auch für Einzelserien-Balken.</summary>
    public static readonly SKColor SeriesBlue = new(0x2a, 0x78, 0xd6);

    /// <summary>Kategoriale Slot-2-Farbe (Orange).</summary>
    public static readonly SKColor SeriesOrange = new(0xeb, 0x68, 0x34);

    /// <summary>Kategoriale Slot-3-Farbe (Aqua).</summary>
    public static readonly SKColor SeriesAqua = new(0x1b, 0xaf, 0x7a);

    /// <summary>Achsen-/Label-Grau.</summary>
    public static readonly SKColor Muted = new(0x89, 0x87, 0x81);

    /// <summary>Gitternetz-Haarlinie.</summary>
    public static readonly SKColor Grid = new(0xe1, 0xe0, 0xd9);

    /// <summary>Primäre Textfarbe.</summary>
    public static readonly SKColor Ink = new(0x0b, 0x0b, 0x0b);

    /// <summary>Textfarbe auf gefüllten Marken (weiß).</summary>
    public static readonly SKColor OnFill = new(0xff, 0xff, 0xff);

    /// <summary>Farbe für eine Kategorie; unbekannte Kategorien werden neutral grau.</summary>
    public static SKColor Category(string category) => category switch
    {
        "Alk" => SeriesBlue,
        "Anti" => SeriesAqua,
        "Essen" => SeriesOrange,
        _ => Muted,
    };

    /// <summary>
    /// Kategoriale Slot-Reihenfolge (auf Farbfehlsichtigkeit geprüfte Palette). Wird nur mit
    /// sekundärer Kodierung (Legende + Datenlabels) über drei Serien hinaus verwendet.
    /// </summary>
    public static readonly SKColor[] Categorical =
    [
        SeriesBlue,                 // 1 Blau
        SeriesOrange,               // 2 Orange
        SeriesAqua,                 // 3 Aqua
        new(0xed, 0xa1, 0x00),      // 4 Gelb
        new(0xe8, 0x7b, 0xa4),      // 5 Magenta
        new(0x00, 0x83, 0x00),      // 6 Grün
        new(0x4a, 0x3a, 0xa7),      // 7 Violett
        new(0xe3, 0x49, 0x48),      // 8 Rot
    ];

    /// <summary>Slot-Farbe (mit Umlauf) für dynamische kategoriale Zuordnungen.</summary>
    public static SKColor Slot(int index) => Categorical[index % Categorical.Length];
}
