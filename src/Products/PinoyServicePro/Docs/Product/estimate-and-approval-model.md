# Estimate and Approval Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decisions:** PSP-D-00-06, estimate amendment/expiry policy (record under PSP-D-00-15 / future)

## Capability

Estimates / quotations are **optional**. Mechanic and repair businesses will likely rely on them. Barber shops may disable them.

## Typical flow

```text
Draft Estimate
    ↓
Presented
    ↓
Customer accepts / rejects
    ↓
Accepted estimate can seed Service Job / Work Order
```

## Open policy areas

- Amendment after presentation
- Expiry
- Partial acceptance
- Price lock vs re-estimate
- Deposit required on acceptance (PSP-D-00-06)

Safe default: estimates capability off until template enables it; accepted estimates snapshot terms conceptually (no silent post-accept edits without revision rules).

## Authorization

Estimate actions require `estimates` grant area intent. Acceptance is a workflow transition, not a UI-only flag.
