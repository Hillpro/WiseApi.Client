namespace WiseApi.Client.Http.Handlers;

/// <summary>
/// Adds an <c>X-External-Correlation-Id</c> header to every outgoing request when one is not already present.
/// Wise echoes the value back in responses and in error logs, which makes it invaluable for support tickets.
/// </summary>
public sealed class WiseCorrelationIdHandler : DelegatingHandler
{
    internal const string HeaderName = "X-External-Correlation-Id";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, Guid.NewGuid().ToString("D"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
