# POS-MULTI-BRANCH-V2 MB2-02A — Branch Inventory Read Authority

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02A  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED_READ_AUTHORITY  
**Start SHA:** `2683f88d5f6ce2724a8c97d67ca8ce9a0e11447c`  
**Implementation SHA:** `a711019c284aeffd1d8bdfb692dc0abbd98d42c3`
**Hard stop:** YES — do not start MB2-02B in this package.

---

## Summary

Normal operational inventory **reads** now resolve **organization + selected branch** and return **branch on-hand** as `onHandQuantity`. Organization aggregate is never returned as branch stock. Branch-specific reorder configuration foundation (`InventoryBranchReorderSetting`) is persisted and used for low-stock / reorder suggestions / reorder mutation. Physical inventory **writes** remain deferred to MB2-02B.

---

## Read contract (final)

| Surface | Branch required | `onHandQuantity` meaning |
|---------|-----------------|---------------------------|
| `GET /inventory` | YES | Selected branch on-hand |
| `GET /inventory/{productId}` | YES | Selected branch on-hand |
| `GET /inventory/low-stock` | YES | Branch low-stock before pagination |
| `GET /inventory/reorder-suggestions` | YES | Branch stock vs branch reorder |
| `PUT /inventory/{productId}/reorder` | YES | Writes branch reorder row only |
| `GET /inventory/{productId}/lots` | YES | Branch lots (+ primary null-branch legacy) |
| `GET /inventory/lots` (expiring) | YES | Branch lots |
| `GET /inventory/{productId}/movements` | YES | Branch-filtered (+ primary null legacy) |
| `GET /inventory/{productId}/reconciliation` | NO (org management) | Explicit `organizationOnHandQuantity`, `sumExplicitBranchOnHand`, `unallocatedQuantity` |

Missing branch context → `400` / `pos.inventory.branch_required`. No silent org fallback.

---

## Resolver compatibility (BranchStockResolver)

- Explicit branch balance row → use row quantity.
- Missing non-primary branch row → **0**.
- Missing primary branch row → org aggregate minus sum of other explicit branch balances (unallocated primary legacy).
- Read paths do **not** materialize balance rows.

Primary reorder fallback: org `InventoryAccount.ReorderLevel/ReorderQuantity` applies to primary branch only until branch row exists. Secondary branches have no default reorder until configured.

---

## SQL / pagination strategy

`BranchInventoryQueryRepository` composes EF query from `CatalogProduct` membership (governance filter before count), left joins `InventoryAccount`, explicit branch balance, branch reorder setting, and correlated subquery for other-branch sum. Branch on-hand and low-stock membership computed in SQL **before** `Count` / `Skip` / `Take`. Movement summaries loaded in one bounded query per page.

---

## Migration

**Name:** `20260901044750_AddInventoryBranchReorderSettings`  
**Table:** `pos.inventory_branch_reorder_settings` PK `(organization_id, branch_id, product_id)`  
No stock rewrite, no balance mass backfill, no product duplication.

---

## Offline cache audit

Inventory management APIs are **online-only** (`PosOfflineCapabilityPolicy.InventoryManage = OnlineRequired`). React inventory queries use TanStack Query keys including `organizationId` + `branchId`. No product-only offline inventory quantity cache identified in React POS client. Full offline branch cache matrix deferred to MB2-06.

---

## Write-path audit (MB2-02B)

| Operation | Current org write | Current branch write | Branch source | Lot branch | Movement branch | Gap | 02B action |
|-----------|-------------------|----------------------|---------------|------------|-----------------|-----|------------|
| Enable + opening | Org account | Partial/none | Header | Often null | Optional | Opening may not credit branch | Harden branch overlay |
| Add opening stock | Org account | Partial | Acting branch | Mixed | Optional | Same | Harden |
| Adjustment | Org + overlay if branchId | When branchId supplied | Body/header | Yes | Yes | Enforce branch required | Harden |
| Stock count complete | Org + overlay paths | Session-dependent | Acting | — | — | Verify branch session | Audit/harden |
| Direct Purchase | Org account | No overlay | — | Often null | Often null | No branch write | Harden receive branch |
| PO/GRN receipt | Org account | No overlay | — | Often null | Often null | No branch write | Harden receive branch |
| Purchase reversal | Org | Mixed | Original | Mixed | Mixed | Branch reverse | Harden |
| Sale reserve/deduct | Org + overlay | At reserve | Sale.BranchId | Yes | Yes | Mostly OK | Verify |
| Customer order reserve | Org + overlay | At reserve | FulfillmentBranchId | Yes | Yes | OK | Verify |
| Sale return | Org | Mixed | Via Sale.BranchId | Mixed | Mixed | Infer from sale | Harden |
| Stock Use | Org + overlay | Yes | Acting | Yes | Yes | Mostly OK | Verify |
| Waste/Loss | Org + overlay | Yes | Acting | Yes | Yes | Mostly OK | Verify |
| Production | Org + overlay | Yes | Acting | Yes | Yes | Verify | Verify |
| Transfer dispatch/receive | Org lifecycle | Source/dest balances | Transfer branches | Yes | Yes | Proven | Audit only |
| Transfer cancel | Reverse | Same | — | — | — | — | Audit |
| Expiration lot receive/write-off | Org + lot | Mixed | Mixed | Yes | Yes | Null-branch legacy | Harden attribution |

---

## Schema provenance

| Entity | Branch field | Evidence |
|--------|--------------|----------|
| `InventoryLot` | `BranchId` nullable | `InventoryLotRecord.BranchId`, domain `InventoryLot.BranchId` |
| `StockMovement` | `BranchId` nullable | `StockMovementRecord.BranchId`, domain `StockMovement.BranchId` |
| `InventoryTransfer` | `SourceBranchId`, `DestinationBranchId` | Transfer records |
| `DirectPurchaseReceipt` | **None** | No `BranchId` on purchasing records |
| `GoodsReceipt` | **None** | No `BranchId` on purchasing records |
| `Sale` | `BranchId` nullable | `SaleRecord.BranchId` |
| `CustomerOrder` | `FulfillmentBranchId` | `CustomerOrderRecord.FulfillmentBranchId` |
| `SaleReturn` | **None direct** | Branch inferred from linked `Sale.BranchId` in queries |

Historical rows with null movement/lot branch: attributed to **primary only** on read (compatibility). Write cleanup deferred to MB2-02B/02C.

---

## Tests

| Suite | Result |
|-------|--------|
| `BranchStockResolverTests` | 4 passed |
| `BranchInventoryReadAuthorityIntegrationTests` | 6 passed |
| `BranchInventoryQueryPersistenceTests` | 2 passed |
| `PosInventoryApiTests` + `PosAdvancedInventoryApiTests` | Passed (with branch header) |
| React `InventoryBranchReadAuthority.test.tsx` | 5 passed |
| React `InventoryDetailPage.test.tsx` | 11 passed |
| React typecheck | Pass |

---

## Deferred

- **MB2-02B:** Physical inventory write-path authority  
- **MB2-02C:** Lots/movement write hardening, reconciliation UI if needed  
- **MB2-02D:** Final inventory closure  
- **MB2-03+:** Pricing, customer/supplier ACL, guided setup, offline matrix, E2E

**NEXT:** MB2-02B

---

## Files changed (representative)

- Application: `BranchInventoryReadService`, `BranchInventoryContextResolver`, `IBranchInventoryQueryRepository`, reorder repository interface, `InventoryQueryService` branch reads  
- Infrastructure: `BranchInventoryQueryRepository`, `InventoryBranchReorderRepository`, migration, DI  
- API: `InventoryEndpoints` branch resolution, `PosOrganizationBranchDirectory` testing primary  
- React: inventory query keys, branch labels, i18n  
- Tests: integration + persistence + React UX
