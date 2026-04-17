using System.Net;
using WiseApi.Client.Http;
using WiseApi.Client.Models.MultiCurrencyAccounts;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IMultiCurrencyAccountsApi" />
public sealed class MultiCurrencyAccountsApi : IMultiCurrencyAccountsApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="MultiCurrencyAccountsApi"/>.</summary>
    public MultiCurrencyAccountsApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public async Task<MultiCurrencyAccount?> GetAsync(long profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetAsync<MultiCurrencyAccount>(
                $"/v4/profiles/{profileId}/multi-currency-account",
                cancellationToken).ConfigureAwait(false);
        }
        catch (WiseApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Task<MultiCurrencyAccountEligibility> GetEligibilityAsync(long profileId, CancellationToken cancellationToken = default)
        => _http.GetAsync<MultiCurrencyAccountEligibility>(
            $"/v4/multi-currency-account/eligibility?profileId={profileId}",
            cancellationToken);

    /// <inheritdoc />
    public Task<MultiCurrencyAccountEligibility> GetEligibilityForLocationAsync(
        string country,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        if (string.Equals(country, "US", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("A 2-letter state code is required when country is 'US'.", nameof(state));
        }

        var uri = $"/v4/multi-currency-account/eligibility?country={Uri.EscapeDataString(country)}";
        if (!string.IsNullOrWhiteSpace(state))
        {
            uri += $"&state={Uri.EscapeDataString(state)}";
        }

        return _http.GetAsync<MultiCurrencyAccountEligibility>(uri, cancellationToken);
    }
}
