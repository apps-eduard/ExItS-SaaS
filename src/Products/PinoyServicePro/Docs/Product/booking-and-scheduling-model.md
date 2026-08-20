# Booking and Scheduling Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decisions:** PSP-D-00-05, PSP-D-00-13, PSP-D-00-20, PSP-D-00-12, PSP-D-00-04

## First-class capability

Booking is a first-class capability. Booking is **not** the same thing as a completed service transaction.

Some organizations may primarily use walk-ins; booking remains available as a capability, not a mandatory daily path for every template.

## Planning information a booking may contain

- Customer
- Service (ServiceOffering)
- Staff / Resource
- Branch / Location
- Start date/time
- Expected duration
- Status
- Notes

## Lifecycle vocabulary (candidates — not implemented contracts)

| Candidate status | Notes |
|---|---|
| Pending | Unconfirmed request |
| Confirmed | Accepted appointment |
| CheckedIn | Customer arrived |
| InProgress | Service underway (may instead live on Job) |
| Completed | Booking path finished (do not confuse with paid job) |
| Cancelled | Cancelled before/at service |
| NoShow | Customer did not arrive |

Unresolved semantics (who may transition, whether Completed requires payment, whether InProgress belongs on Booking vs Job): mark as product-policy candidates — **PSP-D-00-20** and related workflow WPs. Do not treat these names as API contracts.

## Scheduling correctness (future concerns)

Document for later design; concurrency must be **server-authoritative**:

- staff working hours
- resource availability
- service duration
- buffer time
- blocked time
- leave
- branch hours
- concurrent booking conflicts
- rescheduling
- cancellation
- no-show
- walk-in insertion
- timezone policy

Do not rely only on UI slot availability.

**Safe default until PSP-D-00-20:** deny overlapping exclusive staff/resource bookings.

## Customer-facing booking (future)

Potential flow (not assumed in early MVP — PSP-D-00-05):

```text
Customer → Select business/branch → Select service → Select staff or "Any"
→ Available slots → Book → Confirmation
```

Concerns to record: identity, abuse prevention, availability consistency, overbooking. **No anonymous public booking** until PSP-D-00-13 is decided (safe default: none).

## Offline

Booking create/update conflict sensitivity → treat as **online required** until PSP-D-00-04 decides otherwise. See [../Architecture/mobile-offline-boundary.md](../Architecture/mobile-offline-boundary.md).
