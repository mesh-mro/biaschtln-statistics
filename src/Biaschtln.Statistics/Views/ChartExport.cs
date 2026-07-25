using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Biaschtln.Statistics.Views;

/// <summary>
/// View-seitiger PNG-Export eines WPF-Elements (z. B. der Diagrammbereich einer Seite).
/// Reine Darstellungslogik — bewusst nicht im ViewModel, damit die ViewModels WPF-frei und
/// testbar bleiben.
/// </summary>
internal static class ChartExport
{
    /// <summary>
    /// Fragt per Speichern-Dialog einen Pfad ab und rendert <paramref name="element"/> als PNG
    /// (in Bildschirmauflösung inkl. DPI-Skalierung). Bei Abbruch passiert nichts.
    /// </summary>
    public static void SaveElementAsPng(FrameworkElement element, string suggestedName)
    {
        var width = element.ActualWidth;
        var height = element.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG-Bild (*.png)|*.png",
            FileName = suggestedName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? suggestedName
                : suggestedName + ".png",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(width * dpi.DpiScaleX),
            (int)Math.Ceiling(height * dpi.DpiScaleY),
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        bitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(dialog.FileName);
        encoder.Save(stream);
    }
}
