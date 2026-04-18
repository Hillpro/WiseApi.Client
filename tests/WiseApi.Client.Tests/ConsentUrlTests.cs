using WiseApi.Client.Authentication.OAuth;

namespace WiseApi.Client.Tests;

public sealed class ConsentUrlTests
{
    [Fact]
    public void Builds_sandbox_url_with_url_encoded_parameters()
    {
        var url = ConsentUrl.Build(
            clientId: "partner-app",
            redirectUri: new Uri("https://partner.example.com/cb?tenant=acme"),
            state: "csrf/value+with spaces",
            environment: WiseEnvironment.Sandbox);

        Assert.Equal("wise-sandbox.com", url.Host);
        Assert.Equal("/oauth/authorize", url.AbsolutePath);

        var query = url.Query;
        Assert.Contains("client_id=partner-app", query, StringComparison.Ordinal);
        Assert.Contains("response_type=code", query, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fpartner.example.com%2Fcb%3Ftenant%3Dacme", query, StringComparison.Ordinal);
        Assert.Contains("state=csrf%2Fvalue%2Bwith%20spaces", query, StringComparison.Ordinal);
    }

    [Fact]
    public void Builds_production_url()
    {
        var url = ConsentUrl.Build(
            clientId: "partner-app",
            redirectUri: new Uri("https://partner.example.com/cb"),
            state: "s",
            environment: WiseEnvironment.Production);

        Assert.Equal("wise.com", url.Host);
    }

    [Fact]
    public void Throws_when_state_is_empty()
    {
        Assert.Throws<ArgumentException>(() => ConsentUrl.Build(
            "client", new Uri("https://partner.example.com/cb"), "", WiseEnvironment.Sandbox));
    }

    [Fact]
    public void Throws_when_redirect_uri_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => ConsentUrl.Build(
            "client", null!, "state", WiseEnvironment.Sandbox));
    }
}
