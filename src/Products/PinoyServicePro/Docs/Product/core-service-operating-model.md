# Core Service Operating Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## Conceptual domain (planning names — not implemented contracts)

| Concept | Role |
|---|---|
| Customer | Service-business customer of the organization |
| ServiceOffering | Sellable/bookable service definition |
| Booking | Planned appointment (≠ completed transaction) |
| ServiceJob / WorkOrder | Execution unit for service work |
| ServiceJobLine | Line within a job (service/labor/part/material) |
| Staff / ServiceProvider assignment | Who performs or owns the work |
| CustomerAsset | Optional subject of work (vehicle/device/…) |
| Estimate / Quote | Optional priced proposal |
| Material / Part | Optional consumable or component |
| Payment | Operational customer payment |
| ServiceHistory | Durable historical view |
| OperationalAudit | Sensitive/action audit trail |

Names and exact aggregates remain planning concepts until authorized work packages implement them. Do not fabricate database tables from this list alone.

## Canonical operating flow

```text
Booking
    ↓
Check-in / Arrival
    ↓
Service Job / Work Order
    ↓
Service execution
    ↓
Completion
    ↓
Payment
    ↓
Service History
```

Walk-in may enter at check-in / job creation without a prior booking. Estimate/approval may precede job creation for repair businesses.

## Barber workflow reference (use-case — not hard-coded architecture)

```text
Customer
    ↓
Booking OR Walk-in
    ↓
Choose service
    ↓
Choose/assign barber
    ↓
Perform service
    ↓
Complete
    ↓
Payment
    ↓
Customer service history
```

Example services (terminology only): Haircut, Shave, Hair coloring, Hair treatment, Package.

Do **not** hard-code barber terminology into the core domain.

## Mechanic workflow reference (same product)

```text
Customer
    ↓
Vehicle (CustomerAsset)
    ↓
Booking / Walk-in intake
    ↓
Inspection / diagnosis
    ↓
Estimate
    ↓
Customer approval
    ↓
Work Order
       ├── Labor
       └── Parts / materials
    ↓
Completion
    ↓
Payment
    ↓
Vehicle/service history
```

## Branch scope

Organization and branch scope are planned from the beginning. Single-branch organizations use one default branch. Bookings, jobs, staff/resource assignments, operational money, and reports must eventually have explicit scope rules (PSP-D-00-12).

## Money ownership reminder

Operational charges and payments belong to PinoyServicePro. Platform SaaS billing remains independent.
