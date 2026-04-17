using System.Net;

namespace WiseApi.Client;

/// <summary>Exception thrown when the Wise API returns a non-successful response.</summary>
public class WiseApiException : Exception
{
    /// <summary>HTTP status code returned by Wise.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Structured errors parsed from the response body, if any.</summary>
    public IReadOnlyList<WiseError> Errors { get; }

    /// <summary>Raw response body as a string, for diagnostics.</summary>
    public string? RawBody { get; }

    /// <summary>Value of <c>X-External-Correlation-Id</c> echoed by Wise (useful for support tickets).</summary>
    public string? CorrelationId { get; }

    /// <summary>Value of <c>x-trace-id</c> header, if present.</summary>
    public string? TraceId { get; }

    /// <summary>Value of the <c>Retry-After</c> header when Wise returns 429; <c>null</c> otherwise.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>HTTP method of the failing request.</summary>
    public string? HttpMethod { get; }

    /// <summary>Request URI of the failing request.</summary>
    public Uri? RequestUri { get; }

    /// <summary>Create a new <see cref="WiseApiException"/>.</summary>
    public WiseApiException(
        string message,
        HttpStatusCode statusCode,
        IReadOnlyList<WiseError>? errors = null,
        string? rawBody = null,
        string? correlationId = null,
        string? traceId = null,
        TimeSpan? retryAfter = null,
        string? httpMethod = null,
        Uri? requestUri = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
        RawBody = rawBody;
        CorrelationId = correlationId;
        TraceId = traceId;
        RetryAfter = retryAfter;
        HttpMethod = httpMethod;
        RequestUri = requestUri;
    }
}

/// <summary>Thrown when Wise returns HTTP 429 (rate limited).</summary>
public sealed class WiseRateLimitException : WiseApiException
{
    /// <summary>Create a new <see cref="WiseRateLimitException"/>.</summary>
    public WiseRateLimitException(
        string message,
        TimeSpan? retryAfter,
        string? rawBody = null,
        string? correlationId = null,
        string? traceId = null,
        string? httpMethod = null,
        Uri? requestUri = null)
        : base(message, HttpStatusCode.TooManyRequests, errors: null, rawBody, correlationId, traceId, retryAfter, httpMethod, requestUri)
    {
    }
}

/// <summary>
/// Thrown when Wise requires Strong Customer Authentication (SCA): HTTP 403 with an
/// <c>X-2FA-Approval</c> challenge header. Signing is not yet implemented — see the
/// Wise SCA guide to configure an approval key, then intercept this exception to re-issue the
/// request with <c>X-2FA-Approval</c> and <c>X-Signature</c> headers.
/// </summary>
public sealed class WiseScaChallengeException : WiseApiException
{
    /// <summary>One-time token returned in the <c>X-2FA-Approval</c> header.</summary>
    public string OneTimeToken { get; }

    /// <summary>Create a new <see cref="WiseScaChallengeException"/>.</summary>
    public WiseScaChallengeException(
        string oneTimeToken,
        string? rawBody = null,
        string? correlationId = null,
        string? traceId = null,
        string? httpMethod = null,
        Uri? requestUri = null)
        : base(
            "Wise returned an SCA challenge. The endpoint requires a signed X-Signature header. " +
            "This client does not yet sign SCA challenges — see WiseApi.Client documentation.",
            HttpStatusCode.Forbidden,
            errors: null, rawBody, correlationId, traceId, retryAfter: null, httpMethod, requestUri)
    {
        OneTimeToken = oneTimeToken;
    }
}

/// <summary>A single structured error entry returned by Wise.</summary>
public sealed record WiseError(string? Code, string Message, string? Path = null, string? Argument = null);
