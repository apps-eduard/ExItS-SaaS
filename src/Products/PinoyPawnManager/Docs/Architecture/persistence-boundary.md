# Pinoy Pawn Manager — Persistence Boundary

> Architecture index: [README.md](README.md)  
> Parent overview: [../architecture.md](../architecture.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |

## Intent

PPM operational data lives in a **separate logical database** owned by the product. Portfolio isolation rules apply: no cross-product database access, no cross-database foreign keys, no EF navigation into Platform/POS/PLM/BNPL entities.

## Proposed database identity (Open)

| Field | Proposed | Decision |
|---|---|---|
| Database name | `ExItS_PinoyPawnManager` | **PPM-D-00-04** OPEN |
| Schema naming | Unset | OPEN |

PPM-00 creates **neither** database nor migrations.

## Allowed external identifiers (contracts)

PPM may store **Guid / value references** received via approved contracts:

- `OrganizationId`
- `BranchId`
- `PlatformUserId` (actors)
- Optional Personal identity link for customer presentation
- Future Commerce handoff acknowledgment ids (**PPM-D-00-15**)

These are **not** foreign keys into other products’ tables.

## Forbidden

| Forbidden pattern | Principle |
|---|---|
| Direct Platform / POS / PLM / BNPL DB reads or writes | `DIRECT_*_DB_ACCESS` = NO |
| Cross-product EF includes / shared DbContext | Isolation |
| Cross-database FKs | Portfolio rule |
| Reusing PLM loan tables or POS inventory tables as pawn collateral | Domain ownership |
| `Migrate()` on production startup paths | Portfolio safety (when implemented) |

## What PPM persistence will own (planning)

- Customer references / profile extensions (not Platform login identity)
- Pledged items, evidence references, appraisals
- Pawn agreements / tickets and historical snapshots
- Payments / fund releases against pawn obligations
- Custody locations, movements, release confirmations
- Disposition workflow records and handoff attempts
- Product-local grants and PPM operational audit

Exact entity design is deferred to implementation packages after PPM-00.

## Snapshots and history

Historical agreement, appraisal, and identifying-item snapshots must not silently mutate when configuration or display names change. Custody **movement history** is required; current location alone is insufficient (`CUSTODY_HISTORY_REQUIRED` = YES).

## Related

- [platform-integration.md](platform-integration.md)
- [api-contract-boundary.md](api-contract-boundary.md)
- [pos-commerce-boundary.md](pos-commerce-boundary.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
