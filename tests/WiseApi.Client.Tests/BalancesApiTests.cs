using WiseApi.Client.Models.Balances;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class BalancesApiTests
{
    private const string BalanceListBody = """
    [
      {
        "id": 200001,
        "currency": "EUR",
        "type": "STANDARD",
        "investmentState": "NOT_INVESTED",
        "amount": {"value": 310.86, "currency": "EUR"},
        "reservedAmount": {"value": 0, "currency": "EUR"},
        "cashAmount": {"value": 310.86, "currency": "EUR"},
        "totalWorth": {"value": 310.86, "currency": "EUR"},
        "creationTime": "2020-05-20T14:43:16.658Z",
        "modificationTime": "2020-05-20T14:43:16.658Z",
        "visible": true
      }
    ]
    """;

    [Fact]
    public async Task ListAsync_defaults_to_STANDARD_type()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(BalanceListBody);
        var api = new BalancesApi(http);

        var balances = await api.ListAsync(profileId: 101, types: null, CancellationToken.None);

        var balance = Assert.Single(balances);
        Assert.Equal(BalanceType.Standard, balance.Type);
        Assert.Equal(310.86m, balance.Amount.Value);
        Assert.Equal("EUR", balance.Amount.Currency);
        Assert.Equal(InvestmentState.NotInvested, balance.InvestmentState);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v4/profiles/101/balances", request.Uri.AbsolutePath);
        Assert.Equal("types=STANDARD", request.Uri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task ListAsync_comma_separates_requested_types()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("[]");
        var api = new BalancesApi(http);

        await api.ListAsync(42, [BalanceType.Standard, BalanceType.Savings], CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("types=STANDARD,SAVINGS", request.Uri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task CreateAsync_sends_idempotency_header()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"id":1,"currency":"GBP","type":"STANDARD","amount":{"value":0,"currency":"GBP"}}""");
        var api = new BalancesApi(http);
        var idempotencyKey = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await api.CreateAsync(77, new CreateBalanceRequest("GBP", BalanceType.Standard), idempotencyKey, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v4/profiles/77/balances", request.Uri.AbsolutePath);
        Assert.Equal(idempotencyKey.ToString("D"), request.Headers["X-idempotence-uuid"]);
        Assert.Contains("\"currency\":\"GBP\"", request.Body);
        Assert.Contains("\"type\":\"STANDARD\"", request.Body);
    }

    [Fact]
    public async Task CreateAsync_requires_name_for_savings_balance()
    {
        var (http, _) = TestHost.CreateHttpClient();
        var api = new BalancesApi(http);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            api.CreateAsync(1, new CreateBalanceRequest("EUR", BalanceType.Savings), idempotencyKey: null, CancellationToken.None));
    }
}
