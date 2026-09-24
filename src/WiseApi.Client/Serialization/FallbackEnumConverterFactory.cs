using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Reflection-path companion to <see cref="LenientEnumConverter{TEnum}"/>: serializes enums that
/// carry no <see cref="JsonConverterAttribute"/> of their own — i.e. consumer-defined enums passing
/// through <see cref="WiseJsonDefaults.Options"/> — as strict <c>SCREAMING_SNAKE</c> strings, as the
/// library did before it moved to source-generated JSON.
/// </summary>
/// <remarks>
/// Wise's own enums are excluded on purpose: a converter in <see cref="JsonSerializerOptions.Converters"/>
/// takes precedence over a <c>[JsonConverter]</c> on the type, so an unfiltered factory here would
/// silently switch them back to strict parsing.
/// </remarks>
[RequiresUnreferencedCode(WiseJsonDefaults.ReflectionMessage)]
[RequiresDynamicCode(WiseJsonDefaults.ReflectionMessage)]
internal sealed class FallbackEnumConverterFactory : JsonConverterFactory
{
    private readonly JsonStringEnumConverter _inner = new(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false);

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum && !typeToConvert.IsDefined(typeof(JsonConverterAttribute), inherit: false);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => _inner.CreateConverter(typeToConvert, options);
}
