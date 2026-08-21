# RMAP-B01 — Sale price override backend

## Status

**BACKEND IMPLEMENTED** (Domain + API + persistence + tests). React UI (**RMAP-12b**) is **not** in this package.

## Locked PO policy (authoritative — supersedes CashierAdjustable)

| Principal | Override authority |
|-----------|-------------------|
| **Cashier** | **DENY** all overrides |
| **Manager / StoreManager** | `abs(requested − baseline) / baseline ≤ 1.00` inclusive (exact 100% OK); `> 1.00` **DENY** |
| **Owner** (and `PosRole.Admin` Owner-equivalent) + `OverrideSalePriceUnlimited` | Unlimited **positive** unit prices |
| **OrganizationAdministrator alone** | **NOT** unlimited; no override unless they independently hold Owner POS role |
| **Platform Admin alone** | **No** |
| `requested ≤ 0` | **DENY** (free = Commercial Discount B03 only) |
| Reason | Required (trim, non-whitespace; same max length as B03) |
| Experience ≠ authority | UI presence never grants capability |
| Catalog | Override changes transaction `UnitPrice` **only** — never `Product.SellingPrice` / Today's Price |

### Order of operations

1. Resolve baseline (live catalog / selling unit)  
2. Apply override to draft `UnitPrice`  
3. `Sale.Checkout` builds lines (GrossLineTotal from overridden UnitPrice)  
4. B03 commercial discount on GrossLineTotal  

Money uses `decimal` / `SaleMoney` only — never float.

Offline snapshot + override intents: **fail closed** (`pos.sale.price_override.offline_not_supported`), same posture as B03.

## Capabilities

| Capability | Feature code |
|------------|--------------|
| `UtangCapability.OverrideSalePrice` | `store-sales-override-price` |
| `UtangCapability.OverrideSalePriceUnlimited` | `store-sales-override-price-unlimited` |

### PosRoleMatrix

| Role | OverrideSalePrice | OverrideSalePriceUnlimited |
|------|-------------------|----------------------------|
| Cashier | No | No |
| StoreManager | Yes | No |
| Owner / Admin | Yes | Yes |

Organization management projection excludes both override capabilities (checkout-only).

## Domain

- `SalePriceOverrideIntent`, `SalePriceOverrideRules`, `SalePriceOverrideApplier`
- `SalePriceOverrideAdjustment` (+ Id) — audit evidence
- Naming: consistent `SalePriceOverride*` (architecture tests updated)

## Persistence

Migration: **`AddPosSalePriceOverrides`** (`20260821145228_AddPosSalePriceOverrides`)

Table: `pos.sale_price_override_adjustments`

| Column | Meaning |
|--------|---------|
| SaleId / SaleLineId | Parent sale + line |
| BaselineUnitPrice | Resolved catalog/unit baseline at checkout |
| AppliedUnitPrice | Overridden unit price on the sale line |
| Reason / AppliedBy / RecordedAtUtc | Operator evidence |

Old sales without rows = no override.

## API

| Surface | Behavior |
|---------|----------|
| `CheckoutSaleRequest.PriceOverrides[]` | Parallel to `Discounts`; intent = `RequestedUnitPrice` + `Reason` (+ optional line/product + `ExpectedBaselineUnitPrice`) |
| Gate | Any intent → `OverrideSalePrice`; server computes deviation; unlimited capability widens ceiling (client % never trusted) |
| `POST .../sales/quote` | Applies same math; returns baseline vs applied on quote DTOs |
| Idempotency | Replay of completed `SaleId` returns existing sale — no duplicate audit rows |
| Stale baseline | `ExpectedBaselineUnitPrice` mismatch → `pos.sale.price_override.stale_baseline` (conflict, not clamp) |

## Explicit exclusions

- React RMAP-12b override UX  
- Per-product Fixed / CashierAdjustable policy (**SUPERSEDED** by role matrix above)  
- Promotions / regulatory discounts  
- Catalog Today's Price mutation via override  

## Docs reconciled

- `pricing-and-price-authority.md` — CashierAdjustable marked **SUPERSEDED**  
- Owner decision register / UD-02 / roadmap / capability-parity-matrix — aligned to locked PO policy  

## Tests (required matrix coverage)

Domain + API + migration suites cover: Cashier deny; Manager 90/150/200 PASS, 200.01 DENY, 0 DENY, blank reason DENY; Owner 250 PASS / 0 DENY; OrgAdmin-like grants >100 DENY; no override unchanged; Today's Price isolation; multi-UOM; ByWeight; override+B03; Cash/GCash/Utang; audit row; cross-org; stale baseline; money decimals; offline fail-closed; idempotent replay.
