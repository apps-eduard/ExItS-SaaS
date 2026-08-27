# PPM-01 — Product Scaffold + Platform Registration Closeout

| Field | Value |
|---|---|
| Package | **PPM-01** Product scaffold + Platform registration |
| Product | Pinoy Pawn Manager (PPM) |
| Status | **COMPLETE** — scaffold + Local Validation / Dev registration |
| Implementation present | **Yes** — empty product projects only (no operational domain) |
| Last updated | 2026-08-27 |
| Legal claim | `LEGAL_AUTHORIZATION_CLAIMED` = **NO** |
| Commit intent | `feat(ppm): scaffold product and register platform access` |

## Delivered capability

PPM-01 delivers an independent product scaffold and Platform **Local Validation / Dev** registration for Pinoy Pawn Manager:

### Product identity (provisionally approved for implementation)

| Concern | Value | Decision status |
|---|---|---|
| Display name | **Pinoy Pawn Manager** | **PPM-D-00-01** Provisionally Approved for Implementation (Product Owner, PPM-01) — **not** final marketing approval |
| Platform product code | **`pinoy-pawn-manager`** | **PPM-D-00-02** Provisionally Approved for Implementation (Product Owner, PPM-01) — **not** final marketing approval |
| Product directory | **`src/Products/PinoyPawnManager/`** | **PPM-D-00-03** Provisionally Approved for Implementation (Product Owner, PPM-01) — **not** final marketing approval |

### Solution projects created

| Project | Role |
|---|---|
| `ExItS.PinoyPawnManager.Domain` | Domain layer marker / product identity string only |
| `ExItS.PinoyPawnManager.Application` | Application layer marker |
| `ExItS.PinoyPawnManager.Infrastructure` | Infrastructure layer marker (**no DbContext**) |
| `ExItS.PinoyPawnManager.Api` | Minimal API host (health surface only) |
| `ExItS.PinoyPawnManager.UnitTests` | Scaffold / safety guard tests |

Projects are registered under `ExItS.slnx` in `/src/Products/PinoyPawnManager/` and `/tests/`.

### Platform registration (Local Validation / Dev fixture only)

- `ProductCode.PinoyPawnManager` = `"pinoy-pawn-manager"` registered on Platform Domain
- `EnsurePpmLocalValidationCatalog` — idempotent Local Validation catalog/commercial fixture (display name **Pinoy Pawn Manager**; test-only plan/trial schema values — **not** production commercial policy)
- `GrantPpmProductAccess` for ABC Local Validation identities (**maria.santos**, **carlo.reyes**)
- XYZ Mini Grocery identities remain **denied by default** (`GrantPpmProductAccess` defaults to `false`)

This is **Local Validation / Dev fixture registration**, not a claim of full production catalog commercial registration.

## Explicit non-goals / exclusions

- No `DbContext`, EF migrations, or operational database creation (`ExItS_PinoyPawnManager` remains **PPM-D-00-04** OPEN)
- No pawn operational entities (customer, pledged item, appraisal, ticket, custody, payment, disposition)
- No Organization Web / PWA / MAUI / LocalStore projects
- No product-local grant catalog closure (**PPM-D-00-18** OPEN)
- No closed interest rates, LTV, grace, auction, or licensing claims (**PPM-D-00-08** … **PPM-D-00-20** remain OPEN where applicable)
- No POS / PLM / BNPL / PSP product code ownership changes beyond Platform Local Validation access flags for PPM
- No claim that ExItS/PPM is pawnshop-licensed

## Evidence of isolation

| Evidence | What it shows |
|---|---|
| Architecture tests (`PinoyPawnManagerArchitectureTests`) | Domain/Application do not reference Infrastructure, EF, Npgsql, or sibling products; Infrastructure/Api avoid sibling products, Platform Infrastructure, EF, DbContext |
| Unit safety guards | No sibling-product project references under PPM tree; no migration sources |
| Domain `PpmProductIdentity` | Product code string aligned to `pinoy-pawn-manager` without referencing Platform Domain |
| Solution layout | First-class folder `src/Products/PinoyPawnManager/` — not nested under PLM or POS |
| Access fixture | ABC granted PPM access; XYZ denied by default (tenant isolation intent for Local Validation) |

## Persistence / migrations

**None.** No DbContext. No migrations. Proposed DB name `ExItS_PinoyPawnManager` remains planning-only (**PPM-D-00-04** OPEN).

## API / UI capability

- API: scaffold host + health only — **no** pawn operational endpoints
- UI: **none** in this package

## Build / test / runtime evidence

Recorded by the implementing session when authorized. Expected gates for this package:

- Release build of PPM projects + solution inclusion
- `ExItS.PinoyPawnManager.UnitTests` scaffold/safety tests
- `ExItS.ArchitectureTests` PinoyPawnManager isolation tests
- Platform unit coverage for `ProductCode.PinoyPawnManager` and `EnsurePpmLocalValidationCatalog`

Validation checklist for docs foundation remains: [../Validation/PPM-00-readiness-checklist.md](../Validation/PPM-00-readiness-checklist.md) (PPM-00). PPM-01 validation is scaffold + Local Validation registration evidence above—not a separate legal readiness gate.

## Security limitations

- Portfolio production auth maturity (**R-091**) unchanged
- Local Validation grants are **Dev fixture** access only; product-local PPM grants (**PPM-D-00-18**) remain OPEN
- Empty/test plan entitlements in `EnsurePpmLocalValidationCatalog` do **not** close pricing or grant decisions
- `LEGAL_AUTHORIZATION_CLAIMED=NO`

## Portfolio independence

- PPM remains a first-class product under `src/Products/PinoyPawnManager/`
- No cross-product DbContext, migrations, or foreign keys introduced
- No claim that PPM is a PLM, POS, or BNPL module
- Platform registers an independent product code; sibling product codes unchanged

## Risks / open decisions

| ID | Status after PPM-01 |
|---|---|
| **PPM-D-00-01** | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — not final marketing approval |
| **PPM-D-00-02** | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — not final marketing approval |
| **PPM-D-00-03** | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — not final marketing approval |
| **PPM-D-00-04** … **PPM-D-00-20** | Remain **OPEN** |

ADR-001 reflects the same provisional implementation approval; it is **not** Closed as final marketing naming. See [../risks-and-decisions.md](../risks-and-decisions.md) and [../Decisions/ADR-001-product-identity.md](../Decisions/ADR-001-product-identity.md).

## Files / docs changed (this closeout)

- This report: `Reports/PPM-01-product-scaffold-platform-registration.md`
- Index updates: product `README.md`, `roadmap.md`, `risks-and-decisions.md`, `FILE-MANIFEST.md`, `product-definition.md`, ADR-001, Reports index

Code (delivered under PPM-01, outside this Docs-only edit set): Domain/Application/Infrastructure/Api/UnitTests projects; Platform `ProductCode`, `EnsurePpmLocalValidationCatalog`, `GrantPpmProductAccess` Local Validation wiring; architecture/unit tests.

## Git / push evidence

Commit message intent: **`feat(ppm): scaffold product and register platform access`**.

Hash and push evidence are recorded by the integrating agent/session when commit/push are authorized. This report does **not** authorize commit or push by itself.

## Exact next work package

**PPM-02** — Authorization + Organization/Branch foundation (requires explicit authorization; still no appraisal/ticket/custody operational domain unless separately authorized).

## Principle snapshot

| Principle | Value |
|---|---|
| `PPM_FIRST_CLASS_PRODUCT` | YES |
| `PPM_IS_PLM_MODULE` | NO |
| `PPM_IS_POS_MODULE` | NO |
| `PPM_IS_BNPL_MODULE` | NO |
| `DIRECT_POS_DB_ACCESS` | NO |
| `DIRECT_PLM_DB_ACCESS` | NO |
| `DIRECT_BNPL_DB_ACCESS` | NO |
| `PAWN_ITEM_IS_NORMAL_POS_INVENTORY_WHILE_PLEDGED` | NO |
| `PHYSICAL_RELEASE_SEPARATE_FROM_PAYMENT` | YES |
| `CUSTODY_HISTORY_REQUIRED` | YES |
| `LEGAL_AUTHORIZATION_CLAIMED` | NO |
| Implementation | Scaffold projects only — **no** operational pawn domain |
| Platform catalog scope | Local Validation / Dev fixture — **not** full production commercial registration |
| Web/PWA mutations (initial) | ONLINE_ONLY |
