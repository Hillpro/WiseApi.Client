using WiseApi.Client.Authentication;
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
    private readonly IDisposable? _ownedInfrastructure;

    /// <summary>Create a new <see cref="WiseClient"/> from its service dependencies.</summary>
    public WiseClient(
        IProfilesApi profiles,
        IBalancesApi balances,
        IBalanceMovementsApi balanceMovements,
        IQuotesApi quotes,
        IRatesApi rates)
        : this(profiles, balances, balanceMovements, quotes, rates, owned: null) { }

    private WiseClient(
        IProfilesApi profiles,
        IBalancesApi balances,
        IBalanceMovementsApi balanceMovements,
        IQuotesApi quotes,
        IRatesApi rates,
        IDisposable? owned)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(balanceMovements);
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(rates);

        Profiles = profiles;
        Balances = balances;
        BalanceMovements = balanceMovements;
        Quotes = quotes;
        Rates = rates;
        _ownedInfrastructure = owned;
    }

    /// <inheritdoc />
    public IProfilesApi Profiles { get; }

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
    /// </remarks>
    public static WiseClient Create(WiseClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
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
        var balances = new BalancesApi(wiseHttp);
        var movements = new BalanceMovementsApi(wiseHttp);
        var quotes = new QuotesApi(wiseHttp);
        var rates = new RatesApi(wiseHttp);

        return new WiseClient(profiles, balances, movements, quotes, rates, http);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _ownedInfrastructure?.Dispose();
    }
}
