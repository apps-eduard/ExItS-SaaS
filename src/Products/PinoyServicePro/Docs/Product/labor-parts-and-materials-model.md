# Labor, Parts, and Materials Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-11

## Conceptual separation

| Concept | Description |
|---|---|
| Service Offering | Catalog definition |
| Labor / service performed | Work effort on a job |
| Material / consumable | Supplies consumed |
| Part / physical component | Component installed or replaced |

## Inventory boundary (optional capability)

| Business | Direction |
|---|---|
| Barber | Consumables optional |
| Mechanic | Parts inventory may be important |
| Cleaning | Supplies may be tracked |
| Consulting-like service | Inventory completely disabled |

Do **not** assume the complete PinoyBusinessPOS inventory model should be copied. Integration/reuse of technical primitives may be evaluated later behind approved shared boundaries — **no POS project reference authorized**.

**Safe default (PSP-D-00-11):** track job-line consumption; defer full stock engine.

## Job lines

ServiceJobLines may represent labor, service, part, or material lines with decimal amounts. Exact cost/price posting rules remain open.
