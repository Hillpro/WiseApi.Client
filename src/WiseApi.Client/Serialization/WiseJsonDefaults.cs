using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used when (de)serializing Wise request/response payloads.
/// Exposed so consumers can match the client's behaviour when building custom payloads.
/// </summary>
public static class WiseJsonDefaults
{
    /// <summary>The shared read-only options. Safe to reuse across threads.</summary>
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
        options.Converters.Add(new LenientDateTimeOffsetConverter());
        options.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
        options.MakeReadOnly();
        return options;
    }
}
