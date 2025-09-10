using System.Globalization;

namespace NasreddinsSecretListener.Companion
{
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
}
