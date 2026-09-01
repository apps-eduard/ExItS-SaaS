# Product Governance and Branch Assortment

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** MB2-01A COMPLETE_VALIDATED_FOUNDATION; MB2-01B COMPLETE_VALIDATED_AUTHORITY; MB2-01B-H1 COMPLETE_VALIDATED; MB2-01C COMPLETE_VALIDATED_UX; MB2-01C-H1 COMPLETE_VALIDATED_PRODUCT_IDENTITY; **MB2-01D COMPLETE_VALIDATED_BASELINE** — **MB2_01_STATUS=COMPLETE_VALIDATED_BASELINE**; NEXT=MB2-02
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-01A → MB2-01B → MB2-01B-H1 → MB2-01C → MB2-01C-H1 → MB2-01D
**Owner review:** [POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md](../../Reports/POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md)
**01A report:** [POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01A-PRODUCT-GOVERNANCE-DATA-FOUNDATION.md)
**01B report:** [POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY.md)
**01B-H1 report:** [POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01.md)
**01C report:** [POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX.md)
**01C-H1 report:** [POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md)
**01D report:** [POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md](../../Reports/POS-MULTI-BRANCH-V2-MB2-01D-PRODUCT-GOVERNANCE-FINAL-CLOSURE.md)

---

## 1. CURRENT_PROVEN (after MB2-01B-H1)

- One `CatalogProduct` per org product identity (`OrganizationId` + `ProductId`).
- Unique filtered indexes: org `NormalizedSku`, org `Barcode` (shared across scopes).
- **MB2-01A schema:** `CatalogProductScope`, `OriginBranchId`, sparse `BranchProductAvailability`.
- **MB2-01B authority:** governance, bulk availability resolver, commercial gates, promote, import org-governance.
- **MB2-01B-H1 hardening:**
  - List membership (scope + commercial offering) applied in SQL **before** Count/Skip/Take; `TotalCount` is full filtered total.
  - `CanBeSold` ≠ `commerciallyOffered` (Sell sends both).
  - SKU/barcode/image management visibility blocks foreign BranchLocal; commercial exact lookup optionally rejects Not offered.
  - Connected Buyer: Owner/Admin only; OrganizationStandard only; BranchLocal = promote first (`NOT_SUPPORTED_V1_PROMOTE_FIRST`).
  - Security-critical use cases require non-optional governance dependencies.
- **MB2-01C React UX:**
  - Merchant wording: Organization product / Branch product.
  - Catalog scope filters via server `scope=` (before pagination).
  - Owner create Standard vs BranchLocal; branch actor creates BranchLocal only; origin server-derived.
  - Standard master read-only for normal branch actors; Local editable at origin / Owner.
  - Promote UX; Owner branch-availability toggles; Today's Prices matches interim price authority.
  - Global import + Connected Buyer org mutations gated to org governance.
- **MB2-01C-H1 product name identity:**
  - Org + `NormalizedName` = one `ProductId` (Active/Inactive, Standard/Local).
  - Unique index + create/rename/import/Connected Supplier guards; advisory name-conflict API.
  - React: no Create anyway; foreign Local privacy; identity mutations ONLINE_REQUIRED.
  - `OFFLINE_PRODUCT_DRAFT=DEFERRED`; `PRODUCT_MERGE=NO`.
- **MB2-01D final closure:** PGA-HARD-PAGE PostgreSQL pagination proof; H1 test-fixture repairs (`normalized_name` SQL, LocalValidation disable); React suite green; **MB2_01_STATUS=COMPLETE_VALIDATED_BASELINE**.
- **Not yet:** branch inventory (MB2-02), branch pricing (MB2-03).

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

### Platform Global Catalog — LOCKED clarification

Platform Global Catalog is a **SOURCE/TEMPLATE** concept only. It does **not** determine organization product scope automatically.

After import into an organization, the resulting `CatalogProduct` must still receive a valid `OrganizationStandard` or `BranchLocal` scope according to the authorized workflow.

- Ordinary branch-level authority must **not** gain power to create OrganizationStandard merely because the source was Platform Global Catalog.
- Organization-level governance may create/import OrganizationStandard.
- Authorized branch workflow may create/import BranchLocal for its own branch if import capability is exposed there.
- No naming collision: merchant “Organization product” / “Branch product” ≠ Platform Global Catalog.

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

### OD-01 = CLOSED — ALLOW_DISABLE_WITH_NONZERO_STOCK_WITH_WARNING

Branch product availability controls **commercial offering**, not physical inventory existence.

When OrganizationStandard becomes **Not offered** at Branch A:

**BLOCK new:**

- Sell-floor commercial selection/sale
- Customer storefront offering
- New customer-order quote/place for that branch

**RETAIN:**

- Existing ProductId
- Branch stock, lots/expiry
- Historical sales/orders
- Branch price records, stock movements, audit

**ALLOW** legitimate inventory lifecycle (per existing permissions/domain): inventory viewing, stock adjustment, transfer out, valid returns/reversals, stock-use/waste where applicable.

Do **not** hard-delete or force stock to zero. Availability is **not** inventory existence.

---

## 5. Promotion BranchLocal → OrganizationStandard — TARGET_LOCKED

- Same ProductId; no clone.
- Keep sales, purchases, movements, lots, balances, audit.
- Retain origin metadata for audit.
- Org governance authority required.
- **STANDARD_TO_LOCAL_DEMOTION = NOT_SUPPORTED_V1**
- **BRANCH_LOCAL_MULTI_BRANCH_SHARING = DEFERRED**
- **LOCAL_PROMOTION_INSIDE_BRANCH_WIZARD = DEFERRED**

### Promotion price — phased (LOCKED)

**MB2-01 (01B) — price preservation only:**

`BranchLocal → OrganizationStandard`

- Same ProductId
- Preserve current SellingPrice
- Current Local selling price becomes/continues as OrganizationDefaultPrice
- No automatic price change
- No temporary branch-pricing mechanism
- **No BranchPriceOverride** (table does not exist until MB2-03)
- All historical data retained

Example: Remote Local Fresh Bangus = 180 → after promote: OrganizationStandard default = 180; Remote effective = 180.

**MB2-03 enhancement (deferred):**

`PROMOTION_CUSTOM_DEFAULT_WITH_ORIGIN_OVERRIDE=DEFERRED_TO_MB2_03`

Only after BranchPriceOverride exists may promotion support: Organization default = 190 with Remote override = 180.

Do **not** make MB2-01 depend on a nonexistent override table.

---

## 6. Barcode / SKU — TARGET_LOCKED

BranchLocal and OrganizationStandard share the **same org identity space**.

On Local create: check existing org catalog (barcode/SKU/rules). Prefer UX: “This product already exists…” → authorized existing-product workflow. No fuzzy merge in V1.

**CURRENT uniqueness:** org-scoped unique barcode/SKU — reuse.

---

## 6A. Canonical product name identity — TARGET_LOCKED (MB2-01C-H1)

```
ONE ORGANIZATION + ONE NORMALIZED PRODUCT NAME = ONE CatalogProductId
```

| Aspect | Lock |
|--------|------|
| Identity key | `NormalizedName` (NFC, trim, collapse whitespace, uppercase invariant) |
| Display | Separate; casing preserved after whitespace cleanup |
| Uniqueness | Org-wide unique index — Active+Inactive, OrganizationStandard+BranchLocal |
| Scope | Does **not** allow duplicate names across Standard vs Local or across branches |
| Soft delete | Inactive name remains reserved |
| Merge | **PRODUCT_MERGE=NO** — no auto-merge of ProductIds |
| Fuzzy | Exact normalized match only; fuzzy blocking = NO |
| Privacy | Foreign BranchLocal may block create without revealing metadata |
| UX | Advisory name-conflict check; **no Create anyway**; server create/update authoritative |

See [MB2-01C-H1 report](../../Reports/POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY.md).

### Identity mutations — ONLINE_REQUIRED

| Operation | Policy |
|-----------|--------|
| Create canonical product | ONLINE_REQUIRED |
| Rename product | ONLINE_REQUIRED |
| Change SKU | ONLINE_REQUIRED |
| Change barcode | ONLINE_REQUIRED |
| Promote Local → Standard | ONLINE_REQUIRED |
| Change branch availability | ONLINE_REQUIRED |
| Organization master edit | ONLINE_REQUIRED |
| Today's Prices mutation | ONLINE_REQUIRED_FOR_CURRENT_BASELINE |
| Offline product draft | DEFERRED (not a CatalogProduct until accepted online) |

No offline-generated canonical ProductId may later sync as a new product. Full offline capability matrix remains MB2-06.

### Offline capability matrix (catalog identity — partial)

| Operation | Policy |
|-----------|--------|
| View synced catalog | Existing behavior (audit later) |
| Create canonical product | ONLINE_REQUIRED |
| Rename / SKU / barcode | ONLINE_REQUIRED |
| Promote Local → Standard | ONLINE_REQUIRED |
| Branch availability governance | ONLINE_REQUIRED |
| Organization master mutation | ONLINE_REQUIRED |

---

## 7. Existing product backfill — TARGET (MB2-01A migration)

- All existing `CatalogProduct` → `OrganizationStandard`.
- No ProductId rewrite; no clones.
- Availability default preserves current observable behavior (offered at existing branches).
- No automatic availability rows needed where Standard default = true.
- No migration files in MB2-00 / MB2-00A.

---

## 8. Authorization target

| Actor | OrganizationStandard master | BranchLocal at origin | Other branch Local | Availability disable |
|-------|----------------------------|-----------------------|--------------------|----------------------|
| Owner/Admin | Yes | Yes | Yes (governance) | Yes |
| Branch catalog role at branch | Read-only master | Create/edit if capability | Deny | Only if org policy grants (default Deny) |
| Cashier | Deny master edit | Deny unless capability says otherwise | Deny | Deny |

**Server enforces** (MB2-01B); UI is not the boundary. Not-offered or foreign Local must not be sellable by bypassing React.

---

## 9. Acceptance scenario IDs (MB2-01D)

| ID | Expectation |
|----|-------------|
| PRODUCT-01 | BranchLocal A invisible to normal Branch B staff |
| PRODUCT-02 | Owner sees Branch A Local |
| PRODUCT-03 | Promotion retains ProductId; history intact |
| PRODUCT-04 | Branch Manager cannot edit OrganizationStandard master |
| PRODUCT-05 | Unselected Standard product not offered on branch |
| PRODUCT-06 | Duplicate barcode rejected across scopes |

Only MB2-01D may declare `MB2_01_STATUS=COMPLETE_VALIDATED_BASELINE`.

---

## 10. Data model proposal (conceptual)

### CatalogProduct additions

- `Scope` (OrganizationStandard | BranchLocal)
- `OriginBranchId` (nullable Guid; required when BranchLocal)

### BranchProductAvailability

As §4. Soft state via `IsOffered`; no hard delete of product.

Indexes: unique `(org, branch, product)`; query by `(org, branch, IsOffered)`.
