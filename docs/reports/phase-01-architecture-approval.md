# Phase 1 — Architecture Approval

[Dashboard](../portfolio-progress.md) | [Approved summary](../engineering/approved-architecture-summary.md) | [Phase 2 readiness](../engineering/phase-02-readiness-checklist.md) | [P1-WP04 closeout](P1-WP04-architecture-approval-closeout.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

**Work package:** P1-WP04
**Date:** 2026-07-29
**Commit:** `01ab65b511721d5dd2173188bc6d962a5feea803`

## 1. Executive approval

| Field | Decision |
|---|---|
| Phase 1 recommendation | **Close with documented risks** |
| Architecture | **Approved** for controlled implementation |
| Implementation readiness | **Approved with documented non-blocking risks** |
| First Phase 2 work package | **P2-WP01 — Platform Foundation Baseline and Safety Checks** |
| Migration status | **Not started** |

## 2. Portfolio architecture

```text
ExItS Platform — identity, organizations, memberships, catalog, plans, trials,
                 subscriptions, SaaS payments, entitlements, Admin, audit

PinoyBusinessPOS — businesses, stores, branches, registers, local roles,
                   customers, Utang, retail payments, catalog, sales,
                   inventory, expenses, suppliers, shifts, returns, reports
```

Products do not own Platform subscriptions. Platform does not own retail operational data. Products do not access each other’s databases.

## 3. Identity, organizations, and authorization

Platform User is the authentication identity. Platform Organization is the SaaS account boundary. Platform owns account, membership, and product access; products own operational roles, permissions, and resource scope.

```text
Authentication → account status → membership → product access → entitlement
→ product-local role → permission → resource scope → business rule
```

Tenant and resource scope are server-derived. Client-supplied organization identifiers are not authoritative. Platform administrators do not automatically receive product-operational access.

## 4. Data ownership and contracts

Platform is system of record for identity, organizations, catalog, plans, subscriptions, SaaS payments, and entitlements. PinoyBusinessPOS is system of record for retail operations.

Boundaries use stable identifiers and versioned additive contracts. Consumers must support idempotent at-least-once delivery, out-of-order handling, and fail-closed unsupported major versions. No cross-database foreign keys or shared `DbContext`/domain entities are permitted.

## 5. Entitlements and availability

Platform is authoritative for entitlements; products use validated local projections. Ordinary product transactions must not synchronously depend on Platform availability. Never-initialized, unknown, financial, privacy, and administrative capabilities fail closed.

For Utang trial expiry, viewing balances/history and repaying existing debt remain available; creating or increasing debt is blocked.

## 6. Payment boundaries

| Concept | Owner | MVP |
|---|---|---|
| SaaS Payment | Platform | Separate from POS |
| Retail Sale Payment | POS | `cash`, `gcash`, `customer-credit` |
| Credit Payment | POS | `cash`, `gcash` |

GCash MVP uses manual confirmation with normalized references and duplicate warnings. Provider APIs, QR/webhooks, gateways, and split tender remained deferred at this milestone.

## 7. UI architecture

- **Platform Admin:** Blazor Web App with Ant Design Blazor under ADR-015; no Tailwind or Fluent UI.
- **PinoyBusinessPOS:** MAUI Blazor Hybrid using native CSS and DesignSystem conventions.
- Share models, token semantics, localization, accessibility, and formatting conventions; keep framework components in their owning UI.

## 8. Repository and dependency rules

The root Git repository owns Platform and PinoyBusinessPOS. Shared code requires two verified consumers, product-neutral behavior, clear ownership, and no framework-specific UI coupling.

Forbidden dependencies include product → Platform Infrastructure/`DbContext`, Platform → product domain, UI → persistence entities, and cycles. Architecture tests enforce these boundaries.

## 9. Security requirements

Server-derived scope, least privilege, permission APIs, credential/session revocation, audit, redaction, secret exclusion, contract version validation, idempotent events, and offline financial integrity are mandatory. These requirements were architecture decisions at Phase 1 and were not yet implementation claims.

## 10. Exit-criteria assessment

| Criterion | Classification | Notes |
|---|---|---|
| Every Phase 1 work package complete | **Satisfied** | P1-WP01, P1-WP02, and P1-WP04 evidence accepted; obsolete transition-only P1-WP03 report removed |
| Risks and decisions recorded | **Satisfied** | Risk register and ADRs |
| Runtime regression/security tests | **Deferred by design** | Phase 1 was documentation-only |
| Next phase approved | **Satisfied** | Phase 2 / P2-WP01 |

## 11. Implementation readiness

**Approved with documented non-blocking risks.**

The authorized next slice was a narrow root solution and Platform project foundation with build conventions, dependency tests, and repository safety checks. Identity, billing, POS, production authentication, persistence, and migration remained outside P2-WP01.
