using System.Text.Json.Serialization;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Models.Balances;

/// <summary>Type of balance account.</summary>
[JsonConverter(typeof(LenientEnumConverter<BalanceType>))]
public enum BalanceType
{
    /// <summary>A value this client version does not recognise. Never send it to Wise.</summary>
    Unknown,

    /// <summary>A standard balance — only one per currency per profile.</summary>
    Standard,

    /// <summary>A savings &quot;jar&quot; balance — multiple allowed per currency; requires a <see cref="Balance.Name"/>.</summary>
    Savings,
}

/// <summary>Investment state of a balance.</summary>
[JsonConverter(typeof(LenientEnumConverter<InvestmentState>))]
public enum InvestmentState
{
    /// <summary>Wise reported <c>UNKNOWN</c>, or returned a value this client version does not recognise.</summary>
    Unknown,

    /// <summary>Not invested.</summary>
    NotInvested,

    /// <summary>Invested.</summary>
    Invested,

    /// <summary>Being invested.</summary>
    Investing,

    /// <summary>Being divested.</summary>
    Divesting,
}

/// <summary>Icon associated with a balance (typically an emoji).</summary>
public sealed record BalanceIcon(string Type, string Value);

/// <summary>A Wise multi-currency balance account.</summary>
public sealed record Balance(
    long Id,
    string Currency,
    BalanceType Type,
    string? Name,
    BalanceIcon? Icon,
    InvestmentState? InvestmentState,
    Money Amount,
    Money? ReservedAmount,
    Money? CashAmount,
    Money? TotalWorth,
    DateTimeOffset? CreationTime,
    DateTimeOffset? ModificationTime,
    bool? Visible);

/// <summary>Request body for creating a new balance.</summary>
public sealed record CreateBalanceRequest(string Currency, BalanceType Type, string? Name = null);
