using WiseApi.Client.Models.MultiCurrencyAccounts;

namespace WiseApi.Client.Services;

/// <summary>
/// Multi-currency account (MCA) operations (<c>/v4/multi-currency-account/...</c> and
/// <c>/v4/profiles/{profileId}/multi-currency-account</c>): retrieve the MCA attached
/// to a profile, and check eligibility for a profile or a location.
/// </summary>
public interface IMultiCurrencyAccountsApi
{
    /// <summary>
    /// Retrieve the multi-currency account attached to a profile. Returns <c>null</c> when the
    /// profile does not yet have an MCA (Wise answers <c>404 Not Found</c> in that case).
    /// </summary>
    Task<MultiCurrencyAccount?> GetAsync(long profileId, CancellationToken cancellationToken = default);

    /// <summary>Check whether a specific profile is eligible to open a multi-currency account.</summary>
    Task<MultiCurrencyAccountEligibility> GetEligibilityAsync(long profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check MCA eligibility for a location. <paramref name="country"/> is a 2-letter ISO 3166
    /// code; <paramref name="state"/> is a 2-letter state/province code and is required when
    /// <paramref name="country"/> is <c>"US"</c>.
    /// </summary>
    Task<MultiCurrencyAccountEligibility> GetEligibilityForLocationAsync(
        string country,
        string? state = null,
        CancellationToken cancellationToken = default);
}
