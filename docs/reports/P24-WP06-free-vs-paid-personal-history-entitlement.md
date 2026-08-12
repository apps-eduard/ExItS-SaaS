# P24-WP06 — Free vs Paid Personal History Entitlement

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP05](P24-WP05-receipt-summary-detail-and-lazy-loading.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (Personal history entitlement foundation + free/open-debt policy) |
| Date | 2026-08-12 |
| Starting SHA | `92c8da6d9d5b1f478e56519d06aed8198a490b43` on `main` |
| Implementation commit | `7a9a8301de1489c75ccc3a4a9890d0a7d3b54196` |
| Docs commit | `cdd991b51e02fbb1de576c6337793e2b809503e8` |
| Docs/hash-stamp commit | `4b111e9feedc04a20997008e326e3bca402b86e7` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **Yes** — `20260812152011_AddPersonalFeatureEntitlements` (Platform only) |

## Status legend

WP06 introduces a **PersonalUserId-scoped** feature entitlement subject and applies free-window + open-debt + extended-history rules to linked-customer activity and receipt APIs. No reward points, ads, or pricing. **Not Device Verified. Not Production Ready.**

## Personal feature model

| Concept | Location |
|---|---|
| `PersonalFeatureDefinition` | Platform Domain + `platform.personal_feature_definitions` |
| `PersonalFeatureEntitlement` | Platform Domain + `platform.personal_feature_entitlements` |
| Subject | `PersonalUserId` (never `OrganizationId`) |
| Grant sources | `CashPurchase`, `RewardPoints` (reserved), `Promotion`, `AdminGrant` |
| Resolver | `IPersonalFeatureEntitlementService.HasActiveEntitlementAsync` |

### Feature code

```text
personal-digital-records-extended
```

Hyphenated (matches `FeatureCode` rules). Organization plan features never satisfy this check.

### Grant / revoke

- `GrantPersonalFeature` / `RevokePersonalFeature` — application/internal (no Personal self-grant endpoint)
- First grant auto-seeds an active definition when missing
- Idempotent when an overlapping Active grant already covers the window
- Platform Personal session can only **read** active status: `GET /api/v1/personal/features/{featureCode}/active`

## Free-history configuration

```text
Section: PersonalStatements
Key: FreeRecentMonths (default 3)
```

Bound in POS API `appsettings.json`. Interpretation: **current UTC calendar month + previous (N−1) months**. Example: as of 2026-08-12 with N=3 → free window starts **2026-06-01T00:00:00Z**.

## Free vs Extended rules

```text
FREE:
- current outstanding (statement summary)
- recent history inside free window
- open-debt evidence (Active credits + Active repayments) via dedicated endpoint
- receipt detail inside free window
- receipt detail for old Utang sales whose linked credit is still Active while outstanding > 0

EXTENDED (personal-digital-records-extended active):
- older settled activity via the normal activity endpoint (still page size ≤ 20)
- older settled receipt detail
```

Never paywall information required to understand **current open debt**.

## API behavior

| Endpoint | Policy |
|---|---|
| `.../statement` | Unchanged; outstanding always visible after WP03 |
| `.../activity` | Free: `notBeforeUtc = FreeHistoryStartsAtUtc`. Entitled: no date floor. Still default 10 / max 20. Adds `CanAccessExtendedHistory`, `FreeHistoryStartsAtUtc` |
| `.../open-debt-activity` | **New.** Active ledger only when outstanding &gt; 0; empty when outstanding = 0 (does not unlock settled history) |
| `.../receipts/{saleId}` | WP03 + ownership first (404). Then free window → open-debt Active credit → else entitlement. Premium denial: `pos.personal.extended_history_required` **403** |

POS calls Platform entitlement via `IPersonalFeatureEntitlementClient` (session-forwarding HttpClient). Unreachable Platform → fail-closed as **not entitled** (free/open-debt still work).

## Query efficiency

- Activity date filter applied in SQL (`recorded_at_utc >= @not_before`)
- Open-debt query filters `status = 'Active'` in SQL with OFFSET/LIMIT
- No full-ledger load into memory for visibility decisions

## Security

Authorization order:

1. WP03 linked-customer authorization  
2. Free-window / open-debt policy  
3. Extended entitlement (only after ownership for receipts)  
4. Return or deny  

Guessed/wrong-customer receipt IDs remain **404**. Entitlement never replaces identity checks. Org subscriptions do not grant Personal history access.

## Tests / builds

| Suite | Result |
|---|---|
| Platform `PersonalFeatureEntitlement` | **Passed 9**, failed 0, skipped 0 |
| POS `FullyQualifiedName~LinkedCustomer` | **Passed 54**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 774**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 549**, failed 0, skipped 0 |
| Platform API / POS API Release builds | Succeeded |

Pre-existing: Platform CS0618 check-constraint warnings; POS NU1510 / NU1903 warnings.

Not run: full `ExItS.slnx`; live Platform↔POS HTTP; device/UI.

## Known limitations

- No Admin UI for Personal grants (use cases / future WP11)
- No cash purchase / reward redemption wiring
- No prices
- Open-debt explanation uses Active credit/repayment set (lump-sum model), not FIFO line allocation
- Platform entitlement unreachable ⇒ treated as free-only (fail closed for premium)
- Development-stage APIs not production-secure
- Not Device Verified. Not Production Ready.

## Explicit exclusions

Reward ledger, ads, ad-free UI, payment gateway, PDF/export, disputes, archive purge, trial retention cleanup.

## Exact WP07 recommendation

**P24-WP07 — Personal reward points ledger + feature redemption**

- Personal-only points ledger  
- immutable reward transactions  
- earned/spent balance  
- feature redemption into Personal entitlements  
- idempotency/concurrency  
- no cash value; no Organization redemption  

Do **not** start WP07 automatically from this package.

## Checks performed

- Starting HEAD = `origin/main` = `92c8da6d9d5b1f478e56519d06aed8198a490b43`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only (no `git add .`)
- Migration: **Yes** (Platform `AddPersonalFeatureEntitlements`)
