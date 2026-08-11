# P23-WP08 — Offline sale snapshot fidelity

| Field | Value |
|---|---|
| Status | **Implemented** (offline cash sync preserves immutable line economics; WP09+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP08 fixes the critical offline → server re-pricing bug: cash sales queued offline now sync with immutable line snapshots (quantity, UOM, SellingMode, UnitPrice, LineTotal). The server validates arithmetic consistency and product identity/Active status without replacing price/mode from the live catalog. Online checkout (ProductId + Quantity only) remains live-catalog priced.

## Old bug / root cause

1. MAUI cart held preview prices, and `receipt_json` stored local line snapshots.
2. Outbox payload serialized only `CheckoutSaleRequest` with `CheckoutSaleLineRequest(ProductId, Quantity)`.
3. Server `CheckoutSale.ExecuteAsync` always built `SaleLineDraft` from live `product.SellingPrice` / `product.SellingMode`.

So a Monday offline sale of Tomato `1.200` kg @ PHP 120 could become PHP 180 after Tuesday’s catalog price change to 150/kg.

## Corrected sync flow

MAUI offline cash commit  
→ `LocalCashSaleLineSnapshot` in `receipt_json` (unchanged)  
→ outbox `CheckoutSaleRequest` lines include snapshots (`payload_version = 2`)  
→ `SaleCheckoutOfflineDispatcher` → `POST /api/v1/pos/sales`  
→ `CheckoutSale` uses `CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot` when snapshot fields present  
→ `Sale` / `SaleLine` persist historical economics  
→ inventory deduction uses sale-line quantity + `SellingModeSnapshot` / `UnitOfMeasureSnapshot`  
→ idempotent `SaleId` replay returns existing sale

## Authoritative snapshot contract

`CheckoutSaleLineRequest` (additive optional fields):

| Field | Required for trusted offline path |
|---|---|
| ProductId | yes |
| Quantity | yes |
| UnitPriceSnapshot | yes |
| UnitOfMeasure | yes |
| SellingMode | yes |
| LineTotal | yes |
| NameSnapshot / SkuSnapshot / BarcodeSnapshot | optional (prefer over live when present) |

Online carts continue to omit snapshot fields → live catalog pricing.

## Server validation vs live re-pricing

Still validates: org/shift/register, product exists + **Active**, quantity/precision, ByWeight↔Kilogram compatibility, money rounding, LineTotal = `RoundMoney(UnitPriceSnapshot × Quantity)`, idempotent SaleId, commercial/device rules.

Does **not**: replace UnitPrice/UOM/SellingMode from live `CatalogProduct.SellingPrice`.

Forged totals → `pos.sale.snapshot.line_total_mismatch`. Incomplete snapshots → `pos.sale.snapshot.incomplete`.

## ByWeight behavior

Examples preserved through sync:

- `1.200` kg @ 120 → 144.00
- `0.350` kg @ 120 → 42.00
- `0.750` kg @ 220 → 165.00
- Mixed: `2×25 + 1.200×120 = 194.00` even when live Coke=30 and Tomato=150

## LocalStore behavior

| Item | Value |
|---|---|
| Schema version | **7** (unchanged by WP08) |
| Line economics | `receipt_json` (`LocalCashSaleLineSnapshot`) already held UnitPrice/Qty/UOM/SellingMode |
| Money/qty storage | invariant decimal TEXT (no REAL) |

**No LocalStore schema bump.**

## Outbox versioning

| Version | Meaning |
|---|---|
| 1 | Legacy ProductId + Quantity only (already-queued rows still sync via live catalog) |
| **2 (Current)** | Immutable line snapshots |

Dispatcher deserializes the same `CheckoutSaleRequest` shape; additive JSON fields are ignored by older readers and optional for v1.

## Idempotency

Existing `SaleId` client key: replay returns the same sale; inventory deduction remains once (`HasSaleDeductionAsync`). MarkSynced / MarkSyncFailed policies unchanged. Explicit server Validation/NotFound → Permanent; Offline/Timeout/Unavailable → Transient pending.

## Archived / product-change policy

- Product must still **exist** and be **Active** at sync (same as online). Inactive → permanent `pos.sale.product.not_active`.
- Rename / price / SellingMode / UOM changes on the live product do **not** rewrite historical line snapshots when the offline payload carries them.
- No separate “Archived” status (Active/Inactive only).

## Rejection vs unreachable

Unchanged Phase 19 semantics: permanent/conflict failures mark local sale failed; transient keeps pending for retry. Offline grant / device security not weakened (Production still requires device header; Testing may omit for WebApplicationFactory harnesses without Platform).

## Migration / schema impact

| Store | Impact |
|---|---|
| PostgreSQL | **No WP08 migration** (sale_lines already hold snapshots from WP06) |
| LocalStore | Schema **v7** unchanged |
| Outbox | `payload_version` **2** for new cash sales |

## Tests / results (Release)

| Suite | Result |
|---|---|
| `OfflineSaleSnapshotFidelityTests` | **12** passed |
| `LocalCashSaleOfflineStoreTests` (incl. v2 payload + weighted receipt) | **7** passed |
| `PosSaleApiTests` (incl. 3 new snapshot cases) | **15** passed |
| Maui SalePage/Offline filter | **96** passed |
| Offline unit filter (broader) | **148** passed (+1 migration expectation fix for v7) |

## Known gaps deferred (WP09+)

- WP09 weighted MAUI entry UX (kg/g keypad)
- WP10 Today’s Prices; WP11 onboarding
- Legacy outbox v1 rows still live-reprice until drained
- Physical device validation

## Files changed (summary)

- Application: `CheckoutSaleLineRequest`, `CheckoutSaleLineSnapshots`, `CheckoutSale`, inventory deduction from line snapshots, error codes, payload version constants
- Maui: cart SellingMode + offline `ToCheckoutLines(includePriceSnapshots: true)`
- LocalStore: enqueue `payload_version = 2`
- Api: endpoint docs; Testing-only missing-device bypass for integration harness
- Tests + Phase 23 + this report

## Implementation commit hash

`2a5552e843b8f7340e9e7d1925a488e6366d94fb`
