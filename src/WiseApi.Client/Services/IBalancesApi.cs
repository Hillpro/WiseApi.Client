using WiseApi.Client.Models.Balances;

namespace WiseApi.Client.Services;

/// <summary>
/// Balance CRUD operations (<c>/v4/profiles/{profileId}/balances</c>) for a profile's
/// multi-currency account. For MCA-level operations (eligibility, retrieve MCA),
/// see <see cref="IMultiCurrencyAccountsApi"/>.
/// </summary>
public interface IBalancesApi
{
    /// <summary>List balances of the given types for a profile. Defaults to <see cref="BalanceType.Standard"/>.</summary>
    /// <param name="profileId">The profile that owns the balances.</param>
    /// <param name="types">Types to include. Pass <c>null</c> or empty for <see cref="BalanceType.Standard"/> only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Balance>> ListAsync(
        long profileId,
        IReadOnlyCollection<BalanceType>? types = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieve a single balance by ID.</summary>
    Task<Balance> GetAsync(long profileId, long balanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new balance. <c>STANDARD</c> balances are unique per currency; <c>SAVINGS</c> balances
    /// (Jars) require a <c>name</c> and support multiple per currency.
    /// </summary>
    /// <param name="profileId">The profile that will own the new balance.</param>
    /// <param name="request">Currency + type (+ name for SAVINGS).</param>
    /// <param name="idempotencyKey">Optional idempotency key. Auto-generated when <c>null</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Balance> CreateAsync(
        long profileId,
        CreateBalanceRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>Delete a balance. The balance must be at zero for the call to succeed.</summary>
    Task DeleteAsync(long profileId, long balanceId, CancellationToken cancellationToken = default);
}
