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

public class StatusToBackgroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.ToLowerInvariant() ?? string.Empty;

        // Reihenfolge: Fehler > Verbunden/Lauschen > Scanne > Bereit/Idle > Default
        if (s.Contains("fehler") || s.Contains("error"))
            return Colors.IndianRed;
        if (s.Contains("verbunden") || s.Contains("connected") || s.Contains("lausche") || s.Contains("listening"))
            return Colors.SeaGreen;
        if (s.Contains("scan"))
            return Colors.Gold;
        if (s.Contains("bereit") || s.Contains("idle") || s.Contains("warte"))
            return Colors.SlateGray;

        return Colors.Gray; // Fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToForegroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Ermittele die Hintergrundfarbe über den obigen Converter …
        var bgObj = new StatusToBackgroundColorConverter().Convert(value, typeof(Color), parameter, culture);
        var bg = bgObj is Color c ? c : Colors.Gray;

        // Luminanz-basierte Kontrastwahl: dunkel -> Weiß, hell -> Schwarz Rec. 709 Gewichtung
        double luminance = 0.2126 * bg.Red + 0.7152 * bg.Green + 0.0722 * bg.Blue;
        return luminance < 0.6 ? Colors.White : Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
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
