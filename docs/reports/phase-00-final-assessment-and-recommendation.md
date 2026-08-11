# Phase 0 Final Assessment and Recommendation

[Dashboard](../portfolio-progress.md) | [Closeout report](P0-WP04-assessment-closeout.md) | [Final boundaries](../engineering/final-portfolio-boundaries.md) | [Phase 0](phase-00-final-assessment-and-recommendation.md)

**Work package:** P0-WP04  
**Date:** 2026-07-29  
**Closeout commit:** `f52316ae60198cb3dfee367a8ec99d550965ea44`  
**HealthCare freeze:** Held (read-only; not tracked by root Git)

---

## 1. Executive decision

**Close Phase 0 with documented risks.**

HealthCare is suitable for **controlled platform extraction** of identity, organization, permission, audit, and cross-cutting API patterns — not a wholesale move. New ExITS Platform Admin and PinoyBusinessPOS share a **native** CSS/Razor UI foundation (**no Ant Design**, **no Tailwind**). Existing HealthCare Staff Web retains Ant Design. Plans/billing/entitlements are **missing** and must be built on the Platform. Next work: **P1-WP01 — Platform vs Product Capability Boundary** (documentation/architecture only; no HealthCare import; no application skeleton yet unless Phase 1 explicitly expands).

---

## 2. Evidence reviewed

| Source | Role |
|---|---|
| P0-WP01 assessment + matrix + report | Structure, stack, reuse classes |
| P0-WP02 runtime baseline, boundaries, env, report | Git safety, ports, DB, build/test baseline (1102/0/0) |
| P0-WP03 UI assessment, design system, catalog, ADR-010 (+ correction) | UI strategy |
| Architecture, data-ownership, security, authz, contracts | Target boundaries |
| Product vision, POS requirements, subscriptions, release plan | Market and commercial scope |
| Phase 0 / Phase 1 pages | Exit criteria and next WP |

Root remote remains **empty** (`origin` → `ExItS-SaaS`, `origin/main` gone). Nested `HealthCare/.git` preserved and ignored.

---

## 3. Final reuse decision

| Category | Capabilities |
|---|---|
| **Reuse with minimal adaptation** | JWT/refresh design, ProblemDetails/correlation, FluentValidation approach, `PagedResponse`, Hangfire *hosting* pattern, health-check patterns |
| **Reuse after extraction/generalization** | `ApplicationUser` / Identity, Organizations (+ soft limits), permission **handler infrastructure**, SecurityEvents / org audit (generalized), BFF cookie session pattern, org/user admin **workflows** (not Ant UI) |
| **Reuse as pattern only** | TenantAccessService (no EF global filters), pickers (no free-text IDs), page-state Empty/Loading/Error, `--hc-*` token *names*, Mobile/PatientWeb native CSS lessons, staff table/pager UX |
| **Keep exclusively in HealthCare** | Clinics, clinical staff roles, Patients, patient self-scope, Appointments, availability, reminders, medical notes/amendments, clinical permissions/audit, PatientWeb/Mobile clinical UX, Ant Design Staff Web |
| **Build new — ExItS Platform** | Product catalog, Plans, Trials, Subscriptions, Payments, Entitlements/overrides, Platform Admin (**native** UI), platform audit/support ops, multi-org membership model |
| **Build new — PinoyBusinessPOS** | Stores/branches/registers, customers, CustomerCredit/Utang, sales, inventory, expenses, suppliers, purchasing, shifts, returns, offline DB + sync, native UI library |
| **Deferred** | MFA, production email, EF global tenant filters, rich calendar, Full POS modules, iOS shipping validation, HC Ant modernization, HealthCare monorepo import |
| **Do not reuse** | Patient↔Customer / Clinic↔Store renames; Ant into Platform Admin or POS; Tailwind; medical-note auth for Utang; patient self-scope as generic tenant rule; `/auth/dev/*` and lab seed credentials in production |

Evidence: [reuse matrix](../reuse/reuse-classification-matrix.md), [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md).

---

## 4. Product boundaries

See [final-portfolio-boundaries.md](../engineering/final-portfolio-boundaries.md).

**Platform:** global identity/users, organizations/memberships, products/plans/trials/subscriptions/payments/entitlements/overrides, platform admin, platform audit.

**HealthCare:** clinics, clinical workforce, patients, appointments, notes, clinical authz/audit, HC product workflows.

**PinoyBusinessPOS:** retail domain (stores, customers, credit, catalog, sales, inventory, etc.), offline and sync.

Do **not** model Patient as Customer or Clinic as Store.

---

## 5. Market positioning

**Approved:** PinoyBusinessPOS is a compact, multilingual, offline-capable retail management platform initially optimized for Sari-Sari stores and mini groceries, while architected for broader Philippine SME retail (convenience, pharmacy *generic* inventory, hardware, apparel, office supply, electronics accessories, personal care, pet supply, small wholesale, and similar).

MVP scope is **not** expanded by listing industries. Regulated workflows (e.g. pharmacy compliance) remain out of MVP unless separately approved. Domain language stays generic (`Business`, `Store`, `Customer`, `CustomerCredit`, …).

---

## 6. UI decision

| Surface | Stack |
|---|---|
| HealthCare Staff Web | Ant Design Blazor (retain; no rewrite now) |
| HealthCare PatientWeb / MAUI | Existing native implementations (retain) |
| **New** ExItS Platform Admin | Blazor Web App + native CSS/Razor; no Ant; no Tailwind |
| PinoyBusinessPOS | MAUI Blazor Hybrid + same native foundation; Android/Windows first; iOS later |

Shared: models, token semantics, localization conventions, validation, status, pagination/filter models, a11y/motion standards — **not** Ant components.

---

## 7. Repository recommendation

| Option | Verdict |
|---|---|
| Controlled monorepo import of HealthCare | **Later** — after Platform foundation + extraction plan; preserves history carefully |
| Separate repositories forever | Viable but weaker for coordinated contracts |
| Submodule | Possible; more ops friction |
| Subtree | Possible; harder reverse sync |

**Recommended next step (Phase 1 start):** Keep current temporary topology (`HealthCare/` ignored nested Git). **Build new Platform documentation and, when Phase 1 authorizes code, new Platform foundations in the root repository without importing HealthCare.** Do not remove nested `.git`, track HealthCare, or create submodules in Phase 0/early Phase 1.

Remote publication: user-authorized first `git push -u origin main` when ready (remote currently empty).

---

## 8. Database recommendation

```text
ExItS_Platform
ExItS_HealthCare
ExItS_PinoyBusinessPOS
```

| Area | Owner | Notes |
|---|---|---|
| Identity, users, refresh tokens | Platform (target) | Extraction risk: issuer/PK/FK |
| Organizations, memberships | Platform | HC may keep product projections |
| Products/plans/subs/payments/entitlements | Platform | Greenfield |
| Platform audit | Platform | |
| Clinics, patients, appointments, notes | HealthCare | |
| Clinical audit detail | HealthCare | |
| Stores, customers, credit, sales, inventory… | POS | |
| Product entitlement snapshots | Product DBs | Version, effective time, expiry/refresh, fail-safe, grace, audit |

**Rule:** product daily operations must **not** synchronously call Platform on every request. Use controlled local entitlement projection/cache. Do not implement in P0-WP04.

---

## 9. Security readiness

Mandatory for future phases:

- Server-derived tenant scope; never trust client org IDs  
- Platform roles ≠ product roles; patient self-scope HC-only; POS customer access ≠ patient self-scope  
- Concealment where required; session revoke; hashed refresh tokens  
- Audit + redaction; no secrets in Git; DB separation  
- Entitlement enforcement; offline/sync rules; device registration later  
- Dev-only users disabled outside Development/Testing  

**Open risk (non-blocking for Phase 1 docs):** no EF global tenant filters (service-layer only) — R-011.

---

## 10. Risks mapped to later phases

| Risk | Later phase / action |
|---|---|
| R-001 HC regression | Phase 2 extraction + regression gates |
| R-002 HC rules becoming platform rules | Phase 1 boundary + matrix enforcement |
| R-003 Platform outage | Phase 3 entitlement snapshots |
| R-004 Cross-tenant leakage | Phase 1–2 isolation tests; filters later |
| R-005/R-019 dual UI stacks | Controlled separation; Phase 4–5 native UI |
| R-006 component scope creep | Catalog phase gating |
| R-007/R-008 i18n/theme | Phase 5+ POS / Phase 4 Platform Admin |
| R-009 offline duplicates | Phase 7 |
| R-010 nested Git | Import WP after Platform foundation |
| R-011 no EF tenant filters | Phase 2 hardening |
| R-012 missing billing | Phase 3 |
| R-014 Android SDK env | Agents that build Mobile |
| R-015 nested dirty HC files | HC repo / import WP |
| R-016 empty root remote | User push when authorized |

**Blockers for Phase 1 documentation WP:** none.

---

## 11. Exit-criteria review

| Criterion | Status |
|---|---|
| Every Phase 0 WP complete | **Satisfied** (P0-WP01…04) |
| Risks and decisions recorded | **Satisfied** |
| Required regression/security tests pass | **Partially satisfied** — Windows-safe suite 1102/0/0 (P0-WP02); Integration/E2E **deferred by design** (infra) |
| Next phase explicitly approved | **Satisfied** — Phase 1 / **P1-WP01** recommended |

Open risks do not block Phase 0 close when documented (above).

---

## 12. Phase 0 recommendation

**Close Phase 0 with documented risks.**

---

## 13. Exact next work package

| Field | Value |
|---|---|
| ID | **P1-WP01** |
| Name | Platform vs Product Capability Boundary |
| Goal | Approve target Platform/product capability boundary using Phase 0 evidence; produce an authoritative boundary document for extraction and greenfield work |
| Expected outputs | Updated/approved boundary docs, matrices, decisions — **documentation first**; no HealthCare import |
| Explicit exclusions | No HC code changes; no POS app; no billing implementation; no UI components; no DB migrations; no monorepo import |
| Required tests | Doc/link validation; no HC test re-run required unless Phase 1 later expands |
| Dependencies | Phase 0 closed; ADR-010; final boundaries |
| Primary risks | Over-scoping into extraction (R-001/R-002); sneaking Ant into Platform Admin (R-005) |

**Do not begin P1-WP01 in this work package.**
