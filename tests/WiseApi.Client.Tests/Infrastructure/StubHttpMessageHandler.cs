using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace WiseApi.Client.Tests.Infrastructure;

/// <summary>Records outgoing HTTP requests and returns a queued response for each.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<StubResponse> _responses = new();
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests => _requests;

    public StubHttpMessageHandler EnqueueJson(string json, HttpStatusCode status = HttpStatusCode.OK, Action<HttpResponseMessage>? customize = null)
    {
        _responses.Enqueue(new StubResponse(status, json, "application/json", customize));
        return this;
    }

    public StubHttpMessageHandler EnqueueText(string body, string contentType, HttpStatusCode status, Action<HttpResponseMessage>? customize = null)
    {
        _responses.Enqueue(new StubResponse(status, body, contentType, customize));
        return this;
    }

    public StubHttpMessageHandler EnqueueEmpty(HttpStatusCode status)
    {
        _responses.Enqueue(new StubResponse(status, null, null, null));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        _requests.Add(new RecordedRequest(
            Method: request.Method,
            Uri: request.RequestUri!,
            Headers: request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase),
            ContentHeaders: request.Content?.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase),
            Body: body));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No queued response for {request.Method} {request.RequestUri}.");
        }

        var stub = _responses.Dequeue();
        var response = new HttpResponseMessage(stub.Status);
        if (stub.Body is not null)
        {
            response.Content = new StringContent(stub.Body, Encoding.UTF8, stub.ContentType ?? "application/json");
        }
        else
        {
            response.Content = new ByteArrayContent([]);
        }

        stub.Customize?.Invoke(response);
        return response;
    }

    private sealed record StubResponse(HttpStatusCode Status, string? Body, string? ContentType, Action<HttpResponseMessage>? Customize);
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string>? ContentHeaders,
    string? Body);

internal static class HttpHeadersExtensions
{
    public static MediaTypeHeaderValue WithCharset(this MediaTypeHeaderValue value, string charset)
    {
        value.CharSet = charset;
        return value;
    }
}
