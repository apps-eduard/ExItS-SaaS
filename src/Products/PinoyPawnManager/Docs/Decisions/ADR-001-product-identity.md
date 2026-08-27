# ADR-001 — Product Identity Naming

| Field | Value |
|---|---|
| ADR | ADR-001 |
| Status | **PROPOSED** (not Closed / not Accepted) |
| Date | 2026-08-27 |
| Related decisions | **PPM-D-00-01**, **PPM-D-00-02**, **PPM-D-00-03**, **PPM-D-00-04** |
| Product | Pinoy Pawn Manager (PPM) |

## Context

PPM-00 establishes documentation for a first-class ExItS product. Portfolio and scaffold work need stable naming for:

- Human display name  
- Platform product code / slug  
- Source directory / project naming  
- Operational database name  

These must not collide with PinoyLoanManager, PinoyBusinessPOS, BNPL, or other products, and must not imply PPM is a module of those products.

## Decision (proposed)

| Concern | Proposed value |
|---|---|
| Display name | **Pinoy Pawn Manager** |
| Short code | **PPM** |
| Platform product code / slug | **`pinoy-pawn-manager`** |
| Product directory / solution naming | **`PinoyPawnManager`** under `src/Products/` |
| Operational database name | **`ExItS_PinoyPawnManager`** |

## Consequences if accepted later

- Catalog registration and subscription plans use `pinoy-pawn-manager`  
- Solution projects live under `src/Products/PinoyPawnManager/`  
- Persistence targets `ExItS_PinoyPawnManager` (schema still Open)  
- Docs path already used in PPM-00 remains aligned  

## Consequences while PROPOSED

- Docs may use these names as **proposed** only  
- Do **not** register Platform catalog entries in PPM-00  
- Do **not** create DB, migrations, or code projects solely from this ADR  
- risks-and-decisions.md entries remain **OPEN** until Product Owner closes them  

## Alternatives considered (brief)

| Option | Why not defaulted |
|---|---|
| Nest under PLM (`PinoyLoanManager/.../Pawn`) | Violates first-class product principle |
| Reuse POS inventory DB | Violates pledged ≠ retail stock |
| Generic `PinoyCollateral` slug | Less clear pawn domain; still Open if PO prefers |

## Non-claims

- Naming acceptance ≠ legal authorization to operate pawnshops  
- `LEGAL_AUTHORIZATION_CLAIMED=NO`  

## References

- [README.md](README.md) — ADR process  
- [../risks-and-decisions.md](../risks-and-decisions.md)  
- [../product-definition.md](../product-definition.md)  
- [../Architecture/platform-integration.md](../Architecture/platform-integration.md)  
- [../Architecture/persistence-boundary.md](../Architecture/persistence-boundary.md)  
