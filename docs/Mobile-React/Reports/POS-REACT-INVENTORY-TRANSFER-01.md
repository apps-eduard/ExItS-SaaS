# POS-REACT-INVENTORY-TRANSFER-01

**Task:** POS-REACT-INVENTORY-TRANSFER-01  
**Branch:** `feat/organization`  
**Start SHA:** `bf0fd9de7f0457aaf382ef8d5fc754c9e4b5ab74`

## Audit summary

| Field | Value |
| --- | --- |
| EXISTING_TRANSFER_BACKEND | YES — domain, use cases, endpoints, DTOs, branch balances, lots, alerts |
| EXISTING_TRANSFER_REACT_UI | NONE (before this package); Maui/Web existed |
| EXISTING_TRANSFER_REACT_CLIENT | IMPLEMENTED (`pos-inventory-transfer-client.ts`) |
| TRANSFER_LIFECYCLE | Draft → InTransit → Received \| PartiallyReceived; Draft/InTransit → Cancelled |
| TRANSFER_BRANCH_MODEL | Same organization; Source ≠ Destination; acting branch required |
| TRANSFER_SOURCE_AUTHORITY | Create / Dispatch / Cancel require `actingBranch == SourceBranch` |
| TRANSFER_DESTINATION_AUTHORITY | Receive requires `actingBranch == DestinationBranch` |
| TRANSFER_LOT_MODEL | Expiration-tracked products require explicit `SourceLotId`; multi-lot = separate lines (product+lot unique) |
| TRANSFER_DISCREPANCY_MODEL | ShortShipment, Damaged, LostInTransit, WrongItem, Other |
| TRANSFER_RECEIVING_MODEL | One receive submission; ReceivedQty ∈ [0, SentQty]; destination gets actual received qty |
| TRANSFER_RECEIVE_FINALITY | FINAL — Received and PartiallyReceived reject further receive |
| OVER_RECEIPT_POLICY | REJECTED |
| ZERO_RECEIVED_POLICY | ALLOWED |
| TRANSFER_CANCEL_MODEL | Draft cancel (no stock); InTransit cancel restores source via TransferCancelRestore |
| TRANSFER_DRAFT_EDIT | NOT_SUPPORTED (no PUT API) |
| TRANSFER_UNIT_MODEL | Base product `UnitOfMeasure` only (no ProductUnit packages) |
| MULTI_LOT_TRANSFER_MODEL | Same product allowed on multiple lines when SourceLotId differs |
| SOURCE_BRANCH_AVAILABILITY_API | NONE (no React branch-balance read endpoint) |
| SOURCE_BRANCH_AVAILABILITY_UX | Do not claim branch OnHand; dispatch validates server-side |
| TRANSFER_COST_BASIS_BEHAVIOR | TransferOut/TransferIn do not set UnitCost; no revenue/COGS |
| TRANSFER_ALERT_UI | DEFERRED (incoming list/filter sufficient) |
| TRANSFER_PERMISSION_MODEL | ViewInventory list/detail; ManageInventory mutations |
| TRANSFER_OFFLINE_MODE | ONLINE_ONLY |
| TRANSFER_REACT_N_PLUS_ONE | PASS (paged list; one detail; branch names on DTO) |
| BACKEND_CHANGE_REQUIRED | NO |
| MIGRATION_REQUIRED | NO |

## React delivery

### Routes

- `/inventory/transfers`
- `/inventory/transfers/new`
- `/inventory/transfers/:transferId`

### Navigation

Inventory toolbar chip **Transfers** (with Stock Count, Stock Use, Waste/Loss, Production).

### Flows

| Flow | Behavior |
| --- | --- |
| Create | Source = current acting branch; destination picker excludes source; tracked products; lot required when tracks expiry; Save draft only |
| Dispatch | Source-only confirm → TransferOut |
| Receive | Destination-only; defaults Received=Sent; discrepancy when short; final |
| Cancel | Source-only; draft or in-transit restore |

## Validation

- React typecheck / lint / build (see completion evidence)
- Targeted React transfer tests
- Backend `InventoryTransfer*` unit tests
- Conflict markers: 0

## Next

`POS-REPORTS-BRANCH-SCOPING-01`
