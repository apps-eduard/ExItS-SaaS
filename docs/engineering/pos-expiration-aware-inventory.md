# POS optional expiration-aware inventory lots

Per-product expiration tracking for PinoyBusinessPOS. Expiration belongs to a **quantity of stock** (a lot), not to `CatalogProduct`. Default is off. Existing non-expiry inventory, sales, and receiving behavior is unchanged.

## Branch-scoped expiration tracking (invariant)

**Authoritative expiration enablement is branch-scoped**, not catalog-global:

- `EnableExpirationTracking` **requires** a selected `branchId`.
- Authoritative on-hand for enable / lot allocation is the **current branch physical quantity** via `BranchStockResolver` (explicit `InventoryBranchBalance`, or primary-branch unallocated overlay) — **not** the organization `InventoryAccount` total.
- Existing-stock lot quantities must sum exactly to that **branch** on-hand.
- Operational policy is stored in `InventoryBranchExpirationSetting` (per org + branch + product).
- `CatalogProduct.TracksExpiration` / `ExpirationWarningDays` remain **legacy compatibility** fields only; they are not authoritative for branch inventory operations and must not be turned on from normal catalog create/edit UX.
- Branch operational attention (`GET /api/v1/pos/inventory/attention-summary`) uses `CountExpiryAsync` for the bound branch only. Organization `GET /api/v1/pos/management/overview` lot counts stay org-wide for management/reporting screens — do not mix the two on one operational surface.

## Audit result

Expiration tracking was **missing**. Inventory was org-level `InventoryAccount` plus optional `InventoryBranchBalance`. `StockMovement` is the product-level ledger. There was no lot/batch table. Sales deduct org on-hand. Branch transfers already exist and identified stock by product+qty only.

Purchase goods receipts remain product-level in this overlay (same as non-expiry receiving). Expiration-aware stock-in for perishables is **Adjust In** / opening stock. Do not leak organization expiry into Global Catalog.

## Product configuration

| Field | Default | Meaning |
|---|---|---|
| `InventoryBranchExpirationSetting.TracksExpiration` | off | **Authoritative** per branch + product. When off for the branch, no lot UI or FEFO for that branch. |
| `InventoryBranchExpirationSetting.ExpirationWarningDays` | `7` when tracking is on | Near-expiry window for the branch. Does not block sale. |
| `CatalogProduct.TracksExpiration` | `false` | **Legacy** compatibility only — not authoritative for enable/allocate. |
| `CatalogProduct.ExpirationWarningDays` | nullable | **Legacy** compatibility only. |

Not an organization-wide mandate. Global Catalog does not store live `ExpirationDate`.

## Expiry sale policy (StopSellingDaysBeforeExpiry)

Separate from expiration **tracking** and **warning days**:

| Concept | Meaning |
|---|---|
| `TracksExpiration` | Whether the branch/product captures expiry lots |
| `ExpirationWarningDays` | Near-expiry **alert / presentation** window only — never sale eligibility |
| `StopSellingDaysBeforeExpiry` | Normal **POS sale** eligibility cutoff (organization / category / branch hierarchy) |

Default organization behavior: `StopSellingDaysBeforeExpiry = 0` → stock may be sold **on** its expiry date; the next calendar day it is **Expired** and never sellable.

Formal lot sale eligibility (business date = today):

- `ExpirationDate < today` → **Expired** (absolute; no override may allow sale)
- else if `StopSellingDaysBeforeExpiry > 0` and `ExpirationDate <= today + StopSellingDays` → **SaleBlockedByExpiryPolicy** (physically on hand, not sellable)
- else → **Sellable**

Policy precedence (most specific wins): Branch+Category → Branch default → Organization Category → Organization default.

Canonical resolver: `ExpirySalePolicyResolver`. FEFO for **normal sales** allocates only Sellable lots under the effective policy. Transfers / stock-use / waste / custody keep absolute-expiry rules and do **not** apply the sale cutoff.

Physical on-hand does **not** decrease when a lot becomes policy-blocked or expired by time. Expired physical stock is removed via existing Waste/Loss → Expired (or equivalent authorized disposal). There is no midnight auto-write-off and no separate “unlock expired” mutation — locks are derived from lot dates + policy.

Inventory list/detail project:

- `SellableQuantity` / `SalePolicyBlockedQuantity` / `ExpiredQuantity` (lot partition)
- `AvailableQuantity` for expiration-tracked products = `Min(operational available, sellable lot qty)` so the main Available number matches what a normal sale can consume

## Lot model

```text
InventoryBranchExpirationSetting (branch + product)   authoritative tracking flag
CatalogProduct.TracksExpiration                       legacy compatibility only
  InventoryAccount              org on-hand total (unchanged)
  InventoryLot                  org + product + optional branch + expiry + optional lot number
  InventoryLotMovement          lot-level ledger (idempotent per source+lot+type)
```

Do not duplicate `CatalogProduct` per batch. `LotNumber` is optional. `ExpirationDate` is required for new tracked stock. Same product + location + expiry + normalized lot number aggregates; different expiry dates stay separate lots.

Stable lot ids. PostgreSQL unique identity:

- org-level lots: `(organization_id, product_id, expiration_date, normalized_lot_number)` where `branch_id IS NULL`
- branch lots: same keys plus `branch_id` where `branch_id IS NOT NULL`

## FEFO

Expiration-tracked **normal sales** consume **First Expire, First Out** among lots whose effective sale eligibility is **Sellable** under `StopSellingDaysBeforeExpiry` (past-expiry always excluded; policy-blocked lots are skipped). Earliest expiry first; continue to the next eligible lot when the first is exhausted. Non-tracked products keep existing product-level deduction.

Non-sale physical flows (branch transfer FEFO, stock-use, production materials) continue to exclude only **expired** lots (`ExpirationDate < today`) and do not apply the sale cutoff.

## Expired stock

Expired lots stay on-hand until an authorized Waste/Loss (reason `Expired`) or other authorized disposal. They are not sellable, not auto-deleted, and not silently zeroed at midnight. Checkout rejects when sellable qty is insufficient, even if expired or policy-blocked qty would cover the sale.

Near-expiry (warning window) remains a presentation concept and is independent of stop-selling days.

## Inventory totals

| Figure | Source |
|---|---|
| Physical on-hand | Branch / account on-hand (lots sum when tracking) |
| Sellable | Sum of lot qty with sale eligibility Sellable |
| Sale policy blocked | Sum of lot qty blocked by stop-selling days (not yet expired) |
| Expired | Sum of lot qty with `ExpirationDate < today` |
| Available (list/detail main) | For expiration-tracked: `Min(operational available, sellable)` |

List enrichment batches lots + effective policies (no N+1 per row). Lot detail pages and `GET .../lots` remain available for lot identity.

## Receiving / adjustments

When branch expiration tracking is off: existing Enable / Adjust In/Out.

When on for the branch:

- In / opening qty > 0 requires expiration date
- Out requires `LotId` or expiry + optional lot number
- Adjustments mutate the specific lot, not only the aggregate account
- Manual Adjust has no idempotency key (same as before). Opening stock remains unique per product. Transfer receive / sale deduction are source-idempotent.

## Branch transfers

Transfers preserve lot identity. Lines may repeat the same product with different `SourceLotId`. Unique line constraint is `(transfer_id, line_number)`, not product.

**Normal branch replenishment / transfer**

- Default product-picker availability and auto FEFO prefer **non-expired** lots (`ExpirationDate >= businessToday`; expires-today is eligible).
- Change lots may allocate **any** positive on-hand lot, including expired, via QuantityStepper — physical moves for disposal / relocation are allowed.
- Create and dispatch accept an expired `SourceLotId` when the operator selects it intentionally (not a sale).
- Transferable picker availability remains the lesser of branch available quantity and the sum of non-expired lot quantities (expired stock is not offered as default replenishment capacity).

**In-transit receive**

- Stock already dispatched may still be received if it expires during transit. Destination preserves `LotNumber` / `ExpirationDate` / lot identity; the destination lot is naturally expired / non-sellable.

**Exception / custody / return / disposal**

- Explicit Return-to-Source, discrepancy returns, damage/exception custody, and expired write-off remain physical/exception movements and are not blocked merely because the lot is expired.
- Branch transfer Change-lots can also move expired stock when the operator chooses those lots.

Receive retry uses existing transfer idempotency. Replacement / remaining fulfillment reallocates from **current** sellable FEFO lots (does not reuse a depleted or expired original `SourceLotId`).

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

## Lot identity correction (metadata only)

**DRAFT / UNUSED LOT** — When a lot has only origin/acquisition movements (opening stock, purchase/direct purchase receipt, manual increase, expiration initialization, production output) and is **not** referenced by an active transfer draft, expiration date and batch/lot number may be corrected with an audit record (`InventoryLotIdentityCorrection`). Quantity never changes.

**DOWNSTREAM USAGE EXISTS** — Once inventory from a lot has participated in an operational transaction (sale, transfer, stock use, waste/loss, variance, return, reversal, transfer damage/exception, etc.), **both** expiration date and batch/lot number are locked through normal editing. A blank lot number is **not** an exception.

> Once inventory from a lot has participated in an operational transaction, the lot's expiration date and batch/lot number are immutable through normal editing.

Additional permanent locks for normal edit:

- Lots created by **TransferIn** (destination must preserve source identity for cross-branch traceability).
- Lots referenced by an **active transfer Draft** (`SourceLotId`) — cancel/remove the draft reference before correcting.

Identity collision (same org + branch + product + expiry + normalized lot number) is rejected without merging quantities. Post-usage reclassification of remaining stock is a separate future workflow.
