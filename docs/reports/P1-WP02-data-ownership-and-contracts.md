# P1-WP02 — Data Ownership and Contracts

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 1 — Platform Boundary and Architecture |
| Work package | P1-WP02 — Data Ownership and Contracts |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Defined authoritative data ownership and versioned Platform↔product contracts: stable IDs, identity/org/subscription/entitlement/payment projections, event envelope, versioning, idempotency/ordering, projection states, reconciliation, audit correlation, deletion/retention, and privacy exclusions. Created ADR-012. P1-WP01 recorded Complete. No schemas, APIs, brokers, or application code. HealthCare unchanged.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| P1-WP01 Complete | Met | portfolio-progress, phase-01 |
| Stable IDs / identity / org / catalog / subscription / entitlement | Met | contracts §§3–8 |
| Trial expiry / event envelope / versioning / idempotency / ordering | Met | contracts §§9–12 |
| Projection states / failure / reconciliation | Met | §13–14; entitlement-state-matrix |
| SaaS vs retail payments / audit / sensitive exclusions / deletion | Met | §§15–18, 21 |
| API/event/projection rules | Met | §19 |
| Matrices + ADR-012 | Met | contract / state / classification matrices; ADR-012 |
| Open decisions carried | Met | contracts §20 |
| Tracking / report / MD-only / HC freeze | Met | This report; validation §7 |

## 4. Files changed

Added:

- `docs/engineering/platform-product-contract-matrix.md`
- `docs/engineering/entitlement-state-matrix.md`
- `docs/engineering/data-classification-matrix.md`
- `docs/decisions/ADR-012-versioned-platform-contracts-and-local-projections.md`
- `docs/reports/P1-WP02-data-ownership-and-contracts.md`

Rewrote/expanded:

- `docs/engineering/platform-product-contracts.md`
- `docs/engineering/data-ownership.md`
- `docs/engineering/data-authority-matrix.md`

Modified (tracking/reconcile):

- `docs/portfolio-progress.md`, `docs/phases/phase-01-platform-boundary.md`
- `docs/index.md`, `docs/decisions/README.md`, `docs/reports/README.md`
- `docs/risks-and-issues.md`, `docs/release-plan.md`
- `docs/engineering/architecture.md`, `security.md`, `final-portfolio-boundaries.md`, `platform-product-capability-boundary.md`
- `docs/product/subscriptions-and-billing.md`, `docs/reuse/extraction-rules.md`
- `FILE-MANIFEST.md`

## 5. Architecture/reuse impact

Gives Phase 2–3 implementers contract rules without choosing transport. Reinforces ADR-011 projections and privacy boundaries.

## 6. Database and migration impact

None (documentation only). Confirms no cross-DB FKs and projection-only commercial replication.

## 7. Tests and validation

| Check | Result |
|---|---|
| HealthCare runtime tests | Skipped (docs-only + freeze) |
| `git ls-files HealthCare` empty | Yes |
| `git check-ignore -v HealthCare/` | Yes |
| Markdown-only changes | Yes |
| Link/path/ADR/manifest spot-check | Yes |

## 8. Security and tenant review

Minimal identity/org projection; clinical and POS operational payloads excluded; fail-closed for financial/privacy; correlation without sensitive content.

## 9. UI, localization and theme review

No UI changes. ADR-010 unchanged.

## 10. Documentation updated

Dashboard, Phase 1, ADR index, index, manifest, risks, release plan, and related engineering/product pointers.

## 11. Risks, blockers, unknowns and deferred items

OD-01–OD-10 and R-022 (durations), R-024 (version skew), OD-03 (transport). No blockers for P1-WP02 acceptance.

## 12. Git evidence

| Field | Value |
|---|---|
| Commit hash | `32534fa31501217f021e73b36ba27f49c448b36c` |
| Commit message | `docs(contracts): define data authority and projections` |
| Branch | `main` |
| Upstream | `origin/main` gone; not pushed |
| Final working tree | Clean after hash-record |

## 13. Progress update

P1-WP01 Complete. P1-WP02 Ready for Review. Next: P1-WP03 after acceptance.

## 14. Next approved work package

**P1-WP03 — Extraction Sequence and Rollback Plan** — do not begin until authorized.
