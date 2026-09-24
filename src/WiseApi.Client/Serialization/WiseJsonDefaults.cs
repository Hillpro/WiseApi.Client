using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used when (de)serializing Wise request/response payloads.
/// Exposed so consumers can match the client's behaviour when building custom payloads.
/// </summary>
public static class WiseJsonDefaults
{
    internal const string ReflectionMessage =
        "Serializes arbitrary types through reflection. Use an overload taking a JsonTypeInfo<T> for trimming / Native AOT.";

    private static JsonSerializerOptions? s_options;

    /// <summary>
    /// The client's serializer settings, able to handle any type. Safe to reuse across threads.
    /// </summary>
    /// <remarks>
    /// Library types resolve through source-generated metadata; any other type falls back to
    /// reflection, which is why this property is not trimming / Native AOT safe. Under AOT, build
    /// a <c>JsonSerializerContext</c> for your own types instead.
    /// </remarks>
    public static JsonSerializerOptions Options
    {
        [RequiresUnreferencedCode(ReflectionMessage)]
        [RequiresDynamicCode(ReflectionMessage)]
        get => s_options ?? InitializeOptions();
    }

    // CompareExchange publishes the instance with a full fence, and guarantees every caller sees
    // the same one even when two threads race on first access.
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private static JsonSerializerOptions InitializeOptions()
    {
        var created = CreateReflectionOptions();
        return Interlocked.CompareExchange(ref s_options, created, null) ?? created;
    }

    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private static JsonSerializerOptions CreateReflectionOptions()
    {
        var options = new JsonSerializerOptions(WiseJsonContext.Default.Options)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(WiseJsonContext.Default, new DefaultJsonTypeInfoResolver()),
        };

        // Consumer-defined enums keep serializing as SCREAMING_SNAKE strings; without this they
        // would fall back to integers, which Wise rejects.
        options.Converters.Add(new FallbackEnumConverterFactory());
        options.MakeReadOnly();
        return options;
    }
}
