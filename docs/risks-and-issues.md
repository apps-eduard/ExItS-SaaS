# Risks and Issues

[Home](index.md) | [Dashboard](portfolio-progress.md)

| ID | Risk | Priority | Mitigation | Status |
|---|---|---|---|---|
| R-001 | HealthCare regression during extraction | Critical | Baseline tag, tests, small extraction steps, rollback | Open |
| R-002 | Healthcare-specific rules become generic platform rules | Critical | Classification matrix and architecture review | Open — matrix started in P0-WP01 |
| R-003 | Platform outage blocks product operations | Critical | Local projections + state matrix (ADR-011/012, P1-WP02); numeric stale windows still Phase 3/7 | Open — states documented; durations deferred (R-022) |
| R-004 | Cross-product or cross-tenant data leakage | Critical | Separate DBs, server context, isolation tests | Open |
| R-005 | Ant Design coupling spreads into POS **or new Platform Admin** | Medium | ADR-010: Ant only in existing HC Staff Web; native stack for Platform Admin + POS | **Mitigated** (strategy) — watch during Phase 4–5+ |
| R-006 | Native reusable components become a full UI-framework project | High | Phase-gated catalog (MVP/Utang/Store/Full); build only phase-needed components | Open — catalog defined P0-WP03 |
| R-007 | English-only strings escape into release | Medium | Resource-completeness tests; POS `en`/`fil` from MVP | Open — HC has no i18n; POS greenfield (P0-WP03) |
| R-008 | Dark theme creates poor contrast | High | Semantic tokens and accessibility tests; Light/Dark/System for POS | Open — HC lacks product theme switch; POS tokens planned (P0-WP03) |
| R-009 | Duplicate offline financial transactions | Critical | Idempotency and append-only ledger | Open |
| R-010 | Nested `HealthCare/.git` inside ExITS monorepo | High | Root ignores `HealthCare/`; decide import/submodule/subtree later — do not delete nested `.git` | Mitigated (ignore) — integration decision still Open |
| R-011 | No EF global tenant query filters (service-only isolation) | Critical | Keep service checks; add filters/tests before multi-product sharing | Open — verified P0-WP01 |
| R-012 | Plans/trials/subscriptions/billing/entitlements missing | High | Catalog persistence in P3-WP01; subscription/billing still Phase 3+ | Open — catalog done; billing incomplete |
| R-013 | Parent repo missing root `.gitignore` | High | Root `.gitignore` added in P0-WP02 | **Mitigated** (P0-WP02) |
| R-014 | Full `HealthCare.sln` build fails without Android SDK env | Medium | Non-MAUI build path documented; set `ANDROID_HOME` or `AndroidSdkDirectory` on agents that need Mobile | Open — SDK folder present but env unset (P0-WP02) |
| R-015 | Pre-existing dirty PatientWeb files inside nested HealthCare git | Medium | Do not overwrite; resolve in HealthCare repo or later import WP | Open — still present P0-WP02 |
| R-016 | Root `origin` remote empty; `origin/main` gone | High | User-authorized first push of `main` + `phase-1-approved` | **Closed** (P2-WP05 Part A — remote verified) |
| R-017 | Accidental HealthCare parent tracking | Critical | Root ignore + architecture `RepositorySafetyTests` + pre-commit checks | **Mitigated** (tests + process) — keep verifying |
| R-018 | Nested HealthCare local `.env` / lab secrets | High | Remain gitignored; never commit or paste values into portfolio docs | Open — presence known; values not documented |
| R-019 | Dual UI stacks (HC Ant vs **native** Platform Admin + POS) — brand drift, duplicated visuals, separate a11y/theme work, future HC modernization cost | Medium | Shared semantic tokens, branding, terminology, UI-independent contracts; separate framework impls; no forced HC rewrite in current MVP | Open — **controlled technical separation** (ADR-010) |
| R-020 | Phase 0 closed while Integration/E2E not re-baselined on this machine | Medium | Run Integration/E2E on approved Ubuntu/Compose agents before extraction (Phase 2 gate) | Open — deferred by design from P0-WP02 |
| R-021 | Empty root remote delays shared portfolio publication | Medium | User-authorized `git push -u origin main` when ready | **Closed** with R-016 (P2-WP05 Part A) |
| R-022 | Entitlement projection staleness durations / conflict numerics underspecified | High | P1-WP02 defined states, idempotency, fail-closed rules; set exact windows in Phase 3 / 7 | Open — categorical behavior accepted; durations TBD |
| R-023 | Premature shared library / mega-utility before two consumers | Medium | Shared-code governance in capability boundary §22; prefer contracts/conventions | Open — governance documented P1-WP01 |
| R-024 | Contract major-version skew between Platform and products | High | Version negotiation, migration windows, quarantine unsupported majors (ADR-012) | Open — policy documented P1-WP02; runtime later |
| R-025 | Manual GCash recording errors / duplicate references | Medium | Required normalized reference; warn on duplicates (OD-11); cashier confirmation UX; no secrets stored; sync re-check | Open — documented POS MVP payment correction |
| R-026 | Premature HealthCare import or wholesale copy before Platform gates | Critical | ADR-013; P2-WP01 foundation only; HC not in solution | **Mitigated** (P2-WP01 evidence) — watch later WPs |
| R-027 | Cutover without restore rehearsal / rollback evidence | Critical | Rollback plan L5; gate G6–G7 require rehearsal | Open — enforced at Phase 2 cutover |
| R-028 | Phase 1 closed while Integration/E2E and numeric entitlement windows remain open | Medium | Documented deferred; re-baseline before HC cutover; R-022 for durations | Open — accepted with Phase 1 closeout |
| R-029 | Solution format / SDK pin drift (`.slnx` + `global.json` 10.0.302) | Low | Pin recorded; CI should use `global.json` when added | Open — introduced P2-WP01 |
| R-030 | Local port collision for Platform API (5188 busy on assessment machine) | Low | Default launch URL set to **5288** | **Mitigated** (P2-WP01) |
| R-031 | Identity domain exists without authentication/persistence — misuse if callers assume login works | Medium | Docs + API has no identity routes; P2-WP02 report states auth absent | Open — introduced P2-WP02 |
| R-032 | Active membership uniqueness only enforced in application until DB unique index | Medium | Documented invariant; add unique constraint in persistence WP | Open — introduced P2-WP02 |
| R-033 | Commercial catalog codes uniqueness only at application boundary until persistence | Medium | Duplicate checks in use cases; DB unique indexes later | **Mitigated (P3-WP01)** — DB unique constraints + integration tests |
| R-034 | Entitlement composer policies (suspend/cancel/expiry) may need product-specific tuning | Medium | POS Utang codes explicit; keep composer product-neutral where possible | Open — introduced P2-WP03 |
| R-035 | PinoyBusinessPOS Utang trial is three calendar months; end-of-month rule undecided; Platform must not use 90-day substitute | High | Document calendar-month policy; keep TrialDefinition configurable; implement calendar math + EOM rule in later catalog/config WP | Open — corrected after P2-WP03 |
| R-036 | Contract major-version incompatibility between Platform and HealthCare consumers | High | Fail closed on unsupported majors; version negotiation later | Open — introduced P2-WP04 |
| R-037 | Projection version gaps / duplicate / conflict until transport + checkpoint persistence exist | High | Apply policy + reconciliation interfaces; implement transport later | Open — introduced P2-WP04 |
| R-038 | Organization mapping errors (1 Platform org → many clinics) | High | Explicit reversible mapping contracts; no destructive ID rewrite | Open — introduced P2-WP04 |
| R-039 | Accidental clinical role escalation from Platform org roles | Critical | Contracts exclude clinical roles; docs + architecture tests | Open — introduced P2-WP04 |
| R-040 | Premature assumption that contracts equal completed HealthCare integration | High | Report states foundation only; HC freeze continues | Open — introduced P2-WP04 |
| R-041 | Duplicate identity / normalized-identifier collision during future mapping | High | Preflight detects duplicates; ambiguous → manual review | Open — introduced P2-WP05 |
| R-042 | False-positive email/username mapping treated as safe identity merge | High | Exact identifier match alone warns; explicit approved mapping preferred | Open — introduced P2-WP05 |
| R-043 | Migration dry-run mistaken for completed production migration | High | Status uses `Validated` not `Migrated`; docs + R-040 related | Open — introduced P2-WP05; reinforced P2-WP06 |
| R-044 | Incomplete rollback evidence before cutover | Critical | Rollback readiness validator; R-027 restore rehearsal still required | Open — introduced P2-WP05 |
| R-045 | Catalog API endpoints are unauthenticated (development-stage) | Critical | Document limitation; require auth before production; no fake identity | Open — introduced P3-WP01 |
| R-046 | Local-dev connection strings / accidental auto-migrate or wrong DB target | High | No Migrate() at startup; isolated Docker port 5434; document workflow | Open — introduced P3-WP01 |

## Phase 2 closeout note (P2-WP06)

Phase 2 **closed with documented non-blocking risks**. Dry-run/contract success does **not** close R-020, R-027, R-031–R-044, or cutover gates. Next: Phase 3 / P3-WP01 when authorized.
