namespace WiseApi.Client.Models;

/// <summary>An amount and its ISO 4217 currency code, as returned by many Wise endpoints.</summary>
public sealed record Money(decimal Value, string Currency);
