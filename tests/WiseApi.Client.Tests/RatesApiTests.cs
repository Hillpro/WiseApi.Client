using WiseApi.Client.Models.Rates;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class RatesApiTests
{
    [Fact]
    public async Task GetLatestAsync_parses_unusual_offset_format()
    {
        const string body = """
        [
          {"rate": 1.166, "source": "EUR", "target": "USD", "time": "2018-08-31T10:43:31+0000"}
        ]
        """;
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body);
        var api = new RatesApi(http);

        var rate = await api.GetLatestAsync("EUR", "USD", CancellationToken.None);

        Assert.NotNull(rate);
        Assert.Equal(1.166m, rate!.Value);
        Assert.Equal("EUR", rate.Source);
        Assert.Equal("USD", rate.Target);
        Assert.Equal(DateTimeOffset.Parse("2018-08-31T10:43:31Z", System.Globalization.CultureInfo.InvariantCulture), rate.Time);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/rates", request.Uri.AbsolutePath);
        Assert.Contains("source=EUR", request.Uri.Query);
        Assert.Contains("target=USD", request.Uri.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_builds_grouped_query()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("[]");
        var api = new RatesApi(http);

        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);
        await api.GetHistoryAsync("EUR", "USD", from, until, RateGrouping.Day, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/rates", request.Uri.AbsolutePath);
        Assert.Contains("source=EUR", request.Uri.Query);
        Assert.Contains("target=USD", request.Uri.Query);
        Assert.Contains("group=day", request.Uri.Query);
        Assert.Contains("from=2024-01-01T00%3A00%3A00", request.Uri.Query);
        Assert.Contains("to=2024-01-31T00%3A00%3A00", request.Uri.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_rejects_inverted_interval()
    {
        var (http, _) = TestHost.CreateHttpClient();
        var api = new RatesApi(http);
        var a = DateTimeOffset.UtcNow;
        var b = a.AddDays(-1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            api.GetHistoryAsync("EUR", "USD", a, b, RateGrouping.Day, CancellationToken.None));
    }
}
