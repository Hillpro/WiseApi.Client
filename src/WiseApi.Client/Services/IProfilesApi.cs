using WiseApi.Client.Models.Profiles;

namespace WiseApi.Client.Services;

/// <summary>Profile read/list operations (<c>/v2/profiles</c>).</summary>
public interface IProfilesApi
{
    /// <summary>List all profiles belonging to the authenticated user.</summary>
    Task<IReadOnlyList<Profile>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieve a single profile by its ID.</summary>
    Task<Profile> GetAsync(long profileId, CancellationToken cancellationToken = default);
}
