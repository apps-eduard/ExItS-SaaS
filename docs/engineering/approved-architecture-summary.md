# Approved Architecture Summary

[Home](../index.md) | [Phase 1 approval](../reports/phase-01-architecture-approval.md) | [Phase 2 readiness](phase-02-readiness-checklist.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

**Status:** Authoritative entry point for implementation agents (Phases 0–2 closed with documented risks).
**Date:** 2026-07-29

---

## One-page decisions

| Topic | Decision | Detail |
|---|---|---|
| Platform | Global identity, orgs, catalog, plans, trials, subscriptions, SaaS payments, entitlements, Admin, audit | [capability boundary](platform-product-capability-boundary.md) |
| HealthCare | Clinical ops + existing UIs; frozen nested repo | [repository boundaries](repository-boundaries.md) |
| PinoyBusinessPOS | Retail ops, offline/sync, Cash/GCash/Utang | [POS requirements](../product/pinoy-business-pos-requirements.md) |
| Authz | Platform access ≠ product permissions | [authorization](authorization-matrix.md) |
| Data | Separate DBs; no cross-DB FKs; stable IDs | [data ownership](data-ownership.md) |
| Contracts | Versioned projections; idempotent; transport deferred | [contracts](platform-product-contracts.md) |
| Entitlements | Platform SoR; local projections; fail closed | [entitlement states](entitlement-state-matrix.md) |
| Payments | SaaS ≠ retail ≠ credit; GCash manual MVP | [POS payments](../product/pinoy-business-pos-requirements.md) |
| UI | HC Staff Ant; **Platform Admin Ant Design (ADR-015)**; POS native (no Ant/Tailwind) | [UI design system](ui-design-system.md) · ADR-010 · ADR-015 |
| Build order | New Platform in root before HC reconnection | [extraction sequence](../reuse/extraction-sequence.md) · ADR-013 |
| Rollback | L0–L6 | [rollback plan](extraction-rollback-plan.md) |
| Shared code | Two consumers + product-neutral only | Phase 1 approval §14 |
| Phase 2 | Closed with documented risks — foundations only | [Phase 2 closeout](../reports/phase-02-extraction-closeout.md) · [evidence matrix](phase-02-evidence-matrix.md) |
| Next WP | **P15-WP01 complete when pushed.** Exact next: **P15-WP02** (Users/memberships) or **P14-WP03** only when authorized. | [Phase 15](../phases/phase-15-ant-design-platform-admin.md) · [Phase 14](../phases/phase-14-production-deployment-and-operations.md) |

## Prohibited

- Product↔product DB access; Platform owns clinical/retail ops
- Cross-DB FKs; shared DbContext/domain entities
- Sync entitlement check every transaction
- Tailwind or Fluent UI in Platform Admin; Ant Design in POS / DesignSystem
- Wholesale HC copy; HC import without approved WP
- Password-hash migration without separate plan
- Mega shared libraries / shared permission catalogs
- Treating Phase 2 contracts/dry-runs as completed HC integration or migration

## Open (non-blocking for Phase 3 start when authorized)

R-020, R-022, R-027, R-031–R-044 and related — see [risks-and-issues.md](../risks-and-issues.md). Auth, persistence, and HC cutover remain unimplemented.
