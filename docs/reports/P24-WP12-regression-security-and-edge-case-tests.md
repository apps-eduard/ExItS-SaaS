# P24-WP12 — Regression, Security, and Edge-Case Tests

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP11](P24-WP11-admin-configuration-for-personal-features.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (regression / security / edge-case hardening for WP06–WP11) |
| Date | 2026-08-12 |
| Starting SHA | `92bffb9a30f14b5b855d80593b2312f6a1885fb1` on `main` |
| Implementation commit | `5ccf12e2a1a420ae4ff9ef3cdbc586868f33126c` |
| Docs commit | `c6f013ace3e1800bfbcb9a5179fae4ffb1df005b` |
| Docs/hash-stamp commit | `10db6ccd5f99f86c936e8e5b56035e8d55051c83` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status legend

WP12 aggressively re-tests authorization, history free-window / entitlement, receipt privacy ordering, reward ledger arithmetic, Ad-Free/ads claim fail-closed behavior, and Admin Personal feature commercial authority delivered in WP06–WP11. Roadmap extended through WP24 without renumbering WP01–WP15. **Not Device Verified. Not Production Ready.**

## Canonical WP12 scope (Phase-24)

```text
WP12 | Regression, security, and edge-case tests | Authz matrix, ledger arithmetic, privacy DTOs
```

## Delivered tests

| Area | New / extended coverage |
|---|---|
| POS HTTP privacy mapping | `P24Wp12PosApiResultsPrivacyTests` — 404 (`ReceiptNotFound` / `LinkedCustomerNotFound` / `CustomerNotFound`) vs 403 (`LinkedCustomerDenied` / `ExtendedHistoryRequired`) |
| History / entitlement | Fail-closed entitlement client; UTC month boundary just outside free window; settle removes open-debt receipt exception; page-size max 20; DTO denylist |
| Reward ledger | Balance equals Σ signed deltas; expired entitlement + updated admin price/duration for subsequent redemption; org feature codes rejected on Personal admin update |
| Admin Personal Features | Ant Design + `ViewPortfolio`/`ManageCatalog`; no hard-coded prices/durations; immutable feature code; Platform Admin API permission source guard |

Supporting change: `InternalsVisibleTo` for `ExItS.PinoyBusinessPOS.UnitTests` on the POS API project so `PosApiResults.MapStatusCode` privacy mapping is testable without widening the public API.

## Architecture decisions

- No new product APIs or schema.
- Tests assert existing server authority; no client-controlled price/duration/debit.
- Roadmap continuation WP16–WP24 approved for mobile-first delivery; WP12 does not implement mobile UI.
- WP13 remains architecture-first / optional per roadmap.

## Authorization / privacy behaviors reaffirmed

- Linked-customer access remains Personal-session authorized; staff/org principals stay separate.
- Premium lock returns **403** `pos.personal.extended_history_required`; non-owned / guessed remains **404**.
- Open-debt receipt exception disappears when outstanding reaches zero.
- Personal feature admin updates limited to known Personal codes (`personal-digital-records-extended`, `personal-ad-free`).

## Tests / builds

| Suite | Result |
|---|---|
| `FullyQualifiedName~P24Wp12` (POS) | **Passed 12**, failed 0, skipped 0 |
| `FullyQualifiedName~P24Wp12` (Platform) | **Passed 3**, failed 0, skipped 0 |
| `FullyQualifiedName~P24Wp12` (Admin) | **Passed 5**, failed 0, skipped 0 |
| Personal feature / reward / ads / WP12 filter (Platform) | **Passed 59**, failed 0, skipped 0 |
| LinkedCustomer + PersonalSettled + WP12 filter (POS) | **Passed 83**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 824**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 578**, failed 0, skipped 0 |
| Full `ExItS.Platform.Admin.UnitTests` | **135 passed / 5 failed** — see classification below |

## Admin UnitTests — all 6 historical failures classified

WP11 reported **129 passed / 6 failed**. After WP11 localization keys + WP12 Personal Features guards, the suite is **140 total: 135 passed / 5 failed**. The sixth prior failure (Admin localization missing WP11 keys) is **resolved** and no longer fails.

| # | Test | Classification | Notes |
|---|---|---|---|
| 1 | `AdminPaymentsWorkspaceTests.Payments_filters_and_table_use_supported_fields_without_raw_guid_labels` | **Pre-existing** (unrelated) | Expects `FormatMoney(` substring in Payments page source |
| 2 | `AdminDashboardRefactoringTests.Dashboard_uses_antdesign_landing_composition` | **Pre-existing** (unrelated) | Expects `<Statistic` on Admin dashboard |
| 3 | `AdminReportingFrameworkTests.Representative_pages_use_reporting_framework` | **Pre-existing** (unrelated) | Expects `<Statistic` on representative reporting pages |
| 4 | `AdminDataDisplayTests.Representative_pages_use_shared_data_components` | **Pre-existing** (unrelated) | Expects `AmountDisplay` on representative pages |
| 5 | `AdminPaymentsWorkspaceTests.Payments_page_preserves_authorization_audit_surface` | **Pre-existing** (unrelated) | Expects gateway warning copy substring |
| 6 | Admin localization / WP11 Personal Features keys (prior WP11 report) | **Fixed / no longer failing** | WP11 keys present; WP12 Personal Features guards pass |

None of the remaining five are caused by Phase 24 Personal Features / statements / rewards work. No Phase-24 regression was found in Admin UnitTests.

## Roadmap extension

Phase-24 roadmap now continues through **WP24** (mobile stream + owner gate) without renumbering WP01–WP15. See phase document work-package table.

## Known limitations

- No Device Verified claim from automated tests
- Remaining Admin source-guard failures are outside Phase 24 scope (not fixed here)
- Dispute/request architecture deferred to WP13 (architecture-first)
- Mobile Personal statement UX starts at WP16
- Not Production Ready

## Exact next WP

**P24-WP13 — Dispute/request architecture (optional)**

- Architecture-first; defer implementation if scope expands excessively

## Checks performed

- Starting HEAD = `origin/main` = `92bffb9a30f14b5b855d80593b2312f6a1885fb1`
- Migration: None
- Focused test + InternalsVisibleTo only
- Portfolio independence preserved
