# Pricing and Price Authority

## CURRENT contract

| Capability | Status | Evidence |
|------------|--------|----------|
| Product `SellingPrice` | PROVEN_CURRENT | `CatalogProduct.SellingPrice` |
| Sell-unit independent prices | PROVEN_CURRENT | `CatalogProductUnit` sell prices; Rice test ₱55 vs ₱2,600 |
| Today’s Prices bulk update | PROVEN_CURRENT | `POST /api/v1/pos/catalog/products/prices`; concurrency `ExpectedUpdatedAtUtc` |
| Sale-line price snapshots | PROVEN_CURRENT | Checkout snapshots; offline fidelity tests |
| Connected buyer-specific PO price | PROVEN_CURRENT | `BuyerSpecificPoPrice` on shares; `DefaultConnectedPoPrice` on product |
| Persistent catalog cost master | PROVEN_MISSING | Cost lives on PO/GRN/direct receipt lines |
| Dedicated price history / audit table | PROVEN_MISSING | Overwrite current price; historical sales retain snapshots |
| First-class sale-line price override model | **PROVEN_CURRENT** (backend) | RMAP-B01 `SalePriceOverride*`; audit table `sale_price_override_adjustments` |
| `SalePricePolicy` / `CashierAdjustable` | **SUPERSEDED** | Locked PO policy uses role capabilities — not per-product Fixed/CashierAdjustable |

## Distinction required by owner

| Concept | Meaning | CURRENT |
|---------|---------|---------|
| **Change current selling price** | Updates catalog/unit price for future transactions | PROVEN_CURRENT (Today’s Prices / product edit) |
| **Sale-line price override** | Exceptional per-sale unit price; never rewrites catalog | PROVEN_CURRENT backend (RMAP-B01); React UI still RMAP-12b |

## LOCKED PO POLICY (authoritative — supersedes CashierAdjustable)

| Principal | Authority |
|-----------|-----------|
| Cashier | **DENY** all overrides |
| Manager / StoreManager | Deviation `abs(requested−baseline)/baseline ≤ 1.00` inclusive; `>1.00` DENY |
| Owner (+ Admin Owner-equivalent) with `OverrideSalePriceUnlimited` | Unlimited positive unit prices |
| OrganizationAdministrator alone | Not unlimited; no override without Owner POS role |
| Platform Admin alone | No |
| `requested ≤ 0` | DENY (free = commercial discount B03) |
| Reason | Required (non-whitespace) |
| Experience ≠ authority | No UI-only grants |

Override changes transaction `UnitPrice` only. Order: baseline → override → checkout → B03 discount on GrossLineTotal.

## Historical OWNER-CONFIRMED CHANGE (SUPERSEDED)

The earlier Fixed / optional CashierAdjustable / min-max product policy draft is **SUPERSEDED** by the locked role matrix above. Do not implement per-product CashierAdjustable for RMAP-B01.

Classification retained for audit trail only:

- ~~`POS_SALE_PRICE_POLICY_CONTRACT_MISSING`~~ → superseded by capability matrix  
- ~~`POS_CASHIER_PRICE_OVERRIDE_CONTRACT_MISSING`~~ → backend delivered; Cashier remains DENY  
- `POS_PRICE_HISTORY_AUDIT_UNRESOLVED` (catalog history table still optional; sale override audit rows exist)

## Authorization (current)

Today’s Prices / catalog price changes require catalog management roles. Per-sale override requires `store-sales-override-price` (+ unlimited feature when above manager ceiling).

## Offline

Sale payloads snapshot unit price; override intents on trusted offline snapshots **fail closed**. Status: **PROVEN_CURRENT**.

## React

No override UI yet (RMAP-12b). Cart uses catalog prices; override is backend-gated.
