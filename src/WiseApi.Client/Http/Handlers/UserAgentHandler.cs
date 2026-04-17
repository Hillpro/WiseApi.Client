using System.Net.Http.Headers;
using System.Reflection;

namespace WiseApi.Client.Http.Handlers;

/// <summary>
/// <see cref="DelegatingHandler"/> that attaches a stable <c>User-Agent</c> identifying
/// the library version, plus any caller-supplied suffix.
/// </summary>
public sealed class WiseUserAgentHandler : DelegatingHandler
{
    private static readonly string DefaultUserAgent = BuildDefault();
    private readonly string _userAgent;

    /// <summary>Create a handler with just the default identifier.</summary>
    public WiseUserAgentHandler() : this(suffix: null) { }

    /// <summary>Create a handler with an additional suffix appended after the default.</summary>
    /// <param name="suffix">Optional caller identifier, e.g. <c>"MyApp/1.4"</c>.</param>
    public WiseUserAgentHandler(string? suffix)
    {
        _userAgent = string.IsNullOrWhiteSpace(suffix)
            ? DefaultUserAgent
            : $"{DefaultUserAgent} {suffix}";
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.UserAgent.Count == 0)
        {
            if (ProductInfoHeaderValue.TryParse(_userAgent, out var parsed))
            {
                request.Headers.UserAgent.Add(parsed);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string BuildDefault()
    {
        var asm = typeof(WiseUserAgentHandler).Assembly;
        var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "0.0.0";
        var cleanVersion = version.Split('+')[0];
        return $"WiseApi.Client/{cleanVersion}";
    }
}
