using WiseApi.Client.Authentication;

namespace WiseApi.Client;

/// <summary>Options for configuring a <see cref="WiseClient"/>.</summary>
public sealed class WiseClientOptions
{
    /// <summary>Target environment. Defaults to <see cref="WiseEnvironment.Sandbox"/> for safety.</summary>
    public WiseEnvironment Environment { get; set; } = WiseEnvironment.Sandbox;

    /// <summary>
    /// Override for the API base address. When set, takes precedence over <see cref="Environment"/>.
    /// Only needed for mocks or a proxy — leave <c>null</c> in production.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Static API bearer token (Personal API Token, User Access Token or Client-Credentials token).
    /// Convenient shortcut for <see cref="Credentials"/> — if set and <see cref="Credentials"/> is <c>null</c>,
    /// an <see cref="ApiTokenCredentialsProvider"/> will be created automatically.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Pluggable credentials provider. Takes precedence over <see cref="ApiToken"/>.
    /// Use this for OAuth (<see cref="OAuthClientCredentialsProvider"/>) or custom token refresh logic.
    /// </summary>
    public IWiseCredentialsProvider? Credentials { get; set; }

    /// <summary>Optional user-agent fragment appended after the library's default.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When <c>true</c>, every outgoing request gets an auto-generated
    /// <c>X-External-Correlation-Id</c> header if the caller didn't supply one. Defaults to <c>true</c>.
    /// </summary>
    public bool AutoCorrelationId { get; set; } = true;

    /// <summary>Default per-request timeout. Only used by the non-DI <see cref="WiseClient.Create(WiseClientOptions)"/> factory.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    internal Uri ResolveBaseAddress() => BaseAddress ?? Environment.BaseAddress();

    internal IWiseCredentialsProvider ResolveCredentials()
    {
        if (Credentials is not null)
        {
            return Credentials;
        }

        if (!string.IsNullOrWhiteSpace(ApiToken))
        {
            return new ApiTokenCredentialsProvider(ApiToken!);
        }

        throw new InvalidOperationException(
            $"No Wise credentials configured. Set either {nameof(ApiToken)} or {nameof(Credentials)}.");
    }
}
