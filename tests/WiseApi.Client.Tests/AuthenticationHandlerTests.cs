using System.Net;
using WiseApi.Client.Authentication;
using WiseApi.Client.Http.Handlers;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class AuthenticationHandlerTests
{
    [Fact]
    public async Task Attaches_bearer_token_when_authorization_is_missing()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson("{}");
        var handler = new WiseAuthenticationHandler(new ApiTokenCredentialsProvider("secret-token")) { InnerHandler = stub };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.wise-sandbox.com") };

        using var response = await http.GetAsync(new Uri("/v2/profiles", UriKind.Relative), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(stub.Requests);
        Assert.True(recorded.Headers.TryGetValue("Authorization", out var auth));
        Assert.Equal("Bearer secret-token", auth);
    }

    [Fact]
    public async Task Preserves_caller_supplied_authorization_and_skips_credentials_provider()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson("{}");
        var credentials = new CountingCredentialsProvider("default-token");
        var handler = new WiseAuthenticationHandler(credentials) { InnerHandler = stub };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.wise-sandbox.com") };

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/v2/profiles", UriKind.Relative));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "override-token");
        using var response = await http.SendAsync(request, CancellationToken.None);

        var recorded = Assert.Single(stub.Requests);
        Assert.Equal("Bearer override-token", recorded.Headers["Authorization"]);
        Assert.Equal(0, credentials.CallCount);
    }

    private sealed class CountingCredentialsProvider : IWiseCredentialsProvider
    {
        private readonly string _token;
        public int CallCount { get; private set; }

        public CountingCredentialsProvider(string token) => _token = token;

        public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(_token);
        }
    }
}
