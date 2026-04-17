using WiseApi.Client.Models.Quotes;

namespace WiseApi.Client.Services;

/// <summary>Quote operations (<c>/v3/profiles/{profileId}/quotes</c>).</summary>
public interface IQuotesApi
{
    /// <summary>
    /// Create a quote. For balance-to-balance conversions, set <c>payOut</c> to <c>"BALANCE"</c>.
    /// </summary>
    Task<Quote> CreateAsync(long profileId, CreateQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience helper: create a quote with <c>payOut = "BALANCE"</c> for a source-currency conversion.
    /// </summary>
    Task<Quote> CreateForBalanceConversionAsync(
        long profileId,
        string sourceCurrency,
        string targetCurrency,
        decimal sourceAmount,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieve a previously-created quote by ID.</summary>
    Task<Quote> GetAsync(long profileId, Guid quoteId, CancellationToken cancellationToken = default);
}
