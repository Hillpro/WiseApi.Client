namespace WiseApi.Client.Authentication.OAuth;

/// <summary>
/// Builds the Wise consent URL a browser should be redirected to in order to obtain a
/// user authorisation code (the <c>authorization_code</c> OAuth 2.0 grant).
/// </summary>
/// <remarks>
/// The consent page is served from the public Wise web host (<c>wise.com</c> /
/// <c>wise-sandbox.com</c>), <b>not</b> the <c>api.*</c> host used for API calls.
/// Wise validates the <c>redirect_uri</c> against the value registered when your
/// partner credentials were issued — it must match exactly, including scheme and path.
/// </remarks>
public static class ConsentUrl
{
    /// <summary>
    /// Build a consent URL for the <c>authorization_code</c> grant.
    /// </summary>
    /// <param name="clientId">Your partner client ID.</param>
    /// <param name="redirectUri">The pre-registered callback URI. Must match Wise's stored value exactly.</param>
    /// <param name="state">Opaque CSRF token. Verify this matches on the callback.</param>
    /// <param name="environment">Which Wise environment hosts the consent page. Defaults to Sandbox.</param>
    public static Uri Build(string clientId, Uri redirectUri, string state, WiseEnvironment environment = WiseEnvironment.Sandbox)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var host = environment switch
        {
            WiseEnvironment.Production => "https://wise.com",
            WiseEnvironment.Sandbox => "https://wise-sandbox.com",
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown Wise environment."),
        };

        var query = $"client_id={Uri.EscapeDataString(clientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri.ToString())}"
            + "&response_type=code"
            + $"&state={Uri.EscapeDataString(state)}";

        return new Uri($"{host}/oauth/authorize?{query}");
    }
}
