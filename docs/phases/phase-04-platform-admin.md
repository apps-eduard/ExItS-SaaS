# Phase 4 — Platform Admin Expansion

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-03-billing-entitlements.md) | [Next](phase-05-pos-maui-foundation.md)

## Objective

Build a **new** native-CSS ExITS Platform Admin (Blazor Web App) for multi-product operations. Do **not** extend HealthCare Staff Web Ant Design as the long-term Platform Admin UI.

## Work packages

### P4-WP01 — Portfolio Navigation and Product Views

Status: **Complete**

Feature commit: `aa340e1`
Rules commit: `4399961`
Report: [P4-WP01-portfolio-navigation-and-product-views.md](../reports/P4-WP01-portfolio-navigation-and-product-views.md)

#### Required outcomes

- Permanent Cursor workflow rules (`.cursor/rules/exits-workflow.mdc`)
- Platform Admin Blazor Web App with native CSS
- Portfolio navigation + read-only product/org/subscription/payment/entitlement views
- Typed API client; loading/empty/error/unavailable states
- Focused Admin read APIs where needed
- Tests, docs, runtime evidence

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (317 passed).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean (after push).

### P4-WP02 — Organizations, Users and Product Access

Status: **Complete**

Feature commit: `6f1cacb`
Report: [P4-WP02-organizations-users-and-product-access.md](../reports/P4-WP02-organizations-users-and-product-access.md)

#### Required outcomes

- Platform users, organization memberships, Platform organization roles, product-access assignments
- Effective commercial access evaluation (Trialing/Active only for new grants)
- PostgreSQL persistence + migration apply/rollback/re-apply
- Admin APIs and UI for users/memberships/product access
- Tests, docs, runtime evidence; no auth / product-local roles / HealthCare / POS

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (331 passed).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean (after push).

### P4-WP03 — Subscriptions, Payments and Trials

Status: **Complete**

Feature commit: `91e88c3`
Report: [P4-WP03-subscriptions-payments-and-trials.md](../reports/P4-WP03-subscriptions-payments-and-trials.md)

#### Required outcomes

- Admin subscription lifecycle, trial start, and manual SaaS payment workflows
- Reuse Phase 3 domain/application/API behavior (no duplicated lifecycle logic)
- Confirm-and-activate atomicity; duplicate reference and payment reuse blocked
- Responsive Admin UI; architecture guards; no gateway/auth/delivery
- Tests, docs, runtime evidence

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (336 passed).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean (after push).

### P4-WP04 — Audit, Authorization and Closeout

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
