# POS optional expiration-aware inventory lots

Per-product expiration tracking for PinoyBusinessPOS. Expiration belongs to a **quantity of stock** (a lot), not to `CatalogProduct`. Default is off. Existing non-expiry inventory, sales, and receiving behavior is unchanged.

## Audit result

Expiration tracking was **missing**. Inventory was org-level `InventoryAccount` plus optional `InventoryBranchBalance`. `StockMovement` is the product-level ledger. There was no lot/batch table. Sales deduct org on-hand. Branch transfers already exist and identified stock by product+qty only.

Purchase goods receipts remain product-level in this overlay (same as non-expiry receiving). Expiration-aware stock-in for perishables is **Adjust In** / opening stock. Do not leak organization expiry into Global Catalog.

## Product configuration

| Field | Default | Meaning |
|---|---|---|
| `TracksExpiration` | `false` | Per-product. When false, no lot UI or FEFO. |
| `ExpirationWarningDays` | `7` when tracking is on | Near-expiry window. Does not block sale. |

Not an organization-wide mandate. Global Catalog does not store live `ExpirationDate`.

## Lot model

```text
CatalogProduct.TracksExpiration
  InventoryAccount              org on-hand total (unchanged)
  InventoryLot                  org + product + optional branch + expiry + optional lot number
  InventoryLotMovement          lot-level ledger (idempotent per source+lot+type)
```

Do not duplicate `CatalogProduct` per batch. `LotNumber` is optional. `ExpirationDate` is required for new tracked stock. Same product + location + expiry + normalized lot number aggregates; different expiry dates stay separate lots.

Stable lot ids. PostgreSQL unique identity:

- org-level lots: `(organization_id, product_id, expiration_date, normalized_lot_number)` where `branch_id IS NULL`
- branch lots: same keys plus `branch_id` where `branch_id IS NOT NULL`

## FEFO

Expiration-tracked sales consume **First Expire, First Out** among **non-expired** lots (`ExpirationDate >= today`). Earliest expiry first; continue to the next lot when the first is exhausted. Non-tracked products keep existing product-level deduction.

Sales remain org-scoped (no sale `BranchId`). FEFO therefore considers on-hand lots for the product across branches.

## Expired stock

Expired lots stay on-hand until an authorized `Out` adjustment (reason `Expired` or existing reasons). They are not sellable, not auto-deleted, and not silently zeroed. Checkout rejects when non-expired sellable qty is insufficient, even if expired qty would cover the sale.

Near-expiry (including expires today) remains sellable. UI distinguishes Expired / Expires today / Expires in N days.

## Inventory totals

| Figure | Source |
|---|---|
| Total on-hand | `InventoryAccount.OnHandQuantity` = sum of lots |
| Sellable | sum of non-expired lot qty (detail only) |
| Expired | sum of expired lot qty (detail only) |
| Near-expiry | warning-window lots, not expired (detail only) |

Product **list** does not load lots (no N+1). Lot summaries are on inventory/product detail and `GET .../lots` (paged).

## Receiving / adjustments

When `TracksExpiration = false`: existing Enable / Adjust In/Out.

When true:

- In / opening qty > 0 requires expiration date
- Out requires `LotId` or expiry + optional lot number
- Adjustments mutate the specific lot, not only the aggregate account
- Manual Adjust has no idempotency key (same as before). Opening stock remains unique per product. Transfer receive / sale deduction are source-idempotent.

## Branch transfers

Transfers preserve lot identity. Lines may repeat the same product with different `SourceLotId`. Unique line constraint is `(transfer_id, line_number)`, not product. Dispatch consumes the source lot; destination receive creates/updates the dest-branch lot with the snapshotted expiry and lot number. Partial receive shortages stay on that line/lot. Expired lots may be transferred (physical move, not a sale). Receive retry uses existing transfer idempotency.

## Offline / concurrency

Inventory mutations stay **online-only** (no offline inventory queue, no peer-to-peer sync). Sale retry uses client `SaleId` and does not double-deduct lots. Lot rows use `xmin`. Lot qty cannot go negative. Only changed lot/account rows persist; there is no full-inventory sync.

## Authorization

Existing `ViewInventory` / `ManageInventory`. Organization id from request scope. Lot must belong to the product and organization. Personal users cannot mutate organization inventory. Cross-tenant lot list is empty.

## Notifications

No new notification engine. Near-expiry is a UI/query state, not a broadcast.

## Migration

`AddPosInventoryLots`:

- `products.tracks_expiration` default false; `products.expiration_warning_days` nullable
- `inventory_lots`, `inventory_lot_movements`
- `stock_movements.inventory_lot_id`
- transfer line `source_lot_id`, `lot_number`, `expiration_date`
- transfer unique index replaced with `ux_inventory_transfer_lines_transfer_line_number`

## Owner acceptance

Device Verified is **No** until the owner validates on a physical device.

### NON-EXPIRY PRODUCT

1. Create USB Cable.
2. Leave Track expiration OFF.
3. Receive stock.
4. Sell normally.
5. Confirm no expiry fields block workflow.

### EXPIRY PRODUCT

1. Create Milk 1L.
2. Enable Track expiration.
3. Receive 20 units exp Aug 20 and 30 units exp Sep 5.
4. Confirm total = 50.
5. Confirm two separate lots exist.

### FEFO

1. Sell 5 Milk.
2. Confirm deduction comes from the Aug 20 lot.
3. Confirm Sep 5 lot unchanged.

### EXPIRED STOCK

1. Receive a lot with an expired date in test.
2. Confirm quantity remains visible.
3. Confirm it is not sellable and checkout cannot use it.
4. Perform authorized Expired write-off.
5. Confirm audit history.

### NEAR EXPIRY

1. Warning = 7 days.
2. Create a lot expiring within 7 days.
3. Confirm warning appears.
4. Confirm still sellable before expiry.

### MULTIPLE LOTS

1. Same product, different expiry dates.
2. Confirm no product duplication.
3. Confirm stock remains lot-aware.

### BRANCH TRANSFER

1. Branch A has Lot A exp Aug 20 qty 10 and Lot B exp Sep 5 qty 20.
2. Transfer 4 from A + 6 from B.
3. Confirm Branch B keeps lot identities and expiry dates.
4. Partial receive Lot A = 3; shortage 1 stays on Lot A.
5. Retry receive does not duplicate destination stock.
