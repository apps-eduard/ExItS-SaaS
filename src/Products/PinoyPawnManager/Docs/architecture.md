# Pinoy Pawn Manager — Architecture

> Product definition: [product-definition.md](product-definition.md)  
> Boundaries: [Architecture/](Architecture/README.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning architecture |
| Implementation | None |
| Last updated | 2026-08-27 |

## System shape

```text
┌─────────────────────────────────────────────────────────┐
│                   ExItS Platform                        │
│  Identity · Org · Branch facts · Catalog · Entitlements │
└───────────────────────────┬─────────────────────────────┘
                            │ approved contracts / Guids only
┌───────────────────────────▼─────────────────────────────┐
│              Pinoy Pawn Manager (PPM)                   │
│  Customers* · PledgedItems · Appraisal · Tickets        │
│  Payments · Custody · Storage · Release · Disposition   │
│  Product-local grants · PPM audit · PPM reports         │
└───────────────┬─────────────────────────┬───────────────┘
                │ future disposition handoff (contract)
                ▼
        PinoyBusinessPOS / Commerce
        (retail inventory + sale — only after authorized handoff)
```

\*PPM customer references may link to Platform Personal identity; they are not a second auth system.

## Persistence boundary

| Rule | Intent |
|---|---|
| Separate logical DB | Proposed `ExItS_PinoyPawnManager` (**PPM-D-00-04** Open) |
| No cross-product EF navigation | Required |
| No foreign keys into Platform/POS/PLM/BNPL tables | Required |
| Stable external identifiers | `OrganizationId`, `BranchId`, `PlatformUserId`, optional Personal link |
| Disposition → Commerce | Explicit handoff contract later (**PPM-D-00-15** Open) |

Detail: [Architecture/persistence-boundary.md](Architecture/persistence-boundary.md).

## Separate state machines (do not collapse)

| Machine | Owns | Doc |
|---|---|---|
| **A. Pawn transaction** | Agreement lifecycle (draft → active → redeemed/unredeemed/…) | [Product/pawn-transaction-model.md](Product/pawn-transaction-model.md) |
| **B. Pledged-item custody** | Physical control (receiving → in custody → released / disposition) | [Custody/custody-state-model.md](Custody/custody-state-model.md) |
| **C. Payment / financial operation** | Release, renewal payment, redemption payment (idempotent) | [Architecture/idempotency-and-reconciliation.md](Architecture/idempotency-and-reconciliation.md) |
| **D. Disposition process** | Eligibility → authorize → handoff → Commerce reference | [Product/unredeemed-and-disposition-model.md](Product/unredeemed-and-disposition-model.md) |

Critical: **payment accepted** does not alone set custody to **RELEASED**.

## Proposed pawn transaction states (minimal analysis)

Planning set (names may refine at implementation):

| State | Meaning |
|---|---|
| `DRAFT` | Intake started; not yet binding |
| `APPRAISED` | Appraisal recorded |
| `OFFERED` | Terms proposed to customer |
| `ACCEPTED` | Customer accepted; agreement pending activation |
| `ACTIVE` | Funds released; item in custody; obligation open |
| `MATURED` | Past maturity; redemption/renewal rules still apply per policy |
| `RENEWAL_PENDING` | Renewal payment in progress |
| `REDEEMED` | Obligation settled **and** release process completed (or release pending separate custody state) |
| `UNREDEEMED` | Operational unredeemed classification (legal disposition eligibility separate) |
| `DISPOSITION_PENDING` | Disposition workflow started |
| `CLOSED` | Terminal closed |
| `CANCELLED` | Cancelled before activation (policy required) |

Entry/exit money and custody implications: [Product/pawn-transaction-model.md](Product/pawn-transaction-model.md).

## Snapshots

Historical records must not silently mutate when:

- customer display name changes
- fee configuration changes
- category definitions change
- staff configuration changes

Required snapshot concepts:

- Appraisal snapshot at agreement time
- Pawn agreement / ticket snapshot
- Pledged-item identifying snapshot (including photo evidence references)

## Organization and branch

Every operational pawn record must carry:

- `OrganizationId`
- `BranchId` (originating / holding branch for custody)

Cross-branch custody transfer is **never implicit** (**PPM-D-00-16** Open).

## Client runtime

Initial PPM Web/PWA: **ONLINE-ONLY** for money release, payments, custody moves, redemption, and item release.  
Installable PWA shell may cache static assets only.

Detail: [Architecture/web-pwa-runtime-policy.md](Architecture/web-pwa-runtime-policy.md).

## Integration pointers

| Boundary | Doc |
|---|---|
| Platform | [Architecture/platform-integration.md](Architecture/platform-integration.md) |
| PLM | [Architecture/plm-boundary.md](Architecture/plm-boundary.md) |
| BNPL | [Architecture/bnpl-boundary.md](Architecture/bnpl-boundary.md) |
| POS / Commerce | [Architecture/pos-commerce-boundary.md](Architecture/pos-commerce-boundary.md) |
| API contracts | [Architecture/api-contract-boundary.md](Architecture/api-contract-boundary.md) |
| Idempotency | [Architecture/idempotency-and-reconciliation.md](Architecture/idempotency-and-reconciliation.md) |

## Non-goals (architecture)

- Shared database with POS or PLM
- Treating pledged items as POS stock while pledged
- Offline financial/custody mutation outbox in initial Web
- Claiming accounting GL completeness
- Inventing regulatory maturity/grace calendars
