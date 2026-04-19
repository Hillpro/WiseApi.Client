namespace WiseApi.Client.Authentication.OAuth;

/// <summary>
/// OAuth 2.0 <c>client_credentials</c> provider for partner-level tokens.
/// Fetches tokens via <c>POST /oauth/token</c> with HTTP Basic auth (client-id:client-secret)
/// and caches them until shortly before expiry.
/// </summary>
/// <remarks>
/// Wise's <c>client_credentials</c> tokens are valid for 12 hours and cannot be refreshed —
/// the provider simply re-authenticates when the cached token is about to expire. For
/// user-scoped flows (<c>authorization_code</c>, <c>registration_code</c>, <c>refresh_token</c>),
/// use <see cref="UserTokenProvider"/>.
/// </remarks>
public sealed class ClientCredentialsProvider : IWiseCredentialsProvider, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private static readonly IReadOnlyDictionary<string, string> GrantFields =
        new Dictionary<string, string> { ["grant_type"] = "client_credentials" };

    private readonly TokenClient _tokenClient;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private CachedToken? _cached;

    /// <summary>Create a provider that manages its own <see cref="HttpClient"/>.</summary>
    public ClientCredentialsProvider(string clientId, string clientSecret, WiseEnvironment environment = WiseEnvironment.Sandbox)
        : this(clientId, clientSecret, TokenClient.TokenEndpointFor(environment), httpClient: null)
    {
    }

    /// <summary>Create a provider reusing a supplied <see cref="HttpClient"/>.</summary>
    public ClientCredentialsProvider(string clientId, string clientSecret, Uri tokenEndpoint, HttpClient? httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentNullException.ThrowIfNull(tokenEndpoint);

        if (httpClient is null)
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }

        _tokenClient = new TokenClient(_httpClient, clientId, clientSecret, tokenEndpoint);
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = _cached;
        if (snapshot is not null && now + RefreshSkew < snapshot.ExpiresAt)
        {
            return snapshot.Token;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = DateTimeOffset.UtcNow;
            snapshot = _cached;
            if (snapshot is not null && now + RefreshSkew < snapshot.ExpiresAt)
            {
                return snapshot.Token;
            }

            var response = await _tokenClient.ExchangeAsync(GrantFields, cancellationToken).ConfigureAwait(false);
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(response.ExpiresIn, 60));
            _cached = new CachedToken(response.AccessToken, expiresAt);
            return response.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _refreshLock.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private sealed record CachedToken(string Token, DateTimeOffset ExpiresAt);
}
