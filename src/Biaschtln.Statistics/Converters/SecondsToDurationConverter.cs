using System.Globalization;
using System.Windows.Data;

namespace Biaschtln.Statistics.Converters;

/// <summary>
/// Formatiert eine Dauer in Sekunden als "m:ss" (bzw. "h:mm:ss" ab einer Stunde).
/// <c>null</c> ergibt einen leeren String (z. B. Getränke ohne Zubereitungsdauer).
/// </summary>
public sealed class SecondsToDurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int seconds)
        {
            return string.Empty;
        }

        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes}:{ts.Seconds:00}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
