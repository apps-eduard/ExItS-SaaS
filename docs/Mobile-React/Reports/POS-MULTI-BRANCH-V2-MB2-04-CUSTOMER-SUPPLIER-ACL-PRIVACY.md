# POS-MULTI-BRANCH-V2 MB2-04 — Customer / Supplier Branch ACL + Privacy

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-04  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Depends on:** MB2-03-H1 COMPLETE_VALIDATED

---

## Model (LOCKED)

- Customer and supplier masters remain **organization-owned** (one canonical record).
- Branch visibility via sparse `customer_branch_access` / `supplier_branch_access`.
- Formula: staff see party at branch **only if** access row exists **or** Owner/Admin org governance.
- Fail closed: inaccessible party → **404** (no metadata leak).

---

## Schema

**Tables:** `pos.customer_branch_access`, `pos.supplier_branch_access`

Composite PK: `(organization_id, branch_id, customer_id|supplier_id)`

**Migration:** `20260901180417_AddPartyBranchAccess`

**Backfill (OD-02 privacy-first):** infer from `Sale.BranchId`, `CustomerOrder.FulfillmentBranchId`, branch-attributed receipts; ambiguous legacy → primary branch only.

---

## Grant paths

| Source | Trigger |
|--------|---------|
| CreateAtBranch | Customer/supplier created with acting branch header |
| Transaction | Sale checkout with customer; CustomerOrder placed |
| MigrationBackfill | One-time provenance SQL |

---

## Central service

`PartyBranchAccessService` + `PartyBranchAccessGovernanceAuthority`

Wired: customer list/get/checkout-search, supplier list/get, credit endpoints, sale checkout, customer order place.

---

## Integration proofs

`BranchCustomerSupplierAccessIntegrationTests` — **17/17 PASS**

- CUSTOMER-SEC-01 … 08
- SUPPLIER-SEC-01 … 08
- MICA_E2E (Maria + Alpha Supplier branch isolation)

---

## React

`WorkspaceProvider` invalidates `customers` / `suppliers` TanStack Query keys on branch switch.

---

## Explicit exclusions

- ExplicitAssign grant/revoke management API (future)
- Runtime supplier grant on GRN/direct purchase (migration backfill only)
- PRIVACY-04 transaction-history branch scoping depth → follow-up if needed
- MB2-05 guided branch setup
- MB2-06 offline hardening

---

## Next

**MB2-05** — guided branch setup
