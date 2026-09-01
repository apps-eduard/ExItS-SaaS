# Branch Inventory Authority

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** OWNER_APPROVED (MB2-00A) — TARGET_LOCKED
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-02 (02A read authority delivered; 02B writes deferred)

### MB2-02A read authority (delivered)

- Normal workspace inventory APIs require selected branch (`X-Pos-Branch-Id` / workspace scope).
- `onHandQuantity` = branch on-hand via `BranchStockResolver` (no org aggregate mislabel).
- `InventoryBranchReorderSetting` per `(OrganizationId, BranchId, ProductId)`; primary legacy reorder fallback from org account only.
- Low-stock and reorder-suggestion pagination filters branch membership in SQL before `Count`/`Skip`/`Take`.
- Reconciliation endpoint returns explicit org aggregate fields (not branch `onHandQuantity`).

**Report:** [POS-MULTI-BRANCH-V2-MB2-02A-BRANCH-INVENTORY-READ-AUTHORITY.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-02A-BRANCH-INVENTORY-READ-AUTHORITY.md)

**HARD STOP** before MB2-02B write-path hardening unless explicitly authorized.

### MB2-02A-H1 primary branch legacy stock isolation (delivered)

- Operational branch access remains ACL-filtered via Platform `ListBranches` / `CanAccessBranchAsync`.
- Structural primary branch id resolves via `GET /api/v1/platform/organizations/{orgId}/primary-branch` (org member, not assignment-filtered).
- `BranchStockResolver` fails closed when primary is unknown (`primaryBranchId == null` → 0, never org unallocated).
- Unallocated legacy stock belongs only to the **known** structural Primary branch.

**Report:** [POS-MULTI-BRANCH-V2-MB2-02A-H1-PRIMARY-BRANCH-LEGACY-STOCK-ISOLATION.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-02A-H1-PRIMARY-BRANCH-LEGACY-STOCK-ISOLATION.md)

---
**Owner review:** [POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md](../../Reports/POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md)

---

## 1. CURRENT_PROVEN (Model A)

| Store | Role |
|-------|------|
| `InventoryAccount` | Org/product projection: OnHand, Reserved, Available; unique `(OrganizationId, ProductId)` |
| `InventoryBranchBalance` | Branch overlay PK `(OrganizationId, BranchId, ProductId)` |
| `BranchStockResolver` | Explicit branch row → else unallocated attributed to **primary**; non-primary missing → 0 |

**Writes that update branch overlay today (examples):** adjustments with branchId, Stock Use / Waste / Production paths using `BranchBalanceMutation`, transfers, sale/customer-order **reserve** with branch.

**Writes that typically do NOT write branch overlay today:** opening stock, direct purchase receipt, PO/GRN receive (org account + lots often `branchId: null`).

**Inventory list / detail UI:** commonly surfaces **org** `InventoryAccount.OnHandQuantity` — GAP vs “selected branch stock.”

Pilot documentation (`POS-PILOT-STOCK-USE-BRANCH-BALANCE-FIX-01` family) records Model A as intentional interim accountability, not final multi-branch display contract.

---

## 2. TARGET invariant — LOCKED

```
NORMAL BRANCH OPERATION MUST USE BRANCH INVENTORY.
```

Example:

| | Main | Remote | Org aggregate |
|--|------|--------|---------------|
| Coke | 100 | 25 | 125 |

- Inventory screen Main → **100**
- Inventory screen Remote → **25**
- Never Main screen = 125 or Remote = 125 as “branch stock”

`InventoryAccount` remains aggregate / control / reconciliation authority — not the selected-branch display quantity.

### OD-04 = CLOSED — BRANCH_INVENTORY_BY_DEFAULT

Normal workspace inventory APIs must resolve **organization + selected branch** and return branch on-hand as:

`onHandQuantity`

Do **not** return organization aggregate under the same field while the user is operating a branch.

Organization aggregate may be exposed through:

- an explicit organization summary endpoint, **or**
- an unmistakably named field such as `organizationOnHandQuantity` on authorized management surfaces

Never overload one field with two meanings.

Main=100 / Remote=25 must return: Main workspace → 100; Remote workspace → 25; **not** 125 for both.

---

## 3. Operation matrix (CURRENT → TARGET)

| Operation | CURRENT READ | CURRENT WRITE | TARGET SCOPE | BranchId source | Org aggregate | Branch balance | GAP | PKG |
|-----------|--------------|---------------|--------------|-----------------|---------------|----------------|-----|-----|
| Inventory list | Org account | — | Selected branch via resolver | Workspace / header | Optional summary | Display | List uses org | MB2-02 |
| Inventory detail | Org (+ lots) | — | Branch stock + lots | Same | Aggregate panel optional | Display | Same | MB2-02 |
| Opening stock | — | Org account | Org + **branch balance** | Acting branch | +qty | +qty | No branch write | MB2-02 |
| Enable/disable tracking | Org product/account | Org | Org master + branch visibility rules | — | Policy | N/A | Align assortment | MB2-01/02 |
| Adjustment | Org | Org + overlay if branch | Branch-required for branch ops | Acting branch | Δ | Δ | Enforce branch | MB2-02 |
| Stock count | Session | Overlay paths | Branch session | Acting | Reconcile | Apply | Verify | MB2-02 |
| Reorder level/qty | Org account fields | Org | **Branch-specific** settings | Branch | — | Config row | Evolve | MB2-02 |
| Direct purchase receipt | — | Org; lots often null branch | Org + branch overlay | Acting / receive branch | + | + | No branch write | MB2-02 |
| PO/GRN receipt | — | Org | Org + branch | Receive branch | + | + | Same | MB2-02 |
| Purchase reversal | Org movements | Org | Branch-correct reverse | Original branch | Δ | Δ | Audit paths | MB2-02 |
| Sale | Resolver avail | Reserve overlay | Branch of sale | Sale.BranchId / header | Reserve/consume | At reserve | OK-ish | MB2-02/06 |
| Customer order reserve | FulfillmentBranch | Overlay | Fulfillment branch | Order.FulfillmentBranchId | Same | Same | OK | — |
| Return | Mixed | Mixed | Sale’s branch | Sale.BranchId | Δ | Δ | Harden | MB2-02 |
| Stock Use / Waste / Loss | — | Org + overlay | Branch | Acting | Δ | Δ | Mostly OK | MB2-02 |
| Production in/out | — | Org + overlay | Branch | Acting | Δ | Δ | Verify | MB2-02 |
| Transfer dispatch/receive | — | Org + both branches | Source/dest | Transfer branches | Net 0 (lifecycle) | Move | Proven path | — |
| Transfer cancel | — | Reverse | Same | — | — | — | — | — |
| Lots / expiry | Optional BranchId | Mixed | Lot at physical branch | Location | — | Lot qty | Null-branch lots | MB2-02 |
| Reconciliation | Org vs sum branches | Tools | Explicit unallocated + primary rules | — | Truth | Overlay | Document ops | MB2-02 |

---

## 4. Reorder configuration — TARGET

**CURRENT:** `ReorderLevel` / `ReorderQuantity` on org `InventoryAccount`.

**TARGET:** Branch-specific thresholds (demand differs by location).

**Migration:** Backfill initial branch values from org fields for existing branches; then treat org fields as optional aggregate defaults or deprecate after cutover (MB2-02 decision detail).

---

## 5. Lots / expiry — TARGET

Physical lots are branch/location-specific. Main lot must not appear as Remote sellable stock. Transfers keep existing expiration-aware lot snapshot behavior. Remaining organization-scoped / null-branch lot paths are **GAP** for MB2-02.

---

## 6. Starting stock vs transfer — TARGET_LOCKED

| Mechanism | Meaning |
|-----------|---------|
| **Transfer** | Move accountable quantity Main→Remote; Main decreases, Remote increases; org total reconciled per transfer lifecycle |
| **Opening stock** | New physical beginning inventory; updates **aggregate + branch** consistently |
| **Never** | Copy Main quantity into new branch on create |

Default new branch: **ZERO**.

---

## 7. Acceptance IDs

| ID | Expectation |
|----|-------------|
| STOCK-01 | Main 100 / Remote 25; each inventory page correct |
| STOCK-02 | Opening stock credits selected branch overlay |
| STOCK-03 | DP/GRN receipt credits selected branch overlay |
| STOCK-04 | Transfer 20 Main→Remote yields 80/20 without cloning |
| STOCK-05 | Org aggregate = sum of branch + unallocated rules documented |

---

## 8. Conceptual data additions (MB2-02 proposals)

- Branch reorder settings: `(OrganizationId, BranchId, ProductId)` → ReorderLevel, ReorderQuantity
- Hardening: require `BranchId` on receive/opening paths writing balances
- No duplicate product rows
