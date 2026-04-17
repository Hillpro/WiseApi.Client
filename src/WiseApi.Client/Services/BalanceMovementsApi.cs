using WiseApi.Client.Http;
using WiseApi.Client.Models;
using WiseApi.Client.Models.Balances;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IBalanceMovementsApi" />
public sealed class BalanceMovementsApi : IBalanceMovementsApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="BalanceMovementsApi"/>.</summary>
    public BalanceMovementsApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<BalanceMovement> ConvertAsync(
        long profileId,
        Guid quoteId,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => CreateAsync(profileId, BalanceMovementRequest.ForConversion(quoteId), idempotencyKey, cancellationToken);

    /// <inheritdoc />
    public Task<BalanceMovement> MoveAsync(
        long profileId,
        long sourceBalanceId,
        long targetBalanceId,
        Money amount,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(amount);
        return CreateAsync(
            profileId,
            BalanceMovementRequest.ForSameCurrencyMove(sourceBalanceId, targetBalanceId, amount),
            idempotencyKey,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<BalanceMovement> CreateAsync(
        long profileId,
        BalanceMovementRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var headers = new Dictionary<string, string>
        {
            [WiseHttpClient.IdempotencyHeader] = (idempotencyKey ?? Guid.NewGuid()).ToString("D"),
        };

        return _http.PostJsonAsync<BalanceMovementRequest, BalanceMovement>(
            $"/v2/profiles/{profileId}/balance-movements",
            request,
            headers,
            cancellationToken);
    }

    private static void ValidateRequest(BalanceMovementRequest request)
    {
        var hasQuote = request.QuoteId.HasValue;
        var hasAmount = request.Amount is not null;
        if (hasQuote == hasAmount)
        {
            throw new ArgumentException(
                "Exactly one of QuoteId (cross-currency conversion) or Amount (same-currency move) must be provided.",
                nameof(request));
        }

        if (hasAmount && (!request.SourceBalanceId.HasValue || !request.TargetBalanceId.HasValue))
        {
            throw new ArgumentException(
                "SourceBalanceId and TargetBalanceId are required for same-currency moves.",
                nameof(request));
        }
    }
}
