# Service Catalog and Pricing Baseline

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## ServiceOffering

PinoyServicePro owns its service catalog. Do **not** automatically reuse PinoyBusinessPOS catalog/inventory entities or project references.

A ServiceOffering may include (planning):

- Name / description
- Duration guidance
- Price / price components (decimal money)
- Eligibility for booking
- Template visibility
- Optional package composition

Exact pricing rules, tax treatment, and package composition: open (related to PSP-D-00-16 for tax).

## Separation

| Concept | Meaning |
|---|---|
| Service Offering | What can be sold/booked |
| Labor / service performed | Work done on a job |
| Material / consumable | Consumed supplies |
| Part / physical component | Installed/replaced component |

## Money

Prices and charges use decimal monetary concepts. Authoritative totals are server-calculated when implemented.
