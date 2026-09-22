# Connected PO receiving-issue seller review (hardening)

**Status:** Development evidence — **not** Device / Browser / Production Ready.

## Purpose

Buyer goods receipts remain immutable evidence of what was physically received (good / damaged / missing).
Seller receiving-issue review records decisions and may restore seller stock via a separate
`ConnectedPurchaseFulfillmentReconciliation` movement. Buyer on-hand is never decreased by seller
resolution, and original `ConnectedPurchaseFulfillment` deductions are never rewritten.

## Inventory invariant

| Event | Seller on-hand | Buyer on-hand | Notes |
| --- | --- | --- | --- |
| Fulfill wave | −shipped | unchanged | `ConnectedPurchaseFulfillment` SourceId = order id or wave id |
| Buyer receive good qty | unchanged | +good | GRN / purchase receipt path |
| Buyer report missing/damaged | unchanged | unchanged (evidence only) | Creates pending receiving issue |
| Resolve `FoundAtSeller` / `NeverShipped` | +resolutionQty | unchanged | New reconciliation movement; SourceId = issue line id |
| Resolve `LostInTransit` (and other non-restore) | unchanged | unchanged | No reconciliation movement |
| Resolve damaged `ReturnRequested` | unchanged immediately | unchanged | Links connected-PO return batch when buyer PO is `Received` |
| Retry same resolution | unchanged | unchanged | Idempotent; no second reconciliation / batch |

## Multi-wave attribution

When creating an issue from a goods receipt, each line’s `FulfillmentSourceId` is attributed from the
**most recent** `ConnectedPurchaseFulfillment` movement for that supplier product among candidates
`{ order.Id } ∪ { Wave(orderId, r) for r = 1..InventoryReservationRevision }`.

If no movement is found, the revision heuristic (`ResolveFulfillmentSourceIdForReceipt`) is used.
Reconciliation reason text embeds the attributed fulfillment source for audit.

## API safety

`ResolveConnectedPoReceivingIssueRequest` / line request DTOs have **no** `inventoryDelta` field.
Inventory effects are derived only from the resolution code on the server.

## Persistence

- Hand-written migration `20260921120000_AddConnectedPoReceivingIssues` creates issue tables and widens
  `stock_movements` / `inventory_lot_movements` `movement_type` to 48.
- `xmin` concurrency is Fluent `IsRowVersion` (Npgsql system column — not in `CreateTable`).
- `PosDbContextModelSnapshot` includes receiving-issue entity blocks (synced without a duplicate CreateTable migration).

## Explicit exclusions

- Not production authentication / authorization hardening beyond existing commercial capabilities.
- Not Device / Browser / Production Ready certification.
- Buyer GRN quantities remain immutable; seller cannot rewrite buyer evidence.
