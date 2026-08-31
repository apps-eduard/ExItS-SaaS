# Product Governance and Branch Assortment

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** TARGET_LOCKED (MB2-00)
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-01

---

## 1. CURRENT_PROVEN

- One `CatalogProduct` per org product identity (`OrganizationId` + `ProductId`).
- Unique filtered indexes: org `NormalizedSku`, org `Barcode`.
- Master fields include (domain): Name, Description, Sku/NormalizedSku, Barcode, CategoryId, BrandId, UnitOfMeasure, SellingMode, SellingPrice, Status, units (`CatalogProductUnit`), images, expiration-tracking characteristics, Platform import provenance.
- No `CatalogProductScope`, no `OriginBranchId`, no branch availability table.
- Today's Prices / product edit update **organization** selling price.
- ManageCatalog (and equivalent) gates catalog mutations; cashiers denied Today's Prices (RMAP-06).

---

## 2. TARGET scopes — LOCKED

```csharp
enum CatalogProductScope {
  OrganizationStandard,
  BranchLocal
}
```

| | OrganizationStandard | BranchLocal |
|--|----------------------|-------------|
| Belongs to | Organization | Organization |
| ProductId | One | One |
| OriginBranchId | null or audit-only after promotion | Required (origin) |
| Default availability | All Active branches | Origin branch only |
| Master edit | Owner/Admin (org catalog authority) | Authorized origin-branch users |
| Owner/Admin visibility | Yes | Yes (governance) |
| V1 cross-branch Local share | N/A | **DEFERRED** — promote instead |

---

## 3. Master vs branch-owned fields — TARGET

**Master (OrganizationStandard — org governance only):**
name, description, SKU, barcode, brand, category, base UOM, selling mode, weighted/item semantics, images, unit definitions (structure), expiration-tracking product characteristics, other catalog-master attributes discovered on `CatalogProduct` / units.

**Branch-owned (when authorized):**
branch availability (org policy), branch price override (MB2-03), branch stock / reorder (MB2-02), not master identity.

Do not grant org governance merely because a user manages one branch.

---

## 4. Availability model — TARGET_LOCKED

### Recommended architecture: default policy + override rows

**Concept:** `BranchProductAvailability`

| Field | Purpose |
|-------|---------|
| OrganizationId | Tenant |
| BranchId | Location |
| ProductId | Canonical product |
| IsOffered | true/false |
| UpdatedAtUtc / UpdatedBy | Audit |
| Concurrency | Match POS patterns (token/version) |

**Uniqueness:** `(OrganizationId, BranchId, ProductId)`.

**Semantics:**

| Scope | Default if no row | Explicit row |
|-------|-------------------|--------------|
| OrganizationStandard | Offered = true | May set Offered = false (“not offered at this branch”) |
| BranchLocal | Offered only for OriginBranch | No multi-branch offer in V1 |

Disable ≠ delete/archive/clone. History, balances, overrides retained. Re-enable restores offer without new ProductId.

**OD-01 (recommended):** Allow disable with nonzero stock + warning; block new commercial sell/storefront; stock mgmt remains.

---

## 5. Promotion BranchLocal → OrganizationStandard — TARGET_LOCKED

- Same ProductId; no clone.
- Keep sales, purchases, movements, lots, balances, audit.
- Retain origin metadata for audit.
- Org governance authority required.
- **STANDARD_TO_LOCAL_DEMOTION = NOT_SUPPORTED_V1**
- **BRANCH_LOCAL_MULTI_BRANCH_SHARING = DEFERRED**
- **LOCAL_PROMOTION_INSIDE_BRANCH_WIZARD = DEFERRED**

### Promotion price — TARGET_LOCKED

1. Owner/Admin must set **OrganizationDefaultPrice** (and unit defaults as applicable).
2. UI may prefill from origin effective/local price.
3. Server must not silently invent defaults without explicit command.
4. If org default equals origin price → origin inherits.
5. If org default differs → origin may retain prior effective via **branch override** (explicit promotion option).

---

## 6. Barcode / SKU — TARGET_LOCKED

BranchLocal and OrganizationStandard share the **same org identity space**.

On Local create: check existing org catalog (barcode/SKU/rules). Prefer UX: “This product already exists…” → authorized existing-product workflow. No fuzzy merge in V1.

**CURRENT uniqueness:** org-scoped unique barcode/SKU — reuse.

---

## 7. Existing product backfill — TARGET (MB2-01 migration)

- All existing `CatalogProduct` → `OrganizationStandard`.
- No ProductId rewrite; no clones.
- Availability default preserves current observable behavior (offered at existing branches).
- No migration files in MB2-00.

---

## 8. Authorization target

| Actor | OrganizationStandard master | BranchLocal at origin | Other branch Local | Availability disable |
|-------|----------------------------|-----------------------|--------------------|----------------------|
| Owner/Admin | Yes | Yes | Yes (governance) | Yes |
| Branch catalog role at branch | Read-only master | Create/edit if capability | Deny | Only if org policy grants (default Deny) |
| Cashier | Deny master edit | Deny unless capability says otherwise | Deny | Deny |

Server enforces; UI is not the boundary.

---

## 9. Acceptance scenario IDs (MB2-01)

| ID | Expectation |
|----|-------------|
| PRODUCT-01 | BranchLocal A invisible to normal Branch B staff |
| PRODUCT-02 | Owner sees Branch A Local |
| PRODUCT-03 | Promotion retains ProductId; history intact |
| PRODUCT-04 | Branch Manager cannot edit OrganizationStandard master |
| PRODUCT-05 | Unselected Standard product not offered on branch |
| PRODUCT-06 | Duplicate barcode rejected across scopes |

---

## 10. Data model proposal (conceptual)

### CatalogProduct additions

- `Scope` (OrganizationStandard | BranchLocal)
- `OriginBranchId` (nullable Guid; required when BranchLocal)

### BranchProductAvailability

As §4. Soft state via `IsOffered`; no hard delete of product.

Indexes: unique `(org, branch, product)`; query by `(org, branch, IsOffered)`.
