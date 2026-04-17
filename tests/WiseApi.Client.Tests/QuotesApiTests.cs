using WiseApi.Client.Models.Quotes;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class QuotesApiTests
{
    private const string QuoteBody = """
    {
      "id": "11144c35-9fe8-4c32-b7fd-d05c2a7734bf",
      "sourceCurrency": "GBP",
      "targetCurrency": "USD",
      "sourceAmount": 100,
      "targetAmount": 129.24,
      "payOut": "BALANCE",
      "rate": 1.30445,
      "createdTime": "2019-04-05T13:18:58Z",
      "user": 55,
      "profile": 101,
      "rateType": "FIXED",
      "providedAmountType": "SOURCE",
      "paymentOptions": [],
      "status": "PENDING",
      "expirationTime": "2019-04-05T13:48:58Z"
    }
    """;

    [Fact]
    public async Task CreateForBalanceConversionAsync_sets_payout_balance_and_posts_to_v3_quotes()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(QuoteBody);
        var api = new QuotesApi(http);

        var quote = await api.CreateForBalanceConversionAsync(
            profileId: 101,
            sourceCurrency: "GBP",
            targetCurrency: "USD",
            sourceAmount: 100m,
            CancellationToken.None);

        Assert.Equal(Guid.Parse("11144c35-9fe8-4c32-b7fd-d05c2a7734bf"), quote.Id);
        Assert.Equal(QuoteStatus.Pending, quote.Status);
        Assert.Equal(RateType.Fixed, quote.RateType);
        Assert.Equal("BALANCE", quote.PayOut);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v3/profiles/101/quotes", request.Uri.AbsolutePath);
        Assert.Contains("\"payOut\":\"BALANCE\"", request.Body);
        Assert.Contains("\"sourceCurrency\":\"GBP\"", request.Body);
        Assert.Contains("\"targetCurrency\":\"USD\"", request.Body);
        Assert.Contains("\"sourceAmount\":100", request.Body);
    }

    [Fact]
    public async Task GetAsync_hits_quote_by_id()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(QuoteBody);
        var api = new QuotesApi(http);
        var id = Guid.Parse("11144c35-9fe8-4c32-b7fd-d05c2a7734bf");

        await api.GetAsync(profileId: 101, id, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v3/profiles/101/quotes/11144c35-9fe8-4c32-b7fd-d05c2a7734bf", request.Uri.AbsolutePath);
    }
}
