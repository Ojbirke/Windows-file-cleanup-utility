using System.Globalization;
using System.Windows.Data;

namespace Byteroom;

/// <summary>Gjør om et antall bytes (long) til lesbar tekst, f.eks. "1,4 GB".</summary>
public class SizeConverter : IValueConverter
{
    public static string Format(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        string num = unit == 0 ? size.ToString("0", CultureInfo.CurrentCulture)
                               : size.ToString("0.0", CultureInfo.CurrentCulture);
        return $"{num} {units[unit]}";
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is long l ? Format(l) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
