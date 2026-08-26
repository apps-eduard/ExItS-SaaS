# Business Template and Capability Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decisions:** PSP-D-00-10, PSP-D-00-11, PSP-D-00-08

## Principle

```text
One Product
+ Stable Core Domain
+ Capabilities
+ Business Templates
+ Configurable Terminology
= Different Service-Business Experiences
```

Do **not** create separate products such as PinoyBarber, PinoySalon, or PinoyMechanic. Do **not** design customer-specific source forks.

## What templates may configure

- Enabled capabilities
- Terminology (e. for example “Barber” vs “Technician” as labels)
- Required vs optional fields
- Defaults
- Workflow variants **explicitly supported** by the core domain
- Navigation visibility
- Business-specific presentation

## What templates must not do

- Dynamically generate arbitrary database schemas
- Use Entity/Field/Value EAV as the primary operational model
- Weaken authorization, tenant isolation, audit, money integrity, or server-authoritative rules
- Bypass product-local grants via configuration

## Capability candidates (planning)

| Capability | Notes |
|---|---|
| Booking | First-class; some orgs may prefer walk-ins |
| Walk-in | Unscheduled intake |
| Staff / resource assignment | People and optional resources |
| CustomerAsset | Optional (vehicle, appliance, device, custom) |
| Estimate / quote | Optional; repair-heavy |
| Parts / materials | Optional inventory/consumption |
| Labor tracking | Optional depth |
| Commission | Optional (PSP-D-00-08) |
| Warranty / follow-up | Potential; not finalized |
| Customer-facing booking | Future (PSP-D-00-05) |

## Initial template families (configuration, not products)

| Template family | Intent |
|---|---|
| Barber Shop | Booking + walk-in; staff assignment; assets/estimates usually off |
| Hair Salon | Similar to barber; packages/treatments terminology |
| Spa / Massage | Duration and resource/room important |
| Auto Repair | Assets (vehicle), estimates, parts, full work orders |
| Motorcycle Repair | Similar to auto with asset subtype |
| Appliance Repair | Asset = appliance |
| Computer / Electronics Repair | Asset = device |
| Cleaning Service | Scheduling + optional supplies |
| Tailoring / Alteration | Job lines; limited assets |
| Field Technician / Contractor | Branch/field assignment concerns |
| General Service Business | Balanced defaults |
| Custom Service Business | Owner-configured capability set |

Exact template catalog membership and defaults: refine in PSP-12; not frozen as contracts in PSP-00.

## Reference configurations

### Barber Shop (example)

| Concern | Direction |
|---|---|
| Booking | Enabled |
| Walk-in | Enabled |
| Staff Assignment | Enabled |
| Customer Asset | Disabled |
| Estimate | Usually disabled |
| Parts/Materials | Optional (consumables) |
| Commission | Optional |
| Service Duration | Important |
| Work Order complexity | Simple |

### Auto Repair (example)

| Concern | Direction |
|---|---|
| Booking | Enabled |
| Walk-in | Enabled |
| Customer Asset | Enabled (Vehicle) |
| Estimate | Enabled |
| Parts/Materials | Enabled |
| Staff Assignment | Enabled |
| Labor tracking | Enabled |
| Work Order | Full |
| Warranty | Potential capability |

## Sanity-check templates

Hair Salon, Appliance/Computer Repair, and Cleaning Service should be expressible without forking the core domain. If a proposed core rule only works for mechanics **or** only for retail POS patterns, reject or capability-gate it.

## Validation goal (PSP-00)

Prove conceptually that the same product can support at least **Barber Shop** and **Auto Repair / Mechanic** without being too retail-specific, too mechanic-specific, or too generic to enforce real business rules.
