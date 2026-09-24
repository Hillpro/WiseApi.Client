using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Forward-compatible enum converter. Maps C# members to Wise's <c>SCREAMING_SNAKE</c> spelling
/// (<c>BalanceType.Standard</c> ↔ <c>"STANDARD"</c>) and, when <typeparamref name="TEnum"/> declares
/// a member named <c>Unknown</c>, reads any unrecognised value as that member instead of throwing.
/// </summary>
/// <remarks>
/// Wise adds enum values without bumping the endpoint version. With a strict converter a new
/// value (e.g. a new balance-movement <c>state</c>) would fail deserialization of a response to a
/// request that already succeeded — for money-moving POSTs, a caller retrying on that exception
/// would repeat the operation. Enums without an <c>Unknown</c> member keep the strict behaviour.
/// <para>
/// Applied per enum with <c>[JsonConverter(typeof(LenientEnumConverter&lt;T&gt;))]</c> rather than
/// through a <see cref="JsonConverterFactory"/>: the source generator instantiates it statically,
/// so no <c>MakeGenericType</c> is needed under Native AOT. It is public so that consumers' own
/// <see cref="JsonSerializerContext"/>s can serialize types containing Wise enums — the generated
/// code in their assembly must be able to construct it.
/// </para>
/// </remarks>
/// <typeparam name="TEnum">The Wise enum to convert.</typeparam>
public sealed class LenientEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly FrozenDictionary<string, TEnum> _byName;
    private readonly FrozenDictionary<TEnum, string> _byValue;
    private readonly TEnum? _fallback;

    /// <summary>Create a converter for <typeparamref name="TEnum"/>.</summary>
    public LenientEnumConverter()
    {
        var values = Enum.GetValues<TEnum>();
        _byValue = values.ToFrozenDictionary(v => v, v => JsonNamingPolicy.SnakeCaseUpper.ConvertName(v.ToString()));
        _byName = values.ToFrozenDictionary(v => _byValue[v], v => v, StringComparer.OrdinalIgnoreCase);
        _fallback = Enum.TryParse<TEnum>("Unknown", ignoreCase: false, out var unknown) ? unknown : null;
    }

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // A null on a non-nullable enum means the same as an omitted field: Unknown. (Nullable
        // enums never get here — System.Text.Json maps null to null before calling the converter.)
        if (reader.TokenType == JsonTokenType.Null && _fallback is { } unknown)
        {
            return unknown;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for {typeof(TEnum).Name}, got {reader.TokenType}.");
        }

        var text = reader.GetString();
        if (text is not null && _byName.TryGetValue(text, out var value))
        {
            return value;
        }

        return _fallback ?? throw new JsonException($"Unsupported {typeof(TEnum).Name} value: '{text}'.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (!_byValue.TryGetValue(value, out var name))
        {
            throw new JsonException($"Undefined {typeof(TEnum).Name} value: {value}.");
        }

        writer.WriteStringValue(name);
    }
}
