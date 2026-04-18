# Changelog

All notable changes to this project are documented here. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] — 2026-04-18

Full OAuth 2.0 user-token support.

### Added
- OAuth user-token flows: `authorization_code`, `registration_code`,
  `refresh_token`. New `UserTokenProvider` with a factory per seed, 12h
  auto-refresh, `CurrentRefreshToken` property, and `TokenRefreshed` event
  for persisting rotated refresh tokens.
- OAuth `ConsentUrl.Build(clientId, redirectUri, state, environment)` to
  construct the Wise consent-page URL.
- `WiseClientOptions` shortcut fields for every OAuth flow: `ClientId`,
  `ClientSecret`, `AuthorizationCode`, `RegistrationCode`, `UserEmail`,
  `RefreshToken`, `RedirectUri`.
- `TokenResponse` record covering every `/oauth/token` response field.
- New `WiseApi.Client.Authentication.OAuth` namespace.

### Changed
- **Breaking.** `OAuthClientCredentialsProvider` renamed to
  `ClientCredentialsProvider` and moved into the new
  `WiseApi.Client.Authentication.OAuth` namespace. Update your `using`.
- `WiseClient.Create(...)` disposes an implicit OAuth provider it builds
  on your behalf; an explicit `WiseClientOptions.Credentials` is still
  owned by the caller.

## [0.2.0] — 2026-04-17

### Added
- `MultiCurrencyAccounts` service: retrieve a profile's MCA
  (`/v4/profiles/{id}/multi-currency-account`) and check eligibility for a
  profile or location (`/v4/multi-currency-account/eligibility`).
  `GetAsync` returns `null` when the profile has no MCA yet (Wise 404).

## [0.1.1] — 2026-04-17

Follow-ups from v0.1.0 and a small set of polish items.
No runtime API was renamed.

### Fixed
- Symbols package (`.snupkg`) is now published. Switched to
 `DebugType=portable` so consumers can step into library source via SourceLink.

### Changed
- **Breaking.** `Profile.PublicId` is now `Guid?` instead of `string?`.
  Wise always returns a UUID in this field.
- `WiseHttpClient` no longer trusts the `Content-Length` header when
  deciding whether a response body is empty. Chunked responses with no
  `Content-Length` are now correctly handled.
- Bumped `Microsoft.SourceLink.GitHub` from `8.0.0` to `10.0.202`.

### Added
- Direct unit tests for `LenientDateTimeOffsetConverter` covering all
  three Wise timestamp shapes (ISO-Z, naive, and the non-standard
  compact `+0000` offset returned by `/v1/rates`).
- Test that `WiseAuthenticationHandler` does *not* call the credentials
  provider when the caller has pre-set an `Authorization` header.

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
  (client-credentials flow, token caching, race-free fast path).
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

[Unreleased]: https://github.com/hillpro/WiseApi.Client/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/hillpro/WiseApi.Client/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/hillpro/WiseApi.Client/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/hillpro/WiseApi.Client/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/hillpro/WiseApi.Client/releases/tag/v0.1.0
