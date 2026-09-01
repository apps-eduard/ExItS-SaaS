# POS-MULTI-BRANCH-V2 MB2-02C-H1 — Dedicated Security, Concurrency, and E2E Proof Closure

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02C-H1  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED_NO_INVENTORY_P2  
**Start SHA:** `89b73c3766c249ff3bdf38d9214f943cb4218965`

---

## Scope delivered

### Dedicated integration proofs (`BranchInventory02CH1ProofIntegrationTests`)

| Proof | Scenario | Result |
|-------|----------|--------|
| BWRITE-SEC-03 | Mica A-only staff cannot mutate Main inventory | PASS |
| BWRITE-SEC-04 | Mica A-only staff cannot mutate Mica B inventory | PASS |
| BWRITE-SEC-05 | Inactive branch physical write rejected | PASS |
| BWRITE-SEC-06 | Foreign branch / cross-org product write rejected | PASS |
| BWRITE-SEC-07 | Return restores original sale branch (not workspace branch) | PASS |
| BWRITE-SEC-08 | Not-offered sale blocked; inventory adjust still allowed | PASS |
| BWRITE-CONC-02 | Concurrent sale vs transfer dispatch (real PostgreSQL) | PASS |
| BWRITE-CONC-03 | Concurrent sale vs waste (real PostgreSQL) | PASS |
| BWRITE-CONC-04 | Concurrent duplicate direct purchase idempotency | PASS |
| MICA_FULL_API_E2E | Full API Mica Store transfers/sales/reserve/consume | PASS |

### Test support

- `H1ProofOrganizationBranchDirectory` — caller-access-filtered branch lists for security proofs
- `H1ProofCustomerOrderBranchDirectory` — Mica A/B exposure for customer-order API in Testing

### Production fix (real defect)

- `DirectPurchaseReceiptUseCases`: on concurrent idempotency-key unique violation, replay existing receipt instead of 500 (CONC-04)

### Regression alignment (branch inventory model)

Updated stale assertions in transfer/stock-use/waste/production/sale-return tests for branch-scoped on-hand, org-summary totals, and document `BranchId` persistence.

---

## MICA full API E2E final totals

| Metric | Value |
|--------|-------|
| Org OnHand | 81 |
| Main | 70 |
| Mica A | 1 |
| Mica B | 10 |
| Branch sum | 81 |
| Org Reserved | 0 |
| Reservation audit | clean |
| Physical audit | clean |

---

## Protected baseline (unchanged)

MB2-02A / 02B / H1 / H2 / H3 reservation projection and write authority preserved.  
Completed migrations not edited.

---

## Explicit exclusions / deferred

- MB2-02D final inventory closure
- MB2-03 branch pricing
- MB2-04 customer/supplier branch ACL/privacy
- MB2-05 guided branch setup
- MB2-06 offline hardening
- MB2-07 final multi-branch E2E

Optional expiry lot assertion inside Mica E2E deferred — covered by existing `PosInventoryLotApiTests` and H1/H3 lot proofs.

---

## Validation evidence

- H1 suite: 10/10 `BranchInventory02CH1ProofIntegrationTests`
- Regression filter: 98/98 (02C + H3/H2 + write/read authority + lots + returns + stock use + waste + production + transfers + H1)
- H1 unit filter: 35/35
- Release build: POS Domain / Application / Infrastructure / Api / IntegrationTests / UnitTests passed
- React: unchanged (no production React files modified)

---

## Next

**MB2-02D** — final inventory closure
