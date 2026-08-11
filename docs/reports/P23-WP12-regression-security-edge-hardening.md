# P23-WP12 — Regression, Security, and Edge-Case Hardening

| Field | Value |
|---|---|
| Status | **Implemented** (hardening + regression matrix; WP13 not started) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-12 |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status

WP12 hardens the completed Phase 23 flow (WP01–WP11) without adding product features. Focus: fail-closed authorization/concurrency, Today’s Prices concurrency tokens, offline trusted-snapshot provenance, and regression guards/tests.

WP13+ **not started**.

## Bugs found

| # | Area | Severity | Finding |
|---|---|---|---|
| 1 | BT activation capacity | High | Check-then-act activation could race two different BTs into the final capacity slot and exceed `MaxActiveBusinessTypes`. |
| 2 | BT activation PK race | Medium | Concurrent same-BT activate could surface as unhandled `PersistenceConflictException` (500) instead of idempotent success. |
| 3 | BT deactivation | Low | Missing activation row returned 404; duplicate deactivate was not idempotent. |
| 4 | Today’s Prices | High | Omitting `ExpectedUpdatedAtUtc` treated as non-stale → last-write-wins / silent overwrite. |
| 5 | Online checkout + snapshots | High | Client could send trusted line snapshots without offline `SaleId` and undercharge vs live catalog. |

## Fixes made

1. **Org advisory lock on activate** — `IPlatformUnitOfWork.ExecuteWithOrganizationLockAsync` + PostgreSQL `pg_advisory_xact_lock` (transaction-scoped). Capacity re-evaluated under lock. Non-Npgsql providers no-op (unit-test fakes unchanged).
2. **PK conflict → idempotent activate** — catch `PersistenceConflictException` and return existing activation when present.
3. **Idempotent deactivate** — missing non-primary activation → `Success`.
4. **`CatalogConcurrency.IsStaleOrMissing`** — Today’s Prices requires expected token; null fails closed with concurrency conflict.
5. **Trusted snapshots require `SaleId`** — online carts must omit snapshot fields; incomplete offline provenance rejected as `pos.sale.snapshot.invalid`.

## Security / auth matrix (server)

| Actor / case | BT activate/deactivate | Global/template discovery | Today’s Prices | Checkout (money) |
|---|---|---|---|---|
| Owner / commercial manager | Allowed (org-scoped) | Effective entitled BTs | ManageCatalog | Device + commercial rules |
| Cashier | Denied (commercial manage) | View per grants | Denied (ManageCatalog) | Allowed with device/PIN rules |
| Wrong org | Denied / not found (no leak) | Org scope | ProductNotFound for foreign IDs | Org scope |
| Unauthenticated | Denied | Denied | Denied | Denied |
| Platform Admin | Platform subscription paths where intended | Unrestricted Platform catalog mgmt | N/A (POS path) | N/A |

UI hiding is not authorization; commercial and catalog mutations remain server-enforced (WP03/WP04/WP10).

## Business Type edge cases

| Case | Result |
|---|---|
| Primary always effective | Covered by prior WP03/WP10B tests |
| Primary cannot deactivate | Covered |
| Duplicate activate idempotent | Covered |
| Duplicate deactivate safe | **Fixed + tested** |
| Capacity at/over limit | Covered |
| Concurrent final-slot race | **Mitigated** via org advisory lock (Postgres) |
| Downgrade over capacity blocked | Covered |
| No merchant catalog delete on deactivate/downgrade | Unchanged invariant |
| GeneralRetail not silent-assigned | Unchanged (WP10A/WP11) |

## Catalog / template

Regression retained from WP03/WP04: omitted filter → effective set; explicit unentitled filter denied; direct IDs entitlement-checked; import rechecks at mutation; merchant products retained after entitlement removal; WP10A shared applicability unchanged. No new schema.

## Variable quantity / money

Prior WP05–WP07 suites remain authoritative (g→kg, ≤3 dp reject, money 2 dp AwayFromZero, mixed carts, returns, reports). No float/double introduced. WP12 did not change arithmetic rules.

## Offline snapshot fidelity (WP08)

| Case | Result |
|---|---|
| Queued v2 snapshots honored after live price/mode/UOM/name change | Prior WP08 unit + sale API tests |
| Forged line total | Rejected (`line_total_mismatch`) when `SaleId` present |
| Snapshots without `SaleId` | **Rejected** (`snapshot.invalid`) — new |
| Transient unreachable vs explicit reject | Unchanged semantics |
| Legacy v1 | Documented; not redesigned |

## Today’s Prices

| Case | Result |
|---|---|
| Missing `ExpectedUpdatedAtUtc` | **Fail closed** (concurrency conflict) |
| Cross-org product | ProductNotFound |
| Duplicate IDs / negatives / ManageCatalog | Prior + retained |
| Unchanged row timestamp | Preserved |
| No price-edit outbox | Unchanged |

## Onboarding / WP11

No redesign. Prior WP11 Maui guards (Owner vs Cashier, Starter skip, capacity copy) remain. Authoritative activate/deactivate still online server APIs.

## Device / org scope (Phase 22)

No device architecture change. Money ops still require registered device / grant binding per existing guards. Owner does not bypass device registration.

## Offline / connectivity

Plan change, BT activate/deactivate, global/template import, Today’s Prices remain online-required and out of POS sales outbox. Offline sales continue under grant/PIN/device rules.

## Regression matrix (WP12 focus)

| Suite / filter | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| Platform unit (`BusinessTypeCapacity` + `P23Wp12`) | 10 | 0 | 0 | Incl. idempotent deactivate + lock guards |
| Platform unit (`BusinessType\|Entitlement\|CatalogFilter\|P23`) | 100 | 0 | 0 | Broader Phase 23 unit |
| Platform integration (`OrganizationBusinessType\|PlanBusinessType`) | 5 | 0 | 0 | Persistence |
| POS unit (`OfflineSaleSnapshotFidelity` + `P23Wp12`) | 14 | 0 | 0 | |
| POS unit (`Offline\|SellingMode\|SaleMoney\|Quantity`) | 126 | 0 | 0 | |
| POS integration (`TodaysPrices`) | 3 | 0 | 0 | Incl. missing token |
| POS integration (`PosSaleApiTests`) | 16 | 0 | 0 | Incl. SaleId + forged total |
| Maui (`Wp11\|PersonalPageGuard`) | 19 | 0 | 0 | |

### Unrelated / pre-existing (not hidden)

Broader Platform integration filter (`BusinessType|Entitlement|CatalogTemplate|GlobalCatalog`) also matched older Start Business / subscription admin suites: **12 failed / 48 passed / 60 total**. Failures were `Start_business` → `NotFound` and unrelated subscription admin asserts — **not caused by WP12 BT lock/idempotency/Today’s Prices/SaleId changes**. Documented; not “fixed” by weakening tests.

## Cross-org results

- Today’s Prices foreign product → not found (no existence leak of other-org catalog semantics beyond existing ProductNotFound).
- Sale/catalog org scoping unchanged from prior phases.

## Migration impact

**None.** No schema change. Prefer no migration satisfied.

## Remaining known limitations / deferred

- Primary remains effective even without plan grant (Phase 23 intentional).
- Trusted snapshots with a forged but consistent `SaleId` still possible until device-bound signing (deferred).
- Soft capacity overage if inactive types drop from effective count without removing stored activations (by design: stored ≠ effective).
- Import entitlement TOCTOU after queue (worker recheck exists; not redesigned).
- Physical device verification (WP14) not run.
- WP13 documentation/closeout **not started**.

## Implementation commit hash

`1c3be320d3fc85d7e1dbabc0e0d842e1f52f85da`

## Explicit stop

**WP13 not started.** Device Verified = **No**. Production Ready = **No**.
