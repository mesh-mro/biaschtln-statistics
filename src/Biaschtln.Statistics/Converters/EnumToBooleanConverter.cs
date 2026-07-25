using System.Globalization;
using System.Windows.Data;

namespace Biaschtln.Statistics.Converters;

/// <summary>
/// Bindet einen Enum-Wert an <see cref="System.Windows.Controls.RadioButton.IsChecked"/>:
/// True, wenn der gebundene Wert dem als <c>ConverterParameter</c> übergebenen Enum-Wert
/// entspricht. Beim Anwählen wird der Parameter zurückgeschrieben.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is not null ? parameter : Binding.DoNothing;
}
