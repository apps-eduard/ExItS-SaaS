# Product, Subscription, and Entitlement Lifecycle

## Platform commercial spine

```text
Platform Product definition
  → Plans / commercial offerings
    → Organization subscription
      → Entitlements / feature codes / capacity
        → Product access assignment
          → ProductLocalRoleGrant (Owner|Manager|Cashier|Viewer)
            → POS role mapping + operational authorization
```

Status: **PROVEN_CURRENT** for the PinoyBusinessPOS path used by Start a Business / Explore POS.

## Key Platform concepts

| Concept | Role | Evidence |
|---------|------|----------|
| Platform product catalog | Defines sellable SaaS products | Platform Products domain |
| Commercial plans | Explore / trial / subscribe offerings | `GET /api/v1/commercial/plans`, `CommercialEndpoints.cs` |
| Subscriptions | Org commercial state | `/api/v1/platform/subscriptions`, org subscription routes |
| Entitlements / feature overrides | Capability gates (including capacity) | Entitlement endpoints + domain |
| Product access assignment | Org may use product | Platform authorization model |
| `ProductLocalRoleGrant` | Product-local role codes separate from org membership | `ProductLocalRoleGrant.cs` |

## Product-local roles vs organization membership

| Layer | Codes | Meaning |
|-------|-------|---------|
| Organization membership | `OrganizationOwner`, `OrganizationAdministrator`, `OrganizationMember` | Org governance |
| Product-local (Platform) | `Owner`, `Manager`, `Cashier`, `Viewer` | Product grant |
| POS mapped | `Owner`, `StoreManager`, `Cashier`, `ReportingUser` | POS authorization |

Mapping evidence: `ProductLocalRoleGrant.MapToPosRoleCode`.

**Invariant:** Organization Owner membership does **not** by itself authorize POS checkout. Start a Business may explicitly grant POS Owner; staff invites may grant a product role optionally.

## Capacity / branches / devices (entitlement-adjacent)

Phase 22 capabilities (branches, registered POS devices, offline grant/device binding) are Platform-owned configuration with POS consumption of session branch/device ids.

| Capability | Authority | Status |
|------------|-----------|--------|
| Branch capacity / multi-branch | Platform org branches + entitlements | PROVEN_CURRENT |
| POS device registration / revoke | Platform `pos-devices` APIs | PROVEN_CURRENT |
| Offline operating grant / PIN | POS LocalStore + Platform device binding | PROVEN_CURRENT (MAUI) |

## Personal feature entitlements / rewards / ads

Platform Personal feature definitions, rewards, and related abstractions exist from Phase 24 work. Treat as Platform Personal surface — not POS catalog.

Status: **PROVEN_PARTIAL** for React (not in React client); **PROVEN_CURRENT** in Platform where migrations/APIs exist.

## Global Catalog / Templates → org-local products

| Step | Authority | Status |
|------|-----------|--------|
| Platform Global Catalog + Business Types + Templates | Platform | PROVEN_CURRENT |
| Import into org POS catalog | POS `/api/v1/pos/catalog-imports` | PROVEN_CURRENT |
| Local product ownership after import | POS `CatalogProduct` | PROVEN_CURRENT |

Evidence: Platform GlobalCatalog application, POS `CatalogImportUseCases`, P20 reports.

## Business types

| Topic | Current | Status |
|-------|---------|--------|
| Organization business type(s) | Platform org configuration; multiple/effective types (P23) | PROVEN_CURRENT |
| Effect on inventory engine | Templates may **suggest** defaults; do **not** fork inventory engines | PROVEN_CURRENT — `docs/engineering/product-units-and-inventory-behavior.md` |

## React implications

Before any POS operational React WP:

1. Prove session can resolve product access for PinoyBusinessPOS.
2. Prove local role is available to the client for route guards (`RequireCreateSale` already exists in React).
3. Do not encode commercial subscription UI as a substitute for product-role authorization.
