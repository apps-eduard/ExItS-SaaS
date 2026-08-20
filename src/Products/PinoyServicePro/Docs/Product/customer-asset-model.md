# Customer Asset Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-10

## Capability

`CustomerAsset` is an **optional** capability. One core concept serves multiple industries; templates configure type emphasis and field requirements.

| Business | Typical asset | Capability |
|---|---|---|
| Mechanic | Vehicle | Enabled |
| Appliance shop | Appliance | Enabled |
| Computer repair | Computer / Device | Enabled |
| Electronics | Device | Enabled |
| Other services | Custom Asset | Optional |
| Barber | — | Disabled |

Do **not** build a separate entity model for every industry.

## Core idea

An asset is the subject of service work for a Customer within an Organization. Jobs, estimates, and history may reference it when the capability is enabled.

Exact asset fields and subtype strategy: **Open / Product Owner Decision Required** (PSP-D-00-10). Safe default: capability off for templates that do not need assets; prefer generic attributes + template-required fields over EAV.

## Non-goals

- PHI / medical device clinical records under generic asset notes
- Cross-product vehicle registries
- Unlimited custom field engines as primary storage
