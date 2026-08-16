# P27-WP05 — Fulfillment, Goods Receipt & Discrepancies

Package: **P27-WP05 — Fulfillment, Goods Receipt & Discrepancies**
Phase: [Phase 27 — Connected Supplier Commerce & Purchasing](../phases/phase-27-connected-supplier-commerce-and-purchasing.md)
Design: [purchasing-inventory-ux-mental-model.md](../engineering/purchasing-inventory-ux-mental-model.md)

## Status

**Code Complete.** Phase 27 remains **Open / In Progress**.

| Gate | Value |
|---|---|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

**Starting SHA:** `f92428d1d78a02d61103dc0bbe3836eddf921caa`

## Scope

Align connected supplier fulfillment with buyer goods receipt, including good, damaged, rejected, and short-close quantities while preserving the rule that only good quantity enters inventory.

## Implementation summary

- Supplier can mark accepted orders Preparing and Fulfilled without changing buyer stock.
- Buyer receiving is allowed only after supplier acceptance and records good, damaged, rejected, and short-closed quantities per line.
- Goods receipt lines retain discrepancy kind and optional note for Short, Damaged, Wrong Item, Expired, Rejected, or Other cases.
- Short-close closes the remaining ordered quantity; completed orders with a short close derive `ReceivedWithIssues`.
- Only `QuantityReceived` (good quantity), converted to base units where applicable, produces `PurchaseReceipt` inventory movement.
- Receipt success/issues publish supplier notifications through `IOrganizationBusinessNotificationPublisher`.

## Domain decisions

- Supplier Preparing/Fulfilled is commercial fulfillment state, not buyer stock state.
- `good + damaged + rejected + short-close` cannot exceed outstanding quantity.
- Damaged and rejected quantities are documented but remain outstanding unless separately short-closed.
- Short-close explicitly closes quantity that will not be received and sets the buyer PO receiving-issues flag.
- A normal partial receipt remains Partially Received; a fully closed short receipt is Received With Issues.
- WP01 eligibility and price snapshots are unchanged.

## API changes

- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/prepare`
- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/fulfill`
- Existing purchase-order receive request adds `DamagedQty`, `RejectedQty`, `ShortClosedQty`, `DiscrepancyKind`, and `DiscrepancyNote`.
- Purchase-order and goods-receipt DTOs expose receiving issues, closed-short quantity, discrepancy quantities, kind, and note.
- MAUI purchasing receive/review surfaces capture discrepancies and show Received With Issues.

## Persistence / migration

Migration: `20260816094758_AddConnectedPoLifecycleAndReceivingDiscrepancies`.

It adds connected-order fulfillment timestamps, `purchase_order_lines.closed_short_qty`, and goods-receipt line damaged/rejected/short-close quantities plus discrepancy kind/note. Check constraints enforce nonnegative quantities and at least one receiving activity per receipt line. Migration apply/rollback/re-apply evidence is not yet recorded.

## Security

- Receiving is buyer-organization scoped and requires purchasing-management capability.
- Connected receipt is rejected before supplier acceptance.
- Product ownership, outstanding quantity, and discrepancy inputs are validated server-side.
- Inventory mutation remains centralized in `PurchaseStockService`; damaged/rejected/short-only lines are skipped.
- Notifications are best-effort and are not inventory authority.
- No production-readiness claim is made.

## Validation evidence

| Evidence | Result |
|---|---|
| Targeted unit tests | **Run** — fulfillment, short-close, discrepancy, over-receipt, and display-status tests passed |
| MAUI guard tests | **Run** — purchasing receive/review and connected-order guards passed |
| Device validation | **Not run** |
| Browser validation | **Not run** |
| Migration apply / rollback / re-apply | **Not recorded** |

Device Verified: **No**. Browser Verified: **No**. Production Ready: **No**.

## Implementation commits

Commit hashes: **TBD — recorded after commit**.

## Residuals

- Owner must validate good/damaged/rejected/short-close entry and inventory outcomes on device.
- Full solution Release build/test and PostgreSQL migration apply/rollback/re-apply evidence remain required.
- Returns, supplier credits, invoicing/AP, shipping, and auto-receive remain excluded.
- Phase 27 remains open.

## Exact next

**P27-WP06 — Connected Purchasing UX & Notifications.**
