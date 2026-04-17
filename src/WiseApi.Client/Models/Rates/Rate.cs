using System.Text.Json.Serialization;

namespace WiseApi.Client.Models.Rates;

/// <summary>Grouping interval for historical rate queries.</summary>
public enum RateGrouping
{
    /// <summary>One point per day.</summary>
    Day,

    /// <summary>One point per hour.</summary>
    Hour,

    /// <summary>One point per minute.</summary>
    Minute,
}

/// <summary>An exchange rate quote at a given point in time.</summary>
public sealed record Rate(
    [property: JsonPropertyName("rate")] decimal Value,
    string Source,
    string Target,
    DateTimeOffset Time);
