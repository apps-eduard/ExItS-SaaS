# ADR-001 — Product Identity Naming

| Field | Value |
|---|---|
| ADR | ADR-001 |
| Status | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — **not** Closed as final marketing approval |
| Date | 2026-08-27 |
| Related decisions | **PPM-D-00-01**, **PPM-D-00-02**, **PPM-D-00-03** (provisional implementation); **PPM-D-00-04** remains OPEN |
| Product | Pinoy Pawn Manager (PPM) |
| Closeout | [../Reports/PPM-01-product-scaffold-platform-registration.md](../Reports/PPM-01-product-scaffold-platform-registration.md) |

## Context

PPM-00 established documentation for a first-class ExItS product. PPM-01 needed stable naming for scaffold and Local Validation / Dev Platform registration:

- Human display name
- Platform product code / slug
- Source directory / project naming
- Operational database name (still Open)

These must not collide with PinoyLoanManager, PinoyBusinessPOS, BNPL, or other products, and must not imply PPM is a module of those products.

## Decision (provisionally approved for implementation)

| Concern | Value | Decision status |
|---|---|---|
| Display name | **Pinoy Pawn Manager** | **PPM-D-00-01** Provisionally Approved for Implementation — not final marketing |
| Short code | **PPM** | Working short code |
| Platform product code / slug | **`pinoy-pawn-manager`** | **PPM-D-00-02** Provisionally Approved for Implementation — not final marketing |
| Product directory / solution naming | **`PinoyPawnManager`** under `src/Products/` | **PPM-D-00-03** Provisionally Approved for Implementation — not final marketing |
| Operational database name | **`ExItS_PinoyPawnManager`** (proposed) | **PPM-D-00-04** remains **OPEN** — not created in PPM-01 |

## What provisional approval authorizes

- Scaffold solution projects under `src/Products/PinoyPawnManager/`
- Platform Domain constant `ProductCode.PinoyPawnManager`
- Local Validation / Dev catalog fixture via `EnsurePpmLocalValidationCatalog` (display name Pinoy Pawn Manager)
- Local Validation access grants (`GrantPpmProductAccess`) for designated XYZ identity (**ana.cruz**); ABC remains independently on POS+PLM without PPM

## What provisional approval does **not** mean

- Final marketing / brand lock (display name or slug may still change with Product Owner marketing approval)
- Full production commercial catalog registration beyond Local Validation / Dev fixture
- Database creation, migrations, or schema acceptance (**PPM-D-00-04** OPEN)
- Closure of **PPM-D-00-05** … **PPM-D-00-20**
- Legal authorization to operate pawnshops (`LEGAL_AUTHORIZATION_CLAIMED=NO`)

## Consequences

- Catalog Local Validation registration and subscription fixtures use `pinoy-pawn-manager`
- Solution projects live under `src/Products/PinoyPawnManager/`
- Persistence still targets proposed `ExItS_PinoyPawnManager` only when a later package closes **PPM-D-00-04** and is authorized
- Docs path from PPM-00 remains aligned

## Alternatives considered (brief)

| Option | Why not defaulted |
|---|---|
| Nest under PLM (`PinoyLoanManager/.../Pawn`) | Violates first-class product principle |
| Reuse POS inventory DB | Violates pledged ≠ retail stock |
| Generic `PinoyCollateral` slug | Less clear pawn domain; still Open if PO prefers for final marketing |

## Non-claims

- Naming provisional approval ≠ legal authorization to operate pawnshops
- Local Validation registration ≠ production commercial catalog completeness
- `LEGAL_AUTHORIZATION_CLAIMED=NO`

## References

- [README.md](README.md) — ADR process
- [../risks-and-decisions.md](../risks-and-decisions.md)
- [../product-definition.md](../product-definition.md)
- [../Architecture/platform-integration.md](../Architecture/platform-integration.md)
- [../Architecture/persistence-boundary.md](../Architecture/persistence-boundary.md)
- [../Reports/PPM-01-product-scaffold-platform-registration.md](../Reports/PPM-01-product-scaffold-platform-registration.md)
