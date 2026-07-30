# P6-WP05 — Statements, Receipts and Trial Rules

Phase marker: `P6-WP05-statements-receipts-and-trial-rules`

## Status

**Complete with documented risks.** Derived customer statements and repayment receipts, centralized Utang capability matrix, Platform continuity entry for PinoyBusinessPOS only, and MAUI preview/share handoff. Interest, penalties, credit limits, sales, inventory, gateways, tax invoices, offline sync, and payment-allocation persistence remain excluded. Commercial headers are Development/Testing-stage only — not production-secure. **R-109 remains open** (no interactive Android claimed). **P6-WP06 was not started.**

Feature commit: `271c518cb8c4051502d6370ec71e6498fbbfd6b5`

## Delivered capability

- Projection-based customer statements (date-range opening/closing balances, credits, repayments, reversals, due dates / overdue presentation, running balance, generated-at UTC)
- Projection-based repayment receipts with deterministic reference `RCPT-{guid:N}` (uppercase); no new receipt migration/table
- Authoritative capability matrix in `UtangCapabilityPolicy` (centralized outside Razor)
- Platform `ProductAccessEligibility.CanEnterPinoyBusinessPos` continuity entry for PastDue / Cancelled / Expired when view or repay grants are effective; Suspended always denies; other products unchanged (Trialing/Active only for entry/new grants)
- Product entry vs feature authorization separation — both must pass
- Development-stage commercial headers `X-Pos-Subscription-Status` and `X-Pos-Feature-Grants`
- POS API statement/receipt routes; MAUI statement/receipt preview + share/save handoff; EN + `fil-PH`
- OD-07, OD-08, OD-09 resolved (see below)
- Phase marker `P6-WP05-statements-receipts-and-trial-rules`

## Product entry vs feature authorization

Platform `EvaluateEffectiveProductAccess` may allow PinoyBusinessPOS **entry** for continuity states. Every Utang operation is still gated by effective feature grants (`customer-credit-view`, `customer-credit-repay`, `customer-credit-create`) plus the capability matrix. Entry alone does not restore full product capability.

| Subscription state | POS entry |
|---|---|
| Trialing / Active / GracePeriod | Allow |
| PastDue / Cancelled / Expired | Allow with restricted continuity features (requires view or repay grant) |
| Suspended | **Deny** |
| Missing, stale, unknown, invalid | **Deny** |

## Capability matrix

Feature grants required in addition to subscription state (both product entry and grant checks must pass):

| Capability | Trialing | Active | GracePeriod | PastDue | Cancelled | Expired | Suspended | Grant |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Enter POS | Allow | Allow | Allow | Allow | Allow | Allow | Deny | Continuity: view or repay |
| View customers and Utang history | Allow | Allow | Allow | Allow | Allow | Allow | Deny | `customer-credit-view` |
| Create customer (OD-07) | Allow | Allow | Allow | Deny | Deny | Deny | Deny | `customer-credit-create` |
| Edit customer contact/profile (OD-08) | Allow | Allow | Allow | Deny | Deny | Deny | Deny | `customer-credit-create` |
| Create new credit | Allow | Allow | Allow | Deny | Deny | Deny | Deny | `customer-credit-create` |
| Record repayment | Allow | Allow | Allow | Allow | Allow | Allow | Deny | `customer-credit-repay` |
| Reverse credit (OD-09) | Allow | Allow | Allow | Allow | Allow | Allow | Deny | `customer-credit-view` |
| Reverse repayment (OD-09) | Allow | Allow | Allow | Deny | Deny | Deny | Deny | `customer-credit-repay` |
| Set/change/clear due date | Allow | Allow | Allow | Deny | Deny | Deny | Deny | `customer-credit-create` |
| View/generate statement | Allow | Allow | Allow | Allow | Allow | Allow | Deny | `customer-credit-view` |
| View/generate repayment receipt | Allow | Allow | Allow | Allow | Allow | Allow | Deny | `customer-credit-view` |

## Resolved open decisions

| ID | Decision |
|---|---|
| **OD-07** | **Resolved — Deny.** Creating customers after expiry or restricted continuity states (PastDue / Cancelled / Expired) is denied. |
| **OD-08** | **Resolved — Deny.** Editing customer contact/profile after expiry or restricted continuity states is denied. |
| **OD-09** | **Resolved.** Credit reversal is allowed whenever continuity access is allowed (corrects history; cannot increase debt). Repayment reversal is allowed only in Trialing / Active / GracePeriod (increases outstanding). |

## Explicit exclusions

Interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, tax invoices / tax numbering, offline sync, payment-allocation persistence. No Platform/HealthCare tables or cross-database FKs. Platform SaaS payments remain separate.

## Persistence and migration

- Statements and receipts are **projections** over existing customers, credits, repayments, and ledger read models
- Receipt reference: `RCPT-{repaymentId:N}` (deterministic; idempotent retrieval)
- **No new migration** for receipts or statements
- Outstanding formula unchanged: active credits − active repayments

## API capability

| Method | Route |
|---|---|
| GET | `/api/v1/pos/customers/{customerId}/statement` |
| GET | `/api/v1/pos/repayments/{repaymentId}/receipt` |

Organization scope via `X-Pos-Organization-Id`. Commercial context via Development-stage `X-Pos-Subscription-Status` and `X-Pos-Feature-Grants`. Existing customer/credit/repayment/due-date routes also enforce capabilities via `PosCommercialScope.TryAuthorize`.

## MAUI experience

Routes: `/customers/{id}/statement`, `/customers/{id}/repayments/{repaymentId}/receipt`. Period selection and preview; capability-gated create/edit/credit/repay/due-date actions; localized restricted-state messaging; `IDocumentHandoffService` share/save handoff (initiation reported honestly — completion not claimed without observation). EN + `fil-PH` (`Statement_*`, `Receipt_*`, `Access_*`).

## Organization isolation

All queries filter by organization. Cross-organization access returns 404. Platform continuity entry applies to PinoyBusinessPOS only.

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.PinoyBusinessPOS.UnitTests | 39 | 0 | 0 |
| ExItS.Platform.UnitTests | 265 | 0 | 0 |
| ExItS.PinoyBusinessPOS.Maui.Tests | 27 | 0 | 0 |
| ExItS.DesignSystem.Tests | 28 | 0 | 0 |
| ExItS.PinoyBusinessPOS.ApiClient.Tests | 17 | 0 | 0 |
| ExItS.Platform.Admin.UnitTests | 27 | 0 | 0 |
| ExItS.ArchitectureTests | 41 | 0 | 0 |
| ExItS.PinoyBusinessPOS.IntegrationTests | 13 | 0 | 0 |
| ExItS.Platform.IntegrationTests | 84 | 0 | 0 |
| **Full solution** | **541** | **0** | **0** |

Baseline preserved and exceeded (prior 521). Focused coverage: `UtangCapabilityPolicyTests`, statement/receipt service tests, Platform continuity entry tests, `PosStatementReceiptAndCommercialApiTests` (Testcontainers). Release Android APK: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`. No interactive emulator/device — **R-109 remains open**.

## Security limitations

- POS APIs trust organization and commercial headers without production authentication
- `X-Pos-Subscription-Status` / `X-Pos-Feature-Grants` are Development/Testing-stage only — **not production-secure**
- Development/Testing Platform identity remains the only MAUI auth path
- Not production-secure (R-091 remains open)

## HealthCare freeze

`git ls-files -- HealthCare/` empty; ignored via `.gitignore`; not in `ExItS.slnx`.

## Risks and open decisions

- R-109: no interactive Android emulator validation
- R-091 / R-124 / R-128: commercial and actor headers are not production authz/audit
- R-022: entitlement stale/refresh durations still open
- OD-07 / OD-08 / OD-09: **resolved** (this WP)
- OD-11 (GCash duplicate hard-block) remains open

## Files / docs changed

Platform access eligibility continuity for POS; POS Commercial/Statements Application + API + ApiClient + Maui; phase-06; portfolio; README; FILE-MANIFEST; contracts §9/§20; extraction-sequence; testing-strategy; risks; release-plan; reports index; this report. Feature commit: `271c518cb8c4051502d6370ec71e6498fbbfd6b5`.

## Git evidence

| Field | Value |
|---|---|
| Feature commit | `271c518cb8c4051502d6370ec71e6498fbbfd6b5` |
| Docs commit | `157786b4c6b7f537c82ecb028abbc05c3f33f42c` |
| Finalize tip | `ffc0369437400a4aed2a54227aa000868fb82d97` |
| Phase marker | `P6-WP05-statements-receipts-and-trial-rules` |
| Final working tree | Clean; matches `origin/main` after push |

## Exact next work package

**P6-WP06 — Utang MVP Closeout** (not started — do not begin until explicitly authorized)
