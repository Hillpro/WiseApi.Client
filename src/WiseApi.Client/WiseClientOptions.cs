using WiseApi.Client.Authentication;
using WiseApi.Client.Authentication.OAuth;

namespace WiseApi.Client;

/// <summary>Options for configuring a <see cref="WiseClient"/>.</summary>
public sealed class WiseClientOptions
{
    /// <summary>Target environment. Defaults to <see cref="WiseEnvironment.Sandbox"/> for safety.</summary>
    public WiseEnvironment Environment { get; set; } = WiseEnvironment.Sandbox;

    /// <summary>
    /// Override for the API base address. When set, takes precedence over <see cref="Environment"/>.
    /// Only needed for mocks or a proxy — leave <c>null</c> in production.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Static API bearer token (Personal API Token, User Access Token or Client-Credentials token).
    /// Convenient shortcut for <see cref="Credentials"/> — if set and <see cref="Credentials"/> is <c>null</c>,
    /// an <see cref="ApiTokenCredentialsProvider"/> will be created automatically.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Pluggable credentials provider. Takes precedence over every other auth field on this options object.
    /// Use this for custom token-refresh logic or to inject pre-built providers like
    /// <see cref="ClientCredentialsProvider"/> or <see cref="UserTokenProvider"/>.
    /// </summary>
    public IWiseCredentialsProvider? Credentials { get; set; }

    /// <summary>Partner client ID. Required for any OAuth flow (shortcut fields below).</summary>
    public string? ClientId { get; set; }

    /// <summary>Partner client secret. Required for any OAuth flow.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// OAuth <c>authorization_code</c> returned to <see cref="RedirectUri"/> by Wise's consent page.
    /// Single-use; exchanged for user access + refresh tokens on the first API call. If set,
    /// <see cref="ClientId"/>, <see cref="ClientSecret"/> and <see cref="RedirectUri"/> must also be set.
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// OAuth <c>registration_code</c> Wise issued when you created a user via API. Single-use;
    /// exchanged for user access + refresh tokens on the first API call. Requires
    /// <see cref="ClientId"/>, <see cref="ClientSecret"/> and <see cref="UserEmail"/>.
    /// </summary>
    public string? RegistrationCode { get; set; }

    /// <summary>User email that pairs with <see cref="RegistrationCode"/>.</summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Previously-obtained user refresh token. Use this to re-hydrate user-token auth across
    /// process restarts: persist the refresh token returned by an earlier
    /// <c>authorization_code</c> / <c>registration_code</c> exchange, then pass it here.
    /// Requires <see cref="ClientId"/> and <see cref="ClientSecret"/>.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// The pre-registered redirect URI that pairs with <see cref="AuthorizationCode"/>. Must match
    /// exactly the value registered with Wise when partner credentials were issued.
    /// </summary>
    public Uri? RedirectUri { get; set; }

    /// <summary>
    /// When <c>true</c> and only <see cref="ClientId"/> / <see cref="ClientSecret"/> are set (no code,
    /// refresh token or API token), treat the credentials as a partner-level <c>client_credentials</c>
    /// grant. Defaults to <c>true</c>.
    /// </summary>
    public bool UseClientCredentialsWhenNoUserGrant { get; set; } = true;

    /// <summary>Optional user-agent fragment appended after the library's default.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When <c>true</c>, every outgoing request gets an auto-generated
    /// <c>X-External-Correlation-Id</c> header if the caller didn't supply one. Defaults to <c>true</c>.
    /// </summary>
    public bool AutoCorrelationId { get; set; } = true;

    /// <summary>Default per-request timeout. Only used by the non-DI <see cref="WiseClient.Create(WiseClientOptions)"/> factory.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    internal Uri ResolveBaseAddress() => BaseAddress ?? Environment.BaseAddress();

    internal IWiseCredentialsProvider ResolveCredentials()
    {
        if (Credentials is not null)
        {
            return Credentials;
        }

        if (!string.IsNullOrWhiteSpace(ApiToken))
        {
            return new ApiTokenCredentialsProvider(ApiToken!);
        }

        var hasClientPair = !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        if (!string.IsNullOrWhiteSpace(AuthorizationCode))
        {
            RequireClientPair();
            if (RedirectUri is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(AuthorizationCode)} requires {nameof(RedirectUri)} to be set.");
            }
            return UserTokenProvider.FromAuthorizationCode(
                ClientId!, ClientSecret!, AuthorizationCode!, RedirectUri, Environment);
        }

        if (!string.IsNullOrWhiteSpace(RegistrationCode))
        {
            RequireClientPair();
            if (string.IsNullOrWhiteSpace(UserEmail))
            {
                throw new InvalidOperationException(
                    $"{nameof(RegistrationCode)} requires {nameof(UserEmail)} to be set.");
            }
            return UserTokenProvider.FromRegistrationCode(
                ClientId!, ClientSecret!, UserEmail!, RegistrationCode!, Environment);
        }

        if (!string.IsNullOrWhiteSpace(RefreshToken))
        {
            RequireClientPair();
            return UserTokenProvider.FromRefreshToken(
                ClientId!, ClientSecret!, RefreshToken!, Environment);
        }

        if (hasClientPair && UseClientCredentialsWhenNoUserGrant)
        {
            return new ClientCredentialsProvider(ClientId!, ClientSecret!, Environment);
        }

        throw new InvalidOperationException(
            $"No Wise credentials configured. Set one of {nameof(ApiToken)}, {nameof(Credentials)}, "
            + $"{nameof(AuthorizationCode)} (+ {nameof(RedirectUri)}), {nameof(RegistrationCode)} (+ {nameof(UserEmail)}), "
            + $"{nameof(RefreshToken)}, or {nameof(ClientId)}+{nameof(ClientSecret)} for client-credentials.");

        void RequireClientPair()
        {
            if (!hasClientPair)
            {
                throw new InvalidOperationException(
                    $"OAuth user-grant fields require {nameof(ClientId)} and {nameof(ClientSecret)} to be set.");
            }
        }
    }
}
