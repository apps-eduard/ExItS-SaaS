# POS Multi-Branch Commerce V2 — Implementation Plan

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Package map:** MB2-01 … MB2-07
**Architecture lock:** [multi-branch-commerce-v2.md](../Authoritative/POS/multi-branch-commerce-v2.md)
**MB2-00 status:** DOCUMENTED_READY_FOR_OWNER_REVIEW
**HARD STOP:** Do not start MB2-01 until owner review.

---

## Dependency graph

```
MB2-00 → MB2-01 → MB2-02 → MB2-03 → MB2-04 → MB2-05 → MB2-06 → MB2-07
```

---

## MB2-01 — Product Governance & Branch Assortment

**Scope:** CatalogProductScope, OriginBranchId, BranchProductAvailability, backfill existing→OrganizationStandard, barcode/SKU protections, org vs branch edit authority, promotion Local→Standard (same ProductId, one-way), API/React foundation, tests, migration.

**Must complete before:** branch setup wizard.

**Hard stops:** ProductId clone; demotion; Local multi-branch share; Platform Global Catalog confusion.

**Tests:** PRODUCT-01…06; architecture guards; migration apply/rollback plan.

**Migration:** Add scope + origin + availability; backfill Standard; no ProductId rewrite.

**MB2_01_READY:** YES (docs locked; needs owner review of OD-01).

---

## MB2-02 — Branch Inventory Authority Hardening

**Scope:** List/detail branch correctness; operation matrix (opening, adjust, count, reorder, receipts, lots, returns, production, waste, stock use, transfers); org aggregate reconciliation; migrations if required.

**Goal:** Selected branch never sees another branch’s stock as its own.

**Hard stops:** Fake stock copy; ignoring BranchStockResolver for display.

**Tests:** STOCK-01…05; receive/open write branch balance; transfer regression.

**MB2_02_READY:** YES after MB2-01 (assortment interactions); inventory can start after product availability exists for “not offered” edge cases — preferred sequence remains after MB2-01.

---

## MB2-03 — Branch Pricing & Effective Price Authority

**Scope:** Org default + branch overrides (base + unit); central resolver; Today's Prices branch awareness; Sell/checkout/customer ordering baselines; offline lease/cache keys; snapshots; tests/migration.

**Hard stops:** Auto-creating overrides for all products; client-authoritative price; rewriting historical sales.

**Tests:** PRICE-01…05; concurrency; offline WrongBranch.

**Migration:** No automatic override rows; semantics map existing SellingPrice → org default.

**MB2_03_READY:** YES after MB2-01 (product identity stable).

---

## MB2-04 — Customer & Supplier Branch Access

**Scope:** Access tables; privacy-safe search; branch transaction visibility; Utang privacy; backfill strategy; Owner governance; purchasing/customer integration.

**Hard stops:** Silent grant-all; cloning parties; React-only filtering.

**Tests:** PRIVACY-01…05; OD-02 resolution required before migration ship.

**MB2_04_READY:** CONDITIONAL — OD-02 must be closed or explicit Primary-only fallback accepted.

---

## MB2-05 — New Branch Guided Setup

**Scope:** Resumable wizard; template reference; products/prices/stock/customers/suppliers/staff/devices/fulfillment/review; setup progress.

**Consumes:** MB2-01…04. Does not duplicate domains.

**Hard stops:** Cloning stock/customers/devices; auto-promoting Local; fabricating fulfillment ready.

**Tests:** WIZARD-01…05; Remote North scenario.

**MB2_05_READY:** YES after MB2-01…04.

---

## MB2-06 — Cross-Surface + Offline Hardening

**Scope:** Sell, checkout, storefront, orders, returns, purchasing, offline, cache invalidation on branch switch, reports, auth, N+1/performance (bulk availability, effective price, party search, inventory summary).

**Hard stops:** Per-row N+1 wizard/catalog; stale branch caches.

**Tests:** Cross-surface isolation; offline; workspace switch.

**MB2_06_READY:** YES after MB2-05 (or parallel late hardening with care).

---

## MB2-07 — Multi-Branch V2 E2E Closure

**Scope:** Joe Store + Remote North scenarios; security/privacy/price/stock isolation; promotion; wizard; migration compatibility; responsive; offline/online; full regression; finalize authoritative CURRENT stamps.

**MB2_07_READY:** YES as terminal package.

---

## Bulk / performance requirements (design — no implement in MB2-00)

- Branch product availability summary (bulk)
- Effective price bulk resolution
- Branch customer/supplier access search
- Inventory branch summary

Avoid one-request-per-product wizard designs.

---

## Cache / query keys

Branch-specific data caches **must** include `organizationId` + `branchId`:

inventory, effective prices, storefront assortment, sell catalog slice, availability, customer access, supplier access.

Org-master caches may remain organization-scoped. Document stale risk on workspace switch (MB2-06).

---

## Migration safety (all packages)

- No ProductId rewrite
- No historical sale rewrite
- No customer/supplier duplication
- No fake stock creation
- No automatic branch price overrides for existing data
- Existing org price → organization default
- Existing products preserve observable behavior until assortment/price changes
- Branch inventory must reconcile
- Privacy/access migration explicit
- Rollback/forward compatibility per package

---

## Owner review gate

**NEXT after MB2-00:** `OWNER_REVIEW_BEFORE_MB2_01`

Do not implement production code until review accepts locked decisions and resolves or defers OPEN_DECISIONS.
