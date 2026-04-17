using WiseApi.Client.Models.Rates;

namespace WiseApi.Client.Services;

/// <summary>Exchange-rate operations (<c>/v1/rates</c>).</summary>
public interface IRatesApi
{
    /// <summary>Fetch the latest rates for all currency pairs.</summary>
    Task<IReadOnlyList<Rate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetch the latest rate for a single currency pair.</summary>
    Task<Rate?> GetLatestAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken = default);

    /// <summary>Fetch the rate for a pair at a specific historical moment.</summary>
    Task<Rate?> GetAtAsync(string sourceCurrency, string targetCurrency, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Fetch the rate history for a pair over an interval, grouped at the given interval.</summary>
    Task<IReadOnlyList<Rate>> GetHistoryAsync(
        string sourceCurrency,
        string targetCurrency,
        DateTimeOffset from,
        DateTimeOffset until,
        RateGrouping grouping,
        CancellationToken cancellationToken = default);
}
