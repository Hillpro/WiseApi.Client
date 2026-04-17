namespace WiseApi.Client.Models.MultiCurrencyAccounts;

/// <summary>Level of multi-currency account access granted to a profile.</summary>
public enum MultiCurrencyAccountType
{
    /// <summary>Full multi-currency account — can send, receive, hold, and convert.</summary>
    Full,

    /// <summary>Receive-only account — limited to receiving funds.</summary>
    ReceiveOnly,

    /// <summary>Not eligible for a multi-currency account.</summary>
    Ineligible,
}

/// <summary>Multi-currency account attached to a Wise profile.</summary>
public sealed record MultiCurrencyAccount(
    long Id,
    long ProfileId,
    long? RecipientId,
    DateTimeOffset? CreationTime,
    DateTimeOffset? ModificationTime,
    bool Active,
    bool Eligible);

/// <summary>
/// Eligibility result for opening a multi-currency account. Returned for either a profile
/// (when <c>profileId</c> is supplied) or a location (<c>country</c> + optional <c>state</c>).
/// </summary>
/// <remarks>
/// <see cref="EligibilityCode"/> is kept as a string because Wise returns dotted values
/// (<c>"eligible"</c>, <c>"invalid.profile.type"</c>, <c>"invalid.country"</c>, <c>"invalid.state"</c>)
/// </remarks>
public sealed record MultiCurrencyAccountEligibility(
    bool Eligible,
    string? EligibilityCode,
    MultiCurrencyAccountType? AccountType,
    string? IneligibilityReason);
