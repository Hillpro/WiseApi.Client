using System.Net;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class ErrorHandlingTests
{
    [Fact]
    public async Task Parses_standard_error_envelope()
    {
        const string body = """{"error":"Bad Request","message":"Invalid parameters","status":400}""";
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body, HttpStatusCode.BadRequest);
        var api = new ProfilesApi(http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(() => api.ListAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        var error = Assert.Single(ex.Errors);
        Assert.Equal("Invalid parameters", error.Message);
        Assert.Equal("Bad Request", error.Code);
    }

    [Fact]
    public async Task Parses_errors_array_envelope()
    {
        const string body = """{"errors":[{"code":"E001","message":"Quote expired","path":"quoteId"}]}""";
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson(body, HttpStatusCode.UnprocessableEntity);
        var api = new ProfilesApi(http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(() => api.ListAsync(CancellationToken.None));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("E001", error.Code);
        Assert.Equal("Quote expired", error.Message);
        Assert.Equal("quoteId", error.Path);
    }

    [Fact]
    public async Task Surfaces_rate_limit_with_retry_after()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("{}", HttpStatusCode.TooManyRequests, customize: response =>
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
        });
        var api = new ProfilesApi(http);

        var ex = await Assert.ThrowsAsync<WiseRateLimitException>(() => api.ListAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
    }

    [Fact]
    public async Task Surfaces_sca_challenge_from_403_with_one_time_token()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("{}", HttpStatusCode.Forbidden, customize: response =>
        {
            response.Headers.TryAddWithoutValidation("X-2FA-Approval", "otp-abc-123");
        });
        var api = new ProfilesApi(http);

        var ex = await Assert.ThrowsAsync<WiseScaChallengeException>(() => api.ListAsync(CancellationToken.None));

        Assert.Equal("otp-abc-123", ex.OneTimeToken);
    }

    [Fact]
    public async Task Captures_correlation_and_trace_headers()
    {
        var (http, handler) = TestHost.CreateHttpClient();
        handler.EnqueueJson("""{"message":"nope"}""", HttpStatusCode.InternalServerError, customize: response =>
        {
            response.Headers.TryAddWithoutValidation("X-External-Correlation-Id", "corr-1");
            response.Headers.TryAddWithoutValidation("x-trace-id", "trace-1");
        });
        var api = new ProfilesApi(http);

        var ex = await Assert.ThrowsAsync<WiseApiException>(() => api.ListAsync(CancellationToken.None));

        Assert.Equal("corr-1", ex.CorrelationId);
        Assert.Equal("trace-1", ex.TraceId);
    }
}
