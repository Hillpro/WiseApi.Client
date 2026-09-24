using WiseApi.Client.Http;
using WiseApi.Client.Models.Profiles;
using WiseApi.Client.Serialization;

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
        => _http.GetAsync("/v2/profiles", WiseJsonContext.Default.IReadOnlyListProfile, cancellationToken);

    /// <inheritdoc />
    public Task<Profile> GetAsync(long profileId, CancellationToken cancellationToken = default)
        => _http.GetAsync($"/v2/profiles/{profileId}", WiseJsonContext.Default.Profile, cancellationToken);
}
