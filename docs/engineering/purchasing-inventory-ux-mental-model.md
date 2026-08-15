# Purchasing & Inventory UX mental model (MAUI)

**Status:** Code Complete (terminology / navigation). Not Browser/Device Verified. Not production-ready.

## User mental model

| Area | Meaning |
|------|---------|
| **Purchasing** | Buy and receive goods coming into the business |
| **Receive stock** | Goods are already physically here → inventory increases now |
| **Purchase orders** | Order now, receive later → inventory does **not** change until goods receipt |
| **Goods receipts** | Receive against an existing purchase order → inventory increases when completed |
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
- **Goods receipt** → `PurchaseReceipt` stock movement.

Do **not** use user-facing labels “Direct Stock In” or “Manual Purchase”.

## Organization Web

Org Web still defers full Purchasing pages. Inventory stock adjust terminology aligns to **Receive stock** / stock-control focus; PO workflow remains POS-primary for now.
