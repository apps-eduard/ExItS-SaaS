# POS Production Roadmap Policy — Online PWA & Final Gate

**Program:** ExItS-SaaS / PinoyBusinessPOS (React PWA generation)
**Status:** AUTHORITATIVE (roadmap governance)
**Branch:** `feat/organization`
**Effective from:** `390854e6527bbeaf060354ab071193d009ba59b9` (post MB2-05)
**Related:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md) · [POS-MULTI-BRANCH-V2-IMPLEMENTATION-PLAN.md](../../Implementation-Readiness/POS-MULTI-BRANCH-V2-IMPLEMENTATION-PLAN.md) · [offline-local-first-and-device-behavior.md](offline-local-first-and-device-behavior.md)

Historical package reports under `docs/Mobile-React/Reports/` remain truthful evidence for their completion point. This document governs **current forward policy** only.

---

## 1. Current product mode — ONLINE-ONLY PWA

```
React PWA  +  ASP.NET Core API  +  PostgreSQL
= ONLINE-REQUIRED BUSINESS APPLICATION
```

The current React PWA generation is **pure online** for business operations. It is **not** offline-first.

Business transactions require connectivity to the authoritative backend.

### In scope for normal web/PWA behavior

- Static application assets (service worker shell caching)
- Safe TanStack Query / HTTP caching for performance
- Standard browser caching where it does not imply offline transactional support

### Out of scope for the current product generation

Do **not** plan current feature work around:

- SQLite WASM / OPFS transactional persistence
- IndexedDB transactional queues
- Offline sales, payments, CustomerOrder writes, inventory writes, purchasing
- Offline synchronization, outbox/inbox, offline conflict resolution
- Offline lease/device authorization for transactional POS

MAUI LocalStore / outbox patterns documented elsewhere describe **legacy or future** tracks — not the current React PWA release target.

---

## 2. Multi-Branch V2 — implementation status

**MULTI-BRANCH FEATURE IMPLEMENTATION COMPLETE THROUGH MB2-05**

This is **not** a claim that the entire application is production-ready.

| Package | Status |
|---------|--------|
| MB2-01 … MB2-01D | COMPLETE |
| MB2-02A … MB2-02D | COMPLETE |
| MB2-03, MB2-03-H1 | COMPLETE |
| MB2-04, MB2-04-H1 | COMPLETE |
| MB2-05 | COMPLETE |
| **MB2-06** Offline Hardening | **DEFERRED** — future offline/native phase |
| **MB2-07** Final Multi-Branch E2E | **DEFERRED** — absorbed into final application gate |

Completed packages retain their validated package-level proofs. Deferring MB2-06/07 does **not** reopen completed packages.

Multi-Branch work does **not** block continuation into remaining product features.

---

## 3. MB2-06 — deferred / future offline-native phase

| Field | Value |
|-------|-------|
| **STATUS** | DEFERRED |
| **CURRENT_RELEASE** | OUT_OF_SCOPE |

**Reason:** Offline architecture will be reconsidered as a dedicated future product phase with its own discovery/design work.

**Potential future direction (high level only — not frozen):**

- React shared application code
- Capacitor native host
- Native SQLite
- Trusted registered device + secure device credential
- Durable local transaction storage
- Outbox/inbox sync, offline authorization, conflict/recovery model

**Do not implement** any part of this architecture under current online-PWA feature work.

### Moved to future offline/native backlog

- Offline customer/supplier branch-aware synchronization
- Offline branch-price cache key / invalidation hardening
- Full offline capability matrix (cross-surface transactional offline)

---

## 4. MB2-07 — deferred to final application gate

| Field | Value |
|-------|-------|
| **STATUS** | DEFERRED_TO_FINAL_APPLICATION_GATE |

**Reason:** An exhaustive final multi-branch E2E / production gate now would be premature while application features and UI/UX polish are still in progress.

The intended MB2-07 coverage (Joe Store + Remote North; branch isolation; guided setup; full regression) is **absorbed** into **FINAL-PRODUCTION-GATE-01** (see §7).

---

## 5. Production hardening policy

Production hardening is **not** a feature package.

It must **not** be repeatedly executed after:

- each module, screen, feature, or MB package
- each domain implementation

### What feature packages still include (normal engineering quality)

- Relevant unit and integration tests
- Security/authorization checks required by that feature
- Data integrity checks for affected domains
- Build / typecheck / lint where applicable
- Targeted regression for affected domains

### What feature packages must NOT trigger

- Full application-wide production hardening
- Full application-wide E2E closure
- Full security/performance audit
- Release-readiness gate

---

## 6. Feature development policy (until final gate)

Continue normal feature development.

Each future feature/package should:

1. Implement the feature
2. Test the feature
3. Protect existing architecture
4. Run targeted regression
5. Document deferred concerns in the appropriate backlog category

Do **not** append “Production Hardening” to a feature package unless the task **is** the final application-wide gate.

### Rule

> **DO NOT START APPLICATION-WIDE PRODUCTION HARDENING UNTIL FEATURE IMPLEMENTATION AND UI/UX POLISH ARE COMPLETE.**

If future task instructions accidentally request production hardening before entry criteria (§7) are met:

- **STOP** that portion
- Document it as premature
- Continue only feature-level validation

---

## 7. UI polish before hardening

Recommended progression:

```
FEATURE IMPLEMENTATION
        ↓
FEATURE COMPLETENESS
        ↓
APPLICATION-WIDE UI/UX POLISH
        ↓
FINAL E2E
        ↓
FINAL PRODUCTION HARDENING
        ↓
PRODUCTION RELEASE
```

Do not mix final UI redesign with production hardening. Avoid hardening screens that will still change, duplicating E2E, responsive QA, and security/performance audits.

---

## 8. Final production gate — entry criteria

**FINAL-PRODUCTION-GATE-01** may begin only when **all** are true:

| Criterion | Required |
|-----------|----------|
| FEATURE_COMPLETE | All planned production-scope features/modules implemented |
| DOMAIN_COMPLETE | Core domain behavior frozen enough for release |
| UI_COMPLETE | All major screens implemented |
| UX_POLISH_COMPLETE | Application-wide responsive layout, spacing, navigation, loading/empty/error states, confirmations, mobile/tablet/desktop consistency |
| I18N_COMPLETE | Required production locales reviewed |
| NO_MAJOR_REDESIGN_PENDING | No known major architecture/UI redesign still expected before release |

Until then: **DO NOT START PRODUCTION HARDENING NOW.**

---

## 9. Final production gate — scope (once)

**FINAL-PRODUCTION-GATE-01** runs **once** after feature completion + UI polish.

### A. Complete application E2E

All important business journeys crossing modules.

### B. Multi-branch final E2E (absorbs MB2-07)

Branch governance, inventory, pricing, customers, suppliers, sales, orders, purchasing, transfers, privacy, guided setup, and other branch-sensitive modules implemented by that time.

### C. Security hardening

Tenant/branch isolation, authorization, forged IDs, over-posting, mass assignment, sensitive DTO exposure, auth/session behavior, rate limiting where appropriate, headers/cookies/CORS/CSRF as applicable, secrets/configuration, dependency review.

### D. Data integrity

PostgreSQL constraints, migrations, concurrency, idempotency, transaction boundaries, historical snapshots, inventory and financial invariants.

### E. Performance

N+1, slow queries, indexes, large list/grid behavior, API payload sizes, frontend bundle, React rendering, cache behavior.

### F. UI/UX final QA

Phone, tablet, desktop — navigation, dialogs, forms, tables, cards, responsive behavior, loading/empty/error/success feedback, accessibility, touch targets, keyboard workflow, barcode-oriented workflows where applicable.

### G. PWA online production behavior

Installability, service worker, static asset caching, update strategy — **without** offline business transaction support.

### H. Observability

Structured logs, health checks, error handling, audit logs, production diagnostics.

### I. Backup / recovery

PostgreSQL backups, restore proof, deployment rollback considerations.

### J. Deployment readiness

Production configuration, environment variables, migration process, reverse proxy, TLS, domains, CORS, logging, monitoring, release/versioning.

### K. Full regression

Backend tests, frontend tests, integration tests, E2E tests, typecheck, lint, release builds.

**Release requirement:** P0=0, P1=0; release-blocking P2 resolved or explicitly accepted.

---

## 10. Known deferred backlog (preserved)

### Future offline / native

- Offline customer/supplier branch-aware sync
- Offline branch price cache / invalidation
- Offline transaction architecture (outbox/inbox, conflict model)

### Non-offline product backlog

- Promotion custom-default / origin override interaction (if still intentionally deferred beyond MB2-03)

### Known P2 (documented conservative behavior — not silent closure)

- Utang summary outstanding totals may remain org-wide for branch staff while credits/ledger are branch-scoped
- Readiness staff/device counts use POS-side heuristics until Platform assignment integration

---

## 11. Next work (current policy)

**NEXT = CONTINUE_REMAINING_PRODUCT_FEATURE_IMPLEMENTATION**

Do **not** start:

- MB2-06
- MB2-07
- Application-wide production hardening

---

## 12. Terminology

| Say | Do not say (for current state) |
|-----|--------------------------------|
| Multi-Branch feature implementation complete through MB2-05 | Production ready |
| Online-required React PWA | Offline-first PWA (current generation) |
| MB2-06 deferred to future offline/native phase | MB2-06 next on active roadmap |
