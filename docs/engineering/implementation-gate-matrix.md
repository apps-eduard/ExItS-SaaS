# Implementation Gate Matrix

[Extraction sequence](../reuse/extraction-sequence.md) | [Rollback plan](extraction-rollback-plan.md) | [Phase 2 evidence](phase-02-evidence-matrix.md)

Statuses reflect Phase 2 closeout. Distinguish **code/domain foundation** from **runtime cutover readiness**.

| Gate | Required Evidence | Owner | Must Pass Before | Rollback Readiness | Status |
|---|---|---|---|---|---|
| G0 Docs freeze | P1-WP01–03 accepted; HC `git ls-files` empty | Portfolio | Stage 1 code | L0 | **Met** |
| G1 Solution foundation | Solution builds; architecture dependency tests; no HC import | Platform eng | Stage 2 | L1 plan | **Met** (P2-WP01) |
| G2 Identity foundation | Login/refresh/revoke/suspend tests; no credential leakage | Platform eng | Stage 3 | L2 plan reviewed | **Partial** — domain foundation only (no login) |
| G3 Org / membership | Multi-org membership; product-access tests; server-derived scope | Platform eng | Stage 4 | L3 plan reviewed | **Partial** — domain foundation only (no persistence) |
| G4 Catalog / entitlements | Contract version/idempotency/out-of-order/unsupported major tests | Platform eng | Stage 5 / POS gate | L4 plan reviewed | **Partial** — catalog + subscriptions + manual payments + Platform snapshots (P3-WP01–05); product delivery / auth still open |
| G5 Platform Admin native UI | Smoke; a11y; no Ant/Tailwind; theme/i18n basics | Platform UI | Stage 6 prep | L6 plan reviewed | **Not started** |
| G6 Mapping dry run | Mapping report; collision report; reversible audit trail | Platform + HC | HC adapter enable | L3/L5 backups verified | **Partial (P2-WP05)** — Platform dry-run only |
| G7 HC regression | 1102 safe suite (or successor); Integration/E2E in supported env | HC eng | Any HC cutover | L5 restore rehearsal pass | **Not started** (1102 not rerun in Phase 2) |
| G8 Security / tenant | Cross-org denial; no Platform Admin clinical access; concealment | Security | Production integration | L2–L3 authorized | **Not started** |
| G9 Observability | Correlation; migration IDs; projection alerts; no PHI/secrets in logs | Platform eng | Staging cutover | Rollback audited | **Not started** |
| G10 POS readiness | IDs, ProductCode, entitlement contracts, Cash/GCash/Utang, native UI, offline ownership | Portfolio | Phase 5 POS code | N/A (POS independent of HC cutover) | **Not started** |
| G11 Production staged rollout | Allowlist orgs; rollback criteria; approval record | Platform owner | Broad prod enable | L2–L6 ready | **Not started** |

**Cutover and legacy retirement:** not authorized. Persistence and authentication: not started.
