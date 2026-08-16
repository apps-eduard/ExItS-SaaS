# Purchasing & Inventory UX mental model (MAUI)

**Status:** Code Complete (terminology / navigation). Not Browser/Device Verified. Not production-ready.

## User mental model

| Area | Meaning |
|------|---------|
| **Purchasing** | Buy and receive goods coming into the business |
| **Receive stock** | Goods are already physically here → inventory increases now |
| **Purchase orders** | Order now, receive later → inventory does **not** change until goods receipt |
| **Goods receipts** | Receive against an existing purchase order → only good quantity increases inventory |
| **Suppliers** | Businesses/vendors you buy from (including connected suppliers/buyers) |
| **Inventory** | View, count, and control stock you already have |

## MAUI routes

| Surface | Route |
|---------|-------|
| Purchasing hub | `/purchasing` |
| Receive stock (product pick) | `/purchasing/receive-stock` |
| Receive stock confirm | `/inventory/{productId}/adjust?intent=receive` (reuses ManualIncrease) |
| Purchase orders | `/purchasing/orders` |
| Goods receipts list | `/purchasing/receipts` |
| Goods receipt against PO | `/purchasing/{id}/receive` |
| Suppliers | `/suppliers` |
| Inventory hub | `/inventory` |
| Expiration | `/inventory/expiration` |

## Domain behavior (unchanged)

- **Receive stock** → existing inventory adjust **In** / `ManualIncrease` (supplier optional; no fabricated PO).
- **PO create / submit / supplier accept** → no on-hand increase.
- **Supplier Preparing / Fulfilled** → commercial progress only; no on-hand increase.
- **Goods receipt** → `PurchaseReceipt` stock movement for good received quantity only.

## Receiving discrepancies (P27-WP05)

Each purchase-order line may record:

- **Good quantity** — accepted usable goods; the only quantity that enters inventory.
- **Damaged quantity** — recorded as a discrepancy; does not enter inventory and remains outstanding unless short-closed.
- **Rejected quantity** — refused goods; does not enter inventory and remains outstanding unless short-closed.
- **Short-close quantity** — quantity the buyer explicitly closes as not arriving; does not enter inventory.
- **Discrepancy kind/note** — Short, Damaged, Wrong Item, Expired, Rejected, Other, plus an optional bounded note.

Good + damaged + rejected + short-close cannot exceed the outstanding ordered quantity. A normal incomplete receipt remains **Partially Received**. If remaining quantity is short-closed, the PO can complete as **Received With Issues**. Damaged/rejected/short-only receipt lines never produce stock movements.

Do **not** use user-facing labels “Direct Stock In” or “Manual Purchase”.

## Organization Web

Org Web still defers full Purchasing pages. Inventory stock adjust terminology aligns to **Receive stock** / stock-control focus; PO workflow remains POS-primary for now.
