using System.Text.Json.Serialization;

namespace WiseApi.Client.Authentication.OAuth;

/// <summary>
/// Response payload from <c>POST /oauth/token</c>. Shared across all grant types.
/// User-token grants populate <see cref="RefreshToken"/> and related expiry fields;
/// <c>client_credentials</c> grants leave them <c>null</c>.
/// </summary>
public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null,
    [property: JsonPropertyName("refresh_token_expires_in")] int? RefreshTokenExpiresIn = null,
    [property: JsonPropertyName("expires_at")] DateTimeOffset? ExpiresAt = null,
    [property: JsonPropertyName("refresh_token_expires_at")] DateTimeOffset? RefreshTokenExpiresAt = null,
    [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt = null);
