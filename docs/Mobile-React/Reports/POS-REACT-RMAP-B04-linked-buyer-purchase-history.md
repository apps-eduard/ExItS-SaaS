# POS React — RMAP-B04 Linked Buyer Purchase / History Projection

**Package:** RMAP-B04  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `99069c3ca539cc76ebcdc53952e9afa11b4d3dfa`  
**Status:** COMPLETE (Personal linked-customer read projection; React UI on Phase-24 backend)

---

## Summary

Delivered React Personal buyer purchase/history surfaces that reuse **existing Phase-24 (P24-WP03–WP10) POS linked-customer APIs**. No new backend ledger, no duplicate statement system, no Personal Utang copy.

**Seller POS Sale / Business Utang remains authoritative.** Personal views are authorized read projections only.

---

## Backend contracts reused (no new backend work)

| Endpoint | Purpose |
| -------- | ------- |
| `GET /api/v1/pos/personal/linked-customers/{platformBusinessCustomerId}/statement` | Outstanding summary |
| `GET .../activity` | Recent activity (Utang + Cash/GCash purchases + voids) |
| `GET .../open-debt-activity` | Open-debt explanation when outstanding > 0 |
| `GET .../older-activity` | Extended settled history (entitlement-gated) |
| `GET .../receipts/{saleId}` | Lazy receipt detail |

Platform metadata: `GET /api/v1/personal/linked-merchants` (existing).

Authorization chain (fail-closed): Personal session → Platform `LinkedCustomerAppUser` → org-scoped POS customer correlation → projection.

---

## React deliverables

| Area | Files |
| ---- | ----- |
| POS client | `src/api/pos/pos-linked-customers-client.ts` |
| Statement page | `src/features/personal/linked-merchants/LinkedMerchantStatementPage.tsx` |
| Receipt page | `src/features/personal/linked-merchants/LinkedMerchantReceiptPage.tsx` |
| Stores list | `LinkedMerchantsPage.tsx` — **Purchases & activity** entry per merchant |
| Rewards placeholder | `PersonalRewardsPage.tsx` — extended-history unlock entry (MAUI parity) |
| Routes | `/personal/linked-merchants/:organizationId/:businessCustomerId` (+ receipts) |
| i18n | 5 locales — `personal.merchantStatement.*`, `personal.merchantReceipt.*`, `offline.requiredHistory` |

---

## Projection scope (audit result)

Phase-24 projection includes **Completed Cash/GCash sales** (activity type `Purchase`) and **Utang charges/repayments**, not Utang-only. Verified against `LinkedCustomerRecentActivityQuery` and existing unit tests (`LinkedCustomerSaleProjectionTests`).

Void/refund status represented via activity types (`PurchaseVoided`, reversals).

---

## Organization buyer

**Not implemented — documented gap.**

`SaleBuyerParty` domain groundwork exists, but **no approved Organization buyer purchase-history API contract**. UI does not expose Organization buyer history. No invented identity model.

---

## Security / privacy

- Exact linked-customer authorization only; unrelated Personal → 403/404
- Org-scoped queries; cross-org correlation denied at repository/use-case layer
- Receipt/activity DTOs exclude seller cost, margin, internal notes, staff audit fields
- Transaction Summary disclaimer on receipt (`summary.disclaimerBody`) — not a BIR invoice
- No tax UI exposed

---

## Tests (automated)

| Suite | Result |
| ----- | ------ |
| `pos-linked-customers-client.test.ts` | PASS (4) |
| `format-linked-customer-activity.test.ts` | PASS (2) |
| `linked-merchant-statement.test.tsx` | PASS (1) |
| `message-parity.test.ts` | PASS (10) |
| `npm run typecheck` | PASS |
| `npm run build` | PASS |

Backend regression (pre-existing, not modified): `LinkedCustomerAuthorizationUseCaseTests`, `LinkedCustomerSaleProjectionTests`, `P24Wp12HistorySecurityRegressionTests`.

---

## Exclusions (unchanged)

- RMAP-B05 public business discovery/landing
- Organization buyer purchase history (no contract)
- SaleBuyerParty-wide purchase history without merchant link
- Rewards redemption implementation (placeholder only)
- RMAP-TAX

---

## Known gaps

- Playwright E2E for full linked-merchant → statement → receipt journey not run in this package
- Physical device validation not performed
- Native-speaker locale certification pending (existing flag)

---

## Next authorized work

RMAP-23 parity/security/UX hardening (after B04 PASS).
