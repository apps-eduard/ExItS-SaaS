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
| R-012 | Plans/trials/subscriptions/billing/entitlements missing | High | Catalog + subscription + manual payments + snapshots in P3-WP01–04; invoices/auto-billing/delivery still deferred | **Mitigated** (P3-WP05) — collection/delivery incomplete |
| R-013 | Parent repo missing root `.gitignore` | High | Root `.gitignore` added in P0-WP02 | **Mitigated** (P0-WP02) |
| R-014 | Full `HealthCare.sln` build fails without Android SDK env | Medium | Non-MAUI build path documented; set `ANDROID_HOME` or `AndroidSdkDirectory` on agents that need Mobile | Open — SDK folder present but env unset (P0-WP02) |
| R-015 | Pre-existing dirty PatientWeb files inside nested HealthCare git | Medium | Do not overwrite; resolve in HealthCare repo or later import WP | Open — still present P0-WP02 |
| R-016 | Root `origin` remote empty; `origin/main` gone | High | User-authorized first push of `main` + `phase-1-approved` | **Closed** (P2-WP05 Part A — remote verified) |
| R-017 | Accidental HealthCare parent tracking | Critical | Root ignore + architecture `RepositorySafetyTests` + pre-commit checks | **Mitigated** (tests + process) — keep verifying |
| R-018 | Nested HealthCare local `.env` / lab secrets | High | Remain gitignored; never commit or paste values into portfolio docs | Open — presence known; values not documented |
| R-019 | Dual UI stacks (HC Ant vs **native** Platform Admin + POS) — brand drift, duplicated visuals, separate a11y/theme work, future HC modernization cost | Medium | Shared semantic tokens, branding, terminology, UI-independent contracts; separate framework impls; no forced HC rewrite in current MVP | Open — **controlled technical separation** (ADR-010) |
| R-020 | Phase 0 closed while Integration/E2E not re-baselined on this machine | Medium | Run Integration/E2E on approved Ubuntu/Compose agents before extraction (Phase 2 gate) | Open — deferred by design from P0-WP02 |
| R-021 | Empty root remote delays shared portfolio publication | Medium | User-authorized `git push -u origin main` when ready | **Closed** with R-016 (P2-WP05 Part A) |
| R-022 | Entitlement projection staleness durations / conflict numerics underspecified | High | P1-WP02 defined states; P3-WP04 ships provisional 24h refresh policy via `IEntitlementRefreshPolicy` — **not** final | Open — provisional durations only (P3-WP04) |
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
| R-045 | Catalog/organization/subscription API endpoints are unauthenticated (development-stage) | Critical | Document limitation; require auth before production; no fake identity | Open — introduced P3-WP01; expanded P3-WP02 |
| R-046 | Local-dev connection strings / accidental auto-migrate or wrong DB target | High | No Migrate() at startup; isolated Docker port 5434; document workflow | Open — introduced P3-WP01 |
| R-047 | Manual/commercial ActivateSubscription mistaken for payment verification | High | Docs + API comments; confirmed SaaS payment required for payment-activation path; still not gateway-verified | **Mitigated** (P3-WP05) — awareness; auto-verify still open |
| R-048 | Missed trial/paid/grace expiration without background scheduler | Medium | Explicit Expire/PastDue/Grace commands + lifecycle evaluator; no Hangfire yet | Open — introduced P3-WP02 |
| R-049 | Repeat-trial eligibility policy undecided (one-trial-ever vs allow after Cancelled/Expired) | Medium | Safe default: allow when no active-like slot; document open decision | Open — introduced P3-WP02 |
| R-050 | Unsecured subscription lifecycle mutation endpoints before production | Critical | Same gate as R-045; do not expose beyond development | Open — introduced P3-WP02 |
| R-051 | Manual payment confirmation fraud/error without separation of duties | High | Require authenticated operator with payment-confirm permission; audit actor + timestamp; separation of duties | Open — introduced P3-WP03 |
| R-052 | Duplicate external reference scope is provisional (method + org) | Medium | Document provisional scope; review when multi-method or multi-org patterns emerge | Open — introduced P3-WP03 |
| R-053 | Payment amount not auto-reconciled against catalog plan price | High | Record manual amount; require explicit confirmation; defer automated price validation | Open — introduced P3-WP03 |
| R-054 | Void/reversal has no invoice or credit-note linkage | Medium | Void records reason + actor; no invoice/credit-note engine yet | Open — introduced P3-WP03 |
| R-055 | Unauthenticated payment mutation endpoints (production gate) | Critical | Same gate as R-045/R-050; payment confirmation requires auth before production | Open — introduced P3-WP03 |
| R-056 | No reconciliation engine for manual payments | Medium | Manual payments are recorded and confirmed by operator; no automated bank/GCash reconciliation | Open — introduced P3-WP03 |
| R-057 | Manual payment mistaken for automatic gateway integration | High | Documentation explicitly states no gateway; architecture tests forbid gateway/webhook/QR types | **Mitigated** (P3-WP05) — awareness; gateway still absent by design |
| R-058 | Snapshot-version race under concurrent generation | Medium | Unique index on (org, product, version); conflict → 409 | **Mitigated** (P3-WP04/05) |
| R-059 | Feature override misuse without authentication / separation of duties | High | Require authenticated operator with override permission before production | Open — introduced P3-WP04 |
| R-060 | Authoritative snapshot mistaken for completed product delivery | High | Docs + APIs state Platform-only persistence; no broker/delivery routes; closeout E2E asserts 404 on delivery paths | **Mitigated** (P3-WP05) — awareness; delivery still deferred |
| R-061 | Manual snapshot regeneration gaps without scheduler | Medium | Explicit generate/reconcile commands; no Hangfire yet | Open — introduced P3-WP04 |
| R-062 | Unauthenticated entitlement/override mutation endpoints | Critical | Same gate as R-045; do not expose beyond development | Open — introduced P3-WP04 |
| R-063 | Unauthenticated Platform Admin UI | Critical | Banner + docs; require Platform auth before production; no fake login | Open — introduced P4-WP01 |
| R-064 | Development operator context mistaken for authorization | High | Footer labels “not authorization”; disabled outside Development/Testing | Open — introduced P4-WP01 |
| R-065 | Admin UI / Platform API contract drift | Medium | Typed client + integration tests for Admin endpoints; keep DTOs aligned | Open — introduced P4-WP01 |
| R-066 | Partial dashboard counts mistaken for zeros | Medium | PartialFailures list; UI shows “—” for failed sections | **Mitigated** (P4-WP01) — awareness |
| R-067 | Admin screens mistaken for complete operational control | Medium | Explicit exclusions on deferred pages; mutation pages warn about auth/delivery limits | Open — updated P4-WP03 |
| R-068 | Manual SaaS payment view mistaken for provider verification | High | Warning copy on payment pages; confirm action labels manual verification | Open — reinforced P4-WP03 |
| R-069 | Entitlement snapshot view mistaken for completed product delivery | High | Warning copy on entitlement pages; R-060 awareness | Open — reinforced P4-WP01 |
| R-070 | Admin accessibility / large-list performance gaps | Medium | Semantic HTML + pagination; expand a11y tests in later Admin WPs | Open — introduced P4-WP01 |
| R-071 | Platform API unavailable while Admin is running | Medium | Unavailable error state; configurable base URL + timeouts | **Mitigated** (P4-WP01) — awareness |
| R-072 | Unauthenticated user/membership/product-access mutation APIs | Critical | Development-stage only; require auth before production; no fake claims | Open — introduced P4-WP02 |
| R-073 | Development operator acting without authorization on access changes | Critical | Banner/warnings; server-side authorization still required | Open — introduced P4-WP02 |
| R-074 | Platform organization role confused with product-local role | High | Docs + UI warnings; no product-role columns/selectors; architecture guards | Open — introduced P4-WP02 |
| R-075 | Product-access assignment mistaken for completed provisioning | High | Explicit commercial-entry wording; no delivery implementation | Open — introduced P4-WP02 |
| R-076 | Membership revocation propagation gaps | Medium | Cascade revoke of active assignments; effective-access fail-closed | **Mitigated** (P4-WP02) — awareness |
| R-077 | Subscription/entitlement changes not reflected immediately on assignments | Medium | Effective evaluation re-reads subscription + snapshot; historical rows may remain Active | Open — introduced P4-WP02 |
| R-078 | Cross-organization access leakage | High | Org-scoped membership/assignment checks; unique constraints; API tests | **Mitigated** (P4-WP02) — awareness |
| R-079 | Duplicate username/email policy edge cases | Medium | Global unique normalized username/email; 409 conflicts | **Mitigated** (P4-WP02) — awareness |
| R-080 | Missing invitation workflow | Medium | Deferred; add existing users only | Open — introduced P4-WP02 |
| R-081 | Missing identity-provider linkage | High | Deferred with authentication WP | Open — introduced P4-WP02 |
| R-082 | Missing dedicated audit subsystem for access changes | Medium | Actor/reason/UTC metadata on rows; full audit trail deferred to P4-WP04 | Open — introduced P4-WP02 |
| R-083 | Admin UI contract drift for access endpoints | Medium | Typed client + integration tests | Open — introduced P4-WP02 |
| R-084 | Concurrency during concurrent access changes | Medium | PostgreSQL `xmin`; 409 on conflict | Open — introduced P4-WP02 |
| R-085 | Unauthenticated subscription/payment mutation via Admin | Critical | Development-stage only; same production gate as R-055/R-063; no fake auth | Open — introduced P4-WP03 |
| R-086 | Manual payment confirmation fraud or operator error | High | Actor/reason/UTC; void path; no automated bank/GCash reconciliation (R-056) | Open — introduced P4-WP03 |
| R-087 | Admin payment UI mistaken for gateway integration | High | Explicit no-gateway copy; architecture guards forbid Stripe/PayPal/card fields | Open — introduced P4-WP03 |
| R-088 | Subscription Admin changes mistaken for product provisioning | High | UI warnings; no entitlement delivery routes; fail-closed access evaluation only | Open — introduced P4-WP03 |
| R-089 | Provisional repeat-trial policy misapplied as automatic approval | Medium | Conflict/warning only; no automatic repeat-trial approval rules | Open — introduced P4-WP03 |
| R-090 | Concurrent Admin commercial lifecycle mutations | Medium | Domain concurrency + 409 ProblemDetails; UI refreshes after success | Open — introduced P4-WP03 |

## Phase 4 note (P4-WP03)

P4-WP03 delivered Admin subscription lifecycle, trial start, and manual SaaS payment confirmation/activation by reusing Phase 3 APIs. Authentication, gateway/invoice automation, entitlement delivery, R-035 calendar EOM, and production authorization remain open. Next: **P4-WP04 — Audit, Authorization and Closeout** when authorized.

## Phase 4 note (P4-WP02)

P4-WP02 delivered Platform users, memberships, product-access assignments, and effective commercial access evaluation. Authentication, SSO/AD, product delivery, and production authorization remain open. Next: **P4-WP03 — Subscriptions, Payments and Trials** when authorized.

## Phase 4 note (P4-WP01)

P4-WP01 delivered a **read-only** Platform Admin shell. Authentication, membership Admin, mutation workflows, and product delivery remain open. Next: **P4-WP02 — Organizations, Users and Product Access** when authorized.

## Phase 3 closeout note (P3-WP05)

Phase 3 is **Complete with documented risks**. Commercial catalog, subscription lifecycle, manual SaaS payments, and entitlement snapshots are validated. Authentication, product delivery, invoices/gateways, R-022 numeric refresh policy, and R-035 calendar EOM remain open. Next: Phase 4 / P4-WP01 when authorized.

## Phase 2 closeout note (P2-WP06)

Phase 2 **closed with documented non-blocking risks**. Dry-run/contract success does **not** close R-020, R-027, R-031–R-044, or cutover gates. Next: Phase 3 / P3-WP01 when authorized.
