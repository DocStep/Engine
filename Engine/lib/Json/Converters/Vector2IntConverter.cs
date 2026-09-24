using System.ComponentModel;
using System.Globalization;

namespace Engine;


/// <summary>Parses and formats Vector2Int as "(x, y)" so it works as a JSON dictionary key.</summary>
public class Vector2IntConverter : TypeConverter {
    public override bool CanConvertFrom (ITypeDescriptorContext? context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom (ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is string s) {
            string[] parts = s.Trim().Trim('(', ')').Split(',');
            return new Vector2Int(
                int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo (ITypeDescriptorContext? context, Type? destinationType) {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo (ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
        if (destinationType == typeof(string) && value is Vector2Int v)
            return "(" + v.X + ", " + v.Y + ")";
        return base.ConvertTo(context, culture, value, destinationType);
    }
}