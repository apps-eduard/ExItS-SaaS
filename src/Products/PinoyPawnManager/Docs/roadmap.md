# Pinoy Pawn Manager — Roadmap

> Decisions: [risks-and-decisions.md](risks-and-decisions.md) · Delivery notes: [development-plan.md](development-plan.md)

| Field | Value |
|---|---|
| Status | PPM-01 complete — scaffold present; next **PPM-02** |
| Implementation started | Yes — scaffold only (no operational domain) |
| Last updated | 2026-08-27 |

## Package sequence

| Package | Purpose | Prerequisites | Owned areas | Explicit non-goals | Test gates (when implemented) | Status |
|---|---|---|---|---|---|---|
| **PPM-00** | Documentation / architecture foundation | Clean `feat/ppm` baseline | Docs tree only | Any product code | Doc link check; no conflict markers | **COMPLETE** |
| **PPM-01** | Product scaffold + Platform registration prep | PPM-00; provisional **PPM-D-00-01/02/03** for implementation | Solution projects, Local Validation catalog/access | Full domain; production catalog claims | Build empty projects; architecture isolation guards | **COMPLETE** |
| **PPM-02** | Authorization + Organization/Branch foundation | PPM-01; **PPM-D-00-18** | Grants, org/branch context | Appraisal/ticket | Authz unit tests; tenant isolation | Next |
| **PPM-03** | Customer / reference foundation | PPM-02 | PPM customer references, optional Personal link | KYC completeness claims | CRUD + isolation tests | Planned |
| **PPM-04** | Pledged-item + photo/evidence foundation | PPM-03; **PPM-D-00-05** | Item records, evidence upload contracts | AI vision | Evidence access tests; immutability rules | Planned |
| **PPM-05** | Appraisal engine | PPM-04; **PPM-D-00-06/07** | Appraisal create/history/approval hooks | Fixed LTV invention | Appraisal snapshot tests | Planned |
| **PPM-06** | Pawn transaction + agreement lifecycle | PPM-05 | Ticket/agreement states, disclosures snapshot | Disposition | State-machine tests | Planned |
| **PPM-07** | Cash / loan release | PPM-06; **PPM-D-00-17** | Fund release, idempotency | Payment gateway build | Duplicate-release prevention | Planned |
| **PPM-08** | Custody + storage locations | PPM-06/07 | Custody states, locations, movements | Deep warehouse WMS | Movement audit tests | Planned |
| **PPM-09** | Maturity + renewal | PPM-06; **PPM-D-00-09/11/12** | Maturity jobs, renewals | Legal grace invention | Renewal history + new maturity | Planned |
| **PPM-10** | Redemption + payment | PPM-09; **PPM-D-00-08/12** | Redemption quote/payment | Physical release | Idempotent payment tests | Planned |
| **PPM-11** | Physical item release | PPM-08 + PPM-10 | Release checklist, custody CLOSED | Biometrics mandate | Payment≠release enforcement tests | Planned |
| **PPM-12** | Unredeemed / default workflow | PPM-09; **PPM-D-00-10/14/20** | Operational unredeemed states | Auto ownership transfer | No silent inventory handoff | Planned |
| **PPM-13** | Disposition + POS/Commerce handoff | PPM-12; **PPM-D-00-15** | Handoff contract | Direct POS DB writes | Contract/integration tests | Planned |
| **PPM-14** | Reports + audit | PPM-06+ | Operational/custody/financial reports | Full GL | Report accuracy fixtures | Planned |
| **PPM-15** | Customer / Personal experience (optional) | PPM-06+ | Personal ticket/status presentation | Second identity system | Consent/link tests if any | Planned |
| **PPM-16** | Security / E2E / operational hardening | Prior packages | Hardening, E2E, ops runbooks | New domain expansion | E2E + threat regression | Planned |

## Dependency notes

- **Custody (PPM-08)** should land before **physical release (PPM-11)**; payment (PPM-10) may precede release but must not collapse into it.
- **Disposition (PPM-13)** depends on unredeemed workflow **and** an approved Commerce handoff contract.
- **Regulatory decisions (PPM-D-00-20)** gate production claims, not necessarily early technical scaffolds—but must block “compliant/licensed” language.

## Documentation work packages (optional)

| ID | Purpose |
|---|---|
| PPM-DOC-01 | Deepen charge/interest policy once Product Owner inputs exist |
| PPM-DOC-02 | Close maturity/grace with legal review evidence |
| PPM-DOC-03 | Finalize grant identifier strings |
| PPM-DOC-04 | Disposition/auction legal process mapping |

## Next recommended package

**PPM-02** — Authorization + Organization/Branch foundation (requires explicit authorization; still no appraisal/ticket operational domain unless separately authorized).

Closeout: [Reports/PPM-01-product-scaffold-platform-registration.md](Reports/PPM-01-product-scaffold-platform-registration.md).
