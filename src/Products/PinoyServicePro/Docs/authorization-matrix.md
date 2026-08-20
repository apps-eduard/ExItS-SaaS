# PinoyServicePro — Authorization Matrix

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Platform access ≠ product operational permission.

| Field | Value |
|---|---|
| Product | PinoyServicePro / `pinoy-service-pro` (proposed) |
| Status | Draft — presets and grant **intent** only; identifiers open (PSP-D-00-18) |
| Last updated | 2026-08-20 |
| Implementation present | No |

## Layers

```text
Trusted actor → org context → Platform product access → commercial state → entitlements
  → product-local role → product-local grant → resource / workflow rules
```

**DECISION D-P12-03:** how commercial state reaches the product is unresolved — provisional Dev/Testing patterns only; do not invent a final Platform transport.

Provisional commercial approach for this product: consume Platform commercial facts through an approved contract when available; until then, any Dev/Testing commercial headers/gates are **provisional** and must fail closed outside approved environments. Platform entitlement does **not** equal operational permission.

## Platform vs product

| Concern | Platform | Product |
|---|---|---|
| System / org admin roles | Yes | Do not grant ServicePro ops by implication |
| Product access assignment | Yes | Consumed only |
| Subscription / entitlement | Yes | Enforced |
| Operational roles / grants | No | **Yes — authoritative** |
| Business template configuration | No | Product-local (capability config ≠ grant) |

## Product role presets (planning)

| Role code (planning label) | Display name | Purpose |
|---|---|---|
| *Open* (PSP-D-00-18) | Owner | Organization-level ServicePro administration |
| *Open* | Manager | Service operations and supervision |
| *Open* | Front Desk / Reception | Intake, booking, check-in |
| *Open* | Service Provider / Technician | Assigned service execution |
| *Open* | Cashier | Operational payment capture |

Do **not** hard-code authorization to display names. Template labels such as “Barber” or “Mechanic” are terminology over service-provider concepts — not authorization keys.

## Grant areas (intent — identifiers open)

| Grant area (planning) | Description |
|---|---|
| customers | Create/view/update customer records |
| bookings | Create/view/update/cancel bookings |
| services | Manage service offerings / catalog |
| jobs | Create/view/update service jobs / work orders |
| estimates | Draft/present/accept/reject estimates |
| assets | Manage customer assets where capability enabled |
| materials | Parts/materials adjustments where capability enabled |
| payments | Capture/void/refund within policy |
| reports | Operational reports |
| staff_assignments | Assign staff/resources to bookings/jobs |
| configuration | Business template, branch, capability settings |
| audit | View operational audit / history |

Exact grant codes: **Open / Product Owner Decision Required** (PSP-D-00-18).

## Matrix (intent — not implemented)

| Role preset | Typical grant areas | Resource | Action | Org scope | Concealment | Commercial state | Special rules |
|---|---|---|---|---|---|---|---|
| Owner | All areas (planning) | Org ServicePro | Admin | own-org | 404 | Allowed + entitlement | Last-owner/bootstrap open |
| Manager | Most operational | Customers, bookings, jobs, estimates, reports | Operate / supervise | own-org / branch | 404 | Allowed + entitlement | High-risk config limited |
| Front Desk | customers, bookings, walk-in | Bookings, customers | Intake | branch | 404 | Allowed + entitlement | Limited payment/job mutation |
| Service Provider | assigned jobs | Assigned jobs/assets | Execute | assigned | 404 | Allowed + entitlement | No org-wide config |
| Cashier | payments | Payments | Capture | branch | 404 | Allowed + entitlement | Refund policy open (PSP-D-00-19) |

## Continuity / denied commercial states

| Commercial state | Operational effect |
|---|---|
| Active / entitled | Allow when product-local auth also passes |
| Suspended / expired / missing entitlement | Deny operational mutations; exact read-only policy open |
| Unknown / untransported (D-P12-03) | Fail closed outside provisional Dev/Testing |

## Ownership and workflow rules

- Capability-disabled features remain unauthorized even if UI terminology suggests otherwise.
- Resource assignment scope (own vs branch vs org) must be explicit.
- Customer-facing booking, if ever authorized, is a separate authorization surface (PSP-D-00-05, PSP-D-00-13).
- Last-owner / bootstrap rules: **Open / Product Owner Decision Required**.

## Explicit non-grants

- Platform Administrator does **not** automatically receive unrestricted ServicePro operational access
- PinoyBusinessPOS or PinoyLoanManager roles do **not** authorize ServicePro operations
- Template labels (“Barber”, “Mechanic”) are **not** grant codes
- Platform product entitlement alone is **not** an operational grant

Detail: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md).
