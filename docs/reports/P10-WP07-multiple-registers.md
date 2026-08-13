# P10-WP07 — Multiple Registers

Phase marker: `P10-WP07-multiple-registers`

## Status

Implemented. Register is an organization-owned logical POS sales station. Cash authority remains on `CashierShift`. Exact next package: **P10-WP08 — Phase 10 Closeout** (not started). Phase 11 remains unstarted.

## Register definition

A Register is a named sales station inside one organization (for example Main Counter, Pharmacy Counter). It is not a branch, warehouse, cash drawer, printer, payment terminal, device identity, Windows/Android device registration, or accounting cash account.

## Lifecycle

Statuses: **Active** ↔ **Inactive** only. Created Active. No Draft/Suspended/Locked/Retired/Deleted/Decommissioned. Soft deactivate only; reactivation reuses the same identity. Organization ownership and `RegisterCode` are immutable. Name and Description are editable. Deactivation is blocked while an Open shift exists. Inactive registers cannot accept new shifts or sales. Historical operational rows remain linked after deactivation.

## Code rules

Server-generated organization-scoped `REG-NNNNNN` via `pos.register_code_sequences`. Unique within organization (case-insensitive). Not a primary key and not a fiscal document number. Normalized Name uniqueness applies across Active and Inactive rows in the organization.

## Shift relationship

Every new `CashierShift` requires exactly one Active Register. `RegisterId` is immutable after open. Enforced uniqueness:

1. One Open shift per organization and actor
2. One Open shift per organization and Register

Shift open requires trusted actor, same organization, Active Register, shift authorization, no Open shift for actor, and no Open shift for Register. Selection is explicit from `GET /api/v1/pos/registers/available-for-shift` and validated server-side.

## Sale and return linkage

New sales inherit `RegisterId` from the authorized Open shift (server-derived). Clients cannot choose a different sale Register. Voided sales keep the original Register. Returns set `SourceRegisterId` from the sale and `RefundRegisterId` from the cash-refund Open shift when applicable. ManualGCash/Utang retain source-sale Register context. Linkage is immutable.

## Cash-authority boundary

Expected cash remains entirely owned by `CashierShift`:

`ExpectedCash = OpeningCash + NetCashSales + CashIn − CashOut − CashRefunds`

Registers do not store opening/expected/closing cash, variance, or drawer balances. Register cash reports aggregate completed shift records only.

## Authorization

Grants: `store-registers-view`, `store-registers-manage` (reuse P10-WP06 role model).

| Role | View | Manage | Open shift on Active |
|---|---|---|---|
| Owner / Admin / StoreManager | Yes | Yes | Yes |
| Cashier | Active registers needed for own shift | No | Own shift only |
| InventoryStaff | Read-only context | No | No (unless separately shift-authorized) |
| ReportingUser | Yes + reports | No | No |

Commercial-state and entitlement checks remain mandatory. No role bypasses another check.

## Persistence and migration

Database `ExItS_PinoyBusinessPOS`, schema `pos`, migration `AddPosRegisters` (`20260731073815_AddPosRegisters`) after `AddPosOperationalRoles`.

- `pos.registers`, `pos.register_code_sequences`
- nullable `register_id` on `cashier_shifts` and `sales` (legacy null allowed)
- `source_register_id` / `refund_register_id` on `sale_returns`
- filtered unique Open shift per Register; existing Open shift per actor retained
- FK Restrict; UTC timestamps; PostgreSQL `xmin` concurrency

## API

- `GET/POST /api/v1/pos/registers`
- `GET/PUT /api/v1/pos/registers/{registerId}`
- `POST .../activate`, `POST .../deactivate`
- `GET .../activity`
- `GET .../available-for-shift`
- Shift open requires `RegisterId`; shift DTOs include Register summary

## MAUI

Routes: `/registers`, `/registers/new`, `/registers/{id}`, `/registers/{id}/edit`. Register picker on `/shifts/open`. Register shown on shift detail. Online-only management. EN + fil-PH. Themes unchanged.

## Online/offline

Register management and shift opening are online-only. No offline Register create/edit/activate/deactivate/queue. Offline sales may continue only under existing continuity rules tied to a previously server-confirmed Open shift and Register; sync must not remap Register.

## Concurrency and idempotency

Normalized-name uniqueness, code-sequence advisory locks, Open-shift conflicts (actor and Register), deactivate-vs-open races, optimistic concurrency on edit, and idempotent create/activate/deactivate via established POS idempotency headers.

## Reporting

P10-WP06 operational reports remain the authority; Register filters/projections are authorized additions using `store-reports-view` only (no new Register-report grants). Values derive from sales, returns, and shifts — no second cash authority, profit, COGS, tax, or accounting cash.

## Legacy compatibility

Pre-migration shifts/sales/returns may have null RegisterIds. They are legacy unassigned records; no fabricated backfill.

## Explicit exclusions

Warehouses, inventory-by-register, physical drawers, denomination counting, device/printer assignment, licensing/billing, bank deposits/cash drops, accounting journals, tax/fiscal devices, manager approvals, production authentication, Windows MAUI, offline Register management, Phase 11, P10-WP08.

A Register is still not a branch. Intra-organization branch inventory transfers were added later on inventory (Platform `OrganizationBranch` GUIDs), not as inventory-by-register. See [pos-branch-inventory-transfers.md](../engineering/pos-branch-inventory-transfers.md).

## Tests

Focused Register API lifecycle/uniqueness/open-shift conflict/cross-org concealment tests; migration apply/rollback/re-apply; updated shift/sale domain and integration fixtures for RegisterId. Baseline before WP07: 1138 passed / 0 failed / 0 skipped. Full suite after WP07: **1142 / 0 / 0** (`dotnet test ExItS.slnx -c Release`).

## Android evidence

MAUI `net10.0-android` Release build succeeded. No device/emulator was available for interactive validation — retain **R-109**; do not claim interactive Register UI validation.

## Remaining risks

- Legacy null Register rows need careful reporting UX (unassigned bucket)
- Concurrent deactivate vs shift open relies on DB uniqueness + application checks
- Offline close conflicts remain honest unresolved per existing policy

## Exact next package

**P10-WP08 — Phase 10 Closeout**
