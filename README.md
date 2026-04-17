# WiseApi.Client

[![NuGet](https://img.shields.io/nuget/v/WiseApi.Client.svg)](https://www.nuget.org/packages/WiseApi.Client)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Open-source .NET client for the [Wise (formerly TransferWise) Platform API](https://docs.wise.com/api-docs).
This initial release (`0.1.x`) focuses on **Multi-Currency Account** operations:
profiles, balances, FX quotes, balance conversions/moves, and exchange rates.

Built for **.NET 10**, ships with first-class `Microsoft.Extensions.DependencyInjection`,
`IHttpClientFactory`, and `Microsoft.Extensions.Logging` support.

---

## Install

```bash
dotnet add package WiseApi.Client
```

## Quick start

### Dependency injection (recommended)

```csharp
using WiseApi.Client;
using WiseApi.Client.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWiseClient(options =>
{
    options.Environment = WiseEnvironment.Sandbox;       // or Production
    options.ApiToken    = builder.Configuration["Wise:ApiToken"];
});

var app = builder.Build();

var wise    = app.Services.GetRequiredService<IWiseClient>();
var profile = (await wise.Profiles.ListAsync()).First();

// List balances
var balances = await wise.Balances.ListAsync(profile.Id);

// Rate + conversion
var rate  = await wise.Rates.GetLatestAsync("EUR", "USD");
var quote = await wise.Quotes.CreateForBalanceConversionAsync(profile.Id, "EUR", "USD", sourceAmount: 250m);
var movement = await wise.BalanceMovements.ConvertAsync(profile.Id, quote.Id);
```

### Without dependency injection

```csharp
using var wise = WiseClient.Create(new WiseClientOptions
{
    Environment = WiseEnvironment.Sandbox,
    ApiToken    = "<personal or user access token>",
});

var profiles = await wise.Profiles.ListAsync();
```

## Authentication

| Mode | How to wire it up | Notes |
| --- | --- | --- |
| **Personal API Token** | `options.ApiToken = "..."` | Fastest path. Works for personal & small business accounts. PSD2 restrictions apply in EU/UK. |
| **User Access Token** (OAuth `authorization_code` / `registration_code`) | Obtain out of band, then `options.ApiToken = accessToken` (or build your own `IWiseCredentialsProvider` that refreshes). | Full OAuth helpers are planned — see roadmap. |
| **Partner Client Credentials** | `options.Credentials = new OAuthClientCredentialsProvider(clientId, clientSecret, WiseEnvironment.Production);` | Handles token caching and renewal every ~12h. |

All credentials providers implement `IWiseCredentialsProvider` — plug in your own
for custom refresh/rotation logic.

## What's included

| Area | Surface | Status |
| --- | --- | --- |
| Profiles | `/v2/profiles`, `/v2/profiles/{id}` — personal & business discriminated types | ✅ |
| Multi-currency account | `/v4/profiles/{id}/multi-currency-account` retrieve, `/v4/multi-currency-account/eligibility` profile & location checks | ✅ |
| Balances | `/v4/profiles/{id}/balances` list/get/create/delete (STANDARD + SAVINGS) | ✅ |
| Balance movements | `/v2/profiles/{id}/balance-movements` conversions + same-currency moves with idempotency | ✅ |
| Quotes | `/v3/profiles/{id}/quotes` create/get, with BALANCE payOut helper | ✅ |
| Rates | `/v1/rates` current / at-time / history with grouping | ✅ |
| Errors | Parsed error envelope, rate-limit (`Retry-After`) & SCA-challenge exceptions, correlation IDs | ✅ |
| SCA signing (X-Signature) | — | Stubbed: surfaces `WiseScaChallengeException` with one-time token, signing TBD |
| OAuth authorization_code / refresh_token | — | Planned |
| Recipients, Transfers, Statements, Cards, Webhooks | — | Deferred (see [NOTES_FUTURE_WORK.md](NOTES_FUTURE_WORK.md)) |

## Error handling

All non-2xx responses throw `WiseApiException`. Two specific subclasses surface
important behaviours:

- `WiseRateLimitException` — 429 with `RetryAfter` populated from the header.
- `WiseScaChallengeException` — 403 with an `X-2FA-Approval` header. This indicates
  Wise wants a signed request; SCA signing is not yet implemented by this library.

Every exception carries `CorrelationId` and `TraceId` headers when Wise returned
them — forward those to Wise Support tickets for faster triage.

## Idempotency

`POST` endpoints that Wise declares idempotent (`/v4/.../balances`,
`/v2/.../balance-movements`) auto-generate an `X-idempotence-uuid` header on every
call. Override it per-call when you need to retry the exact same operation:

```csharp
var key = Guid.NewGuid();
try
{
    return await wise.BalanceMovements.ConvertAsync(profileId, quoteId, idempotencyKey: key);
}
catch (HttpRequestException)
{
    return await wise.BalanceMovements.ConvertAsync(profileId, quoteId, idempotencyKey: key); // safe retry
}
```

## Environments

| Enum | Base URL |
| --- | --- |
| `WiseEnvironment.Sandbox` *(default)* | `https://api.wise-sandbox.com` |
| `WiseEnvironment.Production` | `https://api.wise.com` |

Override entirely with `options.BaseAddress` when you run behind a corporate proxy or mock.

## Configuration surface

```csharp
public sealed class WiseClientOptions
{
    public WiseEnvironment Environment { get; set; } = WiseEnvironment.Sandbox;
    public Uri?            BaseAddress { get; set; }
    public string?         ApiToken    { get; set; }
    public IWiseCredentialsProvider? Credentials { get; set; }
    public string?         UserAgent   { get; set; }
    public bool            AutoCorrelationId { get; set; } = true;
    public TimeSpan        Timeout     { get; set; } = TimeSpan.FromSeconds(100);
}
```

`AddWiseClient` returns the underlying `IHttpClientBuilder`, so you can plug in
retry handlers (Polly), tracing, or additional middleware:

```csharp
services.AddWiseClient(cfg => cfg.ApiToken = "...")
        .AddStandardResilienceHandler();
```

## Contributing

PRs welcome. To run the test suite:

```bash
dotnet test -c Release
```

## License

[MIT](LICENSE). Not affiliated with Wise Payments Ltd.
