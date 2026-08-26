# Mobile Offline Boundary

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-04

## Policy

Do **not** automatically inherit the PinoyBusinessPOS offline system. Offline support is a deliberate PinoyServicePro product decision.

Reuse of shared technical offline primitives may be evaluated later behind approved shared boundaries.

## Capability classification (planning)

| Capability group | Possible future class |
|---|---|
| Booking lookup | offline readable / online required — **not decided** |
| Booking create/update | **online required** (safe default) |
| Customer lookup | offline readable candidate — **not decided** |
| Walk-in creation | **not decided** (conflict risk) |
| Job / work-order operations | **not decided** |
| Payments | **online required** (safe default) |
| Parts/material changes | **online required** or queued — **not decided** |
| Estimates | **not decided** |
| Asset history | offline readable candidate — **not decided** |
| Staff/resource scheduling | **online required** for mutations (safe default) |

## Explicit PSP-00 exclusions

No offline storage, queues, sync, or device grants are implemented.
