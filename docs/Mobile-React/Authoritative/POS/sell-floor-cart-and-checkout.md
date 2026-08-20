# Sell Floor, Cart, and Checkout

## CURRENT — MAUI

| Capability | Status | Evidence |
|------------|--------|----------|
| Product search / categories / barcode | PROVEN_CURRENT | `SaleCheckout.razor`, catalog lookup |
| Product tiles | PROVEN_CURRENT | Sell floor UI |
| ByWeight entry | PROVEN_CURRENT | Weight dialogs / SellingMode |
| Multi sell-unit (“Sell as”) | PROVEN_CURRENT | `SellingUnit*Dialog`, ProductUnitDraft |
| Cart line edits | PROVEN_CURRENT | `SaleCartService` |
| Tracked stock display/check | PROVEN_CURRENT | Uses `IsTracked` |
| Customer attachment | PROVEN_CURRENT | Checkout customer attach |
| Payment selection | PROVEN_CURRENT | Cash / ManualGCash / Utang paths |
| Active shift / device requirements | PROVEN_CURRENT | Sale use cases |
| Offline cash checkout | PROVEN_CURRENT | Queueable `/sales/new` + outbox |

Route: `/sales/new` (checkout + cart), role homes `/cashier|manager|owner`.

## CURRENT — Backend checkout contract

- `POST /api/v1/pos/sales` with line snapshots (unit, qty, multiplier, base qty, prices, selling mode)
- Inventory deduction for tracked products
- Idempotency / duplicate submission protection
- Payment method rules (non-cash typically OnlineRequired)

## React (baseline)

| Area | Status | Evidence |
|------|--------|----------|
| Sell floor browse/search/categories | PROVEN_PARTIAL | `SellFloorPage.tsx` |
| In-memory session cart | PROVEN_PARTIAL | `SessionCartProvider` |
| Checkout / pay | MISSING (explicitly disabled) | i18n `sell.payDisabledTitle`; no sale POST client |
| ByWeight / sell-unit dialogs | MISSING | |
| Shift gate | MISSING | |
| Offline cart/outbox | MISSING | |

## OWNER notes

Cashier cannot manage base catalog/inventory by default. Price override only when future policy permits — not CURRENT.
