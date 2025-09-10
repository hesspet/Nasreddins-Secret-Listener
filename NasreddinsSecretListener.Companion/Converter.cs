using System.Globalization;
using Xamarin.Google.ErrorProne.Annotations;

namespace NasreddinsSecretListener.Companion;

public class BoolToColorConverterBluetooth : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? Colors.Blue : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class NslBadgeTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? "NSL" : "Other";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value != null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class StatusTextColorRemoveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = (string)value! ?? string.Empty;
        if (text.IndexOf('|') >= 0)
        {
            var splitted = text.Split('|');
            text = splitted[1];
        }
        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
///     Erwarte einen String wie "Green|Connected" und liefere die Farbe zurück. Wenn der Delimiter
///     fehlt, wir Grau zurückgegeben.
/// </summary>
public class StatusTextToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.ToLowerInvariant() ?? string.Empty;
        if (s.IndexOf('|') >= 0)
        {
            var splitted = s.Split('|');
            var colorname = splitted[0];
            if (Color.TryParse(colorname, out var color))
            {
                return color;
            }
        }
        return Colors.Gray; // Fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte b)
            return "";
        return b switch
        {
            0x00 => "None",
            0x01 => "Early",
            0x02 => "Confirmed",
            _ => $"0x{b:X2}"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
