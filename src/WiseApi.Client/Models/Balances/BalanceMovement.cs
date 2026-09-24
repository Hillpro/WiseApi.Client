using System.Text.Json.Serialization;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Models.Balances;

/// <summary>Type of balance movement (from the API's perspective).</summary>
[JsonConverter(typeof(LenientEnumConverter<BalanceMovementType>))]
public enum BalanceMovementType
{
    /// <summary>A value this client version does not recognise.</summary>
    Unknown,

    /// <summary>Funds added to a balance.</summary>
    Deposit,

    /// <summary>Funds removed from a balance.</summary>
    Withdrawal,

    /// <summary>Cross-currency conversion between balances.</summary>
    Conversion,
}

/// <summary>Current state of a balance movement.</summary>
[JsonConverter(typeof(LenientEnumConverter<BalanceMovementState>))]
public enum BalanceMovementState
{
    /// <summary>A value this client version does not recognise.</summary>
    Unknown,

    /// <summary>In-flight.</summary>
    Pending,

    /// <summary>Successfully applied.</summary>
    Completed,

    /// <summary>Cancelled before completion.</summary>
    Cancelled,

    /// <summary>Reversed after completion.</summary>
    Reversed,
}

/// <summary>A balance amount snapshot immediately after a step or movement.</summary>
public sealed record BalanceSnapshot(long? Id, decimal Value, string Currency);

/// <summary>A sub-step inside a balance movement.</summary>
public sealed record BalanceMovementStep(
    long Id,
    string Type,
    DateTimeOffset? CreationTime,
    IReadOnlyList<BalanceSnapshot>? BalancesAfter,
    Money? SourceAmount,
    Money? TargetAmount,
    Money? Fee,
    decimal? Rate);

/// <summary>Response returned after creating a balance movement.</summary>
public sealed record BalanceMovement(
    long Id,
    BalanceMovementType Type,
    BalanceMovementState State,
    IReadOnlyList<BalanceSnapshot>? BalancesAfter,
    DateTimeOffset? CreationTime,
    Money? SourceAmount,
    Money? TargetAmount,
    decimal? Rate,
    IReadOnlyList<Money>? FeeAmounts,
    IReadOnlyList<BalanceMovementStep>? Steps);

/// <summary>
/// Request body for <c>POST /v2/profiles/{profileId}/balance-movements</c>.
/// Use <see cref="ForConversion"/> for cross-currency conversions (requires a quote with
/// <c>payOut: BALANCE</c>) or <see cref="ForSameCurrencyMove"/> for same-currency moves
/// between two of your own balances.
/// </summary>
public sealed record BalanceMovementRequest(
    Guid? QuoteId = null,
    long? SourceBalanceId = null,
    long? TargetBalanceId = null,
    Money? Amount = null)
{
    /// <summary>Build a cross-currency conversion request from a quote ID.</summary>
    public static BalanceMovementRequest ForConversion(Guid quoteId)
        => new(QuoteId: quoteId);

    /// <summary>Build a same-currency move between two balances.</summary>
    public static BalanceMovementRequest ForSameCurrencyMove(long sourceBalanceId, long targetBalanceId, Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        return new BalanceMovementRequest(
            SourceBalanceId: sourceBalanceId,
            TargetBalanceId: targetBalanceId,
            Amount: amount);
    }
}
