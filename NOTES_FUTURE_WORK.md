# Future work

Tracks deferred parts of the Wise Platform API, in target-release order,
with pointers to the relevant endpoints. Kept out of `README.md` so the
package page stays focused on what's shipped.

## Release roadmap

| Target | Scope |
| --- | --- |
| v0.3.1 | Concurrency primitives cleanup: drop `volatile` fields, use `Volatile.Read` / `Volatile.Write` at the sites that need memory ordering. |
| v0.4.0 | §1 Recipients (full surface, including dynamic-requirements discovery) |
| v0.5.0 | §2 Transfers (create / list / get / cancel; funding deferred to v0.6.0) |
| v0.6.0 | §3 Minimal SCA surface (`IScaChallengeHandler`) + fund-transfer endpoint + §4 Balance statements |
| later  | §3 Full in-library SCA (JOSE layer, OTT service, reference challenge handler) |
| later  | §5 Webhooks, §6 Cards/Assets/Bulk, §7 Observability polish, §8 Source-generated JSON |

## 1. Recipients (v0.4.0)

- `GET /v1/quotes/{quoteId}/account-requirements` and
  `POST /v1/quotes/{quoteId}/account-requirements` — dynamic field discovery.
  Also `GET /v1/account-requirements?source=…&target=…&sourceAmount=…` (quoteless,
  discouraged by Wise).
- `POST /v1/accounts` create recipient. Optional `?refund=true` for refund recipients,
  `?addressRequired=true` to force-collect address, `originatorLegalEntityType=BUSINESS|PRIVATE`
  for third-party-partner flows.
- `DELETE /v1/accounts/{id}` remove a recipient.
- `POST /v1/accounts/check` compatibility check (recipient × quote).

Dynamic requirements API:

- The `details` payload has **no fixed shape** — it varies by currency + country +
  payout method (GBP sort_code, EUR IBAN, USD ACH vs Swift, INR IFSC, KRW PayGate,
  …). Codegen-per-corridor rots; a typed DSL over a schema-of-fields ages better.
- Discovery is a **conversation**: fields with `refreshRequirementsOnChange: true`
  (country, legalType, payout-method choice) require POSTing the partial payload
  to get an updated schema. Repeat until stable.
- Fields carry UI metadata: `type` (text/select/date/radio), label, example,
  `minLength`/`maxLength`, `validationRegexp`, `valuesAllowed: [{key, name}]`,
  `displayFormat`, `required`. Worth surfacing — clients that strip it lose 80%
  of the value.
- Requirement groups are **alternatives**: USD returns both "ACH" and "Swift" at
  the top level and the consumer picks one branch.
- v1.1 behind `Accept-Minor-Version: 1` adds dynamic name/email fields; required
  for KRW/JPY/RUB. Default on.
- Field-level errors on `POST /v1/accounts` must be surfaced with keys mapping
  back to the schema the consumer submitted.

**Recommended shape:**

| Surface | Notes |
| --- | --- |
| `DiscoverRequirementsAsync(quoteId, ct)` | → `RequirementsSchema` (list of `RequirementGroup` alternatives) |
| `RefreshRequirementsAsync(quoteId, draft, ct)` | Manual refresh; consumer calls after a `refreshRequirementsOnChange` field changes. |
| `CreateAsync(quoteId, type, details, ct)` | `details` is `IReadOnlyDictionary<string, object?>`. Throws `WiseApiException` with field-level errors mapped. |
| `DeleteAsync(recipientId, ct)` | — |
| `CheckCompatibilityAsync(recipientId, quoteId, ct)` | Wraps `POST /v1/accounts/check`. |

`RequirementsSchema` is a record graph (`RequirementGroup`, `FieldGroup`, `Field`,
`AllowedValue`), not codegen per corridor.

Access: Personal Token ✅ and OAuth ✅ (no SCA, no PSD2 restriction).

## 2. Transfers (v0.5.0 — excluding funding)

- `POST /v1/transfers` create a transfer from a quote + recipient.
- `GET /v1/transfers` list, `GET /v1/transfers/{id}` get.
- `PUT /v1/transfers/{id}/cancel` cancel.

Funding (`POST /v3/profiles/{profileId}/transfers/{transferId}/payments`) is
split out to v0.6.0 because it needs the minimal SCA seam in EU/UK and is
PSD2-blocked for personal tokens there. Non-funding endpoints work with both
auth modes, no SCA.

## 3. SCA (Strong Customer Authentication)

### Current state

The client surfaces the SCA challenge as `WiseScaChallengeException`
(populated with the `x-2fa-approval` OTT and `x-2fa-approval-result: REJECTED`).
Consumers can catch it, but the client does nothing to clear the challenge —
the caller has to handle SCA out-of-band.

### Protocol reality check (verified against Wise docs, 2026-04)

Earlier notes described a "sign the OTT with your RSA private key and replay
with `X-Signature`" flow. That page (`/features/2fa-strong-customer-authentication-sca/personal-token-sca`)
now 404s; the current documented flow is **challenge-clearing via OTT**, not
signing.

The real flow (see
<https://docs.wise.com/guides/developer/auth-and-security/sca-over-api> and
<https://docs.wise.com/guides/developer/auth-and-security/one-time-token>):

1. Protected endpoint returns `403 Forbidden` with
   `x-2fa-approval-result: REJECTED` and `x-2fa-approval: <OTT>`.
2. Caller lists required challenges via
   `GET /v1/one-time-token/status` (header: `One-Time-Token: <OTT>`).
3. Caller clears **at least two** challenges. Primary challenge types:
   - `PIN`          → `POST /v1/one-time-token/pin/verify`
   - `PHONE_OTP`    → phone OTP verification
   - `FACEMAP`      → FaceTec-based inherence challenge
   - `PARTNER_DEVICE_FINGERPRINT` →
     `POST /v1/one-time-token/partner-device-fingerprint/verify`

   **All verify endpoints are JOSE-wrapped** (`Content-Type: application/jose+json`,
   `X-TW-JOSE-Method: jwe`, JWE-encrypted payloads). That requires:
   - Fetch Wise's public encryption key
     (`GET /v1/auth/jose/response/public-keys?algorithm=RSA_OAEP_256&scope=PAYLOAD_ENCRYPTION`).
   - Upload our signing public key
     (`POST /v1/auth/jose/request/public-keys`, scope `PAYLOAD_SIGNING`,
     algorithms `ES256`/`ES384`/`ES512`/`PS256`/`PS384`/`PS512`).
   - Sign request, encrypt with Wise's key, send, decrypt+verify response.

4. Once two challenges are passed, replay the **original** request with
   `x-2fa-approval: <cleared-OTT>` — no `X-Signature` header involved.
   Success returns `x-2fa-approval-result: APPROVED`.

Low-risk operations honour a 5-minute SCA session after one clearance; for
MCA the realistic first user is balance statements (see §4) and transfer
funding (see §2).

### Minimal SCA surface (v0.6.0)

Give integrators a seam without the full JOSE stack. Library handles
detection and replay; consumers handle the challenge-clearing themselves
(or plug in their own JOSE stack).

- Expose the existing `WiseScaChallengeException` as a stable public contract
  — add `ActionType` and `X2FAApprovalResult` fields parsed off the 403.
- Add `IScaChallengeHandler` abstraction:

  ```csharp
  public interface IScaChallengeHandler
  {
      ValueTask<string> ClearChallengeAsync(string oneTimeToken, CancellationToken ct);
  }
  ```

  The handler returns the cleared OTT (same value, just after challenge
  completion). The consumer's implementation may be anything: calling Wise's
  OTT verify endpoints with their own JOSE pipeline, a trusted partner
  backend that performs SCA, a human-in-the-loop console, a test stub, …

- Add `WiseScaRetryHandler` `DelegatingHandler`: on 403 +
  `x-2fa-approval-result: REJECTED`, invoke the configured
  `IScaChallengeHandler`, then replay the request once with
  `x-2fa-approval: <OTT>` added. If no handler is configured or the handler
  returns `null`, preserve today's behaviour and throw
  `WiseScaChallengeException`.
- Wire via `WiseClientOptions.ScaChallengeHandler`; place handler outermost
  in the pipeline so correlation-id / user-agent / auth still apply to the
  replay.
- Request cloning: SCA-protected endpoints Wise currently enforces SCA on
  are all reads (e.g. statements) and small JSON writes (e.g. fund transfer)
  — safe to buffer the content before replay. Guard large request bodies
  with a size cap; bail with a clear exception above it.

**What this unlocks:** transfer funding (§2) and balance statements (§4)
become usable for consumers who bring their own SCA, without the library
taking on JOSE.

### Full SCA (later, multi-release)

The "bring-your-own challenge handler" seam above will be the permanent
extension point. Full in-library SCA means shipping a reference
`IScaChallengeHandler` that completes the challenges natively:

- **JOSE layer** — new `WiseApi.Client.Jose` namespace:
  - `IJoseKeyProvider` for local signing key (EC or RSA-PSS) + cached Wise
    public encryption key.
  - `JoseRequestWrapper` / `JoseResponseUnwrapper` using `System.Security.Cryptography`
    and a JOSE dependency (e.g. `jose-jwt`) — evaluate bringing this in
    only if trimming and AOT constraints allow.
  - Key-upload helper: `POST /v1/auth/jose/request/public-keys`.
- **OTT service**:
  - `GET /v1/one-time-token/status` → strongly-typed challenge list.
  - Per-challenge clients: PIN, phone OTP, partner device fingerprint,
    facemap (facemap likely stays optional — needs FaceTec SDK).
- **Reference handler**: `DefaultScaChallengeHandler` that uses the OTT
  service + a per-challenge-type factory (`IPinProvider`,
  `IPhoneOtpProvider`, …) for values only the caller can supply.

Docs to drive this:
- <https://docs.wise.com/guides/developer/auth-and-security/sca-over-api>
- <https://docs.wise.com/guides/developer/auth-and-security/one-time-token>
- <https://docs.wise.com/guides/developer/auth-and-security/jose-jws-jwe>
- <https://docs.wise.com/api-reference/one-time-token>
- <https://docs.wise.com/api-reference/jose>

## 4. Balance statements (v0.6.0)

`GET /v1/profiles/{profileId}/balance-statements/{balanceId}/statement.{ext}`
— seven formats (`json`, `csv`, `pdf`, `xlsx`, `xml` (CAMT.053), `mt940`,
`qif`). Max 469-day range. **SCA-protected every 90 days**, so this lands
alongside the minimal SCA seam (§3). Personal tokens in EU/UK are also
PSD2-blocked from statement retrieval — OAuth required there.

API: expose `IBalanceStatementsService.DownloadAsync(..., StatementFormat format)`
returning a `Stream` (via the existing `WiseHttpClient.GetRawAsync`).

## 5. Webhooks

- `GET / POST / DELETE /v3/profiles/{profileId}/subscriptions` webhook subscription CRUD.
- Signature verification helper for incoming Wise webhook POSTs (uses a
  Wise-published public key per environment; the library ships the PEM and
  verifies `X-Signature-SHA256` on request bodies).
- Strongly-typed event payloads for `balances#update`, `transfers#state-change`, etc.

## 6. Cards, Assets/Investments, Bulk settlement

Explicitly deferred until a consumer actually asks — these are narrower
surfaces and change more often. Flag for later.

## 7. Observability polish

- Activity/ActivitySource for each outgoing call (`WiseApi.Client` activity source name).
- Optional OpenTelemetry semantic attributes (`wise.profile.id`, `wise.endpoint.group`).
- A `WiseLoggingHandler` that emits redacted request/response pairs at `Debug`.

## 8. Source-generated JSON serialization

Currently uses reflection-based `System.Text.Json` for speed of iteration. Once
the model surface stabilises, add a `JsonSerializerContext` partial with all
request/response types so the client is AOT-friendly and the assembly is
smaller when trimmed.
