using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Authentication;

/// <summary>
/// OAuth 2.0 <c>client_credentials</c> provider for partner-level tokens.
/// Fetches tokens via <c>POST /oauth/token</c> with HTTP Basic auth (client-id:client-secret)
/// and caches them until shortly before expiry.
/// </summary>
/// <remarks>
/// Wise's <c>client_credentials</c> tokens are valid for 12 hours and cannot be refreshed —
/// the provider simply re-authenticates when the cached token is about to expire.
/// User-scoped OAuth flows (<c>authorization_code</c>, <c>registration_code</c>) are not yet
/// implemented: obtain a token out-of-band and wrap it in an <see cref="ApiTokenCredentialsProvider"/>.
/// </remarks>
public sealed class OAuthClientCredentialsProvider : IWiseCredentialsProvider, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly Uri _tokenEndpoint;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private volatile CachedToken? _cached;

    /// <summary>Create a provider that manages its own <see cref="HttpClient"/>.</summary>
    public OAuthClientCredentialsProvider(string clientId, string clientSecret, WiseEnvironment environment = WiseEnvironment.Sandbox)
        : this(clientId, clientSecret, TokenEndpointFor(environment), httpClient: null)
    {
    }

    /// <summary>Create a provider reusing a supplied <see cref="HttpClient"/>.</summary>
    public OAuthClientCredentialsProvider(string clientId, string clientSecret, Uri tokenEndpoint, HttpClient? httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentNullException.ThrowIfNull(tokenEndpoint);
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
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

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = DateTimeOffset.UtcNow;
            snapshot = _cached;
            if (snapshot is not null && now + RefreshSkew < snapshot.ExpiresAt)
            {
                return snapshot.Token;
            }

            var (token, expiresAt) = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
            _cached = new CachedToken(token, expiresAt);
            return token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new WiseApiException(
                $"Failed to obtain client-credentials token: {(int)response.StatusCode} {response.ReasonPhrase}",
                response.StatusCode,
                rawBody: body,
                httpMethod: "POST",
                requestUri: _tokenEndpoint);
        }

        var parsed = System.Text.Json.JsonSerializer.Deserialize<OAuthTokenResponse>(body, Serialization.WiseJsonDefaults.Options)
            ?? throw new WiseApiException("OAuth token response was empty.", response.StatusCode, rawBody: body, requestUri: _tokenEndpoint);

        if (string.IsNullOrEmpty(parsed.AccessToken))
        {
            throw new WiseApiException("OAuth token response did not include an access_token.", response.StatusCode, rawBody: body, requestUri: _tokenEndpoint);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(parsed.ExpiresIn, 60));
        return (parsed.AccessToken, expiresAt);
    }

    internal static Uri TokenEndpointFor(WiseEnvironment environment) => new(environment.BaseAddress(), "/oauth/token");

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

    private sealed record OAuthTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope);
}
