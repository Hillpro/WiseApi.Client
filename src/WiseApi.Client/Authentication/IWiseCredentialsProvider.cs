namespace WiseApi.Client.Authentication;

/// <summary>
/// Supplies bearer tokens to the Wise HTTP pipeline. Implementations may cache
/// and refresh tokens as needed (e.g. OAuth client-credentials or refresh-token flows).
/// </summary>
public interface IWiseCredentialsProvider
{
    /// <summary>
    /// Returns a currently-valid access token. Implementations should refresh transparently when close to expiry.
    /// </summary>
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
