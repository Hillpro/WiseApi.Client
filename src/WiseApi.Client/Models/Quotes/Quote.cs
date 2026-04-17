namespace WiseApi.Client.Models.Quotes;

/// <summary>Current lifecycle state of a quote.</summary>
public enum QuoteStatus
{
    /// <summary>Quote has been created but not yet used.</summary>
    Pending,

    /// <summary>Quote has been accepted (a transfer was created from it).</summary>
    Accepted,

    /// <summary>Associated transfer has been funded.</summary>
    Funded,

    /// <summary>Quote has expired.</summary>
    Expired,
}

/// <summary>Whether the quote rate is guaranteed or floating.</summary>
public enum RateType
{
    /// <summary>Rate is locked until <see cref="Quote.RateExpirationTime"/>.</summary>
    Fixed,

    /// <summary>Rate floats — recomputed at funding time.</summary>
    Floating,
}

/// <summary>Whether the user supplied a source or target amount.</summary>
public enum ProvidedAmountType
{
    /// <summary>The source amount was specified.</summary>
    Source,

    /// <summary>The target amount was specified.</summary>
    Target,
}

/// <summary>Request body for <c>POST /v3/profiles/{profileId}/quotes</c>.</summary>
public sealed record CreateQuoteRequest(
    string SourceCurrency,
    string TargetCurrency,
    decimal? SourceAmount = null,
    decimal? TargetAmount = null,
    long? TargetAccount = null,
    string? PayOut = null,
    string? PreferredPayIn = null);

/// <summary>Fee components on a payment option.</summary>
public sealed record PaymentOptionFee(
    decimal? Transferwise,
    decimal? PayIn,
    decimal? Discount,
    decimal? Partner,
    decimal Total);

/// <summary>A single payment option within a quote.</summary>
public sealed record PaymentOption(
    bool Disabled,
    DateTimeOffset? EstimatedDelivery,
    string? FormattedEstimatedDelivery,
    PaymentOptionFee? Fee,
    decimal SourceAmount,
    decimal TargetAmount,
    string SourceCurrency,
    string TargetCurrency,
    string PayIn,
    string PayOut);

/// <summary>Advisory notice attached to a quote.</summary>
public sealed record QuoteNotice(string? Text, string? Link, string? Type);

/// <summary>A quote returned by the Wise Quotes API.</summary>
public sealed record Quote(
    Guid Id,
    string SourceCurrency,
    string TargetCurrency,
    decimal SourceAmount,
    decimal TargetAmount,
    string PayOut,
    string? PreferredPayIn,
    decimal Rate,
    DateTimeOffset? CreatedTime,
    long User,
    long Profile,
    RateType? RateType,
    DateTimeOffset? RateExpirationTime,
    ProvidedAmountType? ProvidedAmountType,
    IReadOnlyList<PaymentOption>? PaymentOptions,
    QuoteStatus? Status,
    DateTimeOffset? ExpirationTime,
    IReadOnlyList<QuoteNotice>? Notices);
