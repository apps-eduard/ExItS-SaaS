# Dependency Graph

Derived from current contracts. Do not place a capability downstream until prerequisites are clear.

## Identity and access

```text
Identity / Credential
  → Account Profile (AccountClass)
    → Session
      → Organization Context (owners may switch; staff locked)
        → Membership
          → Subscription / Entitlement / Product Access
            → ProductLocalRoleGrant
              → POS operational authorization
```

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
  → Staff invitations → org-scoped staff identities → optional POS roles
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
FOUNDATION PARITY
  account/session/org/product access
    → organization/branch/device context
      → catalog read + product admin
        → UOM/selling mode/units
          → pricing (Today’s Prices)
            → inventory
              → sell floor/cart
                → register/shift
                  → checkout/sale
                    → customers/utang
                      → returns
                        → suppliers → connected → purchasing
                          → ordering/delivery
                            → reports
                              → offline/local-first
                                → hardening/E2E
```

## Backend-gap inserts

When contract missing (e.g. sale price policy):

```text
BACKEND DOMAIN PACKAGE
  → BACKEND TEST/API PACKAGE
    → MAUI compatibility/regression (if UX already assumes behavior)
      → REACT PACKAGE
```

Do not schedule React override UI before UD-02 backend exists.
