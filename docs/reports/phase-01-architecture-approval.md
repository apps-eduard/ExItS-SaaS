# Phase 1 — Architecture Approval

[Dashboard](../portfolio-progress.md) | [Approved summary](../engineering/approved-architecture-summary.md) | [Phase 2 readiness](../engineering/phase-02-readiness-checklist.md) | [P1-WP04 closeout](P1-WP04-architecture-approval-closeout.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

**Work package:** P1-WP04
**Date:** 2026-07-29
**Commit:** `01ab65b511721d5dd2173188bc6d962a5feea803`

---

## 1. Executive approval

| Field | Decision |
|---|---|
| Phase 1 recommendation | **Close with documented risks** |
| Architecture | **Approved** for controlled implementation |
| Implementation readiness | **Approved with documented non-blocking risks** |
| First Phase 2 WP | **P2-WP01 — Extraction Baseline Tag and Safety Checks** (narrow root solution foundation; not started) |
| HealthCare | Remains frozen / ignored; no import in P2-WP01 |
| Migration status | **Not started** — planning only through Phase 1 |

## 2. Evidence reviewed

P1-WP01 capability boundary and matrices · P1-WP02 contracts/ownership/classification/entitlement states · Cash/GCash MVP correction (`c5472e8`) · P1-WP03 extraction sequence, rollback, risk/gate matrices · ADR-009–013 · Phase 0 final assessment · product requirements and subscriptions · repository boundaries · UI design system (ADR-010) · risk register · release plan · FILE-MANIFEST / index / portfolio dashboard.

## 3. Portfolio architecture

```text
ExItS Platform — identity, orgs/memberships, catalog, plans/trials, subscriptions,
                 SaaS payments, entitlements/overrides, Admin, audit, support

HealthCare — clinics, workforce, patients, appointments, notes, clinical authz/audit, existing UIs

PinoyBusinessPOS — businesses, stores/branches/registers, local roles, customers,
                   Utang, retail payments, catalog/sales/inventory, expenses, suppliers,
                   shifts, returns, reports, offline, sync
```

**Confirmed:** Products do not own Platform subscriptions. Platform does not own clinical/retail ops data. Products do not access each other’s DBs. Platform is not a generic operational mega-app.

## 4. Identity and organization

Platform User = global auth identity. Platform Organization = SaaS account boundary. Multi-org users and multi-product orgs: **yes** (target). Multiple clinics/stores/branches via product-local entities. Patient ≠ User; Customer ≠ User; customers may exist without login. Customer login linkage deferred (OD-01). HC IDs preserved early; mapping reversible and auditable.

## 5. Authorization

```text
Authentication → account status → membership → product access → entitlement
→ product-local role → permission → resource scope → business rule
```

Platform owns account/membership/product access. Products own operational roles/permissions. Roles are bundles, not sole API checks. Server-derived tenant/resource scope; client OrganizationId not authoritative. Platform Admin does not auto-gain clinical/POS ops. Patient self-scope HC-only. Break-glass deferred (OD-02). Role/permission changes audited.

## 6. Data ownership

Platform SoR: identity, orgs, catalog, plans, subscriptions, SaaS payments, entitlements. HC SoR: clinical ops. POS SoR: retail ops. No cross-DB FKs; no shared DbContext/domain entities. Stable ID references only. Minimal controlled replication. See [data-ownership.md](../engineering/data-ownership.md).

## 7. Contracts and projections

Versioned additive contracts; at-least-once; idempotent consumers; out-of-order tolerance; unsupported majors quarantined. No sensitive product payloads in Platform commercial contracts. Transport deferred (OD-03). Manual reconciliation replaces commercial projection only. ADR-012.

## 8. Entitlements

Platform authoritative; products use local projections; no sync Platform call per transaction. Never-initialized / unknown paid features **fail closed**. Financial/privacy/admin fail closed. Stale durations TBD (R-022). Reconciliation does not overwrite operational records.

**Utang trial expiry — allowed:** view customers/balances/history; Cash or GCash repayment on existing debt; view payment history; renew/upgrade.
**Blocked:** new credit; increase debt; new credit entries. Post-expiry UX OD-07–09 remain open.

## 9. Payment boundaries

| Concept | Owner | MVP |
|---|---|---|
| SaaS Payment | Platform | Separate from POS |
| Retail Sale Payment | POS | `cash`, `gcash`, `customer-credit` |
| Credit Payment | POS | `cash`, `gcash` |

GCash MVP: manual confirmation; reference required/normalized; warn on duplicates (OD-11 hard-block later); no secrets stored; API/QR/webhook/gateway deferred. Platform GCash (future) ≠ POS GCash. Split tender deferred (OD-12). Voids/corrections need authz, reason, audit.

## 10. UI architecture

HC Staff: Ant retained. PatientWeb/MAUI: existing native retained. **New Platform Admin:** Blazor Web App, native Razor/CSS/isolation, tokens, density, themes, `en`/`fil`, motion + reduced-motion, a11y, responsive — **no Ant, no Tailwind**. **POS:** MAUI Blazor Hybrid, same native foundation; Android/Windows first; iOS/MacCatalyst later — **no Ant, no Tailwind**. Share models/conventions/tokens/a11y/motion — not Ant↔native switcher components. ADR-010.

## 11. Repository and dependencies

Root Git owns docs + future Platform projects. `HealthCare/` ignored nested Git. New Platform built in root; no import/submodule/subtree now; selective pattern adaptation; no wholesale copy. Shared code only with two consumers + neutrality rules. No product→Platform Infra/DbContext; no Platform→product domain; no UI→product entities; no cycles. Architecture tests later.

## 12. Extraction and rollback

Stages 1–7 approved (foundation→identity→org→catalog→Admin→HC adapter→POS). Full HC reconnection not required before POS. No auth/DB cutover without compatibility, restore rehearsal, and rollback evidence. Rollback L0–L6 documented. ADR-013.

## 13. Security

Mandatory future safeguards recorded (server scope, least privilege, permission APIs, hashed refresh, revocation, suspension, audit, redaction, no secrets in Git/contracts, no PHI/POS details in Platform logs, version validation, idempotent events, offline financial integrity, no silent financial conflict resolution, dev-only test users, controlled support). Not yet implemented — risks remain open where applicable.

## 14. Shared-code governance

Two consumers, product-neutral, no product entities, no framework UI libs, clear ownership/versioning. Prefer contracts/primitives/conventions/patterns. Avoid generic repos, shared DbContext, mega-utilities, shared permission catalogs, shared UI pages. **No shared source project in Phase 1.**

## 15. Open decisions

See [extraction-sequence.md §15](../reuse/extraction-sequence.md) and [phase-02-readiness-checklist.md](../engineering/phase-02-readiness-checklist.md). None block P2-WP01 solution foundation. OD-01–13, R-016/022/024/025–027 remain with owners and defaults.

## 16. Exit-criteria assessment

| Criterion | Classification | Notes |
|---|---|---|
| Every Phase 1 WP complete | **Satisfied** | P1-WP01–04 (this closeout) |
| Risks and decisions recorded | **Satisfied** | Register + ADRs + OD table |
| Required regression/security tests pass | **Deferred by design** | Docs-only Phase 1; 1102 baseline recorded; Integration/E2E before HC cutover (R-020) |
| Next phase explicitly approved | **Satisfied** | Phase 2 / **P2-WP01** identified; not started |

**Counts:** Satisfied **3** · Partially satisfied **0** · Deferred by design **1** · Not satisfied **0**

## 17. Implementation readiness

**Approved with documented non-blocking risks.**

**Permitted next (P2-WP01 only, when authorized):** baseline tag/safety checks; narrow root solution and Platform project skeleton; build conventions; dependency/architecture tests; HealthCare freeze verification.

**Not permitted in P2-WP01:** HC modify/import; full Platform modules; POS; billing/GCash; offline sync; HC adapters; complete UI library; DB migration/cutover.

## 18. Exact next work package

| Field | Value |
|---|---|
| ID | **P2-WP01** |
| Name | **Extraction Baseline Tag and Safety Checks** |
| Goal | Establish safe root solution foundation + baseline/safety gates before identity work |
| Expected (future) | Root `.sln` / Platform project skeleton / test projects / Directory.Build conventions / architecture dependency tests — **when authorized** |
| Exclusions | See §17 |
| Tests | Build; architecture boundary tests; `git ls-files HealthCare` empty |
| Git | Focused commits; no push without authorization; HC never staged |
| Risks | R-016 empty remote; R-017 accidental HC tracking; R-026 premature import |
| Status | **Not Started** — do not begin in P1-WP04 |
