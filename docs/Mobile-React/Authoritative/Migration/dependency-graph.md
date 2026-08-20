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
Visual WPs depend on RMAP-00 (UI foundation).

Post-B00 identity path (Master Run 01 execution order):
RMAP-00
  → RMAP-B00 (staff person-link backend; outcome fixed, schema design in-package)
    → RMAP-01 (account/session; validate post-B00)
      → RMAP-01b (React staff identity desired)
        → RMAP-02 (workspace/roles against post-B00)
          → RMAP-03 (branch/device)
            → RMAP-04 → RMAP-05 → RMAP-06 → RMAP-07
              → (Master Run 01 HARD STOP; RMAP-08+ later)

RMAP-B01 Sale price policy backend
  → RMAP-12b override UI
```

This records the **approved first master-run execution order** to prevent known rework. It does not claim UI and identity backend are inherently coupled as a domain rule.

Do **not** skip RMAP-B00 by claiming CURRENT duplicate-staff-principal as desired parity.

## Backend-gap inserts

```text
BACKEND DOMAIN PACKAGE
  → BACKEND TEST/API PACKAGE
    → MAUI compatibility/regression (if UX already assumes behavior)
      → REACT PACKAGE
```

Examples: RMAP-B00 before RMAP-01/01b/02 in Master Run 01; RMAP-B01 before RMAP-12b.

## Execution protocol

Approved batches execute per [master-run-execution-protocol.md](master-run-execution-protocol.md).

**Approved proposed Master Run 01:** see [react-migration-roadmap.md](react-migration-roadmap.md) § APPROVED PROPOSED MASTER RUN 01.