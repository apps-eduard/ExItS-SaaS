# RMAP-07 — Inventory tracking + movements + opening stock

## Status

**COMPLETE**

## Baseline

starting SHA: `ae614cab6cc7ca43d3eff1d829d3840e7ba3606a` (post RMAP-05; RMAP-06/07 shipped together after RMAP-05)

## Contract review

| Area | Finding |
|------|---------|
| Default | Tracking **OFF**; untracked ≠ zero / out-of-stock |
| Enable | Optional opening quantity → OpeningStock movement when > 0 |
| Adjust | In/Out with reason; base quantity |
| Disable | Requires zero on-hand (and reserved) |
| Oversell | Server rejects insufficient stock when tracked |
| Lots/expiry | Excluded (RMAP-08) |
| Owner decision | NO |

## Implementation

- `/inventory` list shows **Not tracked** vs on-hand
- Detail: enable/opening, adjust, disable, movement history
- ManageInventory / ViewInventory UI gates (Owner/StoreManager; Cashier denied)

## Exclusions

- Lots, FEFO, expiry alerts (RMAP-08)
- Stock counts / transfers advanced UX

## Next

**HARD STOP.** Do not start RMAP-08.

## Validation closeout (RESUME 05 REVIEW REPAIR)

**Status:** COMPLETE — validation closeout complete

**Repair baseline:** `d4d81886a5c7159ab39f57d80cc31a1d61833bea`

**Shared implementation commit (process deviation — not rewritten):**
- `RMAP_06_07_SHARED_IMPLEMENTATION_COMMIT=YES`
- Implementation: `d3e4e3da32cbd562c6973bcad18480742ed9d64b`
- Shared docs: `4688709ab774f3cefdfa669fa8c5b4fe67641dbc`
- `HISTORY_REWRITE_USED=NO`

**Validation repair SHA:** `cb91145b0aa3140f7eb47c853998288aec40a66a`

### Application defects closed by validation

| Defect | Evidence | Fix |
|--------|----------|-----|
| Adjustment allowed empty reason | Backend requires reason; client invented none | Client requires reason (`inventory.reasonRequired`) |
| Movements hidden after disable | History must survive disable | Movements query/UI outside tracked-only branch |

### Backend contract

| Suite | Result |
|-------|--------|
| `PosInventoryApiTests` | Passed 7 / Failed 0 / Skipped 0 |
| `InventoryAccountDomainTests` | Passed 9 / Failed 0 / Skipped 0 |
| UOM shared-pool / ByWeight unit suite | Passed 31 / Failed 0 / Skipped 0 (same focused filter as RMAP-06 closeout) |

### React gates

| Gate | Result |
|------|--------|
| Vitest | 32 files / 116 tests passed |
| typecheck | PASS |
| lint | 0 errors (8 pre-existing fast-refresh warnings) |
| Prettier (touched) | PASS |
| build | PASS |

### Playwright (`e2e/rmap-07-inventory.spec.ts`)

Passed **8** / Failed **0**

Functional: untracked default; enable zero (no OpeningStock); enable opening; positive/negative adjust; reason required; disable denied when non-zero; disable at zero; history retained after disable; cashier denied; OrgAdmin alone denied.

Responsive matrix (list + detail):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS |
| 768×1024 | PASS |
| 1024×768 | PASS |
| 1440×900 | PASS |

### Flags

- `RMAP_07_PASS=YES`
- `RMAP_07_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_07_BACKEND_CONTRACT_REVALIDATED=YES`
- `RMAP_07_DISABLE_SEMANTICS_PROVEN=YES`
- `RMAP_07_NEGATIVE_ADJUSTMENT_PROVEN=YES`
- `RMAP_07_UOM_SHARED_POOL_PROVEN=YES` (unit suite; no parallel React inventory bucket)
- Lots/expiry still OUT OF SCOPE (RMAP-08)
- `RMAP_B03_DISCOUNT_STARTED=NO`
- `RMAP_08_STARTED=NO`

### Next

HARD STOP — send report to ChatGPT. Do not start RMAP-08 or RMAP-B03.
