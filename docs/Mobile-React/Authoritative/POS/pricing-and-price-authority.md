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
| First-class sale-line price override model | PROVEN_MISSING | Checkout uses catalog/unit price |
| `SalePricePolicy` / `CashierAdjustable` | PROVEN_MISSING | No domain types |

## Distinction required by owner

| Concept | Meaning | CURRENT |
|---------|---------|---------|
| **Change current selling price** | Updates catalog/unit price for future transactions | PROVEN_CURRENT (Today’s Prices / product edit) |
| **Sale-line price override** | Exceptional per-sale price | PROVEN_MISSING |

## OWNER-CONFIRMED CHANGE (desired future policy)

- Not every product allows Cashier override
- Owner controls product sale-price policy
- Default **Fixed**
- Optional **CashierAdjustable**
- Optional min price / max discount
- Override reason
- Future Manager approval threshold
- Audit original catalog price vs applied price and actor
- No UI-only price authority

Classification: **OWNER_CONFIRMED_CHANGE** + **PROVEN_MISSING** contracts:

- `POS_SALE_PRICE_POLICY_CONTRACT_MISSING`
- `POS_CASHIER_PRICE_OVERRIDE_CONTRACT_MISSING`
- `POS_PRICE_HISTORY_AUDIT_UNRESOLVED` (no dedicated history table; sale snapshots exist)

## Authorization (current)

Today’s Prices / catalog price changes require catalog management roles (Owner/Manager class). Cashier sell path consumes prices; does not own catalog price admin in MAUI defaults.

## Offline

Sale payloads snapshot unit price; server recomputes/validates per checkout rules. Status: **PROVEN_CURRENT** for snapshot fidelity on cash offline sales.

## React

No Today’s Prices UI; cart uses fetched catalog prices in memory; checkout disabled. Status: **MISSING** for price admin; **PARTIAL** for display price on tiles.
