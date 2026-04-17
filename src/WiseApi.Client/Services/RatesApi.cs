using System.Globalization;
using WiseApi.Client.Http;
using WiseApi.Client.Models.Rates;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IRatesApi" />
public sealed class RatesApi : IRatesApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="RatesApi"/>.</summary>
    public RatesApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Rate>> GetAllAsync(CancellationToken cancellationToken = default)
        => _http.GetAsync<IReadOnlyList<Rate>>("/v1/rates", cancellationToken);

    /// <inheritdoc />
    public async Task<Rate?> GetLatestAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        var result = await _http.GetAsync<IReadOnlyList<Rate>>(
            $"/v1/rates?source={Uri.EscapeDataString(sourceCurrency)}&target={Uri.EscapeDataString(targetCurrency)}",
            cancellationToken).ConfigureAwait(false);
        return result is { Count: > 0 } ? result[0] : null;
    }

    /// <inheritdoc />
    public async Task<Rate?> GetAtAsync(string sourceCurrency, string targetCurrency, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        var time = at.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var result = await _http.GetAsync<IReadOnlyList<Rate>>(
            $"/v1/rates?source={Uri.EscapeDataString(sourceCurrency)}&target={Uri.EscapeDataString(targetCurrency)}&time={Uri.EscapeDataString(time)}",
            cancellationToken).ConfigureAwait(false);
        return result is { Count: > 0 } ? result[0] : null;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Rate>> GetHistoryAsync(
        string sourceCurrency,
        string targetCurrency,
        DateTimeOffset from,
        DateTimeOffset until,
        RateGrouping grouping,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);
        if (until < from)
        {
            throw new ArgumentException("'until' must be greater than or equal to 'from'.", nameof(until));
        }

        var uri = $"/v1/rates?source={Uri.EscapeDataString(sourceCurrency)}" +
                  $"&target={Uri.EscapeDataString(targetCurrency)}" +
                  $"&from={Uri.EscapeDataString(Format(from))}" +
                  $"&to={Uri.EscapeDataString(Format(until))}" +
                  $"&group={FormatGrouping(grouping)}";
        return _http.GetAsync<IReadOnlyList<Rate>>(uri, cancellationToken);
    }

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatGrouping(RateGrouping grouping) => grouping switch
    {
        RateGrouping.Day => "day",
        RateGrouping.Hour => "hour",
        RateGrouping.Minute => "minute",
        _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unknown rate grouping."),
    };
}
