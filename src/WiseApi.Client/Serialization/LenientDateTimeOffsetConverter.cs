using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Lenient <see cref="DateTimeOffset"/> converter. Wise mixes several timestamp formats across endpoints:
/// ISO 8601 with <c>Z</c> (<c>2020-05-20T14:43:16.658Z</c>), ISO 8601 without an offset
/// (<c>2023-01-15T10:30:00</c>, assumed UTC), and the Rate endpoint's non-standard
/// <c>+0000</c> form (<c>2018-08-31T10:43:31+0000</c>). The default System.Text.Json parser rejects
/// the last form, hence this converter.
/// </summary>
internal sealed class LenientDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private static readonly string[] AcceptedFormats =
    {
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFzz00",
        "yyyy-MM-ddTHH:mm:sszz00",
    };

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for DateTimeOffset, got {reader.TokenType}.");
        }

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        // Note: we skip STJ's built-in TryGetDateTimeOffset because it assumes *local* time
        // for strings without an offset (e.g. "2023-01-15T10:30:00")
        const DateTimeStyles Styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, Styles, out var value))
        {
            return value;
        }

        // Last-ditch: normalize the Wise-specific compact offset form "+0000" → "+00:00".
        var normalized = NormalizeCompactOffset(text);
        if (DateTimeOffset.TryParseExact(normalized, AcceptedFormats, CultureInfo.InvariantCulture, Styles, out value))
        {
            return value;
        }

        throw new JsonException($"Unsupported date/time format: '{text}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    private static string NormalizeCompactOffset(string text)
    {
        if (text.Length < 5) return text;
        var tail = text.AsSpan(text.Length - 5);
        if ((tail[0] == '+' || tail[0] == '-')
            && char.IsDigit(tail[1]) && char.IsDigit(tail[2])
            && char.IsDigit(tail[3]) && char.IsDigit(tail[4]))
        {
            return string.Concat(text.AsSpan(0, text.Length - 2), ":", tail.Slice(3, 2));
        }

        return text;
    }
}
