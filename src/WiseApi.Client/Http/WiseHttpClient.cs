using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Http;

/// <summary>
/// Low-level typed HTTP client used by the service classes. Wraps an <see cref="HttpClient"/>
/// configured with Wise-specific handlers (auth, correlation-id, user-agent) and handles
/// JSON (de)serialization, error mapping, and idempotency / correlation headers.
/// </summary>
/// <remarks>
/// Not intended for direct use by applications — consume <see cref="WiseClient"/> or one of
/// its service interfaces instead. This class is <c>public</c> so it can be resolved through
/// <see cref="IServiceProvider"/> and extended by advanced consumers.
/// </remarks>
public sealed partial class WiseHttpClient
{
    internal const string IdempotencyHeader = "X-idempotence-uuid";
    internal const string CorrelationHeader = "X-External-Correlation-Id";
    internal const string TraceHeader = "x-trace-id";
    internal const string ScaTokenHeader = "X-2FA-Approval";

    private readonly HttpClient _http;
    private readonly ILogger<WiseHttpClient> _logger;

    /// <summary>Create a new <see cref="WiseHttpClient"/>.</summary>
    public WiseHttpClient(HttpClient httpClient, ILogger<WiseHttpClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _http = httpClient;
        _logger = logger ?? NullLogger<WiseHttpClient>.Instance;
    }

    /// <summary>Issue a GET request and deserialize the JSON response body into <typeparamref name="TResponse"/>.</summary>
    public Task<TResponse> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
        => GetAsync<TResponse>(requestUri, headers: null, cancellationToken);

    /// <summary>Issue a GET request with custom headers.</summary>
    public async Task<TResponse> GetAsync<TResponse>(string requestUri, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        ApplyHeaders(request, headers);
        return await SendAsync<TResponse>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Issue a GET request and return the raw <see cref="HttpResponseMessage"/> for non-JSON responses (e.g. statement files).</summary>
    public async Task<HttpResponseMessage> GetRawAsync(string requestUri, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        ApplyHeaders(request, headers);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowFromFailureAsync(response, request, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>Issue a POST with a JSON body and deserialize the response.</summary>
    public Task<TResponse> PostJsonAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default)
        => PostJsonAsync<TRequest, TResponse>(requestUri, body, headers: null, cancellationToken);

    /// <summary>Issue a POST with a JSON body and custom headers, deserialize the response.</summary>
    public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body, options: WiseJsonDefaults.Options),
        };
        ApplyHeaders(request, headers);
        return await SendAsync<TResponse>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Issue a PATCH with a JSON body and deserialize the response.</summary>
    public async Task<TResponse> PatchJsonAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = JsonContent.Create(body, options: WiseJsonDefaults.Options),
        };
        ApplyHeaders(request, headers);
        return await SendAsync<TResponse>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Issue a DELETE request.</summary>
    public async Task DeleteAsync(string requestUri, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        ApplyHeaders(request, headers);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowFromFailureAsync(response, request, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ApplyHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                await ThrowFromFailureAsync(response, request, cancellationToken).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return default!;
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, WiseJsonDefaults.Options, cancellationToken).ConfigureAwait(false);
            return result!;
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task ThrowFromFailureAsync(HttpResponseMessage response, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = response.Content is null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var correlation = FirstHeader(response, CorrelationHeader);
        var trace = FirstHeader(response, TraceHeader);

        LogWiseCallFailed(_logger, request.Method.Method, request.RequestUri, (int)response.StatusCode, correlation, trace);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            throw new WiseRateLimitException(
                $"Wise rate limit exceeded on {request.Method.Method} {request.RequestUri}.",
                retryAfter,
                rawBody: body,
                correlationId: correlation,
                traceId: trace,
                httpMethod: request.Method.Method,
                requestUri: request.RequestUri);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var scaToken = FirstHeader(response, ScaTokenHeader);
            if (!string.IsNullOrEmpty(scaToken))
            {
                throw new WiseScaChallengeException(
                    scaToken,
                    rawBody: body,
                    correlationId: correlation,
                    traceId: trace,
                    httpMethod: request.Method.Method,
                    requestUri: request.RequestUri);
            }
        }

        var errors = TryParseErrors(body);
        var message = BuildMessage(request, response, errors);
        throw new WiseApiException(
            message,
            response.StatusCode,
            errors,
            rawBody: body,
            correlationId: correlation,
            traceId: trace,
            httpMethod: request.Method.Method,
            requestUri: request.RequestUri);
    }

    private static string BuildMessage(HttpRequestMessage request, HttpResponseMessage response, List<WiseError>? errors)
    {
        var prefix = $"Wise API call failed: {request.Method.Method} {request.RequestUri} -> {(int)response.StatusCode} {response.ReasonPhrase}";
        if (errors is null || errors.Count == 0)
        {
            return prefix;
        }

        var first = errors[0];
        var detail = first.Code is null ? first.Message : $"{first.Code}: {first.Message}";
        return $"{prefix}. {detail}";
    }

    private static string? FirstHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return values.FirstOrDefault();
        }

        if (response.Content is not null && response.Content.Headers.TryGetValues(name, out var contentValues))
        {
            return contentValues.FirstOrDefault();
        }

        return null;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Wise API call failed: {Method} {Uri} -> {Status}. CorrelationId={CorrelationId} TraceId={TraceId}")]
    private static partial void LogWiseCallFailed(
        ILogger logger,
        string method,
        Uri? uri,
        int status,
        string? correlationId,
        string? traceId);

    private static List<WiseError>? TryParseErrors(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Array)
            {
                var list = new List<WiseError>(errorsEl.GetArrayLength());
                foreach (var item in errorsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    list.Add(new WiseError(
                        Code: TryString(item, "code"),
                        Message: TryString(item, "message") ?? "Unknown error.",
                        Path: TryString(item, "path"),
                        Argument: TryString(item, "arguments") ?? TryString(item, "argument")));
                }

                if (list.Count > 0) return list;
            }

            var message = TryString(root, "message") ?? TryString(root, "error_description") ?? TryString(root, "error");
            if (message is not null)
            {
                return
                [
                    new WiseError(
                        Code: TryString(root, "error") ?? (root.TryGetProperty("status", out var st) ? st.ToString() : null),
                        Message: message,
                        Path: TryString(root, "path")),
                ];
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    private static string? TryString(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.ToString(),
        };
    }
}
