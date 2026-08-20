# Dependency Graph

Derived from current contracts + owner-confirmed gaps. Do not place a capability downstream until prerequisites are clear.

## Identity and access

```text
PlatformUser (CURRENT principal; no separate UserIdentity table)
  → Account Profile (AccountClass)
    → Session
      → Organization Context (owners may switch; CURRENT staff locked to HomeOrganizationId)
        → Membership
          → Subscription / Entitlement / Product Access
            → ProductLocalRoleGrant
              → POS operational authorization

OWNER_CONFIRMED_CHANGE (RMAP-B00):
Verified Person / Human
  → Personal Account
  → Org memberships (A, B, …) each with org-scoped login alias
  → optional POS roles per org/product
```

Do **not** implement React desired staff person-link UX before RMAP-B00.

## Organization operations

```text
Organization
  → Business Type(s)
  → Branch (main + additional)
      → Address / Coordinates / Hours
      → Device registration
      → Fulfillment settings
          → Fulfillment readiness
              → Pickup / Delivery
                  → Customer Ordering
  → Staff invitations → (CURRENT: separate staff PlatformUser) / (DESIRED: same-human membership + alias via RMAP-B00)
      → optional POS roles
  → Compliance / sales-document capability (Platform)
```

## Catalog to money

```text
Global Catalog / Template (optional)
  → Merchant Local Product
      → Base UOM + SellingMode
      → CatalogProductUnit (Purchase/Sell, MultiplierToBase)
      → Pricing (product + sell-unit prices; Today’s Prices)
      → InventoryAccount (default untracked → enable → movements/lots)
      → Sell Floor
          → Cart (entered unit/qty)
              → Shift + Device gates
                  → Checkout / Sale (snapshots + base stock effect)
                      → Returns / Void
                      → Reporting
```

## Supplier commerce

```text
Manual Supplier
  → Purchase Order
      → Goods Receipt
          → Inventory movement (only here for PO path)

Connected Organization Supplier
  → Connection (Pending→Active)
      → Exposure eligibility (EXPOSABLE)
          → Per-buyer share (SHARED) + buyer price
              → Linked product adoption (+ conversion metadata)
                  → Connected PO lifecycle
                      → Fulfillment signals
                          → Buyer Goods Receipt
                              → Inventory movement
```

**Invariant:** expose/share/price/connection/PO-accept never mutate buyer on-hand.

## React migration layering

```text
RMAP-00 Shared UI/UX foundation
  → visual WPs (lists/forms/sell/admin) depend on RMAP-00

RMAP-01 Account/session (Personal + CURRENT auth mechanics)
  → RMAP-02 Workspace/roles
    → RMAP-03 Branch/device
      → catalog → UOM/units → pricing → inventory
        → sell/cart → shift → checkout → …

RMAP-B00 Staff person-link backend
  → RMAP-01b React staff identity (desired)
  (must not be skipped by claiming CURRENT duplicate-human staff as desired parity)

RMAP-B01 Sale price policy backend
  → RMAP-12b override UI
```

## Backend-gap inserts

```text
BACKEND DOMAIN PACKAGE
  → BACKEND TEST/API PACKAGE
    → MAUI compatibility/regression (if UX already assumes behavior)
      → REACT PACKAGE
```

Examples: RMAP-B00 before RMAP-01b; RMAP-B01 before RMAP-12b.

## Execution protocol

Approved batches execute per [master-run-execution-protocol.md](master-run-execution-protocol.md) (10-WP batches, per-WP push, hard stops, review between batches).
