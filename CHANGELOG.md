# Changelog

All notable changes to this project are documented here. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] — 2026-04-16

Initial release. Multi-Currency Account (MCA) surface.

### Added
- `IWiseClient` facade exposing `Profiles`, `Balances`, `BalanceMovements`,
  `Quotes`, and `Rates` service groups.
- `Profiles` service for `/v2/profiles` list/get with polymorphic
  `PersonalProfile` / `BusinessProfile` deserialization.
- `Balances` service for `/v4/profiles/{id}/balances` list/get/create/delete
  with STANDARD + SAVINGS types and auto idempotency keys.
- `BalanceMovements` service for `/v2/profiles/{id}/balance-movements`:
  `ConvertAsync(quoteId)` for cross-currency conversions and
  `MoveAsync(...)` for same-currency transfers.
- `Quotes` service for `/v3/profiles/{id}/quotes` with a
  `CreateForBalanceConversionAsync` helper that pre-sets `payOut: BALANCE`.
- `Rates` service for `/v1/rates`: latest, at-time, and grouped history.
- `WiseApiException`, `WiseRateLimitException` (with `Retry-After`),
  `WiseScaChallengeException` (surfaces the `X-2FA-Approval` token).
- `ApiTokenCredentialsProvider` and `OAuthClientCredentialsProvider`
  (client-credentials flow, token caching).
- `services.AddWiseClient(...)` DI helper with configurable
  `WiseClientOptions`, `IHttpClientFactory` integration, auto-correlation-id
  header, user-agent tagging, and logging hooks.
- `WiseClient.Create(options)` factory for non-DI usage.

### Known limitations
- SCA request signing (`X-Signature`) is not yet implemented. Endpoints
  requiring SCA will throw `WiseScaChallengeException`.
- OAuth `authorization_code` / `registration_code` / refresh-token flows are
  not yet bundled — fetch the token externally and wrap it with
  `ApiTokenCredentialsProvider`.
- Recipients, Transfers, Statements, Cards, Webhooks: deferred. See
  [NOTES_FUTURE_WORK.md](NOTES_FUTURE_WORK.md).

[Unreleased]: https://github.com/hillpro/WiseApi.Client/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/hillpro/WiseApi.Client/releases/tag/v0.1.0
