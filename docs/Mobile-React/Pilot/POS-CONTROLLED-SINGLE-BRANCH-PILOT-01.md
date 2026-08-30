# POS-CONTROLLED-SINGLE-BRANCH-PILOT-01

Controlled single-branch Organization POS field validation against the verified pilot baseline.

| Field | Value |
|-------|-------|
| **TASK** | POS-CONTROLLED-SINGLE-BRANCH-PILOT-01 |
| **PILOT_BASELINE_SHA** | `54a25b0dd9e954270274c92e1a7314ea859f8f22` |
| **START_SHA** | `54a25b0dd9e954270274c92e1a7314ea859f8f22` |
| **PILOT_DATE** | 2026-08-30 |
| **PILOT_TYPE** | CONTROLLED |
| **PILOT_SCOPE** | SINGLE_BRANCH_SMALL_STORE |
| **CODE_CHANGES_DURING_PILOT** | NO (product code unchanged; local API harness only) |

Guides used (not rewritten):

- `docs/Mobile-React/Pilot/POS-ORGANIZATION-PILOT-GUIDE-01.md`
- `docs/Mobile-React/Pilot/POS-ORGANIZATION-PILOT-CHECKLIST-01.md`
- `docs/Mobile-React/Pilot/POS-PILOT-FEEDBACK-TEMPLATE-01.md`

---

## Environment

| Item | Value |
|------|-------|
| Branch | `feat/organization` |
| Platform API | `http://127.0.0.1:8091` Healthy |
| POS API | `http://127.0.0.1:8092` Healthy |
| React Vite | `http://127.0.0.1:5177` |
| Execution mode | Controlled demo org via Personal **start-business** + live POS API contracts (aligned with React clients) |

Unrelated local files left untouched (not staged): `tools/Start-PlatformApiOnly.ps1`, `tools/Start-PosApiOnly.ps1`.

---

## Pilot store

| Field | Value |
|-------|-------|
| **ORGANIZATION_USED** | Pilot Sari-Sari Store (`d3cfff47-016c-4670-b0cc-f96e89957888`) |
| **BRANCH_COUNT** | 1 (Main Branch `10ad42d9-02eb-4a34-b931-09f4c52934f3`) |
| **USER_ROLES** | Owner only (live). Cashier / InventoryStaff / ReportingUser invites **NOT_RUN** |
| **PRODUCT_COUNT** | 12 (unit, barcode, weighted, expiry-tracked, service/non-inventory-style) |
| **SUPPLIER_COUNT** | 3 |
| **CUSTOMER_COUNT** | 3 |

---

## Scenario results

For each executed scenario: ROLE / ACTION / EXPECTED vs ACTUAL summarized. Full harness log: local `.tmp-pilot-results.json` (not committed).

| Scenario | Status | Severity | Notes |
|----------|--------|----------|-------|
| **SCENARIO_01** Owner setup | **PASS** | | Org, 1 branch, 12 products, opening stock enable, suppliers, customers. Device re-register returned 400 on repeat (PARTIAL step; setup otherwise OK). |
| **SCENARIO_02** Cash sale | **PASS** | | Open shift → Cash sale qty 2 Lucky Me → total **30.00**, `SALE-20260830-000001`. |
| **SCENARIO_03** Manual GCash | **PASS** | | `paymentMethod=ManualGCash`, reference accepted; no gateway. |
| **SCENARIO_04** Weighted | **PASS** | | Revalidated **Premium Rice** 0.750 kg @ 120 → total **90.00**. (First harness pick used Onion @ 80 → 60; product math OK.) |
| **SCENARIO_05** Customer Utang | **PASS** | | Utang sale with customer completed. |
| **SCENARIO_06** Utang repayment | **PASS** | | Partial repayment amount 5 posted. |
| **SCENARIO_07** Direct purchase paid | **PASS** | | `DPR-20260830-000001` fully paid / source name. |
| **SCENARIO_08** DP supplier credit | **PASS** | | Total 5000, `paidAtReceipt=2000`, payable balance **3000**. |
| **SCENARIO_09** PO + receive | **PASS** | | Create → submit → receive goods receipt OK. |
| **SCENARIO_10** Supplier payment | **PASS** | | Partial 100 with `paymentMethod=Cash`; balance **2900**, status PartiallyPaid. |
| **SCENARIO_11** Receipt void/reversal | **PASS** | | Controlled DPR void succeeded (no later payments). |
| **SCENARIO_12** Stock count | **PASS** | | Create → start → complete with variance. |
| **SCENARIO_13** Waste/loss | **PASS** | | Damaged waste posted on product with prior branch-touching movements. |
| **SCENARIO_14** Stock use | **FAIL** | **P1** | See Real blockers — org on-hand shows qty; Stock Use returns `pos.inventory.insufficient_stock` / insufficient **branch** stock. |
| **SCENARIO_15** Expiry | **PASS** | | Fresh Milk tracksExpiration; lots GET returned coherent lot after enable+opening. |
| **SCENARIO_16** Production | **NOT_APPLICABLE** | | Sari-sari pilot; production not used. |
| **SCENARIO_17** Reports | **PASS** | | Sales, sales-by-payment, inventory-status, purchasing, supplier-payables, expenses, profitability, shifts OK. Canonical paths `/reports/utang` and `/reports/returns` return 200 (not `*-summary` aliases). |
| **SCENARIO_18** CSV export | **PASS** | | Sales + supplier-payables report payloads available for React client CSV. File open in Excel **NOT_RUN** in this session. |
| **SCENARIO_19** Permissions | **NOT_RUN** | P2 | Separate Cashier / Inventory / Reporting users not invited in this pass. |
| **SCENARIO_20** Shift close | **PASS** | | Shift closed successfully after sales. |
| **SCENARIO_21** Responsive UX | **NOT_RUN** | | Interactive 360/768/desktop viewport audit not executed. |
| **SCENARIO_22** Idempotent retry | **PASS** | | Same supplier-payment idempotency headers → second call HTTP 200, no double financial post observed. |

### Scenario status fields (for stamp)

```
SCENARIO_01_STATUS=PASS
SCENARIO_02_STATUS=PASS
SCENARIO_03_STATUS=PASS
SCENARIO_04_STATUS=PASS
SCENARIO_05_STATUS=PASS
SCENARIO_06_STATUS=PASS
SCENARIO_07_STATUS=PASS
SCENARIO_08_STATUS=PASS
SCENARIO_09_STATUS=PASS
SCENARIO_10_STATUS=PASS
SCENARIO_11_STATUS=PASS
SCENARIO_12_STATUS=PASS
SCENARIO_13_STATUS=PASS
SCENARIO_14_STATUS=FAIL
SCENARIO_15_STATUS=PASS
SCENARIO_16_STATUS=NOT_APPLICABLE
SCENARIO_17_STATUS=PASS
SCENARIO_18_STATUS=PASS
SCENARIO_19_STATUS=NOT_RUN
SCENARIO_20_STATUS=PASS
SCENARIO_21_STATUS=NOT_RUN
SCENARIO_22_STATUS=PASS
```

---

## Issue counts

| Metric | Count |
|--------|------:|
| **P0_COUNT** | 0 |
| **P1_COUNT** | 1 |
| **P2_COUNT** | 3 |
| **BUG_COUNT** | 1 |
| **WORKFLOW_FRICTION_COUNT** | 1 |
| **POLISH_COUNT** | 1 |
| **NEW_FEATURE_REQUEST_COUNT** | 0 |
| **TRAINING_ISSUE_COUNT** | 2 |

---

## Feedback entries

### FB-01 — Stock Use vs branch balance (BUG / P1)

| Field | Value |
|-------|-------|
| Date | 2026-08-30 |
| Organization | Pilot Sari-Sari Store |
| Branch | Main Branch |
| Role | Inventory/Owner |
| Screen | Stock Use |
| Action | POST `/api/v1/pos/inventory/stock-uses` for product with org on-hand 20 (opening stock and/or direct purchase only) |
| Expected | Stock decreases; not Sale COGS |
| Actual | HTTP 409 `pos.inventory.insufficient_stock` — “Insufficient branch stock for this movement.” Org inventory still shows on-hand. |
| Severity | P1 |
| Classification | **BUG** |
| Workaround | Prefer Waste/Loss or sales-touched SKUs where branch balance already exists; or receive via flows that update branch balances (investigate Opening/DP). |
| Frequency | Reproduced on Egg Tray (opening only) and Laundry Bar (after additional Direct Purchase). |
| Notes | Stock Use / Waste apply `InventoryBranchBalance` starting at **0** when missing. Opening stock + Direct Purchase appear not to seed branch balances while org `InventoryAccount` does — inventory UI can disagree with Stock Use. |

### FB-02 — Live multi-role gates not field-checked (TRAINING_ISSUE / P2)

| Field | Value |
|-------|-------|
| Classification | **TRAINING_ISSUE** |
| Notes | Owner-only session. Cashier deny-list and Reporting read-only not live-verified. Automated role matrix exists; still required for pilot sign-off. |

### FB-03 — Viewport audit skipped (POLISH / P2)

| Field | Value |
|-------|-------|
| Classification | **POLISH** |
| Notes | Sell / Inventory / DP / Reports / Shift not measured at ~360 / ~768 / desktop in this session. |

### FB-04 — Report path naming in harness (WORKFLOW_FRICTION / P2)

| Field | Value |
|-------|-------|
| Classification | **WORKFLOW_FRICTION** |
| Notes | Operators using the React app hit canonical routes (`/reports/utang`, `/reports/returns`). Aliases `utang-summary` / `returns-summary` 404 — document in training, not a UX dead-end in-app. |

---

## Operator feedback prompts (controlled session)

| Question | Answer (this pass) |
|----------|-------------------|
| WHAT WAS CONFUSING? | Stock Use failing while Inventory shows quantity (FB-01). |
| WHAT TOOK TOO MANY CLICKS? | Not measured in UI session (API-driven pass). |
| WHAT DID YOU EXPECT TO SEE? | Stock Use to consume the same on-hand shown on Inventory. |
| WHAT MANUAL/PAPER STEP DID YOU STILL NEED? | Manual GCash reference (by design). |
| WHAT FEATURE DID YOU NOT USE? | Production; Transfers (single-branch); live staff invites. |
| WHAT FEATURE FELT UNNECESSARY? | None identified. |

---

## Pass decision

| Field | Value |
|-------|-------|
| **PILOT_PASS** | **NO** |
| **REAL_BLOCKERS** | FB-01 Stock Use / branch-balance coherence (P1). Permissions live matrix **NOT_RUN**. |
| **TOP_OPERATOR_FEEDBACK** | Inventory screen quantity and Stock Use disagree after opening stock / direct purchase. |
| **TOP_WORKFLOW_FRICTION** | Need training on correct report names; Stock Use not usable until branch stock is coherent. |
| **CODE_CHANGES_DURING_PILOT** | NO |
| **NEXT** | POS-PILOT-FEEDBACK-POLISH-01 |
| **NEXT_WHY** | No P0 on core sell / Cash / Manual GCash / Utang / purchasing / supplier credit / shift. One P1 inventory coherence bug plus P2 gaps (permissions live, responsive). Fix/polish feedback before expansion — do **not** start device/offline, B2B checkout, real payments, FIFO, or GL. |

### Why not PILOT_PASS=YES

Pass criteria require coherent inventory and safe permissions. Stock Use failure with visible on-hand breaks coherence for a required inventory workflow; multi-role permissions were not live-validated.

Core **sell + cash + GCash + Utang + shift + purchasing + payables + reports** did pass on this baseline.

---

## Explicit exclusions (not in scope)

No implementation of: device/offline architecture, local SQLite, desktop helper, B2B checkout, real payment provider, FIFO, GL, multi-branch redesign.

---

## Git

Stage **only** this pilot results document. Do not stage tooling start scripts, harness, or `.tmp-pilot-*` artifacts.
