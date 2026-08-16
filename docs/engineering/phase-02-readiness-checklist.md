# Phase 2 Readiness Checklist

[Approved summary](approved-architecture-summary.md) | [Phase 1 approval](../reports/phase-01-architecture-approval.md) | [Phase 2](../phases/phase-02-platform-extraction.md) | [Gate matrix](implementation-gate-matrix.md)

Gates for starting **P2-WP01**. Status reflects Phase 1 closeout (documentation). Runtime gates remain Planned until executed.

| Gate | Required Evidence | Status | Blocking? | Owner | Target Work Package |
|---|---|---|---|---|---|
| Repository safety | Root `.gitignore`; `git ls-files legacy product` empty; legacy product ignored | **Met** | Yes | Repo lead | Continuous / P2-WP01 |
| portfolio independence verification | Nested independent Git; no root track; read-only | **Met** | Yes | Portfolio | Until approved import WP |
| Boundary approval | P1-WP01 + ADR-011 | **Met** | Yes | Architecture | Done |
| Data ownership | P1-WP02 matrices | **Met** | Yes | Architecture | Done |
| Contracts | P1-WP02 + ADR-012 | **Met** | Yes | Architecture | Done |
| Entitlements | State matrix + local projection policy | **Met** (durations open) | No for foundation | Platform | Phase 3 / R-022 |
| Authorization | Access vs ops separation documented | **Met** | Yes | Security | Done (impl later) |
| UI decision | ADR-010 native Admin/POS | **Met** | Yes | UI | Done |
| Payment MVP | Cash/GCash/Utang documented | **Met** | No for P2-WP01 | POS | Phase 5–6 |
| Extraction sequence | P1-WP03 + ADR-013 | **Met** | Yes | Architecture | Done |
| Rollback | L0–L6 plan | **Met** | Yes for cutover; No for P2-WP01 skeleton | Architecture | Phase 2+ |
| Test baseline | 1102 Windows-safe recorded | **Met** (re-run before legacy product cutover) | No for P2-WP01 | legacy product eng | P2-WP05 / cutover |
| Toolchain | .NET 10 legacy product evidence; Platform toolchain in P2-WP01 | **Partial** | No | Platform eng | P2-WP01 |
| Git remote | `origin/main` published; `phase-1-approved` remote | **Met (P2-WP05)** | No | Portfolio | Done |
| Open risks recorded | Risk register + ODs | **Met** | — | Portfolio | Ongoing |
| Architecture approval | P1-WP04 / ADR-014 | **Met** | Yes | Portfolio | Done |
| Solution foundation | Root `ExItS.slnx` / Platform projects exist; Release build + tests | **Met (P2-WP01)** | Yes | Platform eng | **P2-WP01** |
| Identity/org domain boundary | Strong IDs, aggregates, roles, ProductCode, app contracts, tests; no auth/EF | **Met (P2-WP02)** | Yes | Platform eng | **P2-WP02** |
| Products/plans/entitlements foundation | Catalog, subscriptions, snapshots, composer; no persistence/payments | **Met (P2-WP03)** | Yes | Platform eng | **P2-WP03** |
| legacy product contract adaptation (Platform-side) | Envelopes, projections, apply policy, adapter interfaces; legacy product frozen | **Met (P2-WP04)** | Yes to leave P2-WP04 | Platform eng | **P2-WP04** |
| Regression / migration dry-run validation | Preflight, simulation, rollback readiness; remote published | **Met (P2-WP05)** | Yes to leave P2-WP05 | Platform eng | **P2-WP05** |
| Phase 2 closeout | Evidence matrix + recommendation; next phase identified | **Met (P2-WP06)** | Yes to leave Phase 2 | Portfolio | **P2-WP06** |

## P2-WP01 entry criteria

Historical checklist for foundation start. Phase 2 is **closed with documented risks** as of P2-WP06. See [phase-02-extraction-closeout.md](../reports/phase-02-extraction-closeout.md).
