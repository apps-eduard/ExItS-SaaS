# Multi-Branch Commerce V2 — Master Contract

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Package:** MB2-00 (Documentation & Architecture Lock)
**Status:** MB2-01C COMPLETE_VALIDATED_UX; MB2-01C-H1 COMPLETE_VALIDATED_PRODUCT_IDENTITY (pending validation) — next MB2-01D when authorized
**Classification key:** CURRENT_PROVEN | TARGET_LOCKED | GAP | SUPERSEDED | DEFERRED | CLOSED_DECISION
**Start SHA (MB2-00):** `dcc2b268894feb84eb742c3f26a0f855e5d330d9`
**Owner review:** [POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md](../../Reports/POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md)
**01A report:** [POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md)

Related:

- [Product governance & branch assortment](product-governance-and-branch-assortment.md)
- [Branch inventory authority](branch-inventory-authority.md)
- [Branch pricing & effective price](branch-pricing-and-effective-price.md)
- [Branch customer & supplier access](branch-customer-supplier-access.md)
- [Branch guided setup](branch-guided-setup.md)
- [Implementation plan MB2-01A…07](../../Implementation-Readiness/POS-MULTI-BRANCH-V2-IMPLEMENTATION-PLAN.md)

---

## 1. Purpose

Lock the next major multi-branch phase so implementation is deliberate, secure, privacy-aware, migration-safe, and package-ordered.

This document is the **master contract**. It does not claim TARGET behavior already ships.

**OWNER_REVIEW (MB2-00A):** `APPROVED_WITH_DOCUMENTATION_AMENDMENTS`. Architecture approved. Foundational locks below remain locked; OD-01…05 closed; MB2-01 split into 01A–01D; promotion custom-default+origin-override deferred to MB2-03.

---

## 2. Naming — locked

| Concept | Canonical term | Merchant UI (suggested) | Must not confuse with |
|---------|----------------|-------------------------|------------------------|
| Org-standard centrally governed product | `CatalogProductScope.OrganizationStandard` | Organization product | Platform **Global Catalog** (SaaS catalog import/source) |
| Branch-originated locally governed product | `CatalogProductScope.BranchLocal` | Branch product | A second physical DB / cloned product row |

**LOCKED:** Do not name product scope `Global`. Platform Global Catalog ≠ OrganizationStandard.

---

## 3. Core ownership invariant — TARGET_LOCKED

```
ONE ORGANIZATION
ONE CANONICAL RECORD
MULTIPLE BRANCH OPERATIONAL / ACCESS STATES
```

Forbidden designs:

- separate database per branch
- cloned `CatalogProduct` per branch
- cloned customer / supplier per branch
- product clone on promotion

Branch scoping is expressed through availability, access, operational state, price overrides, inventory balances, and transaction attribution — **not** duplicated masters.

---

## 4. Governance vs primary branch — TARGET_LOCKED

| Concept | Meaning |
|---------|---------|
| **PRIMARY_BRANCH** | Operational default / template / reference location (`IsPrimary`) |
| **ORGANIZATION_GOVERNANCE** | Security authority: Organization Owner, Organization Administrator, and future explicit org-level capabilities |

Changing primary must **not** transfer product governance. Main/Primary is template/reference, not the security principal.

---

## 5. CURRENT architecture summary — CURRENT_PROVEN

Evidence audit (code + prior reports):

| Area | CURRENT |
|------|---------|
| Branch master | Platform `OrganizationBranch` (Active / Inactive / Archived; one primary) |
| Staff ACL | `organization_membership_branch_assignments`; Owner/Admin → all Active; Member → explicit |
| Catalog | Org-owned `CatalogProduct` / `CatalogProductUnit`; unique org SKU/barcode |
| Price | Org `SellingPrice` (+ unit sell prices); Today's Prices updates org price |
| Sale override | Transaction-only; never rewrites catalog |
| Inventory | Model A: org `InventoryAccount` + optional `InventoryBranchBalance`; `BranchStockResolver` for sell/order |
| Inventory list UI | Often shows **org** on-hand (GAP vs branch display) |
| Receive/opening/DP/GRN | Update org account; typically **do not** write branch balances |
| Sale | `Sale.BranchId?` on new checkouts |
| Customer order | Required `FulfillmentBranchId` |
| Customer / supplier | Org canonical; **no** branch access tables |
| Offline price lease | Signed with org + optional branch context; price source = org catalog; client cache key product+unit |

---

## 6. CURRENT vs TARGET matrix

| AREA | CURRENT | TARGET | GAP | PACKAGE |
|------|---------|--------|-----|---------|
| Product master | Org `CatalogProduct` | Same ProductId; master org-owned | Scope fields missing | MB2-01A–D |
| Product scope | Implicit org-wide | OrganizationStandard \| BranchLocal | No scope enum | MB2-01A–D |
| Product visibility | All sellable products org-wide | Standard: default all + disable; Local: origin only | No availability model | MB2-01A–D |
| Local product | N/A | BranchLocal + OriginBranchId | Missing | MB2-01A–D |
| Promotion | N/A | Local→Standard, same ProductId, one-way | Missing | MB2-01B–D |
| Barcode/SKU | Org unique | Same org identity space for both scopes | Enforcement exists; Local create UX must reuse | MB2-01B–D |
| Base price | Org SellingPrice | OrganizationDefaultPrice | Rename semantics only | MB2-03 |
| Sell-unit price | Org unit prices | Org default + branch unit overrides | No branch override | MB2-03 |
| Inventory aggregate | InventoryAccount | Org control / aggregate | Clarify display vs authority | MB2-02 |
| Branch inventory | Overlay + resolver (partial) | Normal ops use branch stock | List UI / receive write gaps | MB2-02 |
| Reorder | Org account fields | Branch-specific thresholds | Move/evolve | MB2-02 |
| Lots | Optional branch on lot | Physical lot branch-owned | Org-null lot paths | MB2-02 |
| Stock movement | Mixed | Branch-attributed writes | Receive/open gaps | MB2-02 |
| Sale attribution | Sale.BranchId? | Required for new ops (existing) | Historical null OK | — |
| Customer identity | Org BusinessCustomer / POSCustomer | Unchanged canonical | — | MB2-04 |
| Customer visibility | Org-wide to authorized staff | Branch access ACL | No table | MB2-04 |
| Customer history / Utang | Org-scoped in practice | Identity ≠ org-wide history | Scope rules | MB2-04 |
| Supplier identity | Org Supplier | Unchanged | — | MB2-04 |
| Supplier visibility | Org-wide | Branch access ACL | No table | MB2-04 |
| Staff ACL | WP15C proven | Reuse; wizard assigns | Wizard only | MB2-05 |
| Devices | One registration branch | No clone | Wizard empty default | MB2-05 |
| Fulfillment | Per-branch Platform config | Link existing; defaults OFF | Wizard compose | MB2-05 |
| Offline price | Org price + branch lease ctx | Effective branch price in lease/cache | Key/version | MB2-03/06 |
| New branch create | Create API; inventory unallocated | + guided setup | No wizard | MB2-05 |
| Reporting | Mixed | Branch-correct filters | Harden | MB2-06/07 |

---

## 7. Locked owner decisions

1. One canonical organization product identity.
2. Two scopes: OrganizationStandard, BranchLocal.
3. Avoid “Global” for this scope (Platform Global Catalog collision).
4. Owner/Admin govern OrganizationStandard masters.
5. Primary = template/reference, not security authority.
6. Ordinary branch cannot edit OrganizationStandard master fields.
7. Branch may have independent price (override).
8. Stock is branch-specific for normal operation.
9. OrganizationStandard defaults available to branches; may disable per branch.
10. Any authorized branch may create BranchLocal.
11. BranchLocal is origin-only in V1.
12. Owner/Admin see all BranchLocal.
13. Local → Standard promotion allowed.
14. Promotion retains ProductId.
15. Promotion one-way in V1.
16. Customers canonical org records; visibility branch-scoped.
17. Suppliers canonical org records; visibility branch-scoped.
18. New branch does not auto-expose all customers.
19. New branch does not auto-expose all suppliers.
20. New branch stock never copies Main stock.
21. New branch OrganizationStandard products start selected.
22. Prices inherit org default unless override.
23. Branch setup guided and resumable.
24. Devices, staff, stock, fulfillment, history never cloned implicitly.

---

## 8. Deferred

- BranchLocal multi-branch sharing without promotion
- Standard → Local demotion
- Separate DB per branch
- Catalog product duplication
- Automatic GIS/regional price rules
- Transport-cost markup engine
- Dynamic price formulas
- Franchise / intercompany accounting
- Branch-specific customer/supplier duplicate identities
- ERP cost-center redesign
- Unrelated offline device redesign beyond price-authority hardening

---

## 9. Security / privacy threat review

| Threat | Server control | Test package | Failure mode |
|--------|----------------|--------------|--------------|
| Branch A reads Branch B customer | Customer branch ACL + query filters | PRIVACY-01, MB2-04 | Empty / not found; no metadata leak |
| Branch A reads Branch B supplier | Supplier branch ACL | PRIVACY-02 | Same |
| Branch A sells Branch B-only / Local | Availability + scope checks | PRODUCT-01 | Deny sell/storefront |
| Branch A uses Branch B price | Effective price resolver + branch context | PRICE-01 | Wrong price never from client |
| Branch A consumes Branch B stock | BranchStockResolver + mutations | STOCK-01 | Insufficient stock |
| Branch A edits Standard master | Capability + scope gate | PRODUCT-04 | 403 |
| Branch A edits B Local | OriginBranchId gate | PRODUCT-01 | 403 |
| Client spoofs BranchId | Workspace / device binding + ACL | MB2-06 | Reject |
| Offline cache cross-branch | Cache keys include org+branch | MB2-03/06 | Fail closed / refresh |
| Search/count leak | Scoped queries; no existence oracle | MB2-04 | No privileged counts |
| Promotion without org authority | Org governance capability | PRODUCT-03 | 403 |
| Hidden product in storefront | Availability in storefront query | MB2-01/06 | Not listed |
| Branch switch stale caches | Invalidate/refetch on workspace change | MB2-06 | Stale sell/price/stock |

---

## 10. Canonical acceptance scenario — Joe Store

**Organization:** Joe Store

**Branches:** Main Branch (Primary), Remote Branch

**Products:**

| Product | Scope | Org default | Notes |
|---------|-------|-------------|-------|
| Coke 1L | OrganizationStandard | ₱50 | |
| Rice 5kg | OrganizationStandard | ₱340 | |
| Fresh Bangus | BranchLocal | (n/a) | Origin=Remote; ₱180/kg |

**Stock / price:**

| | Main | Remote |
|--|------|--------|
| Coke stock | 100 | 20 |
| Rice stock | 50 | 10 |
| Bangus stock | — | 15 kg |
| Coke effective | 50 | 65 (override) |
| Rice effective | 340 | 380 (override) |

**Parties:** Maria — Main customer access only; ABC Wholesale — Main + Remote supplier access.

Validate: isolation of stock/price/privacy; Owner sees Bangus; promotion keeps ProductId; no history/stock duplication.

---

## 11. Package dependency graph

```
MB2-00 / MB2-00A (owner-approved)
  ↓
MB2-01A → MB2-01B → MB2-01C → MB2-01C-H1 → MB2-01D
  ↓
MB2-02 Inventory Authority
  ↓
MB2-03 Branch Pricing
  ↓
MB2-04 Party Access
  ↓
MB2-05 Branch Setup Wizard
  ↓
MB2-06 Cross-Surface Hardening
  ↓
MB2-07 E2E Closure
```

Correctness over parallelism. **NEXT=`MB2_01D`** only when explicitly authorized (after MB2-01C-H1).

---

## 12. Closed decisions (MB2-00A)

| ID | Decision | Package |
|----|----------|---------|
| OD-01 | **CLOSED** ALLOW_DISABLE_WITH_NONZERO_STOCK_WITH_WARNING — availability = commercial offer, not inventory existence | MB2-01 |
| OD-02 | **CLOSED** PRIVACY_FIRST_PROVENANCE_BACKFILL — infer only reliable branch provenance; else Primary/Main only | MB2-04 |
| OD-03 | **CLOSED** HYBRID_SETUP_PROGRESS — derived checklist; optional UX visit metadata only | MB2-05 |
| OD-04 | **CLOSED** BRANCH_INVENTORY_BY_DEFAULT — `onHandQuantity` = branch; org aggregate separately named | MB2-02 |
| OD-05 | **CLOSED** BRANCH_AWARE_OFFLINE_PRICE_KEY_AT_MB2_03 — `org::branch::product::unit` | MB2-03 |

**OPEN_DECISIONS_COUNT=0**

---

## 13. Evolution of prior docs

CURRENT P28 Model A inventory and org-wide price remain **truthful for today**. Multi-Branch V2 **extends** them; it does not rewrite historical reports.

See TARGET_EVOLUTION pointers on:

- [organization-branches-and-fulfillment-locations.md](../../../engineering/organization-branches-and-fulfillment-locations.md)
- [organization-branch-capability-matrix.md](../../../engineering/organization-branch-capability-matrix.md)
- [data-ownership.md](../../../engineering/data-ownership.md)
- [pricing-and-price-authority.md](pricing-and-price-authority.md)
