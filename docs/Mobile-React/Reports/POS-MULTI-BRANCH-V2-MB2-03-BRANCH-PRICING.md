# POS-MULTI-BRANCH-V2 MB2-03 — Branch Pricing / Effective Price

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-03  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Depends on:** MB2-02D COMPLETE_VALIDATED

---

## Formula (LOCKED)

```
EffectivePrice(branch, product, unit?) =
  BranchOverride(branch, product, unit?)
  ?? OrganizationDefaultPrice(product, unit?)
```

- Organization default remains `CatalogProduct.SellingPrice` / `CatalogProductUnit.SellingPrice`
- Sparse `branch_product_price_overrides` table — no auto-clone of defaults
- Base product uses `Guid.Empty` sentinel for `ProductUnitId` in composite PK
- Historical `SaleLine.UnitPrice` / `CustomerOrderLine.UnitPrice` snapshots unchanged by later price edits

---

## Schema

**Table:** `pos.branch_product_price_overrides`

| Column | Notes |
|--------|-------|
| organization_id, branch_id, product_id, product_unit_id | Composite PK |
| selling_price | numeric, >= 0 |
| created_at_utc, updated_at_utc, updated_by_actor_id | audit |
| xmin | concurrency token |

**Migration:** `20260901170000_AddBranchProductPriceOverrides`

---

## Central resolver

`IEffectivePriceResolver` / `EffectivePriceResolver` — batch resolution for catalog grids, checkout, storefront, customer orders, offline lease.

Consumers wired:

- `SaleUseCases.ResolveDraftsAsync` (online quote/checkout)
- `GetCustomerStorefront` (fulfillment branch)
- `CustomerOrderUseCases` (place order)
- `OfflinePriceAuthorityService.IssueAsync`
- `CatalogProductUseCases` enrichment when branch header present

---

## API

| Method | Path |
|--------|------|
| GET | `/api/v1/pos/catalog/products/{productId}/branch-pricing?branchId=` |
| PUT | `/api/v1/pos/catalog/products/{productId}/branch-pricing` |
| DELETE | `/api/v1/pos/catalog/products/{productId}/branch-pricing?branchId=&unitId=` |

Authorization: `CatalogProductGovernanceAuthority.CanMutateOrganizationStandardPrice`; branch must be active in org.

---

## React

- `BranchProductPricingPanel` on product edit (OrganizationStandard + org governance)
- `resolveSellUnitPrice` prefers `effectiveSellingPrice`
- i18n: `catalog.branchPricing.*` (en, fil-PH, ceb-PH, ilo-PH, hil-PH)

---

## Integration proofs (`BranchPricingIntegrationTests`)

PRICE-01 through PRICE-15 + Mica price E2E + migration apply/rollback — **17/17 PASS**

Inventory regression after pricing: unchanged OnHand/Reserved (MB2-02D suite PASS)

---

## Explicit exclusions

- MB2-04 customer/supplier branch ACL/privacy
- Promotion custom-default + origin override (deferred per authoritative doc)
- Offline cache key bump / legacy key invalidation (MB2-06 scope)

---

## Next

**MB2-04** — customer/supplier branch ACL/privacy
