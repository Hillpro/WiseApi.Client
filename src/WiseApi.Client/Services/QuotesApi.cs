using WiseApi.Client.Http;
using WiseApi.Client.Models.Quotes;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IQuotesApi" />
public sealed class QuotesApi : IQuotesApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="QuotesApi"/>.</summary>
    public QuotesApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<Quote> CreateAsync(long profileId, CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceAmount.HasValue == request.TargetAmount.HasValue)
        {
            throw new ArgumentException(
                "Exactly one of SourceAmount or TargetAmount must be provided.",
                nameof(request));
        }

        return _http.PostJsonAsync(
            $"/v3/profiles/{profileId}/quotes",
            request,
            WiseJsonContext.Default.CreateQuoteRequest,
            WiseJsonContext.Default.Quote,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Quote> CreateForBalanceConversionAsync(
        long profileId,
        string sourceCurrency,
        string targetCurrency,
        decimal sourceAmount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrency);

        var request = new CreateQuoteRequest(
            SourceCurrency: sourceCurrency,
            TargetCurrency: targetCurrency,
            SourceAmount: sourceAmount,
            PayOut: "BALANCE");
        return CreateAsync(profileId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Quote> GetAsync(long profileId, Guid quoteId, CancellationToken cancellationToken = default)
        => _http.GetAsync($"/v3/profiles/{profileId}/quotes/{quoteId:D}", WiseJsonContext.Default.Quote, cancellationToken);
}
