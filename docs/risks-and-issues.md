# Risks and Issues

[Home](index.md) | [Dashboard](portfolio-progress.md)

| ID | Risk | Priority | Mitigation | Status |
|---|---|---|---|---|
| R-001 | HealthCare regression during extraction | Critical | Baseline tag, tests, small extraction steps, rollback | Open |
| R-002 | Healthcare-specific rules become generic platform rules | Critical | Classification matrix and architecture review | Open — matrix started in P0-WP01 |
| R-003 | Platform outage blocks product operations | Critical | Local entitlement projection/snapshot (ADR-011 / P1-WP01); transport & stale policy detail in P1-WP02 / Phase 3 | Open — direction accepted; implementation deferred |
| R-004 | Cross-product or cross-tenant data leakage | Critical | Separate DBs, server context, isolation tests | Open |
| R-005 | Ant Design coupling spreads into POS **or new Platform Admin** | Medium | ADR-010: Ant only in existing HC Staff Web; native stack for Platform Admin + POS | **Mitigated** (strategy) — watch during Phase 4–5+ |
| R-006 | Native reusable components become a full UI-framework project | High | Phase-gated catalog (MVP/Utang/Store/Full); build only phase-needed components | Open — catalog defined P0-WP03 |
| R-007 | English-only strings escape into release | Medium | Resource-completeness tests; POS `en`/`fil` from MVP | Open — HC has no i18n; POS greenfield (P0-WP03) |
| R-008 | Dark theme creates poor contrast | High | Semantic tokens and accessibility tests; Light/Dark/System for POS | Open — HC lacks product theme switch; POS tokens planned (P0-WP03) |
| R-009 | Duplicate offline financial transactions | Critical | Idempotency and append-only ledger | Open |
| R-010 | Nested `HealthCare/.git` inside ExITS monorepo | High | Root ignores `HealthCare/`; decide import/submodule/subtree later — do not delete nested `.git` | Mitigated (ignore) — integration decision still Open |
| R-011 | No EF global tenant query filters (service-only isolation) | Critical | Keep service checks; add filters/tests before multi-product sharing | Open — verified P0-WP01 |
| R-012 | Plans/trials/subscriptions/billing/entitlements missing | High | Implement on Platform in Phase 3; do not fake via HC limits alone | Open — verified P0-WP01 |
| R-013 | Parent repo missing root `.gitignore` | High | Root `.gitignore` added in P0-WP02 | **Mitigated** (P0-WP02) |
| R-014 | Full `HealthCare.sln` build fails without Android SDK env | Medium | Non-MAUI build path documented; set `ANDROID_HOME` or `AndroidSdkDirectory` on agents that need Mobile | Open — SDK folder present but env unset (P0-WP02) |
| R-015 | Pre-existing dirty PatientWeb files inside nested HealthCare git | Medium | Do not overwrite; resolve in HealthCare repo or later import WP | Open — still present P0-WP02 |
| R-016 | Root `origin` remote exists but is empty; `origin/main` gone | High | User-authorized first push: `git push -u origin main` (do not force-push) | Open — verified P0-WP02 |
| R-017 | Accidental HealthCare parent tracking | Critical | Root ignore + `git ls-files HealthCare` / `git check-ignore` checks before commit | Mitigated (P0-WP02 process) |
| R-018 | Nested HealthCare local `.env` / lab secrets | High | Remain gitignored; never commit or paste values into portfolio docs | Open — presence known; values not documented |
| R-019 | Dual UI stacks (HC Ant vs **native** Platform Admin + POS) — brand drift, duplicated visuals, separate a11y/theme work, future HC modernization cost | Medium | Shared semantic tokens, branding, terminology, UI-independent contracts; separate framework impls; no forced HC rewrite in current MVP | Open — **controlled technical separation** (ADR-010) |
| R-020 | Phase 0 closed while Integration/E2E not re-baselined on this machine | Medium | Run Integration/E2E on approved Ubuntu/Compose agents before extraction (Phase 2 gate) | Open — deferred by design from P0-WP02 |
| R-021 | Empty root remote delays shared portfolio publication | Medium | User-authorized `git push -u origin main` when ready | Open — R-016 related |
| R-022 | Entitlement projection staleness / conflict handling underspecified for runtime | High | Specify refresh, fail-safe, idempotency in P1-WP02 / Phase 3; ADR-011 records authority model only | Open — introduced P1-WP01 |
| R-023 | Premature shared library / mega-utility before two consumers | Medium | Shared-code governance in capability boundary §22; prefer contracts/conventions | Open — governance documented P1-WP01 |
