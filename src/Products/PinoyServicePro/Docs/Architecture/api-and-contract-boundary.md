# API and Contract Boundary

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## Intent

PinoyServicePro owns its API surface when authorized. API DTOs must not expose EF entities. Server-authoritative validation for booking conflicts, money, and authorization.

## Contracts

| Boundary | Direction | Notes |
|---|---|---|
| Platform commercial / identity | Consume | Approved contract only; D-P12-03 open; R-091 open |
| Other products (POS, Loan) | None | No DB; no project reference |
| Public/anonymous booking | Future | PSP-D-00-13 — safe default none |

## Non-goals in PSP-00

- Controllers, endpoints, OpenAPI artifacts
- Shared mega-domain libraries with POS/Loan
