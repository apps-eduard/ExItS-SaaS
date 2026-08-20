# PinoyBusinessPOS Domain Overview

## Bounded context

PinoyBusinessPOS is an organization-owned operational product with:

- Own PostgreSQL schema: `pos` (`PosDbContext.SchemaName`)
- Own product-local roles (mapped from Platform grants)
- Own operational aggregates (catalog, inventory, sales, purchasing, customers, etc.)
- No cross-product DB access

## Aggregate map (current)

| Area | Primary aggregates / entities | API prefix |
|------|-------------------------------|------------|
| Catalog | `ProductCategory`, `CatalogProduct`, `CatalogProductUnit`, images, import jobs | `/api/v1/pos/catalog/*`, `/catalog-imports` |
| Inventory | `InventoryAccount`, `StockMovement`, lots, counts, transfers, direct receipts | `/api/v1/pos/inventory/*` |
| Sales | `Sale`, `SaleLine`, payment attempts | `/api/v1/pos/sales` |
| Returns | `SaleReturn*` | `/api/v1/pos/sale-returns` |
| Customers / credit | Customer, `CreditEntry`, repayments | `/api/v1/pos/customers` |
| Suppliers | `Supplier` | `/api/v1/pos/suppliers` |
| Connected suppliers | Relationships, exposures, shares, links, connected POs | `/api/v1/pos/connected-suppliers` |
| Purchasing | `PurchaseOrder`, `GoodsReceipt`, direct purchase receipts | `/api/v1/pos/purchase-orders`, `/goods-receipts` |
| Registers / shifts | `Register`, `CashierShift` | `/api/v1/pos/registers`, `/cashier-shifts` |
| Expenses | Expense categories + expenses | `/api/v1/pos/expenses*` |
| Reports | Read models | `/api/v1/pos/reports`, `/dashboard` |
| Customer ordering | Customer orders + storefront | `/api/v1/pos/customer-orders`, org customer-orders |
| Offline | LocalStore + outbox + capability policy | sync endpoints + MAUI LocalStore |

## Cross-cutting invariants

1. **Branch ≠ Register ≠ Device**
2. **Org membership ≠ POS product role**
3. **EXPOSABLE ≠ SHARED** (connected suppliers)
4. **Buyer inventory changes only through goods receipt / receive paths** (not on PO submit, share, or connection accept)
5. **One shared base inventory pool per product**; package units convert via `MultiplierToBase`
6. **Sale engine is one engine**; current document = Transaction Summary; TaxDocument not issuable
7. **Untracked inventory ≠ zero stock**; tracked + no opening = zero; oversell prohibited when tracked

## Engineering canon for units/inventory

Primary engineering doc (still current at baseline):
`docs/engineering/product-units-and-inventory-behavior.md`

## Domain docs in this set

See [00-README.md](00-README.md) POS index. Each POS doc separates:

- CURRENT contract (source/tests)
- OWNER-CONFIRMED CHANGE (when different)
- React parity status

## React scope warning

The React client at baseline implements session/workspace + read-only catalog sell-floor cart shell. Presence of a sell route is **not** sales parity. Use [Migration/react-current-state.md](Migration/react-current-state.md) and the parity matrix.
