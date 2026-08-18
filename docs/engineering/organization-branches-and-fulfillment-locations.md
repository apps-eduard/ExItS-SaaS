# Organization Branches and Fulfillment Locations

An `OrganizationBranch` is an organization-owned operating location. It remains in the Platform database and is referenced by product operational records only by identifier; products must not query the Platform database directly.

## Location model

- Structured postal address: two address lines, city/municipality, region, postal code, and country code.
- Optional WGS84 latitude and longitude.
- `PickupEnabled` indicates operator intent to offer customer pickup (requires `CustomerOrderingEnabled`).
- `DeliveryEnabled` indicates operator intent to offer local delivery (default **off**; requires readiness + explicit enablement).
- `CustomerOrderingEnabled` is opt-in online ordering for the branch.
- `OnlineOrdersPaused` is a merchant override that blocks new online orders without affecting walk-in POS or in-flight orders.
- Branch operating hours (Mon–Sun) and optional branch timezone override support server-authoritative open/closed evaluation.

Effective pickup requires Active branch, customer ordering enabled, readiness, and operational hours (when configured).

Effective delivery requires Active branch, customer ordering enabled, delivery enabled, readiness (address, hours, phone, coordinates, complete delivery policy, delivery entitlement), and operational hours.

Coordinates identify the fulfillment origin. They are not a customer address and must not be inferred from free-form address text.

## Fulfillment readiness (P28-WP11)

Server evaluator separates entitlement (`CanUse*`), merchant intent (`*Enabled`), setup completeness (`*Ready`), and live operability (`*Operational`). Enablement APIs reject incomplete setup. See [P28-WP11 report](../reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md).

## Management surfaces

MAUI provides a dense branch list and a progressive editor for details, address, coordinates, operating hours, fulfillment activation (readiness-gated), and delivery settings. Organization Web exposes the same core fields in a responsive, desktop-dense layout with a dedicated branch edit page.

Branch capacity remains entitlement-controlled. Primary branches cannot be treated as disposable, and archived branches cannot fulfill new orders.

## Boundaries

Customer ordering (Phase 28 Stage B) consumes branch fulfillment capabilities and delivery policy. Personal linked-merchant storefront/cart UX is delivered for authenticated active links (`CustomerOrder`); courier marketplace, routing, and customer-order payment rails remain separate residuals. POS remains free of PHI and no cross-product database access or foreign keys are introduced.
