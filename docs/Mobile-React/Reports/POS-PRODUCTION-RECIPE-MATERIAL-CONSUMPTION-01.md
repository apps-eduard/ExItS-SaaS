# POS-PRODUCTION-RECIPE-MATERIAL-CONSUMPTION-01

## Summary

Generic **Production** for PinoyBusinessPOS: production setups (definitions), material consumption, produced stock, and material-only cost snapshots. Not bakery-specific.

| Field | Value |
|--------|--------|
| EXISTING_PRODUCTION_MODEL | NONE (pre-package) |
| EXISTING_RECIPE_MODEL | NONE |
| EXISTING_MADE_PRODUCT_MODEL | USAGE_FLAGS_ONLY (`MadeProduct` / `IsProduced`) |
| EXISTING_MATERIAL_CAPABILITY | FLAGS_ONLY (`CanBeUsedAsIngredient`) |
| PRODUCTION_DOMAIN_MODEL | `ProductionDefinition` + `ProductionRun` |
| PRODUCTION_SETUP_MODEL | Definition + components; revision on edit; Active/Inactive |
| PRODUCTION_RUN_MODEL | Immutable posted run; expected/actual materials; void with safety |
| PRODUCT_BUSINESS_USAGE_PRODUCED_ITEM | `ProducedItem` → `MadeProduct` capabilities (no new DB column) |
| PRODUCTION_COMPONENT_ELIGIBILITY | `CanBeUsedAsIngredient == true` (active, tracked for post) |
| PRODUCTION_OUTPUT_ELIGIBILITY | `IsProduced == true` |
| NESTED_PRODUCTION_SUPPORT | LIMITED (only ingredients; cycle guard on definitions) |
| PRODUCTION_CYCLE_GUARD | Graph validation on create/update |
| PRODUCTION_SCALING_MODEL | `scale = outputBase / definitionOutputBase` |
| EXPECTED_ACTUAL_MODEL | Expected from setup scale; Actual editable override |
| PRODUCTION_ATOMICITY | Serializable txn; all materials − and output + or none |
| PRODUCTION_CONCURRENCY | Product reservation + serializable |
| MATERIAL_LOT_POLICY | FEFO via `ConsumeFefoAsync` |
| OUTPUT_LOT_POLICY | `ReceiveAsync` when TracksExpiration (expiry required) |
| PRODUCTION_COST_SCOPE | MATERIAL_ONLY |
| PRODUCTION_COST_SOURCE | Last acquisition UnitCost (Opening/PO/DirectBuy/ProductionOutput) |
| PRODUCTION_COST_STATUS_MODEL | Complete / Partial / Unavailable |
| OUTPUT_UNIT_COST_MODEL | TotalMaterialCost / OutputBaseQuantity when Complete |
| PRODUCTION_OUTPUT_COST_REUSED_BY_INVENTORY | YES (`GetLatestAcquisitionUnitCostAsync` includes ProductionOutput) |
| PRODUCTION_PROFIT_INTEGRATION | DEFERRED (no sale COGS engine) |
| LOW_STOCK_INTEGRATION | PASS (uses existing OnHand) |
| PRODUCTION_CAPACITY_ESTIMATE | DEFERRED |
| PRODUCTION_CORRECTION_MODEL | REVERSAL (void) |
| PRODUCTION_VOID_SAFETY | Block if attributable output stock insufficient |
| PRODUCTION_PERMISSION_MODEL | ManageInventory / ViewInventory |
| PRODUCTION_OFFLINE_MODE | ONLINE_ONLY |
| STOCK_USE_INTEGRATION | SEPARATE |
| SALE_INTEGRATION | Produced stock sold normally; materials not re-consumed |
| PURCHASING_INTEGRATION | Unchanged |
| WASTE_LOSS | DEFERRED |

## User paths

- `/inventory/production` — home
- `/inventory/production/setups` — list/create/edit/detail
- `/inventory/production/produce` — produce flow
- `/inventory/production/runs` — history/detail/void

API: `/api/v1/pos/inventory/production/definitions|runs`

## Migration

`20260829180000_AddPosProduction` — definitions, components, runs, materials, sequences; expands stock_movements check constraints; production unique index (DB-only before transfer, EF convention).

`MIGRATION_APPLIED_LOCAL=YES`

## Validation (this package)

| Check | Result |
|--------|--------|
| Backend Production + ProductBusinessUsage + StockUse | 46 passed |
| React production-labels + business-usage + i18n parity | 19 passed |
| React typecheck / lint / build | PASS (lint warnings only; PWA precache limit raised to 4 MiB) |
| POS API `/health` | Healthy |
| EF pending model changes | None |
| Conflict markers | 0 |

## Explicit non-goals

- Labor/overhead costing
- Waste/loss/spoilage
- Offline mutation queue
- Sale COGS / gross-profit engine
- Capacity estimate UI
