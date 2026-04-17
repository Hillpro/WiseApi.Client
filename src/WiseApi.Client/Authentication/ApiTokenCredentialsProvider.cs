namespace WiseApi.Client.Authentication;

/// <summary>
/// Provides a static API token — a Personal API Token, a User Access Token issued via
/// OAuth, or any pre-obtained bearer value. The token is used verbatim for every request.
/// </summary>
public sealed class ApiTokenCredentialsProvider : IWiseCredentialsProvider
{
    private readonly string _token;

    /// <summary>Create a new <see cref="ApiTokenCredentialsProvider"/>.</summary>
    /// <param name="token">The bearer token. Must be non-empty.</param>
    public ApiTokenCredentialsProvider(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _token = token;
    }

    /// <inheritdoc />
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_token);
}
