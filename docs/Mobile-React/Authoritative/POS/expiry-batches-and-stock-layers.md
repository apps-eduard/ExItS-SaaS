# Expiry, Batches, and Stock Layers

## CURRENT contract

Earlier phases excluded batches/expiry; **later source added lots**.

| Topic | Behavior | Status | Evidence |
|-------|----------|--------|----------|
| Product flag | `CatalogProduct.TracksExpiration` (default false in DTOs) | PROVEN_CURRENT | Domain + Catalog DTOs |
| Lot aggregate | `InventoryLot` + lot movements | PROVEN_CURRENT | `pos.inventory_lots`, migration `AddPosInventoryLots` |
| Expiry field | Lot `ExpirationDate`; optional lot number | PROVEN_CURRENT | Domain |
| Receipt requirement | When `TracksExpiration`, positive receipt requires expiry | PROVEN_CURRENT | DirectPurchaseReceipt / inventory use cases |
| FEFO sell allocation | `InventoryLotFefo.AllocateSellable` (expired never sold) | PROVEN_CURRENT | Domain helper + tests |
| Product.ExpiryDate single field | Not the stock authority | N/A — lot-based |
| `StockBatch` / `BestBefore` type names | Not used | PROVEN_MISSING as names |
| Inventory FIFO allocation | Not implemented for stock (FIFO aging exists for credit) | PROVEN_MISSING for inventory |

## OWNER-CONFIRMED alignment

| Owner requirement | CURRENT |
|-------------------|---------|
| Default OFF | Aligned (`TracksExpiration` false by default) |
| Requires tracked inventory | Enforced in inventory enable/receipt paths when expiration tracked |
| Expiry on batch/layer not Product single date | Aligned (lots) |
| Lot optional | Aligned |
| Positive expiry-tracked receipt requires expiry | Aligned |
| Expired normally not sellable | Aligned (FEFO) |
| FEFO preferred | Aligned |
| Multiple batches | Aligned |
| Canonical quantities on batches | Lots use base quantities |

Classification: **PROVEN_CURRENT** for core expiry/lot/FEFO. Do **not** emit `POS_EXPIRY_BATCH_CONTRACT_MISSING`.

## APIs / MAUI / React

API: inventory lots endpoints under `/api/v1/pos/inventory`.
MAUI: `/inventory/expiration`, lot-aware flows.
React: **MISSING**.

## Tests

`InventoryLotDomainTests`
