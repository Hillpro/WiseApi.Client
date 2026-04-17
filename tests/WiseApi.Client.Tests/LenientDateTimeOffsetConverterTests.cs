using System.Text.Json;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Tests;

/// <summary>
/// Verifies the three Wise timestamp shapes all deserialize to the same UTC instant.
/// Documents the timestamp formats the converter must tolerate so that, if a future
/// .NET release changes <c>DateTimeOffset.TryParse</c>'s behaviour on the compact
/// <c>+0000</c> offset, this file will fail loudly instead of the Rates endpoint
/// breaking in production.
/// </summary>
public sealed class LenientDateTimeOffsetConverterTests
{
    private static readonly DateTimeOffset Expected =
        new(2018, 08, 31, 10, 43, 31, TimeSpan.Zero);

    [Theory]
    [InlineData("\"2018-08-31T10:43:31Z\"", "ISO-Z")]
    [InlineData("\"2018-08-31T10:43:31.000Z\"", "ISO-Z with fractional seconds")]
    [InlineData("\"2018-08-31T10:43:31+00:00\"", "ISO colon-offset")]
    [InlineData("\"2018-08-31T10:43:31+0000\"", "Wise compact offset (seen on /v1/rates)")]
    [InlineData("\"2018-08-31T10:43:31\"", "Naive (no offset, assumed UTC)")]
    public void Reads_all_accepted_timestamp_shapes(string json, string description)
    {
        _ = description;
        var value = JsonSerializer.Deserialize<DateTimeOffset>(json, WiseJsonDefaults.Options);
        Assert.Equal(Expected, value);
    }

    [Fact]
    public void Round_trip_preserves_value()
    {
        var json = JsonSerializer.Serialize(Expected, WiseJsonDefaults.Options);
        var parsed = JsonSerializer.Deserialize<DateTimeOffset>(json, WiseJsonDefaults.Options);
        Assert.Equal(Expected, parsed);
        Assert.Contains("2018-08-31T10:43:31", json);
    }

    [Fact]
    public void Rejects_plainly_invalid_strings()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>("\"not-a-date\"", WiseJsonDefaults.Options));
    }
}
