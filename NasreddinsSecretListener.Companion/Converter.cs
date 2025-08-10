using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace NasreddinsSecretListener.Companion
;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b && b ? Colors.SeaGreen : Colors.Gray;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class NslBadgeTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b && b ? "NSL" : "Other";

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value != null;

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not byte b) return "";
        return b switch { 0x00 => "None", 0x01 => "Early", 0x02 => "Confirmed", _ => $"0x{b:X2}" };
    }

    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}