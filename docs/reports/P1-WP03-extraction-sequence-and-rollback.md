# P1-WP03 — Extraction Sequence and Rollback Plan

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 1 — Platform Boundary and Architecture |
| Work package | P1-WP03 — Extraction Sequence and Rollback Plan |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Documented the safe sequence to build a **new** ExITS Platform in root Git by adapting legacy product patterns selectively, without importing or modifying legacy product. Defined stages 1–7, dependency direction, identity/org/authz/DB continuity, feature-flag concept, test/observability/rollout gates, POS readiness gate, rollback levels L0–L6, risk and gate matrices, and ADR-013. P1-WP02 and Cash/GCash correction recorded Complete/accepted. No extraction or code executed.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| P1-WP02 Complete; payment correction accepted | Met | portfolio-progress; phase-01 |
| New-build vs extraction; legacy product frozen | Met | extraction-sequence §§2–3; ADR-013 |
| Sequence, dependencies, continuity | Met | §§4–8 |
| DB sequence; rollback levels/triggers/backups | Met | §9; rollback plan |
| Feature flags conceptual; test/obs/rollout gates | Met | §§10–12; gate matrix |
| POS readiness; open decisions assigned | Met | §§13, 15 |
| Risk + gate matrices; ADR-013 | Met | engineering matrices; ADR-013 |
| Tracking / report / MD-only / portfolio independence verification | Met | This report; validation §7 |

## 4. Files changed

Added:

- `docs/reuse/extraction-sequence.md`
- `docs/engineering/extraction-rollback-plan.md`
- `docs/engineering/platform-extraction-risk-matrix.md`
- `docs/engineering/implementation-gate-matrix.md`
- `docs/decisions/ADR-013-build-new-platform-before-legacy product-reconnection.md` *(later removed; historical ADR-013 retained in [decisions README](../decisions/README.md))*
- `docs/reports/P1-WP03-extraction-sequence-and-rollback.md`

Modified:

- `docs/reuse/extraction-rules.md`
- `docs/engineering/repository-boundaries.md`
- `docs/engineering/architecture.md`
- `docs/release-plan.md`
- `docs/risks-and-issues.md`
- `docs/index.md`
- `docs/decisions/README.md`
- `docs/portfolio-progress.md`
- `docs/phases/phase-01-platform-boundary.md`
- `docs/reports/README.md`
- `FILE-MANIFEST.md`
- (light links as needed in related engineering docs)

## 5. Architecture/reuse impact

Confirms ADR-002/003 direction without executing extraction. Protects legacy product MVP; enables POS after Platform contract readiness.

## 6. Database and migration impact

None executed. Target DBs and future migration process documented only.

## 7. Tests and validation

| Check | Result |
|---|---|
| legacy product runtime tests | Skipped (docs-only + freeze) |
| `git ls-files legacy product` empty | Yes |
| `git check-ignore -v legacy product/` | Yes |
| Markdown-only | Yes |
| Link/ADR/manifest spot-check | Yes |

## 8. Security and tenant review

No privilege escalation path for Platform Admin into clinical ops; mapping and cutover require security/tenant gates; no password-hash copy without approved plan.

## 9. UI, localization and theme review

Stage 5 native Admin; ADR-010 unchanged; no Ant.

## 10. Documentation updated

Dashboard, Phase 1, ADR index, index, manifest, risks, release plan, extraction rules, repository boundaries, architecture.

## 11. Risks, blockers, unknowns and deferred items

OD-01–OD-13 and R-022/R-024 carried with defaults. R-026/R-027 added. No blockers for P1-WP03 acceptance. Extraction **not** started.

## 12. Git evidence

| Field | Value |
|---|---|
| Commit hash | `b7f99ab6c25fb69f0820ba8bfe746b261e81fd14` |
| Commit message | `docs(extraction): define sequence and rollback plan` |
| Branch | `main` |
| Upstream | `origin/main` gone; not pushed |
| Final working tree | Clean after hash-record |

## 13. Progress update

P1-WP02 Complete; Cash/GCash accepted; P1-WP03 Ready for Review. Next: P1-WP04 after acceptance.

## 14. Next approved work package

**P1-WP04 — Architecture Approval Closeout** — do not begin until authorized.
