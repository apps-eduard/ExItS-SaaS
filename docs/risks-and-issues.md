# Risks and Issues

[Home](index.md) | [Dashboard](portfolio-progress.md)

| ID | Risk | Priority | Mitigation | Status |
|---|---|---|---|---|
| R-001 | HealthCare regression during extraction | Critical | Baseline tag, tests, small extraction steps, rollback | Open |
| R-002 | Healthcare-specific rules become generic platform rules | Critical | Classification matrix and architecture review | Open — matrix started in P0-WP01 |
| R-003 | Platform outage blocks product operations | Critical | Local entitlement projection/snapshot | Open |
| R-004 | Cross-product or cross-tenant data leakage | Critical | Separate DBs, server context, isolation tests | Open |
| R-005 | Ant Design coupling spreads into POS | Medium | UI wrappers and separate native POS library | Open — confirmed staff-only AntDesign 1.6.2 |
| R-006 | Native reusable components become a full UI-framework project | High | Build only phase-needed components | Open |
| R-007 | English-only strings escape into release | Medium | Resource-completeness tests | Open — HC has no i18n yet |
| R-008 | Dark theme creates poor contrast | High | Semantic tokens and accessibility tests | Open — HC lacks Light/Dark/System preference |
| R-009 | Duplicate offline financial transactions | Critical | Idempotency and append-only ledger | Open |
| R-010 | Nested `HealthCare/.git` inside ExITS monorepo | High | Decide: subtree import vs remove nested metadata with approval; do not commit blindly | Open — found P0-WP01 |
| R-011 | No EF global tenant query filters (service-only isolation) | Critical | Keep service checks; add filters/tests before multi-product sharing | Open — verified P0-WP01 |
| R-012 | Plans/trials/subscriptions/billing/entitlements missing | High | Implement on Platform in Phase 3; do not fake via HC limits alone | Open — verified P0-WP01 |
| R-013 | Parent repo has no root `.gitignore`; risk committing `.env`/bin/obj | High | Add root ignore before tracking HealthCare sources | Open — found P0-WP01 |
| R-014 | Full `HealthCare.sln` build fails without Android SDK (Mobile) | Medium | Document non-MAUI build path; install SDK on CI agents that need Mobile | Open — baseline P0-WP01 |
| R-015 | Pre-existing dirty PatientWeb files inside nested HealthCare git | Medium | Do not overwrite; resolve in HealthCare repo or later import WP | Open — observed P0-WP01 |
