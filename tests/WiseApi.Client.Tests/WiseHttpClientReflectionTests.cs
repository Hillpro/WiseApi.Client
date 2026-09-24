using System.Text.Json;
using WiseApi.Client.Models.Balances;
using WiseApi.Client.Serialization;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

/// <summary>
/// The library resolves its own types through source-generated metadata, but the generic
/// <c>WiseHttpClient</c> overloads (no <c>JsonTypeInfo</c>) and <c>WiseJsonDefaults.Options</c> must
/// keep working for callers' own types — e.g. an endpoint the library doesn't wrap yet — with the
/// same conventions as before: camelCase, SCREAMING_SNAKE enums, lenient Wise timestamps.
/// </summary>
public sealed class WiseHttpClientReflectionTests
{
    private enum Priority
    {
        FastTrack,
    }

    private sealed record CustomRequest(string Currency, Priority Priority);

    private sealed record CustomResponse(long Id, BalanceType Type, DateTimeOffset CreationTime);

    [Fact]
    public async Task Generic_overloads_handle_caller_defined_types_with_library_conventions()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"id":7,"type":"SOMETHING_NEW","creationTime":"2018-08-31T10:43:31+0000"}""");

        var response = await http.PostJsonAsync<CustomRequest, CustomResponse>(
            "/v1/custom",
            new CustomRequest("EUR", Priority.FastTrack),
            CancellationToken.None);

        Assert.Equal(7, response.Id);
        Assert.Equal(BalanceType.Unknown, response.Type);
        Assert.Equal(new DateTimeOffset(2018, 08, 31, 10, 43, 31, TimeSpan.Zero), response.CreationTime);
        Assert.Equal("""{"currency":"EUR","priority":"FAST_TRACK"}""", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public void Caller_defined_enums_stay_strict()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Priority>("\"NOPE\"", WiseJsonDefaults.Options));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Priority>("0", WiseJsonDefaults.Options));
    }
}
