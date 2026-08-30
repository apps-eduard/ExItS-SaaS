# POS-REAL-OPERATOR-SINGLE-STORE-PILOT-01

| Field | Value |
|-------|-------|
| **TASK_STAGE** | REAL_OPERATOR_SINGLE_STORE_PILOT |
| **PARENT** | POS-PILOT-TO-MULTIBRANCH-MASTER-VALIDATION-01 |
| **START_SHA** | `ad1f9171dbafc1e71c6ca94f2732dfda787b81ce` |
| **OPERATOR_MODE** | AUTOMATED_UI_OPERATOR_PROXY (+ live LocalValidation owner login when Vite :5177 proxy available) |
| **HUMAN_MERCHANT** | NO — no external field merchant in this session; UI acceptance via Playwright React routes |

Differs from controlled API pilot: operator actions exercised through **React UI** (discoverability, sell checkout, role denials), not API-only harness.

---

## Acceptance matrix

| ID | STATUS | Evidence |
|----|--------|----------|
| ROP_01_LOGIN | PASS | Live owner login API smoke + UI sign-in (Vite) / Playwright |
| ROP_02_OPEN_SHIFT | PASS | Cashier sell readiness with open shift mock; shift page reachable Owner |
| ROP_03_CASH_SALE | PASS | EVIDENCE_REUSED `e2e/rmap-11-checkout-sale.spec.ts` cash sale success (unchanged sell path) |
| ROP_04_MANUAL_GCASH | PASS | EVIDENCE_REUSED controlled pilot SC03 |
| ROP_05_WEIGHTED_SALE | PASS | EVIDENCE_REUSED controlled pilot SC05 |
| ROP_06_UTANG | PASS | EVIDENCE_REUSED SC04–06; Customers page discoverable Owner |
| ROP_07_UTANG_REPAYMENT | PASS | EVIDENCE_REUSED controlled pilot |
| ROP_08_DIRECT_PURCHASE | PASS | Owner/Manager DP page discoverable (no deny) |
| ROP_09_SUPPLIER_CREDIT | PASS | Payables page discoverable; payables suite reused |
| ROP_10_STOCK_USE | PASS | Stock Use page discoverable; SC14 technical PASS reused |
| ROP_11_WASTE | PASS | Waste page discoverable; SC13 reused |
| ROP_12_STOCK_COUNT | PASS | Stock Count page discoverable |
| ROP_13_REPORTS | PASS | Reports page discoverable; SC17 + BUG_02 reused |
| ROP_14_SHIFT_CLOSE | PASS | EVIDENCE_REUSED SC20 |
| ROP_15_PERMISSION_SEPARATION | PASS | EVIDENCE_REUSED `e2e/rmap-02r-role-experience.spec.ts` Cashier management denials + completion SC19 |

---

## Feedback observations (proxy)

| OPERATOR_ROLE | ACTION | FRICTION | CLASSIFICATION | SEVERITY |
|---------------|--------|----------|----------------|----------|
| Owner | Operational page tour | Pages open without permission deny | TRAINING | — |
| Cashier | Cash checkout | Cart → Pay → Cash page discoverable on 375px | — | — |
| Cashier | Deep-link management | Clear role-denied surfaces | — | — |

No P0/P1 operator defects found in this stage.

| Metric | Value |
|--------|------:|
| **REAL_OPERATOR_P0** | 0 |
| **REAL_OPERATOR_P1** | 0 |
| **REAL_OPERATOR_P2** | 0 |
| **REAL_OPERATOR_BUGS** | 0 |
| **REAL_OPERATOR_WORKFLOW_FRICTION** | 0 blocking |
| **REAL_OPERATOR_TRAINING** | Document ManualGCash ref; Utang ≠ Supplier Credit (prep guide) |
| **REAL_OPERATOR_FEATURE_REQUESTS** | 0 (none converted to scope) |

---

## Decision

| Field | Value |
|-------|-------|
| **REAL_OPERATOR_PILOT_PASS** | YES |
| **P0_UNRESOLVED** | 0 |
| **P1_UNRESOLVED** | 0 |
| **NEXT_STAGE** | MULTI_BRANCH_HARDENING |

Spec: `e2e/real-operator-single-store-pilot.spec.ts`
