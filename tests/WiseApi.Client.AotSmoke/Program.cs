using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using WiseApi.Client;
using WiseApi.Client.Authentication.OAuth;
using WiseApi.Client.DependencyInjection;
using WiseApi.Client.Http;
using WiseApi.Client.Models;
using WiseApi.Client.Models.Balances;
using WiseApi.Client.Models.MultiCurrencyAccounts;
using WiseApi.Client.Models.Profiles;
using WiseApi.Client.Models.Quotes;
using WiseApi.Client.Models.Rates;

// Every assertion below runs inside the natively compiled binary: a type missing from the
// source-generated context, or any reflection fallback, fails here rather than in production.
var failures = new List<string>();
void Check(bool condition, string what)
{
    if (!condition)
    {
        failures.Add(what);
    }
}

var stub = new CannedWise();
using var tokenHttp = new HttpClient(stub);
using var credentials = new ClientCredentialsProvider("client", "secret", new Uri("https://api.wise-sandbox.com/oauth/token"), tokenHttp);

var services = new ServiceCollection();
services.AddWiseClient(o => o.Credentials = credentials).ConfigurePrimaryHttpMessageHandler(() => stub);
using var provider = services.BuildServiceProvider();
var wise = provider.GetRequiredService<IWiseClient>();
var ct = CancellationToken.None;

var profiles = await wise.Profiles.ListAsync(ct).ConfigureAwait(false);
Check(profiles is [PersonalProfile { FirstName: "Ada" }, BusinessProfile { BusinessName: "Acme" }], "polymorphic profile list");
Check(profiles[0].CurrentState == ProfileState.Unknown, "unrecognised enum value reads as Unknown");
Check(stub.LastAuthorization == "Bearer smoke-token", "client_credentials token exchanged and applied");

var mca = await wise.MultiCurrencyAccounts.GetAsync(1, ct).ConfigureAwait(false);
Check(mca is { Id: 9, Active: true }, "multi-currency account");
var eligibility = await wise.MultiCurrencyAccounts.GetEligibilityForLocationAsync("US", "CA", ct).ConfigureAwait(false);
Check(eligibility.AccountType == MultiCurrencyAccountType.ReceiveOnly, "eligibility enum");

var balances = await wise.Balances.ListAsync(1, cancellationToken: ct).ConfigureAwait(false);
Check(balances is [{ Type: BalanceType.Standard, InvestmentState: InvestmentState.NotInvested, Amount.Value: 12.5m }], "balance list");
var created = await wise.Balances.CreateAsync(1, new CreateBalanceRequest("EUR", BalanceType.Savings, "Trip"), cancellationToken: ct).ConfigureAwait(false);
Check(created.Type == BalanceType.Savings && stub.LastBody!.Contains("\"type\":\"SAVINGS\"", StringComparison.Ordinal), "balance create round-trip");
await wise.Balances.DeleteAsync(1, 2, ct).ConfigureAwait(false);

var quote = await wise.Quotes.CreateForBalanceConversionAsync(1, "EUR", "USD", 100m, ct).ConfigureAwait(false);
Check(quote is { RateType: RateType.Fixed, Status: QuoteStatus.Pending, PaymentOptions: [{ Fee.Total: 0.5m }] }, "quote");

var movement = await wise.BalanceMovements.MoveAsync(1, 2, 3, new Money(5m, "EUR"), cancellationToken: ct).ConfigureAwait(false);
Check(movement is { Type: BalanceMovementType.Unknown, State: BalanceMovementState.Completed }, "balance movement");

var rate = await wise.Rates.GetLatestAsync("EUR", "USD", ct).ConfigureAwait(false);
Check(rate?.Time == new DateTimeOffset(2018, 8, 31, 10, 43, 31, TimeSpan.Zero), "rate with compact offset");

// A consumer's own context over a type containing Wise enums, via the AOT-safe WiseHttpClient overload.
var http = provider.GetRequiredService<WiseHttpClient>();
var custom = await http.GetAsync("/v1/custom", ConsumerJsonContext.Default.CustomResponse, ct).ConfigureAwait(false);
Check(custom is { Id: 7, Type: BalanceType.Unknown }, "consumer context with library enum");

try
{
    await wise.Profiles.GetAsync(404, ct).ConfigureAwait(false);
    Check(false, "error envelope");
}
catch (WiseApiException ex)
{
    Check(ex.Errors is [{ Code: "profile.not.found" }], "error envelope");
}

foreach (var failure in failures)
{
    await Console.Error.WriteLineAsync($"FAIL: {failure}").ConfigureAwait(false);
}

await Console.Out.WriteLineAsync(failures.Count == 0 ? "AOT smoke test passed." : $"{failures.Count} check(s) failed.").ConfigureAwait(false);
return failures.Count == 0 ? 0 : 1;

/// <summary>A caller-defined response for an endpoint the library doesn't wrap.</summary>
internal sealed record CustomResponse(long Id, BalanceType Type);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CustomResponse))]
internal sealed partial class ConsumerJsonContext : JsonSerializerContext;

/// <summary>Answers each route the smoke test calls with a canned Wise response.</summary>
internal sealed class CannedWise : HttpMessageHandler
{
    public string? LastAuthorization { get; private set; }

    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (request.RequestUri!.AbsolutePath != "/oauth/token")
        {
            LastAuthorization = request.Headers.Authorization?.ToString();
        }

        var (status, json) = (request.Method.Method, request.RequestUri.AbsolutePath) switch
        {
            ("POST", "/oauth/token") => (HttpStatusCode.OK, """{"access_token":"smoke-token","token_type":"bearer","expires_in":43199,"scope":"transfers","expires_at":"2026-05-06T00:57:28.213Z"}"""),
            ("GET", "/v2/profiles") => (HttpStatusCode.OK, """[{"type":"PERSONAL","id":1,"userId":2,"firstName":"Ada","currentState":"ARCHIVED","createdAt":"2023-01-15T10:30:00"},{"type":"BUSINESS","id":3,"userId":2,"businessName":"Acme","currentState":"VISIBLE"}]"""),
            ("GET", "/v2/profiles/404") => (HttpStatusCode.NotFound, """{"errors":[{"code":"profile.not.found","message":"Profile not found"}]}"""),
            ("GET", "/v4/profiles/1/multi-currency-account") => (HttpStatusCode.OK, """{"id":9,"profileId":1,"recipientId":5,"creationTime":"2023-01-15T10:30:00Z","active":true,"eligible":true}"""),
            ("GET", "/v4/multi-currency-account/eligibility") => (HttpStatusCode.OK, """{"eligible":true,"eligibilityCode":"eligible","accountType":"RECEIVE_ONLY","ineligibilityReason":null}"""),
            ("GET", "/v4/profiles/1/balances") => (HttpStatusCode.OK, """[{"id":2,"currency":"EUR","type":"STANDARD","investmentState":"NOT_INVESTED","amount":{"value":12.5,"currency":"EUR"},"visible":true}]"""),
            ("POST", "/v4/profiles/1/balances") => (HttpStatusCode.Created, """{"id":4,"currency":"EUR","type":"SAVINGS","name":"Trip","amount":{"value":0,"currency":"EUR"}}"""),
            ("DELETE", "/v4/profiles/1/balances/2") => (HttpStatusCode.NoContent, null),
            ("POST", "/v3/profiles/1/quotes") => (HttpStatusCode.OK, """{"id":"11144c35-9fe8-4c32-b7fd-d05c2a7734bf","sourceCurrency":"EUR","targetCurrency":"USD","sourceAmount":100,"targetAmount":117,"payOut":"BALANCE","rate":1.17,"user":2,"profile":1,"rateType":"FIXED","status":"PENDING","paymentOptions":[{"disabled":false,"fee":{"total":0.5},"sourceAmount":100,"targetAmount":117,"sourceCurrency":"EUR","targetCurrency":"USD","payIn":"BALANCE","payOut":"BALANCE"}]}"""),
            ("POST", "/v2/profiles/1/balance-movements") => (HttpStatusCode.Created, """{"id":8,"type":"INTERNAL_MOVE","state":"COMPLETED","creationTime":"2017-11-21T09:55:49.275Z"}"""),
            ("GET", "/v1/rates") => (HttpStatusCode.OK, """[{"rate":1.166,"source":"EUR","target":"USD","time":"2018-08-31T10:43:31+0000"}]"""),
            ("GET", "/v1/custom") => (HttpStatusCode.OK, """{"id":7,"type":"SOMETHING_NEW"}"""),
            _ => (HttpStatusCode.NotImplemented, (string?)null),
        };

        var response = new HttpResponseMessage(status) { RequestMessage = request };
        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
