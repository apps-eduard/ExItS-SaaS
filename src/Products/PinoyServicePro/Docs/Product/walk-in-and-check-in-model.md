# Walk-in and Check-in Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## Walk-in

Walk-in is unscheduled intake: customer arrives without a prior booking (or booking is created at arrival). Capability-driven — templates that are appointment-heavy may still enable walk-ins for overflow.

Conceptual outcomes of walk-in:

- create/select Customer
- select ServiceOffering
- assign Staff/Resource if required
- create Booking (optional immediate) and/or ServiceJob
- proceed to execution

## Check-in / arrival

Check-in marks that a booked (or walk-in) customer has arrived and is ready for service. It sits between Booking and Job execution in the canonical flow.

Check-in must not by itself imply payment completion.

## Relationship to booking statuses

Candidate statuses such as `CheckedIn` / `NoShow` are policy candidates (see booking model). Exact mutual exclusivity with Job statuses remains open.

## Branch and queue concerns

Front-desk queues, waiting lists, and priority insertion are presentation/workflow variants over core concepts — not separate products. Cross-branch walk-in routing is open (PSP-D-00-12).
