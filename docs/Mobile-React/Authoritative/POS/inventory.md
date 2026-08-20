# Inventory

## CURRENT contract

| Topic | Behavior | Status | Evidence |
|-------|----------|--------|----------|
| Account model | One `InventoryAccount` per org/product | PROVEN_CURRENT | Domain |
| Default on product create | **No** inventory account auto-created; tracked flag treated as false until enable | PROVEN_CURRENT | `CreateCatalogProduct` has no inventory repo; queries use `account?.IsTracked ?? false` |
| Untracked shell | `InventoryAccount.CreateUntracked` → `IsTracked = false` | PROVEN_CURRENT | Domain |
| Enable tracking | `Enable` sets tracked; optional opening movement | PROVEN_CURRENT | `InventoryAccount.Enable` |
| Opening stock | `StockMovementType.OpeningStock`; actor/time on movement | PROVEN_CURRENT | `StockMovement.OpeningStock` |
| On-hand | Denormalized projection via `ApplyMovementEffect` | PROVEN_CURRENT | Domain comments |
| Movements | Immutable `StockMovement` sources (sale, receipt, adjust, return, transfer, count, etc.) | PROVEN_CURRENT | `StockMovementType` |
| Oversell | Tracked insufficient available rejected (`pos.inventory.insufficient_stock`) | PROVEN_CURRENT | Sale/inventory use cases |
| Untracked meaning | Not quantity-maintained; orderable/sellable without on-hand gate | PROVEN_CURRENT | Domain + storefront docs |
| Tracked + no opening | Zero on-hand | PROVEN_CURRENT | Enable with null/0 opening |
| Concurrency | PostgreSQL `xmin` tokens; concurrency exceptions | PROVEN_CURRENT | Inventory repository |
| Branch association | Branch balances / transfers | PROVEN_CURRENT | Advanced inventory |
| Reservation | Reserved reduces Available for customer orders | PROVEN_CURRENT | `ReservedQuantity` |
| Lots | See expiry doc | PROVEN_CURRENT | |
| Offline inventory admin | OnlineRequired | PROVEN_CURRENT | `PosOfflineCapabilityPolicy` |

## OWNER-CONFIRMED vs CURRENT

| Owner requirement | CURRENT alignment |
|-------------------|-------------------|
| New products default UNTRACKED | **Aligned** (no account / untracked until enable) |
| Untracked ≠ zero stock | **Aligned** |
| Tracked authoritative quantity | **Aligned** |
| Tracked + no opening → zero | **Aligned** |
| Oversell prohibited when tracked | **Aligned** |
| Opening as auditable movement | **Aligned** |
| Actor/time preserved | **Aligned** on movements |
| Multi-UOM consumes canonical/base qty | **Aligned** via `MultiplierToBase` |

No OWNER_CONFIRMED_CHANGE required for default tracking semantics at baseline — preserve CURRENT.

## APIs

`/api/v1/pos/inventory` (list, enable/disable, adjust, reorder, movements, lots, stock-counts, transfers)
`/api/v1/pos/direct-purchase-receipts`

## MAUI / React

MAUI: full inventory surfaces OnlineRequired.
React: **MISSING**.

## Tests

`InventoryAccountDomainTests`, reservation/transfer/receipt/receive-stock semantics tests.
