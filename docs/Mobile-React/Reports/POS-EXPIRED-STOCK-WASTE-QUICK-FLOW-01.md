# POS-EXPIRED-STOCK-WASTE-QUICK-FLOW-01

## Summary

Operational **quick flow** from Inventory Expiration into the **existing** Waste/Loss create form. Expiration never writes inventory itself; the operator reviews a prefilled Waste/Loss and explicitly posts. No new waste domain, movement type, or automatic expiry disposal.

| Field | Value |
|--------|--------|
| EXPIRATION_PAGE | `InventoryExpirationPage` (`/inventory/expiration`) |
| EXPIRING_LOT_SOURCE | `listExpiringLots` / `PosExpiringLotDto` |
| EXPIRATION_BRANCH_SCOPE | Bound branch (`organizationId` + `branchId`) |
| EXISTING_WASTE_LOSS_FLOW | `/inventory/waste-loss/new` → `createWasteLoss` |
| QUICK_FLOW_MODEL | Prefill-only navigation into existing Waste/Loss |
| QUICK_FLOW_ROUTE | `/inventory/waste-loss/new?productId=&lotId=&reason=Expired&source=expiration&quantity=` |
| QUICK_FLOW_REASON | `Expired` |
| QUICK_FLOW_PRODUCT_PREFILL | YES (exact `productId`) |
| QUICK_FLOW_LOT_PREFILL | YES (exact `lotId`; revalidated via `listProductLots`) |
| QUICK_FLOW_QUANTITY_PREFILL | YES — **current** `lot.quantityOnHand` after refetch (query `quantity` is UI hint only) |
| EXACT_LOT_REVALIDATION | FAIL CLOSED if lot missing / wrong product; never FEFO substitute |
| STALE_QUANTITY_POLICY | Ignore navigation quantity; default to refreshed on-hand |
| ZERO_REMAINING_POLICY | Block create; show “No stock remains in this lot” + Back to Expiration |
| WRONG_LOT_POLICY | “This lot is no longer available”; no alternate lot |
| AUTO_WRITE_OFF | **NO** |
| EXPLICIT_CONFIRMATION | **YES** (`Record waste / loss`) |
| EXISTING_MULTI_LOT_SAME_PRODUCT_UI | **NO** (draft lines keyed by `productId`) |
| BATCH_EXPIRED_WRITE_OFF | **DEFERRED** |
| PERMISSION_MODEL | Write off UI: `canManageInventory`; Expiration view uses existing inventory access; server remains authoritative |
| BRANCH_GUARD | Prefill re-runs on branch change; server lot/WasteLoss guards unchanged |
| CROSS_ORG_GUARD | Query IDs untrusted; product/lot fetch scoped to workspace |
| WASTE_LOSS_COST_BEHAVIOR | Unchanged Complete / Partial / Unavailable |
| VOID_RESTORATION_BEHAVIOR | Existing Waste/Loss void + exact-lot restore |
| OFFLINE_MODE | ONLINE_ONLY |
| BACKEND_CHANGE_REQUIRED | **NO** |
| MIGRATION_REQUIRED | **N/A** |

## UX

### Expiration row

- Card shows product, expiry badge, date, lot, on hand
- **[View]** → inventory product detail (row is not itself destructive)
- **[Write off]** only when resolved status is **Expired**, `quantityOnHand > 0`, and ManageInventory

Near-expiry / Expires today: View only (no expired write-off action).

### Waste/Loss quick context

When `source=expiration` + exact lot ready:

- Title: Record expired stock
- Context card: product, lot, expired date, available in lot
- Reason default Expired (editable)
- Quantity default = current lot on-hand (editable)
- Submit remains explicit

## Explicit exclusions

- Automatic expiration write-off / scheduled disposal
- New WasteLoss domain or movement type
- `adjustInventoryStock` shortcut
- Batch multi-lot same-product write-off (deferred)
- Offline Waste/Loss queue
- Near-expiry auto-disposal

## Validation

| Check | Result |
|--------|--------|
| Backend WasteLoss/Expiration unit filter | 55 passed |
| React targeted (quick-flow + expiration + create prefill) | 15 passed |
| React typecheck | PASS |
| React lint | PASS (0 errors; existing warnings) |
| React build | PASS |
| React full suite | TOTAL=1252 PASS=1182 FAIL=70 |
| EXPIRED_WASTE_RELATED_FAILURES | 0 |
| OTHER_ORGANIZATION_FAILURES | 0 |
| Known unrelated fails | Personal ~43 / Platform ~20 / Global Session ~7 |
| Conflict markers | 0 |
| `git diff --check` | clean |
| NEW_TEST_SKIPS / ONLY | 0 |
| BACKEND_CHANGE_REQUIRED | NO |
| MIGRATION | N/A |

## Git

| | |
|--|--|
| START_SHA | `a06ecc9f5b708d3ca769b16d72990f3f76d618c2` |
| FEATURE_COMMIT | `d2af6ad4c5b2ef12945edce3232591afe61dee1c` |
| BRANCH | `feat/organization` |

## NEXT

`POS-DISCOUNT-REPORTING-HARDENING-01`
