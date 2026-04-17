using System.Net.Http.Headers;
using WiseApi.Client.Authentication;

namespace WiseApi.Client.Http.Handlers;

/// <summary>
/// <see cref="DelegatingHandler"/> that attaches a bearer token (from <see cref="IWiseCredentialsProvider"/>)
/// to every outgoing request, unless the caller already supplied an <c>Authorization</c> header.
/// </summary>
public sealed class WiseAuthenticationHandler : DelegatingHandler
{
    private readonly IWiseCredentialsProvider _credentials;

    /// <summary>Create a new <see cref="WiseAuthenticationHandler"/>.</summary>
    public WiseAuthenticationHandler(IWiseCredentialsProvider credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _credentials = credentials;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = await _credentials.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
