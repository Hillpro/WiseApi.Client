using System.Net;
using WiseApi.Client.Models.MultiCurrencyAccounts;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class MultiCurrencyAccountsApiTests
{
    [Fact]
    public async Task GetAsync_deserializes_account_payload()
    {
        const string body = """
        {
          "id": 1,
          "profileId": 33333333,
          "recipientId": 12345678,
          "creationTime": "2020-05-20T14:43:16.658Z",
          "modificationTime": "2020-05-20T14:43:16.658Z",
          "active": true,
          "eligible": true
        }
        """;
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body);
        var api = new MultiCurrencyAccountsApi(http);

        var mca = await api.GetAsync(profileId: 33333333, CancellationToken.None);

        Assert.NotNull(mca);
        Assert.Equal(1L, mca!.Id);
        Assert.Equal(33333333L, mca.ProfileId);
        Assert.Equal(12345678L, mca.RecipientId);
        Assert.True(mca.Active);
        Assert.True(mca.Eligible);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v4/profiles/33333333/multi-currency-account", request.Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetAsync_returns_null_on_404()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"message":"Not found"}""", HttpStatusCode.NotFound);
        var api = new MultiCurrencyAccountsApi(http);

        var mca = await api.GetAsync(profileId: 42, CancellationToken.None);

        Assert.Null(mca);
    }

    [Fact]
    public async Task GetAsync_propagates_non_404_errors()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"message":"Server error"}""", HttpStatusCode.InternalServerError);
        var api = new MultiCurrencyAccountsApi(http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(() => api.GetAsync(42, CancellationToken.None));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
    }

    [Fact]
    public async Task GetEligibilityAsync_by_profile_encodes_profileId_query()
    {
        const string body = """
        {
          "eligible": true,
          "eligibilityCode": "eligible",
          "accountType": "FULL",
          "ineligibilityReason": null
        }
        """;
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body);
        var api = new MultiCurrencyAccountsApi(http);

        var result = await api.GetEligibilityAsync(profileId: 7, CancellationToken.None);

        Assert.True(result.Eligible);
        Assert.Equal("eligible", result.EligibilityCode);
        Assert.Equal(MultiCurrencyAccountType.Full, result.AccountType);
        Assert.Null(result.IneligibilityReason);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v4/multi-currency-account/eligibility", request.Uri.AbsolutePath);
        Assert.Equal("profileId=7", request.Uri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task GetEligibilityForLocationAsync_sends_country_only()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"eligible":true,"eligibilityCode":"eligible","accountType":"FULL"}""");
        var api = new MultiCurrencyAccountsApi(http);

        await api.GetEligibilityForLocationAsync(country: "FR", state: null, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v4/multi-currency-account/eligibility", request.Uri.AbsolutePath);
        Assert.Equal("country=FR", request.Uri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task GetEligibilityForLocationAsync_sends_country_and_state_for_US()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"eligible":false,"eligibilityCode":"invalid.state","accountType":"INELIGIBLE","ineligibilityReason":"not supported"}""");
        var api = new MultiCurrencyAccountsApi(http);

        var result = await api.GetEligibilityForLocationAsync(country: "US", state: "CA", CancellationToken.None);

        Assert.False(result.Eligible);
        Assert.Equal("invalid.state", result.EligibilityCode);
        Assert.Equal(MultiCurrencyAccountType.Ineligible, result.AccountType);
        Assert.Equal("not supported", result.IneligibilityReason);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("country=US&state=CA", request.Uri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task GetEligibilityForLocationAsync_throws_when_US_without_state()
    {
        var (http, _) = TestHost.CreateHttpClient();
        var api = new MultiCurrencyAccountsApi(http);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            api.GetEligibilityForLocationAsync(country: "US", state: null, CancellationToken.None));
    }

    [Fact]
    public async Task GetEligibilityForLocationAsync_accepts_receive_only_account_type()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"eligible":true,"eligibilityCode":"eligible","accountType":"RECEIVE_ONLY"}""");
        var api = new MultiCurrencyAccountsApi(http);

        var result = await api.GetEligibilityForLocationAsync("GB", state: null, CancellationToken.None);

        Assert.Equal(MultiCurrencyAccountType.ReceiveOnly, result.AccountType);
    }
}
