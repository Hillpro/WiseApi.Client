using WiseApi.Client.Http;
using WiseApi.Client.Http.Handlers;
using WiseApi.Client.Services;

namespace WiseApi.Client;

/// <summary>
/// Default implementation of <see cref="IWiseClient"/>. In most applications, prefer registering
/// via <c>services.AddWiseClient(...)</c> and injecting <see cref="IWiseClient"/>.
/// Use <see cref="Create(WiseClientOptions)"/> when you can't use DI.
/// </summary>
public sealed class WiseClient : IWiseClient, IDisposable
{
    private readonly IReadOnlyList<IDisposable> _ownedInfrastructure;

    /// <summary>Create a new <see cref="WiseClient"/> from its service dependencies.</summary>
    public WiseClient(
        IProfilesApi profiles,
        IMultiCurrencyAccountsApi multiCurrencyAccounts,
        IBalancesApi balances,
        IBalanceMovementsApi balanceMovements,
        IQuotesApi quotes,
        IRatesApi rates)
        : this(profiles, multiCurrencyAccounts, balances, balanceMovements, quotes, rates, owned: []) { }

    private WiseClient(
        IProfilesApi profiles,
        IMultiCurrencyAccountsApi multiCurrencyAccounts,
        IBalancesApi balances,
        IBalanceMovementsApi balanceMovements,
        IQuotesApi quotes,
        IRatesApi rates,
        IReadOnlyList<IDisposable> owned)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(multiCurrencyAccounts);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(balanceMovements);
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(rates);

        Profiles = profiles;
        MultiCurrencyAccounts = multiCurrencyAccounts;
        Balances = balances;
        BalanceMovements = balanceMovements;
        Quotes = quotes;
        Rates = rates;
        _ownedInfrastructure = owned;
    }

    /// <inheritdoc />
    public IProfilesApi Profiles { get; }

    /// <inheritdoc />
    public IMultiCurrencyAccountsApi MultiCurrencyAccounts { get; }

    /// <inheritdoc />
    public IBalancesApi Balances { get; }

    /// <inheritdoc />
    public IBalanceMovementsApi BalanceMovements { get; }

    /// <inheritdoc />
    public IQuotesApi Quotes { get; }

    /// <inheritdoc />
    public IRatesApi Rates { get; }

    /// <summary>
    /// Create a fully-wired client without DI. Builds its own <see cref="HttpClient"/> and handler
    /// pipeline from <paramref name="options"/>. Dispose the returned client when you're done.
    /// </summary>
    /// <remarks>
    /// Intended for short-lived tools, scripts, and tests. For long-running processes prefer
    /// <c>services.AddWiseClient(...)</c> so <see cref="IHttpClientFactory"/> manages handler
    /// lifetime and DNS refresh — the <see cref="SocketsHttpHandler"/> built here uses its
    /// default <c>PooledConnectionLifetime</c> (infinite), so DNS changes won't be picked up
    /// across the lifetime of the returned client.
    /// <para>
    /// If <see cref="WiseClientOptions.Credentials"/> is left <c>null</c> and the options imply
    /// a provider we construct (e.g. <see cref="WiseClientOptions.RefreshToken"/>), the implicit
    /// provider is tracked and disposed alongside the client. An externally-supplied
    /// <see cref="WiseClientOptions.Credentials"/> is assumed to be owned by the caller.
    /// </para>
    /// </remarks>
    public static WiseClient Create(WiseClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var callerOwnedCredentials = options.Credentials is not null;
        var credentials = options.ResolveCredentials();

        var primary = new SocketsHttpHandler();
        DelegatingHandler pipeline = new WiseAuthenticationHandler(credentials) { InnerHandler = primary };
        if (options.AutoCorrelationId)
        {
            pipeline = new WiseCorrelationIdHandler { InnerHandler = pipeline };
        }

        pipeline = new WiseUserAgentHandler(options.UserAgent) { InnerHandler = pipeline };

        var http = new HttpClient(pipeline, disposeHandler: true)
        {
            BaseAddress = options.ResolveBaseAddress(),
            Timeout = options.Timeout,
        };

        var wiseHttp = new WiseHttpClient(http);
        var profiles = new ProfilesApi(wiseHttp);
        var mca = new MultiCurrencyAccountsApi(wiseHttp);
        var balances = new BalancesApi(wiseHttp);
        var movements = new BalanceMovementsApi(wiseHttp);
        var quotes = new QuotesApi(wiseHttp);
        var rates = new RatesApi(wiseHttp);

        var owned = new List<IDisposable> { http };
        if (!callerOwnedCredentials && credentials is IDisposable disposable)
        {
            owned.Add(disposable);
        }

        return new WiseClient(profiles, mca, balances, movements, quotes, rates, owned);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var item in _ownedInfrastructure)
        {
            item.Dispose();
        }
    }
}
