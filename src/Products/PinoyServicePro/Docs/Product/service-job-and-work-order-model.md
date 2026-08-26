# Service Job and Work Order Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## Concept

`ServiceJob` / `WorkOrder` is the execution unit for service work. Complexity ranges from simple (barber haircut) to full repair orders (auto) using the **same** product concepts, with capabilities controlling depth.

Planning names are not implemented contracts.

## Typical contents

- Customer reference
- Optional CustomerAsset
- Origin: Booking and/or Walk-in
- Assigned staff/resources
- Branch scope
- Status / lifecycle (open until product policy)
- ServiceJobLines (service, labor, parts, materials)
- Links to Estimate when seeded from an accepted quote
- Notes / inspection findings (non-PHI)

## Complexity variants (capability / template)

| Variant | Example | Notes |
|---|---|---|
| Simple | Barber service | Few lines; short duration |
| Full | Auto repair | Labor + parts; estimate linkage; longer lifecycle |

## Separation from booking and payment

- Booking plans time/resources.
- Job executes work.
- Payment settles operational charges.

A completed booking status must not silently invent payment or history completeness without explicit rules.

## Authorization

Job mutations require product-local grants (`jobs` area intent) plus resource/assignment scope. Service providers typically see assigned jobs only.
