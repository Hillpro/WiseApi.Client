using WiseApi.Client.Http;
using WiseApi.Client.Models.Profiles;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IProfilesApi" />
public sealed class ProfilesApi : IProfilesApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="ProfilesApi"/>.</summary>
    public ProfilesApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Profile>> ListAsync(CancellationToken cancellationToken = default)
        => _http.GetAsync<IReadOnlyList<Profile>>("/v2/profiles", cancellationToken);

    /// <inheritdoc />
    public Task<Profile> GetAsync(long profileId, CancellationToken cancellationToken = default)
        => _http.GetAsync<Profile>($"/v2/profiles/{profileId}", cancellationToken);
}
