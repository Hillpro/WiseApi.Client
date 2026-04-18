using System.Net;
using WiseApi.Client.Authentication.OAuth;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class UserTokenProviderTests
{
    private const string ClientId = "client-id";
    private const string ClientSecret = "client-secret";

    [Fact]
    public async Task AuthorizationCode_seed_posts_expected_grant_fields_on_first_call()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"u-access","refresh_token":"u-refresh","token_type":"bearer","expires_in":43200,"scope":"transfers"}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromAuthorizationCode(
            ClientId, ClientSecret,
            authorizationCode: "auth-code-xyz",
            redirectUri: new Uri("https://partner.example.com/callback"),
            environment: WiseEnvironment.Sandbox,
            httpClient: http);

        var token = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("u-access", token);
        Assert.Equal("u-refresh", provider.CurrentRefreshToken);
        var request = Assert.Single(stub.Requests);
        var body = Assert.IsType<string>(request.Body);
        Assert.Contains("grant_type=authorization_code", body, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", body, StringComparison.Ordinal);
        Assert.Contains("code=auth-code-xyz", body, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fpartner.example.com%2Fcallback", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegistrationCode_seed_posts_expected_grant_fields()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"u-access","refresh_token":"u-refresh","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromRegistrationCode(
            ClientId, ClientSecret,
            userEmail: "new.user@example.com",
            registrationCode: "reg-code-abc",
            environment: WiseEnvironment.Sandbox,
            httpClient: http);

        await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        var body = Assert.Single(stub.Requests).Body!;
        Assert.Contains("grant_type=registration_code", body, StringComparison.Ordinal);
        Assert.Contains("email=new.user%40example.com", body, StringComparison.Ordinal);
        Assert.Contains("registration_code=reg-code-abc", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshToken_seed_posts_refresh_token_grant()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"u-access","refresh_token":"u-refresh-new","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromRefreshToken(
            ClientId, ClientSecret,
            refreshToken: "seed-refresh",
            environment: WiseEnvironment.Sandbox,
            httpClient: http);

        var token = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("u-access", token);
        Assert.Equal("u-refresh-new", provider.CurrentRefreshToken);
        var body = Assert.Single(stub.Requests).Body!;
        Assert.Contains("grant_type=refresh_token", body, StringComparison.Ordinal);
        Assert.Contains("refresh_token=seed-refresh", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cached_access_token_is_reused_within_skew_window()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"u-access","refresh_token":"u-refresh","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromRefreshToken(
            ClientId, ClientSecret, "seed-refresh", WiseEnvironment.Sandbox, http);

        var first = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        var second = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task TokenRefreshed_event_fires_with_rotated_refresh_token()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"u-access","refresh_token":"rotated-refresh","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromRefreshToken(
            ClientId, ClientSecret, "seed-refresh", WiseEnvironment.Sandbox, http);

        TokenRefreshedEventArgs? captured = null;
        provider.TokenRefreshed += (_, args) => captured = args;

        await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("u-access", captured!.AccessToken);
        Assert.Equal("rotated-refresh", captured.RefreshToken);
    }

    [Fact]
    public async Task Subsequent_refresh_uses_current_refresh_token_not_original_seed()
    {
        var stub = new StubHttpMessageHandler()
            .EnqueueJson("""{"access_token":"access-1","refresh_token":"refresh-2","token_type":"bearer","expires_in":1}""")
            .EnqueueJson("""{"access_token":"access-2","refresh_token":"refresh-3","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromRefreshToken(
            ClientId, ClientSecret, "refresh-1", WiseEnvironment.Sandbox, http);

        var first = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        var second = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("access-1", first);
        Assert.Equal("access-2", second);
        Assert.Equal("refresh-3", provider.CurrentRefreshToken);

        Assert.Equal(2, stub.Requests.Count);
        Assert.Contains("refresh_token=refresh-1", stub.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("refresh_token=refresh-2", stub.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OAuth_error_surfaces_as_WiseApiException()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"error":"invalid_grant","error_description":"Authorization code expired."}""",
            HttpStatusCode.Unauthorized);
        using var http = new HttpClient(stub);
        using var provider = UserTokenProvider.FromAuthorizationCode(
            ClientId, ClientSecret, "stale-code", new Uri("https://partner.example.com/cb"),
            WiseEnvironment.Sandbox, http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(
            async () => await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Contains("invalid_grant", ex.Message, StringComparison.Ordinal);
    }
}
