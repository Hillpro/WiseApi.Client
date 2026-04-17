using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WiseApi.Client.Authentication;
using WiseApi.Client.Http;
using WiseApi.Client.Http.Handlers;
using WiseApi.Client.Services;

namespace WiseApi.Client.DependencyInjection;

/// <summary>DI helpers for registering <see cref="IWiseClient"/> and its services.</summary>
public static class WiseClientServiceCollectionExtensions
{
    /// <summary>
    /// Register the Wise client with options configured inline. Idempotent — subsequent
    /// calls re-apply the configuration but do not stack additional HTTP handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Callback to populate <see cref="WiseClientOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for the underlying typed HTTP client, for further customization.</returns>
    public static IHttpClientBuilder AddWiseClient(this IServiceCollection services, Action<WiseClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<WiseClientOptions>().Configure(configureOptions);
        return services.AddWiseClientCore();
    }

    /// <summary>Register the Wise client using an externally-configured <see cref="WiseClientOptions"/>.</summary>
    public static IHttpClientBuilder AddWiseClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<WiseClientOptions>();
        return services.AddWiseClientCore();
    }

    private static IHttpClientBuilder AddWiseClientCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IWiseCredentialsProvider>(sp =>
            sp.GetRequiredService<IOptions<WiseClientOptions>>().Value.ResolveCredentials());

        services.TryAddTransient<WiseAuthenticationHandler>();

        services.TryAddTransient<IProfilesApi, ProfilesApi>();
        services.TryAddTransient<IBalancesApi, BalancesApi>();
        services.TryAddTransient<IBalanceMovementsApi, BalanceMovementsApi>();
        services.TryAddTransient<IQuotesApi, QuotesApi>();
        services.TryAddTransient<IRatesApi, RatesApi>();
        services.TryAddTransient<IWiseClient, WiseClient>();

        if (services.Any(d => d.ServiceType == typeof(WiseClientMarker)))
        {
            // Second+ call: return a builder pointing at the existing typed-client registration
            // without adding another round of handlers / configure actions.
            return services.AddHttpClient<WiseHttpClient>();
        }

        services.AddSingleton<WiseClientMarker>();

        return services
            .AddHttpClient<WiseHttpClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<WiseClientOptions>>().Value;
                http.BaseAddress = options.ResolveBaseAddress();
                http.Timeout = options.Timeout;
            })
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WiseClientOptions>>().Value;
                return new WiseUserAgentHandler(options.UserAgent);
            })
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WiseClientOptions>>().Value;
                return options.AutoCorrelationId
                    ? new WiseCorrelationIdHandler()
                    : new PassthroughHandler();
            })
            .AddHttpMessageHandler<WiseAuthenticationHandler>();
    }

    private sealed class WiseClientMarker;

    private sealed class PassthroughHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => base.SendAsync(request, cancellationToken);
    }
}
