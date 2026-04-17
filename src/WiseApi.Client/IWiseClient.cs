using WiseApi.Client.Services;

namespace WiseApi.Client;

/// <summary>
/// Top-level facade that exposes every Wise service group the library currently implements.
/// Most consumers should inject the individual service interfaces (e.g. <see cref="IBalancesApi"/>)
/// when they only need one surface — resolve <see cref="IWiseClient"/> when you want a one-stop entry point.
/// </summary>
public interface IWiseClient
{
    /// <summary>Profile read/list.</summary>
    IProfilesApi Profiles { get; }

    /// <summary>Balance CRUD and MCA operations.</summary>
    IBalancesApi Balances { get; }

    /// <summary>Balance conversions and moves between balances.</summary>
    IBalanceMovementsApi BalanceMovements { get; }

    /// <summary>Quote creation for conversions and transfers.</summary>
    IQuotesApi Quotes { get; }

    /// <summary>Current and historical exchange rates.</summary>
    IRatesApi Rates { get; }
}
