# Branch Pricing and Effective Price Authority

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** TARGET_LOCKED (MB2-00)
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-03 (core), MB2-06 (cross-surface/offline)
**CURRENT price contract:** [pricing-and-price-authority.md](pricing-and-price-authority.md)

---

## 1. CURRENT_PROVEN

- `CatalogProduct.SellingPrice` and `CatalogProductUnit` sell prices are **organization-wide**.
- Today's Prices: `POST .../catalog/products/prices` with `expectedUpdatedAtUtc` (per-product UX as of POS-TODAYS-PRICES-PER-PRODUCT-SAVE-UX-01).
- Sale-line price override (RMAP-B01): transaction exception; **never** rewrites catalog.
- Offline `OfflinePriceAuthority` lease includes OrganizationId + optional BranchId + ProductId + SellingUnitId; **price bytes come from org catalog**, not a branch price book.
- Client cache key today: `productId::sellingUnitId|base` (no branch in key) — **OD-05** when branch prices land.

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

## 7. Offline — TARGET

- Lease identity: Organization + Branch + Product + Selling Unit.
- Branch A offline must never consume Branch B effective price.
- Cache key/version strategy in MB2-03/MB2-06; invalidate on workspace branch switch.
- Fail closed if lease branch ≠ bound/selected operational branch (existing WrongBranch direction).

---

## 8. Today's Prices — TARGET evolution

- Remain the merchant tool for price edits.
- Branch-aware: edit org default **or** selected-branch override (capability-gated).
- Reuse per-product Save UX; do not reintroduce global sticky Save as primary.
- Creating override only when value ≠ org default (or explicit “set override”).

---

## 9. Acceptance IDs

| ID | Expectation |
|----|-------------|
| PRICE-01 | Main default 50; Remote override 65; Main stays 50 |
| PRICE-02 | Org default 50→55; Remote override remains 65 |
| PRICE-03 | Unit sell price override independent of base |
| PRICE-04 | Sale override 60 does not change 65 override |
| PRICE-05 | Offline lease for Remote uses 65 |

---

## 10. Migration (MB2-03)

- Existing org prices become OrganizationDefaultPrice (no row rewrite beyond semantics).
- **No** automatic branch override rows for existing data.
- Behavior preserved: all branches inherit until overrides created.
