using System.Globalization;
using System.Windows.Data;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using BrushConverter = System.Windows.Media.BrushConverter;

namespace Byteroom;

/// <summary>Gjør en hex-fargestreng ("#1B8A3A") om til en SolidColorBrush.</summary>
public class HexBrushConverter : IValueConverter
{
    private static readonly BrushConverter Inner = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return (Brush)Inner.ConvertFromString(hex)!; }
            catch { /* faller til grå under */ }
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
