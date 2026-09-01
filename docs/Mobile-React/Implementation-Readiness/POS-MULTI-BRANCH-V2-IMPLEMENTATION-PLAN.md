# POS Multi-Branch Commerce V2 — Implementation Plan

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Architecture lock:** [multi-branch-commerce-v2.md](../Authoritative/POS/multi-branch-commerce-v2.md)
**Owner review:** [POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md](../Reports/POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md)
**MB2-01A status:** COMPLETE_VALIDATED_FOUNDATION
**MB2-01B status:** COMPLETE_VALIDATED_AUTHORITY
**MB2-01B-H1 status:** COMPLETE_VALIDATED
**MB2-01C status:** COMPLETE_VALIDATED_UX
**MB2-01C-H1 status:** COMPLETE_VALIDATED_PRODUCT_IDENTITY
**MB2-01D status:** COMPLETE_VALIDATED_BASELINE
**MB2_01_STATUS:** COMPLETE_VALIDATED_BASELINE
**HARD STOP:** Do not start MB2-02 until explicitly authorized as a separate task.
**01D report:** [POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md)
**01A report:** [POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md)
**01B report:** [POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY.md)
**01B-H1 report:** [POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01.md)
**01C report:** [POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX.md)
**01C-H1 report:** [POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md)

---

## Dependency graph

```
MB2-00 → MB2-00A (owner review closure)
  ↓
MB2-01A Product Governance Data Foundation
  ↓
MB2-01B Product Authority & Availability Enforcement
  ↓
MB2-01C Product Governance React UX
  ↓
MB2-01C-H1 Strong Product Duplicate Identity
  ↓
MB2-01D Product Governance Validation Closure ★ COMPLETE_VALIDATED_BASELINE
  ↓
MB2-02 → MB2-03 → MB2-04 → MB2-05 → MB2-06 → MB2-07
```

---

## MB2-01A — Product Governance Data Foundation

**Scope:**

- `CatalogProductScope`: OrganizationStandard | BranchLocal
- `OriginBranchId`
- `BranchProductAvailability` persistence
- constraints/indexes
- existing product backfill → OrganizationStandard
- no ProductId rewrite
- no automatic availability rows where Standard default=true
- repository/domain mapping
- migration + migration tests
- architecture guards

**Out of scope:** React feature completion; promotion UX; branch pricing.

**HARD STOP** before MB2-01B.

**MB2_01A_STATUS:** COMPLETE_VALIDATED_FOUNDATION
**NEXT:** MB2_01B (when authorized)

---

## MB2-01B — Product Authority & Availability Enforcement

**Scope:**

- central server-side product availability/scope resolver
- org governance for Standard master
- origin-branch authority for BranchLocal
- Owner/Admin visibility across Local
- BranchLocal create/edit; other-branch Local deny
- Standard branch master edit deny
- Standard branch availability configuration
- promotion Local → Standard with **same ProductId**
- **MB2-01 promotion price preservation** (current Local SellingPrice continues as OrganizationDefaultPrice; no BranchPriceOverride)
- barcode/SKU collision protection
- bulk availability queries (no N+1)
- API contracts

**CRITICAL:** Availability is **server enforced**. Not-offered or foreign Local must not be sellable via React bypass.

Enforce at minimum for:

- sell catalog query
- sale/checkout validation
- storefront query
- customer-order quote/place

Cross-surface polish may remain MB2-06.

**HARD STOP** before MB2-01C.

**MB2_01B_STATUS:** COMPLETE_VALIDATED_AUTHORITY
**NEXT:** MB2_01B-H1 (hardening) then MB2_01C

---

## MB2-01B-H1 — Product Authority Hardening

**Scope:**

- Pre-pagination SQL membership for scope + commercial offering
- Correct filtered TotalCount
- Split CanBeSold vs commerciallyOffered
- Foreign BranchLocal blocked on SKU/barcode/image management reads
- Connected Buyer org-governance only; Standard-only; Local promote-first
- Mandatory (non-optional) governance dependencies on security-sensitive use cases

**HARD STOP** before MB2-01C.

**MB2_01B_H1_STATUS:** COMPLETE_VALIDATED
**NEXT:** MB2_01C (when authorized)

---

## MB2-01C — Product Governance React UX

**Scope:**

Organization catalog/governance filters:

- All products / Organization products / Branch products

Branch product presentation:

- scope/status, origin branch
- master read-only when OrganizationStandard
- BranchLocal editing at origin only
- availability management for org governance
- promotion review/confirmation
- bulk availability management where appropriate

Merchant wording: Organization product, Branch product, Not offered at this branch.
Avoid Platform Global Catalog confusion.

Responsive 360 / 768 / 1024 / 1440; full i18n.

**Out of scope:** Branch price override UI; Today's Prices remains CURRENT price authority until MB2-03.

**HARD STOP** before MB2-01C-H1 (then MB2-01D).

**MB2_01C_STATUS:** COMPLETE_VALIDATED_UX
**NEXT:** MB2_01C_H1 then MB2_01D (when authorized)

---

## MB2-01C-H1 — Strong Product Duplicate Identity

**Scope:**

- Org-wide `NormalizedName` on `CatalogProduct`
- Unique `(OrganizationId, NormalizedName)` — Active/Inactive, Standard/Local
- Create / rename / import / Connected Supplier guards
- Advisory name-conflict API + React UX (no Create anyway; foreign Local privacy)
- Identity mutations ONLINE_REQUIRED; `OFFLINE_PRODUCT_DRAFT=DEFERRED`; `PRODUCT_MERGE=NO`

**Report:** [POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md)

**HARD STOP** before MB2-01D.

**MB2_01C_H1_STATUS:** COMPLETE_VALIDATED_PRODUCT_IDENTITY
**NEXT:** MB2_01D (when authorized)

---

## MB2-01D — Product Governance Validation Closure

**Status:** COMPLETE_VALIDATED_BASELINE

**Scope delivered:** PRODUCT-01…06 validation; PGA-HARD-PAGE PostgreSQL pagination proof; migration/API/React/offline identity locks; regression fixes for H1 fixtures; authoritative status update.

Declared:

`MB2_01_STATUS=COMPLETE_VALIDATED_BASELINE`

Then: `NEXT=MB2_02`

**Report:** [POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md](../Reports/POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md)

**HARD STOP** before MB2-02.

**MB2-02A (read authority):** COMPLETE — see [MB2-02A report](../Reports/POS-MULTI-BRANCH-V2-MB2-02A-BRANCH-INVENTORY-READ-AUTHORITY.md).
**MB2-02A-H1 (primary legacy isolation):** COMPLETE — see [H1 report](../Reports/POS-MULTI-BRANCH-V2-MB2-02A-H1-PRIMARY-BRANCH-LEGACY-STOCK-ISOLATION.md).
**MB2-02B (write authority):** COMPLETE — see [MB2-02B report](../Reports/POS-MULTI-BRANCH-V2-MB2-02B-PHYSICAL-INVENTORY-WRITE-AUTHORITY.md).
**MB2-02B-H1 (reservation / unknown-primary writes / mixed lots):** COMPLETE — see [MB2-02B-H1 report](../Reports/POS-MULTI-BRANCH-V2-MB2-02B-H1-INVENTORY-RESERVATION-PRIMARY-AND-LOT-HARDENING.md).
**NEXT authorized package:** MB2-02C (lot/movement reconciliation polish). **HARD STOP — do not start MB2-02C without authorization.**

---

## MB2-02 — Branch Inventory Authority Hardening

**Scope:** List/detail branch correctness; operation matrix; org aggregate reconciliation; migrations if required.

**OD-04 closed:** Normal workspace inventory APIs resolve org + selected branch and return branch on-hand as `onHandQuantity`. Org aggregate only via explicit summary endpoint or unmistakably named field (e.g. `organizationOnHandQuantity`).

**Goal:** Selected branch never sees another branch’s stock as its own.

**MB2_02_READY:** YES after MB2-01D.

---

## MB2-03 — Branch Pricing & Effective Price Authority

**Scope:** Org default + branch overrides (base + unit); central resolver; Today's Prices branch awareness; Sell/checkout/customer ordering; offline lease/cache keys; snapshots; tests/migration.

**OD-05 closed:** When effective pricing lands, cache/lease identity = `org::branch::product::unit` (or base). Bump schema version; invalidate/migrate legacy product-only keys; never guess branch; fail closed/refetch when ambiguous; invalidate on workspace branch change.

**Promotion enhancement (deferred from MB2-01):**

`PROMOTION_CUSTOM_DEFAULT_WITH_ORIGIN_OVERRIDE=DEFERRED_TO_MB2_03`

After MB2-03, promotion may set Organization default ≠ origin and retain origin via BranchPriceOverride.

**MB2_03_READY:** YES after MB2-01D.

---

## MB2-04 — Customer & Supplier Branch Access

**Scope:** Access tables; privacy-safe search; branch transaction visibility; Utang privacy; backfill; Owner governance.

**OD-02 closed:** PRIVACY_FIRST_PROVENANCE_BACKFILL — infer only from reliable branch-attributed records; unknown → Primary/Main only; never fan-out ambiguous parties; no duplication.

MB2-04 must still audit real schema/data before migration; fallback policy is locked.

**MB2_04_READY:** YES (OD-02 closed).

---

## MB2-05 — New Branch Guided Setup

**Scope:** Resumable wizard; template; products/prices/stock/customers/suppliers/staff/devices/fulfillment/review.

**OD-03 closed:** HYBRID_SETUP_PROGRESS — domain data is source of truth; optional UX metadata (LastVisitedStep, timestamps, etc.); no duplicate authoritative `ProductsComplete`-style booleans.

**Consumes:** MB2-01D + MB2-02…04.

**MB2_05_READY:** YES after predecessors.

---

## MB2-06 — Cross-Surface + Offline Hardening

**Scope:** Sell, checkout, storefront, orders, returns, purchasing, offline, cache invalidation, reports, auth, N+1/performance.

**MB2_06_READY:** YES after MB2-05.

---

## MB2-07 — Multi-Branch V2 E2E Closure

**Scope:** Joe Store + Remote North; isolation; promotion; wizard; migration; responsive; offline/online; full regression.

**MB2_07_READY:** YES as terminal package.

---

## Bulk / performance

- Branch product availability summary (bulk)
- Effective price bulk resolution
- Branch customer/supplier access search
- Inventory branch summary

Avoid one-request-per-product designs.

---

## Migration safety (all packages)

- No ProductId rewrite; no historical sale rewrite; no party duplication; no fake stock; no automatic branch price overrides for existing data; existing org price → organization default; branch inventory must reconcile; privacy/access migration explicit; rollback/forward compatibility.

---

## Next

**NEXT=`MB2_02B`** — branch inventory write-path hardening (explicit authorization required). MB2-02A read authority is complete on `feat/organization`.
