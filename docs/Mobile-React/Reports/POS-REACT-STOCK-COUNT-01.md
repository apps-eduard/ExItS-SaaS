# POS-REACT-STOCK-COUNT-01

**Task:** POS-REACT-STOCK-COUNT-01  
**Branch:** `feat/organization`  
**Start SHA:** `5e3ce081f24e2ffa37b9560785d6164b02a5d676`

## Audit summary

| Field | Value |
| --- | --- |
| EXISTING_STOCK_COUNT_BACKEND | YES — domain, use cases, endpoints, DTOs, variance movements already present |
| EXISTING_STOCK_COUNT_REACT_UI | NONE (before this package) |
| EXISTING_STOCK_COUNT_API_CLIENT | NONE in React (Maui/ApiClient existed); React client added |
| EXISTING_STOCK_COUNT_LIFECYCLE | Draft → InProgress → Completed; Draft/InProgress → Cancelled |
| STOCK_COUNT_SCOPE | ORGANIZATION |
| STOCK_COUNT_MULTI_BRANCH_SAFETY | DOCUMENTED — org-level `InventoryAccount.OnHand`; UI states org-level scope; no branch selector invented |
| BACKEND_CHANGE_REQUIRED | NO |
| MIGRATION_REQUIRED | NO |

## Branch / multi-branch

Stock Count is **organization-scoped** (`StockCount.OrganizationId` only; no `BranchId`). Completion applies variance deltas to org-level inventory accounts. The React UI labels this clearly (`stockCount.orgScopeNote`) and does not imply branch-shelf counting. Branch-specific redesign remains a separate backend package.

## React delivery

### Routes

- `/inventory/stock-counts`
- `/inventory/stock-counts/new`
- `/inventory/stock-counts/:stockCountId`

### Navigation

Inventory toolbar chip **Stock Count** (alongside Expiring, Stock Use, Waste/Loss, Production). Not added to mobile bottom nav.

### Flows

| Flow | Behavior |
| --- | --- |
| DRAFT_FLOW | Create title/date/notes + tracked products; save draft; edit meta; cancel |
| START_FLOW | Confirm → `POST .../start` (server snapshots OnHand + assigns count number) |
| IN_PROGRESS_FLOW | Enter physical counts; client preview variance from **SystemOnHandSnapshot**; save progress; line filters |
| COMPLETE_FLOW | Review summary → confirm → save lines → `POST .../complete` (server posts variance movements) |
| CANCEL_FLOW | Confirm → `POST .../cancel`; history preserved; no inventory adjustment |

### Semantics

| Field | Value |
| --- | --- |
| SYSTEM_SNAPSHOT_SOURCE | `SystemOnHandSnapshot` from stock-count detail (never live OnHand) |
| COUNTED_QUANTITY_MODEL | `>= 0`; empty = not counted; `0` = explicit zero; fractional allowed |
| VARIANCE_MODEL | Counted − System snapshot; server variance authoritative after save |
| COUNT_ALL_PRODUCTS | IMPLEMENTED — paged `tracked=true` inventory list, capped at domain `MaxLineCount` (500) |
| STOCK_COUNT_BARCODE_FLOW | DEFERRED — line DTO has no barcode/SKU; no contract expansion |
| STOCK_COUNT_PERMISSION_MODEL | ViewInventory list/detail; ManageInventory mutations (server authoritative) |
| STOCK_COUNT_OFFLINE_MODE | ONLINE_ONLY |

### Concurrent mutation policy

**IN_PROGRESS_CONCURRENT_MUTATION_POLICY:** Concurrent inventory mutations during InProgress are allowed by the current domain. UI continues to display the historical `SystemOnHandSnapshot`. On complete, backend applies **variance delta** (`Counted − Snapshot`) to **current** OnHand (not an absolute set-to-counted). Insufficient stock on decrease surfaces as a local API error.

## Explicit non-goals (honored)

No Transfer UI, no branch report redesign, no Waste/Loss reclassification of variance, no offline queue, no new Stock Count tables/lifecycle/movements, no migration.

## Validation evidence

- React typecheck: PASS  
- React lint: 0 errors (pre-existing warnings elsewhere)  
- React build: PASS  
- React targeted tests: stock-count labels, client schema, pages, i18n parity — PASS  
- Backend unit filter `StockCount|AdvancedInventory`: 21 passed  
- Conflict markers: 0  

## Next

`POS-REACT-INVENTORY-TRANSFER-01`
