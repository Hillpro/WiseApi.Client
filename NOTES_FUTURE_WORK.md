# Future work

Tracks deferred parts of the Wise Platform API, in rough priority order, with
pointers to the relevant endpoints. Kept out of `README.md` so the package page
stays focused on what's shipped.

## 1. SCA (Strong Customer Authentication) signing

Wise returns `HTTP 403` with an `X-2FA-Approval` one-time token for sensitive
operations (statements older than 90 days, large conversions, most profile and
account mutations in EU/UK). To complete the request you must re-send it with:

- `X-2FA-Approval`: the one-time token received in the challenge
- `X-Signature`: the token signed with your uploaded RSA private key (SHA256
  digest, `RSA-PKCS1-v1_5`)

The client currently surfaces the challenge as `WiseScaChallengeException`
with `OneTimeToken` populated but does not sign.

**Plan**
- Add `ISignatureProvider` abstraction in `WiseApi.Client.Authentication`.
- Add `RsaFileSignatureProvider(PEM path)` and `RsaInMemorySignatureProvider`.
- Add a `WiseScaRetryHandler` `DelegatingHandler` that, on 403 +
  `X-2FA-Approval`, signs the token and replays the original request with
  `X-Signature` added.
- Gate via `WiseClientOptions.SignatureProvider`.

Docs: <https://docs.wise.com/api-docs/features/2fa-strong-customer-authentication-sca/personal-token-sca>

## 2. OAuth user flows

Currently only `client_credentials` is bundled (partner-level). Still to add:

- `authorization_code` flow with a helper that constructs the Wise consent URL
  and exchanges the returned `code` for access + refresh tokens.
- `registration_code` flow for partners that create users via API.
- Automatic refresh using the refresh token before 12h expiry — similar
  caching approach to `OAuthClientCredentialsProvider`.

Endpoint: `POST /oauth/token`. Docs:
<https://docs.wise.com/api-docs/guides/send-money/choose-auth/direct-integration>

## 3. Recipients (full surface)

- `POST /v1/accounts` create recipient (with requirements-dynamic forms).
- `DELETE /v1/accounts/{id}`.
- `GET /v1/account-requirements` + `POST /v1/account-requirements` (dynamic field discovery).
- `GET /v1/quotes/{id}/account-requirements`.

The dynamic requirements API is the tricky bit — it returns a schema-of-fields
that changes per destination country/currency. Consider a typed DSL over
`JsonDocument` rather than code-generating strongly-typed payloads per
currency corridor.

## 4. Transfers

- `POST /v1/transfers` create transfer (from a quote + recipient).
- `POST /v3/profiles/{profileId}/transfers/{transferId}/payments` fund a
  transfer from a balance. This is the typical MCA-funded send flow.
- `GET /v1/transfers` list, `GET /v1/transfers/{id}`.
- `PUT /v1/transfers/{id}/cancel`.

## 5. Balance statements

`GET /v1/profiles/{profileId}/balance-statements/{balanceId}/statement.{ext}`
— seven formats (`json`, `csv`, `pdf`, `xlsx`, `xml` (CAMT.053), `mt940`,
`qif`). Max 469-day range. **SCA-protected every 90 days**, so this lands
alongside SCA support.

API: expose `IBalanceStatementsService.DownloadAsync(..., StatementFormat format)`
returning a `Stream` (via the existing `WiseHttpClient.GetRawAsync`).

## 6. Webhooks

- `GET / POST / DELETE /v3/profiles/{profileId}/subscriptions` webhook subscription CRUD.
- Signature verification helper for incoming Wise webhook POSTs (uses a
  Wise-published public key per environment; the library ships the PEM and
  verifies `X-Signature-SHA256` on request bodies).
- Strongly-typed event payloads for `balances#update`, `transfers#state-change`, etc.

## 7. Cards, Assets/Investments, Bulk settlement

Explicitly deferred until a consumer actually asks — these are narrower
surfaces and change more often. Flag for later.

## 8. Observability polish

- Activity/ActivitySource for each outgoing call (`WiseApi.Client` activity source name).
- Optional OpenTelemetry semantic attributes (`wise.profile.id`, `wise.endpoint.group`).
- A `WiseLoggingHandler` that emits redacted request/response pairs at `Debug`.

## 9. Source-generated JSON serialization

Currently uses reflection-based `System.Text.Json` for speed of iteration. Once
the model surface stabilises, add a `JsonSerializerContext` partial with all
request/response types so the client is AOT-friendly and the assembly is
smaller when trimmed.
