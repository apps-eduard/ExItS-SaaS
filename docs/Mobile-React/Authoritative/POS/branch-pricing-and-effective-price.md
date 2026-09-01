# Branch Pricing and Effective Price Authority

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** OWNER_APPROVED (MB2-00A) — TARGET_LOCKED
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-03 (core) **COMPLETE**; offline cache hardening **DEFERRED** (future offline/native phase — [production-roadmap-policy.md](production-roadmap-policy.md))
**CURRENT price contract:** [pricing-and-price-authority.md](pricing-and-price-authority.md)
**Owner review:** [POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md](../../Reports/POS-MULTI-BRANCH-V2-OWNER-REVIEW-CLOSURE-01.md)

---

## 1. CURRENT_PROVEN

- `CatalogProduct.SellingPrice` and `CatalogProductUnit` sell prices are **organization-wide**.
- Today's Prices: `POST .../catalog/products/prices` with `expectedUpdatedAtUtc` (per-product UX as of POS-TODAYS-PRICES-PER-PRODUCT-SAVE-UX-01).
- Sale-line price override (RMAP-B01): transaction exception; **never** rewrites catalog.
- Offline `OfflinePriceAuthority` lease includes OrganizationId + optional BranchId + ProductId + SellingUnitId; **price bytes come from org catalog**, not a branch price book.
- Client cache key today: `productId::sellingUnitId|base` (no branch in key) — closed by OD-05 for MB2-03.

WP12 / pricing doc: **no branch price overrides** — CURRENT.

---

## 2. TARGET formula — LOCKED

```
EffectivePrice(branch, product, unit?) =
  BranchOverride(branch, product, unit?)
  ?? OrganizationDefaultPrice(product, unit?)
```

- Organization default **remains**; not replaced by cloning products.
- Overrides only when user/API sets a distinct branch price.
- Display of inherited price must **not** invent override rows.

Applies to **base product** and **CatalogProductUnit** sell prices independently.

Until MB2-03 ships, Today's Prices remains CURRENT org-wide price authority.

---

## 3. Conceptual model — BranchPriceOverride

| Field | Notes |
|-------|-------|
| OrganizationId | Tenant |
| BranchId | Location |
| ProductId | Canonical product |
| ProductUnitId | null = base product SellingPrice; non-null = unit sell price |
| SellingPrice | Branch override amount |
| UpdatedAtUtc / UpdatedBy | Audit |
| Concurrency | Token/version per POS patterns |

**Uniqueness:** `(OrganizationId, BranchId, ProductId, ProductUnitId)` with null-unit uniqueness matching PostgreSQL null semantics (use sentinel or filtered unique as in existing POS patterns).

Indexes: lookup by `(org, branch, product)`; bulk resolve by `(org, branch)` + product set.

**MB2-01 must not depend on this table.** See promotion price phasing below.

---

## 4. Central resolver — TARGET_LOCKED

One **server-side** effective-price authority. Consumers:

Sell Floor, cart baseline, checkout, storefront, customer order quote/place, override baseline, offline price authority, receipts/snapshots.

React must not invent authoritative effective price.

---

## 5. Transaction override remains separate — LOCKED

| Layer | Example |
|-------|---------|
| Org default | 50 |
| Remote override | 65 → effective Remote 65 |
| Manager sale override | 60 → **sale snapshot 60** |

Sale override never rewrites org default or branch override.

---

## 6. Historical snapshots — LOCKED

Changing org price, branch override, promotion, or availability must **not** rewrite historical Sale/Order charged prices. CURRENT sale snapshots remain the model; document any gap where storefront quotes lack durable charged price.

---

## 7. Offline — TARGET (OD-05 CLOSED)

### OD-05 = CLOSED — BRANCH_AWARE_OFFLINE_PRICE_KEY_AT_MB2_03

When branch effective pricing lands, cache/lease identity must include:

- OrganizationId
- BranchId
- ProductId
- ProductUnitId-or-base

Conceptually: `org::branch::product::unit`

Do **not** retain product-only keys for authoritative effective pricing.

MB2-03 must:

- bump offline price cache/schema version
- invalidate or safely migrate legacy organization-only keys
- never guess branch for a legacy cached effective price
- fail closed/refetch when branch identity is ambiguous
- invalidate effective-price state when workspace branch changes

Branch A offline must never consume Branch B effective price.

---

## 8. Today's Prices — TARGET evolution

- Remain the merchant tool for price edits.
- Branch-aware: edit org default **or** selected-branch override (capability-gated) — after MB2-03.
- Reuse per-product Save UX; do not reintroduce global sticky Save as primary.
- Creating override only when value ≠ org default (or explicit “set override”).

---

## 9. Promotion pricing dependency — LOCKED

`PROMOTION_CUSTOM_DEFAULT_WITH_ORIGIN_OVERRIDE=DEFERRED_TO_MB2_03`

MB2-01 promotion preserves Local SellingPrice as OrganizationDefaultPrice with no BranchPriceOverride.

Only after MB2-03 may enhanced promotion set Organization default ≠ origin while retaining origin via BranchPriceOverride.

---

## 10. Acceptance IDs

| ID | Expectation |
|----|-------------|
| PRICE-01 | Main default 50; Remote override 65; Main stays 50 |
| PRICE-02 | Org default 50→55; Remote override remains 65 |
| PRICE-03 | Unit sell price override independent of base |
| PRICE-04 | Sale override 60 does not change 65 override |
| PRICE-05 | Offline lease for Remote uses 65 |

---

## 11. Migration (MB2-03)

- Existing org prices become OrganizationDefaultPrice (no row rewrite beyond semantics).
- **No** automatic branch override rows for existing data.
- Behavior preserved: all branches inherit until overrides created.
