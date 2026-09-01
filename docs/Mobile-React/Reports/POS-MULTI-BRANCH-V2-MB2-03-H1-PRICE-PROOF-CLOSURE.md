# POS-MULTI-BRANCH-V2 MB2-03-H1 — Price Proof Closure

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-03-H1  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Depends on:** MB2-03 COMPLETE_VALIDATED

---

## Closed proof gaps

| ID | Proof |
|----|-------|
| PRICE-H1-01 | Historical `SaleLine.UnitPrice` unchanged after branch override change; new sale uses updated effective price |
| PRICE-H1-02 | Foreign-org branch/product combinations rejected; no override rows created |
| PRICE-H1-03 | Cashier (no ManageCatalog) cannot create/update/remove branch pricing |
| PRICE-H1-04 | 10% sale discount applies to branch effective base (80 → 8 discount → 72 net) |
| PRICE-H1-05 | Full Mica Main/A/B lifecycle: initial sales, price changes, historical immutability, new sales, storefront, CustomerOrder |

---

## Tests

`BranchPricing03H1IntegrationTests` — **5/5 PASS**

Combined with MB2-03 suite: **22/22 PASS**

---

## Explicit exclusions (unchanged)

- Offline price cache-key invalidation → MB2-06
- Promotion custom-default/origin override interaction → later package

---

## Next

**MB2-04** — customer/supplier branch ACL + privacy
