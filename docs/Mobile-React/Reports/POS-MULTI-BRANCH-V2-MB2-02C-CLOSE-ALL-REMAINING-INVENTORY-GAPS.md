# POS-MULTI-BRANCH-V2 MB2-02C — Close All Remaining Inventory Gaps

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02C  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED_NO_INVENTORY_P2
**Start SHA:** `91ee64b9dc53e2a24e8dd028d2541a567832338d`
**H1 closure SHA:** see `POS-MULTI-BRANCH-V2-MB2-02C-H1-DEDICATED-SECURITY-CONCURRENCY-AND-E2E-PROOF-CLOSURE.md`

---

## Scope delivered

### Read-only physical audit

- `IInventoryPhysicalAudit` / `InventoryPhysicalAuditService`
- `GET /api/v1/pos/inventory/physical-audit`
- Verifies org OnHand vs branch sum, org Reserved vs branch sum, org Reserved vs active docs, negative/over-reserved, expiration lot sum vs org OnHand (when tracked)
- Never mutates inventory state

### Organization inventory aggregate

- `OrganizationInventoryQuery` + `GET /api/v1/pos/inventory/{productId}/organization-summary`
- Returns organization OnHand / Reserved / Available and per-branch breakdown (single batched balance query)
- React `InventoryDetailPage` shows organization totals and branch breakdown via `getOrganizationInventorySummary`

### Branch authority hardening

- `InventoryBranchBodyResolver`: workspace header branch wins; forged body `BranchId` → `403 pos.inventory.branch_authority_mismatch`
- Applied to Stock Use, Waste/Loss, Production Run create endpoints

### CustomerOrder lot consume

- `CustomerOrderStockService.ConsumeOnCompleteAsync` calls `ConsumeFefoAsync` for expiration-tracked products (mirrors sale path)

### DTO / movement provenance

- `PosStockMovementDto.BranchId` exposed on API + React client
- `PosInventoryReconciliationDto` extended with organization reserved/available and branch breakdown

### Integration tests (`BranchInventory02CClosureIntegrationTests`)

| Test | Purpose |
|------|---------|
| Physical_audit_clean_after_balanced_branches | Audit read-only clean state |
| Organization_summary_returns_branch_breakdown | Org summary API |
| BWRITE_SALE_01 / 02 | Remote sale isolation |
| BWRITE_SEC_01 | Forged body branch rejected |
| BWRITE_CONC_01 | Dual-client concurrent deduct (no oversell) |
| BWRITE_CONC_05 / 06 | Dual DbContext reservation vs sale / dual reserve |
| CO_E2E_01 | CustomerOrder consume after restart (once) |
| MICA_02C | Reservation release + consume + audit clean |

---

## Protected baseline (unchanged)

MB2-02A / 02B / H1 / H2 / H3 reservation projection and write authority preserved.  
H1/H2/H3 migrations not edited. `MIGRATION_SHA=N/A`.

---

## Lot model (confirmed)

Lots are **optional** — expiration-tracked products only. No global `branch OnHand == SUM(lots)` invariant unless product tracks expiration.

---

## Cross-suite physical write coverage (pre-existing + 02C)

| Scenario | Primary proof |
|----------|----------------|
| Opening / adjust / count / DP | `BranchInventoryWriteAuthorityIntegrationTests` |
| Stock use / waste / production | `PosStockUseApiTests`, `PosWasteLossApiTests`, `PosProductionApiTests` |
| Transfer dispatch/receive | `PosInventoryTransferApiTests` |
| Lot branch FEFO | `PosInventoryLotApiTests` |
| Returns | `PosSaleReturnApiTests` |
| Payment concurrency | `P29Wp13PaymentConcurrencyTests` |

---

## Explicit exclusions / deferred labels

- ~~Dedicated `BWRITE-SEC-03` … `SEC-08` staff/ACL matrix~~ → closed in **MB2-02C-H1**
- ~~Dedicated `BWRITE-CONC-02` … `CONC-04` HTTP labels~~ → closed in **MB2-02C-H1**
- ~~Full API-driven Mica store scripted E2E~~ → closed in **MB2-02C-H1** (`MICA_FULL_API_E2E`)
- MB2-02D / MB2-03+ not started

---

## Validation evidence

- PostgreSQL: 10/10 `BranchInventory02CClosure` + 37/37 H3/H2/BWRITE regression filter passed
- React: typecheck, lint, vitest (inventory detail + stock count), build passed
- Release build: POS Domain/Application/Infrastructure/Api + IntegrationTests + UnitTests passed (full slnx blocked by missing Android SDK on agent — Maui TFM)

---

## Next

**MB2-02D** — final inventory closure
