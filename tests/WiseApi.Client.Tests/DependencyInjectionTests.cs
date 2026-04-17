using Microsoft.Extensions.DependencyInjection;
using WiseApi.Client.DependencyInjection;
using WiseApi.Client.Services;
using WiseApi.Client.Tests.Infrastructure;

namespace WiseApi.Client.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddWiseClient_registers_all_service_interfaces()
    {
        var services = new ServiceCollection();
        services.AddWiseClient(options =>
        {
            options.ApiToken = "test-token";
            options.Environment = WiseEnvironment.Sandbox;
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IProfilesApi>());
        Assert.NotNull(provider.GetService<IBalancesApi>());
        Assert.NotNull(provider.GetService<IBalanceMovementsApi>());
        Assert.NotNull(provider.GetService<IQuotesApi>());
        Assert.NotNull(provider.GetService<IRatesApi>());
        Assert.NotNull(provider.GetService<IWiseClient>());
    }

    [Fact]
    public void AddWiseClient_propagates_base_address_and_accepts_custom_http_builder_config()
    {
        var services = new ServiceCollection();
        var builder = services.AddWiseClient(options =>
        {
            options.ApiToken = "t";
            options.Environment = WiseEnvironment.Production;
        });
        builder.ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(7));

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var http = factory.CreateClient(nameof(WiseApi.Client.Http.WiseHttpClient));
        Assert.Equal(new Uri("https://api.wise.com"), http.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(7), http.Timeout);
    }

    [Fact]
    public async Task AddWiseClient_pipeline_attaches_auth_user_agent_and_correlation_headers()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson("[]");
        var services = new ServiceCollection();
        services
            .AddWiseClient(options =>
            {
                options.ApiToken = "secret-token";
                options.Environment = WiseEnvironment.Sandbox;
                options.UserAgent = "MyApp/1.0";
            })
            .ConfigurePrimaryHttpMessageHandler(() => stub);

        using var provider = services.BuildServiceProvider();
        var profiles = provider.GetRequiredService<IProfilesApi>();
        await profiles.ListAsync(CancellationToken.None);

        var request = Assert.Single(stub.Requests);
        Assert.Equal("Bearer secret-token", request.Headers["Authorization"]);
        Assert.Contains("WiseApi.Client/", request.Headers["User-Agent"]);
        Assert.Contains("MyApp/1.0", request.Headers["User-Agent"]);
        Assert.True(request.Headers.ContainsKey("X-External-Correlation-Id"));
        Assert.True(Guid.TryParse(request.Headers["X-External-Correlation-Id"], out _));
    }

    [Fact]
    public async Task AddWiseClient_is_idempotent_no_duplicate_handlers()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson("[]");
        var services = new ServiceCollection();
        services.AddWiseClient(options => options.ApiToken = "tok");
        services.AddWiseClient(options => options.ApiToken = "tok");

        services
            .AddHttpClient<WiseApi.Client.Http.WiseHttpClient>()
            .ConfigurePrimaryHttpMessageHandler(() => stub);

        using var provider = services.BuildServiceProvider();
        var profiles = provider.GetRequiredService<IProfilesApi>();
        await profiles.ListAsync(CancellationToken.None);

        var request = Assert.Single(stub.Requests);
        // A duplicated pipeline would produce two User-Agent headers (comma-joined by StubHttpMessageHandler).
        var userAgent = request.Headers["User-Agent"];
        Assert.Single(
            userAgent.Split(',', StringSplitOptions.RemoveEmptyEntries),
            v => v.Contains("WiseApi.Client/", StringComparison.Ordinal));
        Assert.Equal("Bearer tok", request.Headers["Authorization"]);
    }

    [Fact]
    public async Task AddWiseClient_respects_AutoCorrelationId_false()
    {
        var stub = new StubHttpMessageHandler().EnqueueJson("[]");
        var services = new ServiceCollection();
        services
            .AddWiseClient(options =>
            {
                options.ApiToken = "t";
                options.AutoCorrelationId = false;
            })
            .ConfigurePrimaryHttpMessageHandler(() => stub);

        using var provider = services.BuildServiceProvider();
        var profiles = provider.GetRequiredService<IProfilesApi>();
        await profiles.ListAsync(CancellationToken.None);

        var request = Assert.Single(stub.Requests);
        Assert.False(request.Headers.ContainsKey("X-External-Correlation-Id"));
    }
}
