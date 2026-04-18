namespace WiseApi.Client.Authentication.OAuth;

/// <summary>
/// OAuth 2.0 provider for user access tokens. Supports the three user-token grant types
/// Wise offers: <c>authorization_code</c>, <c>registration_code</c> and <c>refresh_token</c>.
/// </summary>
/// <remarks>
/// <para>
/// Wise user access tokens are valid for 12 hours; refresh tokens for up to 20 years.
/// Refreshing an access token <b>immediately invalidates</b> the previous one, and Wise may
/// rotate the refresh token in the response. Always persist whichever <see cref="CurrentRefreshToken"/>
/// the provider exposes after a refresh so your next process restart can re-hydrate.
/// </para>
/// <para>
/// Construct with <see cref="FromAuthorizationCode"/>, <see cref="FromRegistrationCode"/> or
/// <see cref="FromRefreshToken"/>. The first two are one-shot seeds: the seed is consumed on the
/// first call to <see cref="GetAccessTokenAsync"/> to obtain a refresh token, and all subsequent
/// renewals go through the <c>refresh_token</c> grant.
/// </para>
/// </remarks>
public sealed class UserTokenProvider : IWiseCredentialsProvider, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    private readonly TokenClient _tokenClient;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Seed? _pendingSeed;
    private volatile CachedToken? _cached;
    private volatile string? _refreshToken;

    private UserTokenProvider(
        TokenClient tokenClient,
        HttpClient httpClient,
        bool ownsHttpClient,
        Seed seed,
        string? initialRefreshToken)
    {
        _tokenClient = tokenClient;
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
        _pendingSeed = seed;
        _refreshToken = initialRefreshToken;
    }

    /// <summary>The currently-held refresh token, if any. Persist this across process restarts.</summary>
    public string? CurrentRefreshToken => _refreshToken;

    /// <summary>Raised after every successful token refresh. Useful for persisting <see cref="CurrentRefreshToken"/>.</summary>
    public event EventHandler<TokenRefreshedEventArgs>? TokenRefreshed;

    /// <summary>
    /// Seed the provider from an <c>authorization_code</c> returned to your <paramref name="redirectUri"/>
    /// by Wise's consent page. The code is single-use and is exchanged on the next
    /// <see cref="GetAccessTokenAsync"/> call.
    /// </summary>
    public static UserTokenProvider FromAuthorizationCode(
        string clientId,
        string clientSecret,
        string authorizationCode,
        Uri redirectUri,
        WiseEnvironment environment = WiseEnvironment.Sandbox,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);
        ArgumentNullException.ThrowIfNull(redirectUri);
        return Build(
            clientId,
            clientSecret,
            TokenClient.TokenEndpointFor(environment),
            httpClient,
            new Seed.AuthorizationCode(clientId, authorizationCode, redirectUri.ToString()),
            initialRefreshToken: null);
    }

    /// <summary>
    /// Seed the provider from a <c>registration_code</c> returned by Wise when you created the
    /// user via API. The code is single-use and is exchanged on the next
    /// <see cref="GetAccessTokenAsync"/> call.
    /// </summary>
    public static UserTokenProvider FromRegistrationCode(
        string clientId,
        string clientSecret,
        string userEmail,
        string registrationCode,
        WiseEnvironment environment = WiseEnvironment.Sandbox,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationCode);
        return Build(
            clientId,
            clientSecret,
            TokenClient.TokenEndpointFor(environment),
            httpClient,
            new Seed.RegistrationCode(clientId, userEmail, registrationCode),
            initialRefreshToken: null);
    }

    /// <summary>
    /// Seed the provider from a previously-obtained refresh token. This is the common
    /// production path: persist the refresh token issued by a prior
    /// <c>authorization_code</c> or <c>registration_code</c> exchange, then re-hydrate on startup.
    /// </summary>
    public static UserTokenProvider FromRefreshToken(
        string clientId,
        string clientSecret,
        string refreshToken,
        WiseEnvironment environment = WiseEnvironment.Sandbox,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return Build(
            clientId,
            clientSecret,
            TokenClient.TokenEndpointFor(environment),
            httpClient,
            new Seed.RefreshToken(),
            initialRefreshToken: refreshToken);
    }

    private static UserTokenProvider Build(
        string clientId,
        string clientSecret,
        Uri tokenEndpoint,
        HttpClient? httpClient,
        Seed seed,
        string? initialRefreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        HttpClient client;
        bool owns;
        if (httpClient is null)
        {
            client = new HttpClient();
            owns = true;
        }
        else
        {
            client = httpClient;
            owns = false;
        }

        var tokenClient = new TokenClient(client, clientId, clientSecret, tokenEndpoint);
        return new UserTokenProvider(tokenClient, client, owns, seed, initialRefreshToken);
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = _cached;
        if (snapshot is not null && DateTimeOffset.UtcNow + RefreshSkew < snapshot.ExpiresAt)
        {
            return snapshot.Token;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            snapshot = _cached;
            if (snapshot is not null && DateTimeOffset.UtcNow + RefreshSkew < snapshot.ExpiresAt)
            {
                return snapshot.Token;
            }

            var fields = BuildGrantFields();
            var response = await _tokenClient.ExchangeAsync(fields, cancellationToken).ConfigureAwait(false);

            // Once any grant succeeds, the seed is consumed and future renewals use refresh_token.
            _pendingSeed = null;
            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                _refreshToken = response.RefreshToken;
            }

            var expiresAt = response.ExpiresAt ?? DateTimeOffset.UtcNow.AddSeconds(Math.Max(response.ExpiresIn, 60));
            _cached = new CachedToken(response.AccessToken, expiresAt);

            TokenRefreshed?.Invoke(this, new TokenRefreshedEventArgs(
                response.AccessToken,
                _refreshToken,
                expiresAt));

            return response.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private Dictionary<string, string> BuildGrantFields()
    {
        if (_pendingSeed is not null)
        {
            return _pendingSeed switch
            {
                Seed.AuthorizationCode seed => new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = seed.ClientId,
                    ["code"] = seed.Code,
                    ["redirect_uri"] = seed.RedirectUri,
                },
                Seed.RegistrationCode seed => new Dictionary<string, string>
                {
                    ["grant_type"] = "registration_code",
                    ["client_id"] = seed.ClientId,
                    ["email"] = seed.Email,
                    ["registration_code"] = seed.Code,
                },
                _ => BuildRefreshGrant(),
            };
        }

        return BuildRefreshGrant();
    }

    private Dictionary<string, string> BuildRefreshGrant()
    {
        var token = _refreshToken
            ?? throw new InvalidOperationException(
                "No refresh token available. The initial grant did not return one, and the provider cannot refresh.");
        return new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token,
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private sealed record CachedToken(string Token, DateTimeOffset ExpiresAt);

    private abstract record Seed
    {
        internal sealed record AuthorizationCode(string ClientId, string Code, string RedirectUri) : Seed;
        internal sealed record RegistrationCode(string ClientId, string Email, string Code) : Seed;
        internal sealed record RefreshToken : Seed;
    }
}

/// <summary>Event payload emitted after a successful <see cref="UserTokenProvider"/> token refresh.</summary>
public sealed class TokenRefreshedEventArgs : EventArgs
{
    /// <summary>Create a new event payload.</summary>
    public TokenRefreshedEventArgs(string accessToken, string? refreshToken, DateTimeOffset accessTokenExpiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
    }

    /// <summary>The newly-issued access token.</summary>
    public string AccessToken { get; }

    /// <summary>The refresh token Wise returned (may be rotated). Persist this.</summary>
    public string? RefreshToken { get; }

    /// <summary>UTC timestamp at which the new access token expires.</summary>
    public DateTimeOffset AccessTokenExpiresAt { get; }
}
