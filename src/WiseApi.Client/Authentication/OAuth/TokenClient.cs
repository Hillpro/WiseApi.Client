using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseApi.Client.Authentication.OAuth;

/// <summary>
/// Low-level client for <c>POST /oauth/token</c>. Callers supply the grant-type-specific
/// form fields; this class handles Basic auth, form encoding, response parsing, and error
/// envelope translation into <see cref="WiseApiException"/>.
/// </summary>
internal sealed class TokenClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly Uri _tokenEndpoint;

    public TokenClient(HttpClient httpClient, string clientId, string clientSecret, Uri tokenEndpoint)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
    }

    public Uri TokenEndpoint => _tokenEndpoint;

    public async Task<TokenResponse> ExchangeAsync(
        IReadOnlyDictionary<string, string> formFields,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new FormUrlEncodedContent(formFields);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var (errorCode, errorDescription) = TryParseOAuthError(body);
            var message = errorCode is null
                ? $"Failed to obtain OAuth token: {(int)response.StatusCode} {response.ReasonPhrase}"
                : $"OAuth token request rejected: {errorCode}{(errorDescription is null ? string.Empty : $" — {errorDescription}")}";
            throw new WiseApiException(
                message,
                response.StatusCode,
                rawBody: body,
                httpMethod: "POST",
                requestUri: _tokenEndpoint);
        }

        var parsed = JsonSerializer.Deserialize<TokenResponse>(body, Serialization.WiseJsonDefaults.Options)
            ?? throw new WiseApiException(
                "OAuth token response was empty.",
                response.StatusCode,
                rawBody: body,
                requestUri: _tokenEndpoint);

        if (string.IsNullOrEmpty(parsed.AccessToken))
        {
            throw new WiseApiException(
                "OAuth token response did not include an access_token.",
                response.StatusCode,
                rawBody: body,
                requestUri: _tokenEndpoint);
        }

        return parsed;
    }

    internal static Uri TokenEndpointFor(WiseEnvironment environment)
        => new(environment.BaseAddress(), "/oauth/token");

    private static (string? Error, string? Description) TryParseOAuthError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null);
        }

        try
        {
            var error = JsonSerializer.Deserialize<OAuthError>(body, Serialization.WiseJsonDefaults.Options);
            return error is null ? (null, null) : (error.Error, error.ErrorDescription);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private sealed record OAuthError(
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);
}
