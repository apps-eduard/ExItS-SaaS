# Organization Branches and Fulfillment Locations

An `OrganizationBranch` is an organization-owned operating location. It remains in the Platform database and is referenced by product operational records only by identifier; products must not query the Platform database directly.

## Location model

- Structured postal address: two address lines, city/municipality, region, postal code, and country code.
- Optional WGS84 latitude and longitude.
- `PickupEnabled` indicates that customers may collect orders at the branch.
- `DeliveryEnabled` indicates operator intent to offer local delivery.
- Effective pickup additionally requires an Active branch.
- Effective delivery additionally requires an Active branch, valid coordinates, and a delivery policy.

Coordinates identify the fulfillment origin. They are not a customer address and must not be inferred from free-form address text.

## Management surfaces

MAUI provides a dense branch list and a progressive editor for details, address, coordinates, fulfillment modes, and delivery settings. Organization Web exposes the same core fields in a responsive, desktop-dense layout.

Branch capacity remains entitlement-controlled. Primary branches cannot be treated as disposable, and archived branches cannot fulfill new orders.

## Boundaries

Customer ordering (Phase 28 Stage B) consumes branch fulfillment capabilities and delivery policy. Storefront catalog UX, couriers, routing, and payment rails remain separate work. POS remains free of PHI and no cross-product database access or foreign keys are introduced.
