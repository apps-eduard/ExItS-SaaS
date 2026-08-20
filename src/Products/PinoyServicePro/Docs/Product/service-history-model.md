# Service History Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-17

## Purpose

Durable service history is an important product capability for returning customers, asset follow-up, and operational accountability.

## Potential history contents

- Bookings
- Completed services
- Jobs / work orders
- Assets
- Estimates
- Payments
- Notes
- Warranty / service follow-up where enabled

## Rules

- Respect tenant isolation and authorization
- Do not define unlimited retention without an explicit decision (PSP-D-00-17)
- Safe default: retain while organization is subscribed; no deletion-without-policy
- History must not claim legal/tax sufficiency

## Views

Staff history views are grant-scoped. Customer-facing history (Personal) is future and presentation-only if authorized.
