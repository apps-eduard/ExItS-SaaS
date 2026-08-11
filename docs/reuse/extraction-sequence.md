# Extraction and Implementation Sequence

[Home](../index.md) | [Extraction rules](extraction-rules.md) | [Rollback plan](../engineering/extraction-rollback-plan.md) | [Risk matrix](../engineering/platform-extraction-risk-matrix.md) | [Gate matrix](../engineering/implementation-gate-matrix.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

**Work package:** P1-WP03  
**Status:** Authoritative planning (documentation only — no extraction executed)  
**Date:** 2026-07-29

---

## 1. Purpose

Define the safe, incremental sequence for building the new ExITS Platform from reusable HealthCare **patterns** without modifying, importing, or destabilizing the existing HealthCare application during early phases.

**This document does not authorize implementation by itself.** Phase 1 closeout (ADR-014) authorizes **P2-WP01** only when explicitly started. Stages below remain future work.

## 2. Preconditions

| Precondition | Status |
|---|---|
| Phase 0 Complete with documented risks | Met |
| P1-WP01 capability boundary | Complete |
| P1-WP02 contracts and projections | Complete |
| POS Cash / GCash / Utang MVP payments documented | Accepted (`c5472e8`) |
| P1-WP03 extraction sequence and rollback | Complete |
| P1-WP04 architecture approval (ADR-014) | Met (Phase 1 closeout) |
| HealthCare nested, ignored, read-only | Required continuously |
| Windows-safe HC baseline 1102/0/0 | Recorded (P0-WP02); re-verify before HC cutover |
| Integration/E2E environment available | Required before HC cutover (R-020) |

## 3. New-build versus extraction decision

**Decision (ADR-013):** Create the Platform foundation **new** in the ExITS root repository. Adapt verified HealthCare patterns selectively. Do **not** initially copy the complete HealthCare solution. Do **not** import HealthCare into root Git until an approved later WP.

### Classification of reusable areas

| Area | Classification |
|---|---|
| Identity (global users) | Reimplement from approved pattern (HC ApplicationUser → Platform user) |
| Authentication / JWT | Reimplement from approved pattern; HC unchanged until reconnection |
| Refresh tokens | Reimplement from approved pattern (rotation/family revoke) |
| Organizations | Reimplement as PlatformOrganization; map HC orgs later |
| Memberships | Build new multi-org/product-access model; adapt from StaffMember lessons |
| Platform roles | Build new / adapt PLATFORM_ADMIN patterns |
| Product operational roles | Keep exclusively in HealthCare / POS |
| Permissions catalogs | Split: Platform access vs product ops — do not share one mega-catalog |
| Tenant context | Adapt pattern (server-derived scope); never trust client org IDs |
| Audit (security/org) | Adapt pattern for Platform; clinical audit stays in HC |
| ProblemDetails / Validation / Pagination | Adapt selected design or shared contracts later (two consumers) |
| BFF / session handling | Adapt pattern; product-local hosts |
| Background jobs | Adapt Hangfire *hosting* pattern; do not share HC job DB/workers |
| Product catalog / Plans / Trials / Subscriptions / SaaS payments / Entitlements | **Build entirely new** |
| Platform Admin UI | **Build entirely new** (native CSS/Razor; no Ant, no Tailwind) |
| Shared UI models / token names | Adapt conventions; no shared Ant components |
| Localization / Themes | Build new for Platform Admin & POS (`en`/`fil`, Light/Dark/System) |
| POS offline and synchronization | Build entirely new in POS (Phase 5–7) |
| Clinical domain (Patient, Clinic, notes, appointments) | Keep exclusively in HealthCare |
| Ant Design Blazor / HC Staff CSS | Keep exclusively in HealthCare Staff Web |
| Patient self-scope | Keep exclusively in HealthCare |
| Password hashes / wholesale Identity DB copy | **Do not reuse** without separately approved migration plan |
| HC migrations history | **Do not reuse** / do not copy into Platform |

## 4. Stage-by-stage sequence

Document only — **do not create projects in P1-WP03**.

### Stage 1 — Repository and solution foundation

Future: root solution, Platform projects, test projects, build conventions, dependency rules. **No HealthCare import.** Gate: architecture tests green; `git ls-files HealthCare` empty.

### Stage 2 — Platform identity foundation

Future: global users, authentication, refresh tokens, sessions, security events. **No HC authentication change.** Gate: identity tests; no credential leakage.

### Stage 3 — Organization and membership foundation

Future: Platform organizations, memberships, Platform roles, product access. Gate: tenant tests; multi-org membership tests.

### Stage 4 — Product catalog and entitlement foundation

Future: products, plans, trials, subscriptions, entitlements, local-projection contracts. Gate: contract tests (versioning, idempotency, unsupported majors).

### Stage 5 — Native Platform Admin foundation

Future: Blazor Web App, native CSS, reusable Razor components, themes, localization, compact responsive layout. **No Ant Design.** Gate: UI smoke + a11y checklist; ADR-010 compliance.

### Stage 6 — HealthCare adapter / reconnection preparation

Only after Platform behavior is proven: identity mapping, organization mapping, contract adapters, migration dry runs, compatibility testing. **HealthCare remains unchanged until an approved Phase 2+ WP.** Gate: mapping reports; HC regression (1102+); Integration/E2E in supported env; rollback rehearsal.

### Stage 7 — PinoyBusinessPOS foundation

Begin only after POS readiness gate (§13). Does **not** require full HC reconnection. Gate: contracts stable; Cash/GCash/Utang requirements intact.

## 5. Dependency direction

Conceptual (future projects):

```text
Platform.Domain
    ↑
Platform.Application
    ↑
Platform.Infrastructure
    ↑
Platform.Api
```

```text
Shared.Contracts (versioned, product-neutral)
    ← Platform and products may reference
```

**Prohibit:** product → Platform EF entities/DbContext; Platform → POS or HC domain; Shared → product projects; UI library → product business entities; cyclic references.

Architecture tests required later to enforce these rules.

## 6. Identity continuity

- Preserve existing HealthCare user IDs; do not rewrite clinical FKs in early phases.
- Introduce explicit, reversible, auditable mapping to PlatformUserId where needed.
- No destructive identifier migration.
- Do not assume HC IDs can be replaced safely.
- Do not copy password hashes without separately approved and validated plan.
- Authentication issuer/audience changes require controlled rollout and session invalidation plan.
- Historical actor references must remain interpretable after mapping.

## 7. Organization continuity

- Existing HC Organization remains authoritative for HealthCare until approved cutover.
- Platform Organization is future SaaS account authority.
- Mapping must support one Platform Organization → multiple clinics.
- Do not rename Clinic→Organization or Organization→Store.
- No destructive schema changes during early extraction.
- Migration must support verification and rollback.

## 8. Authorization continuity

- Platform product access ≠ HC operational roles ≠ POS operational roles.
- HC permission behavior unchanged until approved reconnection.
- Prove: no privilege escalation; no lost clinical permissions; no cross-org access; Platform admins do not gain clinical access; concealment retained; patient self-scope stays HC-local.

## 9. Database sequence

Target databases (future): `ExItS_Platform` / `ExItS_HealthCare` / `ExItS_PinoyBusinessPOS`.

Initial HC database **unchanged**.

Future process: Inventory → Backup → Restore rehearsal → Mapping prep → Dry run → Validation → Controlled migration → Dual-read only if approved → Cutover → Post-cutover verification → Rollback window.

**Prohibit:** editing old HC migrations; copying migration history into Platform; cross-DB FKs; destructive migration without tested restore; automatic cascading deletion across databases.

## 10. Testing gates

See [implementation-gate-matrix.md](../engineering/implementation-gate-matrix.md).

Summary: foundation (architecture, build, unit) → identity → tenant → contract → migration → HC regression (1102 baseline; Integration/E2E before cutover).

## 11. Observability gates

Before integration: CorrelationIds, migration run IDs, mapping counts, failed-record reports, auth failures, entitlement projection status, reconciliation alerts, security-event logging. No secrets in logs. No clinical payloads in Platform logs. Rollback actions audited.

## 12. Rollout gates

Environments: Development → Testing → Staging → Production.

- Platform deploys independently; HC stays on existing deployment initially.
- Integration starts in Development; selected test organizations only.
- No all-customer cutover; staged production with explicit rollback criteria.
- Feature flags conceptual: disabled by default; env enablement; org/product allowlist; read-only verification; shadow comparison; dual-write prohibited unless designed; rollback switch; compatibility-version checks; audit of config changes. **Not implemented in P1-WP03.**

## 13. POS readiness gate

Stable before POS foundation (Stage 7 / Phase 5):

- PlatformOrganizationId, PlatformUserId
- ProductCode conventions
- Subscription and entitlement contracts + local projection policy
- Product-access model
- Audit correlation conventions
- Native UI strategy (ADR-010)
- Repository/solution structure (Stage 1+)
- Cash, GCash, Utang MVP requirements
- Offline ownership boundaries

POS does **not** need full HealthCare reconnection first.

## 14. Explicit exclusions (this WP and early stages)

- No HealthCare file changes or import
- No wholesale HC solution copy
- No Platform/POS/Shared source folders in P1-WP03
- No Ant in Platform Admin or POS
- No password-hash migration without approved plan
- No claim that migration or extraction has occurred
- No P1-WP04 / Phase 2 implementation in this WP

## 15. Open decisions (assigned; do not guess)

| ID | Decision required | Owner | Target | Blocks P1-WP03? | Default safe behavior until resolved |
|---|---|---|---|---|---|
| OD-01 | Customer ↔ Platform User login linkage | Product + Platform | Phase 6+ | No | Customers exist without login |
| OD-02 | Break-glass into product ops | Platform security | Phase 4 / 9 | No | No clinical/POS ops for Platform Admin |
| OD-03 | Entitlement transport | Platform architecture | Phase 3 | No | Document contracts only; no bus chosen |
| OD-04 | MFA | Platform security | Later | No | Password + refresh only |
| OD-05 | HealthCare import timing | Portfolio lead | After Platform foundation | No | Keep nested ignored |
| OD-06 | Multi-org from HC StaffMember | Platform + HC | Phase 2 | No | HC single membership until cutover WP |
| OD-07 | Cash-only customer after trial expiry | POS | Phase 6 | No | **Resolved (P6-WP05) — Deny** in PastDue/Cancelled/Expired |
| OD-08 | Edit customer contact after expiry | POS | Phase 6 | No | **Resolved (P6-WP05) — Deny** in PastDue/Cancelled/Expired |
| OD-09 | Historical credit correction policy | POS | Phase 6 | No | **Resolved (P6-WP05)** — credit reverse in continuity; repayment reverse only Trialing/Active/GracePeriod |
| OD-10 | Legal retention periods | Compliance | Before commercial launch | No | Retain; no cascade delete |
| OD-11 | GCash duplicate hard-block vs warn | POS | Phase 6 / 8 | No | **Warn** minimum |
| OD-12 | Split tender timing | POS | Later | No | Single tender MVP |
| OD-13 | Direct GCash API timing | POS + Platform | Post-MVP | No | Manual GCash only |
| R-022 | Entitlement stale/refresh durations | Platform + products | Phase 3 / 7 | No | Categorical states only |
| R-024 | Contract version skew ops | Platform | Phase 3+ | No | Quarantine unsupported majors |
