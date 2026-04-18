using System.Net;
using System.Text;
using WiseApi.Client.Authentication.OAuth;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class ClientCredentialsProviderTests
{
    private static readonly Uri TokenEndpoint = new("https://api.wise-sandbox.com/oauth/token");

    [Fact]
    public async Task First_call_posts_client_credentials_grant_with_basic_auth()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"cc-token","token_type":"bearer","expires_in":43200,"scope":"partner"}""");
        using var http = new HttpClient(stub);
        using var provider = new ClientCredentialsProvider("client-id", "client-secret", TokenEndpoint, http);

        var token = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("cc-token", token);
        var request = Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(TokenEndpoint, request.Uri);
        Assert.True(request.Headers.TryGetValue("Authorization", out var auth));
        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("client-id:client-secret"));
        Assert.Equal(expected, auth);
        Assert.Equal("grant_type=client_credentials", request.Body);
    }

    [Fact]
    public async Task Cached_token_is_reused_within_skew_window()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"access_token":"cc-token","token_type":"bearer","expires_in":43200}""");
        using var http = new HttpClient(stub);
        using var provider = new ClientCredentialsProvider("client-id", "client-secret", TokenEndpoint, http);

        var first = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        var second = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task Surfaces_oauth_error_envelope_as_WiseApiException()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson(
            """{"error":"invalid_client","error_description":"Bad credentials"}""",
            HttpStatusCode.Unauthorized);
        using var http = new HttpClient(stub);
        using var provider = new ClientCredentialsProvider("client-id", "client-secret", TokenEndpoint, http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(
            async () => await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Contains("invalid_client", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Bad credentials", ex.Message, StringComparison.Ordinal);
    }
}
