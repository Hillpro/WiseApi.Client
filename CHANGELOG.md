# Changelog

All notable changes to this project are documented here. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Native AOT and trimming support.
- `WiseHttpClient` overloads taking a `JsonTypeInfo<T>`, for AOT-safe calls to
  endpoints the library doesn't wrap. `LenientEnumConverter<T>` and
  `LenientDateTimeOffsetConverter` are public for use in your own `JsonSerializerContext`.

### Fixed
- Unrecognised, missing or `null` enum values in responses now read as
  `Unknown` instead of throwing or defaulting to a real value.

### Changed
- **Breaking.** `Unknown` is now the first member (value `0`) of every
  response enum. Only affects code relying on the numeric values.
- **Breaking.** `WiseJsonDefaults.Options` is now a property. It and the
  `WiseHttpClient` overloads without a `JsonTypeInfo<T>` are flagged as not
  AOT-safe. Pass `headers:` by name; a positional `null` is now ambiguous.

## [0.3.1] — 2026-04-18

### Changed
- Internal thread-synchronisation cleanup. No behaviour change.

## [0.3.0] — 2026-04-18

### Added
- OAuth user-token flows (`authorization_code`, `registration_code`,
  `refresh_token`) via `UserTokenProvider`, with automatic renewal and a
  `TokenRefreshed` event for persisting rotated refresh tokens.
- `ConsentUrl.Build(...)` for the Wise consent page.
- `WiseClientOptions` shortcut fields for each OAuth flow.

### Changed
- **Breaking.** `OAuthClientCredentialsProvider` renamed to
  `ClientCredentialsProvider`, now in `WiseApi.Client.Authentication.OAuth`.
- `WiseClient.Create(...)` now disposes the OAuth provider it creates for you.

## [0.2.0] — 2026-04-17

### Added
- `MultiCurrencyAccounts` service: retrieve a profile's multi-currency account
  (`null` if it has none) and check eligibility by profile or location.

## [0.1.1] — 2026-04-17

### Fixed
- Symbols package (`.snupkg`) is now published, so you can step into library
  source via SourceLink.
- Chunked responses without a `Content-Length` header are now handled correctly.

### Changed
- **Breaking.** `Profile.PublicId` is now `Guid?` instead of `string?`.

## [0.1.0] — 2026-04-16

Initial release.

### Added
- Profiles, balances, balance movements (conversions and same-currency moves),
  quotes and rates services, behind the `IWiseClient` facade.
- Typed errors: `WiseApiException`, `WiseRateLimitException` and
  `WiseScaChallengeException`.
- Personal-token and `client_credentials` authentication.
- `services.AddWiseClient(...)` DI registration, plus `WiseClient.Create(...)`
  for use without DI.

[Unreleased]: https://github.com/hillpro/WiseApi.Client/compare/v0.3.1...HEAD
[0.3.1]: https://github.com/hillpro/WiseApi.Client/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/hillpro/WiseApi.Client/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/hillpro/WiseApi.Client/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/hillpro/WiseApi.Client/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/hillpro/WiseApi.Client/releases/tag/v0.1.0
