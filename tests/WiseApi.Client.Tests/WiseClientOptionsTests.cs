using WiseApi.Client.Authentication;
using WiseApi.Client.Authentication.OAuth;

namespace WiseApi.Client.Tests;

public sealed class WiseClientOptionsTests
{
    [Fact]
    public void Credentials_explicit_provider_wins()
    {
        var explicitProvider = new ApiTokenCredentialsProvider("explicit");
        var options = new WiseClientOptions
        {
            Credentials = explicitProvider,
            ApiToken = "ignored",
            ClientId = "ignored",
            ClientSecret = "ignored",
        };

        Assert.Same(explicitProvider, options.ResolveCredentials());
    }

    [Fact]
    public void ApiToken_wins_over_oauth_fields()
    {
        var options = new WiseClientOptions
        {
            ApiToken = "api-token",
            ClientId = "ignored",
            ClientSecret = "ignored",
            RefreshToken = "ignored",
        };

        var provider = options.ResolveCredentials();
        Assert.IsType<ApiTokenCredentialsProvider>(provider);
    }

    [Fact]
    public void AuthorizationCode_resolves_to_user_token_provider()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            AuthorizationCode = "code",
            RedirectUri = new Uri("https://partner.example.com/cb"),
        };

        var provider = options.ResolveCredentials();
        Assert.IsType<UserTokenProvider>(provider);
        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void AuthorizationCode_without_RedirectUri_throws()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            AuthorizationCode = "code",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveCredentials());
        Assert.Contains(nameof(WiseClientOptions.RedirectUri), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationCode_resolves_to_user_token_provider()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RegistrationCode = "reg-code",
            UserEmail = "new.user@example.com",
        };

        var provider = options.ResolveCredentials();
        Assert.IsType<UserTokenProvider>(provider);
        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void RegistrationCode_without_UserEmail_throws()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RegistrationCode = "reg-code",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveCredentials());
        Assert.Contains(nameof(WiseClientOptions.UserEmail), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefreshToken_resolves_to_user_token_provider()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshToken = "refresh-abc",
        };

        var provider = options.ResolveCredentials();
        Assert.IsType<UserTokenProvider>(provider);
        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void ClientPair_alone_resolves_to_client_credentials_provider()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
        };

        var provider = options.ResolveCredentials();
        Assert.IsType<ClientCredentialsProvider>(provider);
        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void ClientPair_with_UseClientCredentialsWhenNoUserGrant_false_throws()
    {
        var options = new WiseClientOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            UseClientCredentialsWhenNoUserGrant = false,
        };

        Assert.Throws<InvalidOperationException>(() => options.ResolveCredentials());
    }

    [Fact]
    public void Empty_options_throws_with_helpful_message()
    {
        var options = new WiseClientOptions();
        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveCredentials());
        Assert.Contains(nameof(WiseClientOptions.ApiToken), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(WiseClientOptions.Credentials), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OAuth_user_grant_without_client_pair_throws()
    {
        var options = new WiseClientOptions
        {
            AuthorizationCode = "code",
            RedirectUri = new Uri("https://partner.example.com/cb"),
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveCredentials());
        Assert.Contains(nameof(WiseClientOptions.ClientId), ex.Message, StringComparison.Ordinal);
    }
}
