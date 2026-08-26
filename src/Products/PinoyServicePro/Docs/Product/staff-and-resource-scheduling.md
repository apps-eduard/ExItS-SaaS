# Staff and Resource Scheduling

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decisions:** PSP-D-00-09, PSP-D-00-08, PSP-D-00-12, PSP-D-00-20

## Concepts

Potential planning concepts:

- Service Provider (person performing work)
- Team
- Bay / Chair / Room / Resource

Template labels such as Barber, Stylist, Mechanic, Technician are **terminology**, not authorization keys.

## Assignment

Service work may be assigned to people and optionally to resources. Bookings and jobs should record assignment when required by template.

Core authorization must **not** depend on UI terminology.

## Scheduling inputs

Working hours, leave, resource availability, concurrent capacity, and branch hours feed the scheduling engine (future). Conflicts are server-authoritative (PSP-D-00-20).

Cross-branch resource sharing: **Open** (PSP-D-00-12); safe default = no implicit sharing.

## Commission

Optional capability (PSP-D-00-08). Safe default: off. Final formulas not authorized.
