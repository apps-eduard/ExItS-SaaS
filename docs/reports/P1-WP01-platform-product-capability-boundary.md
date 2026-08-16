# P1-WP01 — Platform vs Product Capability Boundary

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 1 — Platform Boundary and Architecture |
| Work package | P1-WP01 — Platform vs Product Capability Boundary |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Defined the authoritative capability boundary between ExITS Platform, legacy product, PinoyBusinessPOS, shared contracts, and engineering conventions. Confirmed Platform authority for identity/orgs/catalog/subscriptions/entitlements; product ownership of operational data and permissions; local entitlement projections; prohibited cross-DB FKs and clinical/POS coupling. Created ADR-011. Phase 0 recorded as **Complete with documented risks**. No application code; legacy product unchanged.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| Phase 0 Complete with documented risks | Met | [portfolio-progress.md](../portfolio-progress.md), phase-00 |
| Platform / legacy product / POS ownership explicit | Met | [capability boundary](../engineering/platform-product-capability-boundary.md) §§4–6 |
| Identity / org / membership explicit | Met | §§10–11 |
| Platform vs product roles; access vs ops perms | Met | §12 |
| Customer ≠ User; SaaS ≠ retail payment | Met | §§10, 13 |
| Catalog / subscription / entitlement + projection | Met | §§13–14; ADR-011 |
| Audit / notification / jobs / UI boundaries | Met | §§15–18; ADR-010 consistent |
| Data authority matrix | Met | [data-authority-matrix.md](../engineering/data-authority-matrix.md) |
| Contracts / failure / prohibited / shared-code rules | Met | §§9, 20, 22 + ownership matrix |
| New ADR | Met | ADR-011 |
| Tracking / report / Markdown-only / portfolio independence verification | Met | This report; validation §7 |
| Focused commit + hash | Met | §12 (hash after commit) |

## 4. Files changed

Added:

- `docs/engineering/platform-product-capability-boundary.md`
- `docs/engineering/capability-ownership-matrix.md`
- `docs/engineering/data-authority-matrix.md`
- `docs/decisions/ADR-011-platform-authority-and-product-local-projections.md`
- `docs/reports/P1-WP01-platform-product-capability-boundary.md`

Modified (tracking/reconcile):

- `docs/portfolio-progress.md`
- `docs/phases/phase-01-platform-boundary.md`
- `docs/phases/phase-00-legacy product-assessment.md` *(later removed; see [phase-00 final assessment](phase-00-final-assessment-and-recommendation.md))*
- `docs/index.md`
- `docs/decisions/README.md`
- `docs/reports/README.md`
- `docs/risks-and-issues.md`
- `docs/release-plan.md`
- `docs/engineering/architecture.md`
- `docs/engineering/data-ownership.md`
- `docs/engineering/platform-product-contracts.md`
- `docs/engineering/final-portfolio-boundaries.md`
- `docs/engineering/authorization-matrix.md`
- `docs/reuse/extraction-rules.md`
- `FILE-MANIFEST.md`

## 5. Architecture/reuse impact

Documents ownership so extraction and greenfield work do not put product domain in Platform or share Ant/Tailwind across new surfaces. No runtime extraction performed.

## 6. Database and migration impact

None (documentation only). Confirms target DBs and no cross-DB FKs.

## 7. Tests and validation

| Command / check | Passed | Failed | Skipped | Exit code |
|---|---:|---:|---:|---:|
| legacy product runtime tests | — | — | Yes (docs-only + freeze) | — |
| `git ls-files legacy product` empty | Yes | 0 | — | 0 |
| `git check-ignore -v legacy product/` | Yes | 0 | — | 0 |
| Markdown-only `git diff --name-only` | Yes | 0 | — | 0 |
| Link/path/ADR/manifest spot-check | Yes | 0 | — | — |

## 8. Security and tenant review

Reaffirmed: server-derived org context; no client-trusted OrganizationId; clinical data out of Platform payloads; separate audit owners; Platform Admin does not auto-gain clinical/POS ops access.

## 9. UI, localization and theme review

Consistent with ADR-010: legacy product Staff Ant retained; Platform Admin + POS native; no shared Ant↔native component.

## 10. Documentation updated

Dashboard, Phase 1, ADR index, index, manifest, risks, release plan, and pointers from architecture/contracts/data-ownership/final-boundaries/authorization/extraction-rules.

## 11. Risks, blockers, unknowns and deferred items

| Item | Notes |
|---|---|
| OD-01 Customer↔User login | Deferred |
| OD-02 Break-glass support | Deferred |
| OD-03 Entitlement transport | P1-WP02 / Phase 3 |
| OD-04 MFA | Deferred |
| OD-05 legacy product import timing | After Platform foundation |
| OD-06 Multi-org from legacy product StaffMember | Phase 2 |
| R-003 / R-022 | Projection staleness policy detail still for later WPs |
| R-016 | Root remote empty — do not push without authorization |

No blockers for P1-WP01 acceptance.

## 12. Git evidence

| Field | Value |
|---|---|
| Commit hash | `b6a3133732f6d29c68159447eb1ca43ea0b1212b` |
| Commit message | `docs(architecture): define platform product boundaries` |
| Branch | `main` |
| Upstream | `origin/main` gone (remote empty); not pushed |
| Final working tree | Clean after hash-record commit |

## 13. Progress update

Phase 0 → Complete with documented risks. Phase 1 in progress; P1-WP01 Ready for Review. Next: P1-WP02 only after acceptance.

## 14. Next approved work package

**P1-WP02 — Data Ownership and Contracts** — do not begin until authorized.
