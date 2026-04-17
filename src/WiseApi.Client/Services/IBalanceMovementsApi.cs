using WiseApi.Client.Models.Balances;

namespace WiseApi.Client.Services;

/// <summary>
/// Balance movement operations (<c>/v2/profiles/{profileId}/balance-movements</c>):
/// convert funds between different-currency balances, or move funds between same-currency
/// standard/savings balances.
/// </summary>
public interface IBalanceMovementsApi
{
    /// <summary>
    /// Perform a cross-currency conversion between two <c>STANDARD</c> balances using a quote
    /// created with <c>payOut: BALANCE</c>.
    /// </summary>
    /// <param name="profileId">The profile that owns the balances.</param>
    /// <param name="quoteId">The quote ID obtained from <see cref="IQuotesApi.CreateAsync"/>.</param>
    /// <param name="idempotencyKey">Optional idempotency key. Auto-generated when <c>null</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BalanceMovement> ConvertAsync(
        long profileId,
        Guid quoteId,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move funds between two same-currency balances (e.g. from a STANDARD balance into a SAVINGS jar).
    /// </summary>
    Task<BalanceMovement> MoveAsync(
        long profileId,
        long sourceBalanceId,
        long targetBalanceId,
        Models.Money amount,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Low-level balance movement call. Prefer <see cref="ConvertAsync"/> or <see cref="MoveAsync"/>
    /// for the common cases.
    /// </summary>
    Task<BalanceMovement> CreateAsync(
        long profileId,
        BalanceMovementRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
