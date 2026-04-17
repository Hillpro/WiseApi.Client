using WiseApi.Client.Models;
using WiseApi.Client.Models.Balances;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class BalanceMovementsApiTests
{
    private const string ConversionResponse = """
    {
      "id": 30000001,
      "type": "CONVERSION",
      "state": "COMPLETED",
      "balancesAfter": [{"id": 1, "value": 10000594.71, "currency": "GBP"}],
      "creationTime": "2017-11-21T09:55:49.275Z",
      "sourceAmount": {"value": 113.48, "currency": "EUR"},
      "targetAmount": {"value": 100, "currency": "GBP"},
      "rate": 0.88558,
      "feeAmounts": [{"value": 0.56, "currency": "EUR"}]
    }
    """;

    [Fact]
    public async Task ConvertAsync_posts_quote_id_only()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(ConversionResponse);
        var api = new BalanceMovementsApi(http);
        var quoteId = Guid.Parse("11144c35-9fe8-4c32-b7fd-d05c2a7734bf");

        var result = await api.ConvertAsync(profileId: 101, quoteId, idempotencyKey: null, CancellationToken.None);

        Assert.Equal(BalanceMovementType.Conversion, result.Type);
        Assert.Equal(BalanceMovementState.Completed, result.State);
        Assert.Equal(100m, result.TargetAmount?.Value);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v2/profiles/101/balance-movements", request.Uri.AbsolutePath);
        Assert.Contains("\"quoteId\":\"11144c35-9fe8-4c32-b7fd-d05c2a7734bf\"", request.Body);
        Assert.DoesNotContain("\"sourceBalanceId\"", request.Body);
        Assert.DoesNotContain("\"amount\"", request.Body);
        Assert.True(request.Headers.ContainsKey("X-idempotence-uuid"));
    }

    [Fact]
    public async Task MoveAsync_posts_balances_and_amount_no_quote()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"id":1,"type":"DEPOSIT","state":"COMPLETED"}""");
        var api = new BalanceMovementsApi(http);

        await api.MoveAsync(
            profileId: 5,
            sourceBalanceId: 10,
            targetBalanceId: 20,
            amount: new Money(50m, "EUR"),
            idempotencyKey: null,
            CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("\"sourceBalanceId\":10", request.Body);
        Assert.Contains("\"targetBalanceId\":20", request.Body);
        Assert.Contains("\"amount\":{\"value\":50,\"currency\":\"EUR\"}", request.Body);
        Assert.DoesNotContain("\"quoteId\"", request.Body);
    }

    [Fact]
    public async Task ConvertAsync_auto_generates_valid_idempotency_key_when_none_supplied()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(ConversionResponse);
        var api = new BalanceMovementsApi(http);

        await api.ConvertAsync(profileId: 101, Guid.NewGuid(), idempotencyKey: null, CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        var idempotency = Assert.Contains("X-idempotence-uuid", (IDictionary<string, string>)recorded.Headers);
        Assert.True(Guid.TryParse(idempotency, out var parsed));
        Assert.NotEqual(Guid.Empty, parsed);
    }

    [Fact]
    public async Task ConvertAsync_deserializes_multi_step_response()
    {
        const string stepsBody = """
        {
          "id": 30000002,
          "type": "CONVERSION",
          "state": "COMPLETED",
          "balancesAfter": [{"id": 1, "value": 200.0, "currency": "EUR"}],
          "creationTime": "2017-11-21T09:55:49.275Z",
          "sourceAmount": {"value": 100, "currency": "EUR"},
          "targetAmount": {"value": 88.56, "currency": "GBP"},
          "rate": 0.88558,
          "feeAmounts": [{"value": 0.5, "currency": "EUR"}],
          "steps": [
            {
              "id": 369588,
              "type": "CONVERSION",
              "creationTime": "2017-11-21T09:55:49.276Z",
              "balancesAfter": [
                {"value": 200.0, "currency": "EUR"},
                {"value": 88.56, "currency": "GBP"}
              ],
              "sourceAmount": {"value": 100, "currency": "EUR"},
              "targetAmount": {"value": 88.56, "currency": "GBP"},
              "fee": {"value": 0.5, "currency": "EUR"},
              "rate": 0.88558
            }
          ]
        }
        """;

        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(stepsBody);
        var api = new BalanceMovementsApi(http);

        var result = await api.ConvertAsync(profileId: 101, Guid.NewGuid(), idempotencyKey: null, CancellationToken.None);

        var step = Assert.Single(result.Steps!);
        Assert.Equal(369588L, step.Id);
        Assert.Equal(0.88558m, step.Rate);
        Assert.Equal(0.5m, step.Fee?.Value);
        Assert.Equal("EUR", step.Fee?.Currency);
        Assert.Equal(2, step.BalancesAfter?.Count);
        Assert.Equal(200.0m, step.BalancesAfter![0].Value);
        Assert.Equal("GBP", step.BalancesAfter[1].Currency);
    }

    [Fact]
    public async Task CreateAsync_throws_if_both_quote_and_amount_supplied()
    {
        var (http, _) = TestHost.CreateHttpClient();
        var api = new BalanceMovementsApi(http);
        var bad = new BalanceMovementRequest(
            QuoteId: Guid.NewGuid(),
            SourceBalanceId: 1,
            TargetBalanceId: 2,
            Amount: new Money(10m, "EUR"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            api.CreateAsync(1, bad, idempotencyKey: null, CancellationToken.None));
    }
}
