using System.Globalization;
using System.Text.Json;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// Flattens a raw cell value into an export-friendly scalar. Mirrors the frontend
/// exportResult.ts cellValue(): null → empty, primitives pass through, everything
/// else is JSON so nested objects/arrays survive as text.
/// </summary>
internal static class ReportCellFormatter
{
    public static string ToText(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
        sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
            => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => JsonSerializer.Serialize(value),
    };

    /// <summary>True when the value is a number worth keeping numeric in a spreadsheet cell.</summary>
    public static bool TryGetNumber(object? value, out double number)
    {
        switch (value)
        {
            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            default:
                number = 0;
                return false;
        }
    }
}
