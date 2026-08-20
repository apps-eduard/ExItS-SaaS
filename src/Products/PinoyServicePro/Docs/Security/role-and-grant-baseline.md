# Role and Grant Baseline

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-18

## Role presets (planning)

| Preset | Purpose |
|---|---|
| Owner | Organization-level ServicePro administration |
| Manager | Service operations and supervision |
| Front Desk / Reception | Intake, booking, check-in |
| Service Provider / Technician | Assigned service execution |
| Cashier | Operational payment capture |

Presets must be backed by **explicit product-local grants**. Do not hard-code authorization to role names. Do not implement implicit role hierarchy. Template labels (Barber, Mechanic) are not grant codes.

## Grant areas (intent — codes open)

customers, bookings, services, jobs/work-orders, estimates, assets, materials/parts, payments, reports, staff assignments, configuration, audit.

Exact grant codes: **Open / Product Owner Decision Required** (PSP-D-00-18).

## Separation reminders

- Platform product entitlement ≠ operational permission
- Platform Admin ≠ ServicePro operator
- POS/Loan roles do not authorize ServicePro
