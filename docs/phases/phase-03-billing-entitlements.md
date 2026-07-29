# Phase 3 — Portfolio Billing, Plans and Entitlements

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-02-platform-extraction.md) | [Next](phase-04-platform-admin.md) | [Phase 2 closeout](../reports/phase-02-extraction-closeout.md)

## Objective

Implement portfolio products, plans, trials, payments and resilient entitlement propagation.

## Phase 2 prerequisite

Phase 2 is **Close with documented risks** (P2-WP06). Domain foundations for catalog/subscriptions/entitlements exist. **P3-WP01** adds catalog persistence and API. SaaS payment collection and HealthCare cutover remain out of scope until later WPs. Do **not** begin a Phase 3 WP until explicitly authorized.

## First work package

**P3-WP01** Complete (accepted). **P3-WP02** Ready for Review — organization ownership + trial/subscription lifecycle persistence and API.

## Work packages

### P3-WP01 — Product and Plan Catalog

Status: **Complete** (accepted)

#### Goal

Persist the Platform product/plan catalog and expose it via Platform API without implementing subscriptions, payments, or Admin UI.

#### Outcomes delivered

- EF Core + Npgsql; `PlatformDbContext`; schema `platform`; migration `InitialPlatformCatalog`.
- Repository + unit-of-work implementations; expanded catalog commands/queries.
- Catalog REST API under `/api/v1/platform/catalog` (development-stage, unauthenticated).
- Integration tests via Testcontainers PostgreSQL; 140 root tests passing at acceptance.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No subscription purchase/activation, invoices, GCash, entitlement delivery, Admin UI, HC adapter, POS.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (140/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commit pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P3-WP01 report](../reports/P3-WP01-product-and-plan-catalog.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `9d01f26095c3c76ffd67aa2b7b5bcf1a19a328f2` |
| Message | `feat(platform): implement product and plan catalog` |

### P3-WP02 — Trials and Subscription Lifecycle

Status: **Ready for Review**

#### Goal

Persist Platform organizations (subscription ownership) and the commercial subscription lifecycle (trial through cancel/expire) without payment collection.

#### Outcomes delivered

- `platform.organizations` + `platform.subscriptions`; migration `AddPlatformOrganizationsAndSubscriptions`.
- Partial unique index for one active-like subscription per organization + product.
- Trial start/expire; paid activation without payment processing; grace/past-due/suspend/reactivate/cancel/expire.
- APIs under `/api/v1/platform/organizations` and `/api/v1/platform/subscriptions` (development-stage, unauthenticated).
- 185 root tests passing; isolated PostgreSQL apply/rollback/re-apply validated.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No auth/JWT, invoices, GCash, entitlement delivery, Hangfire, Admin UI, HC adapter, POS, `FromDays(90)`.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (185/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commit pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P3-WP02 report](../reports/P3-WP02-trials-and-subscription-lifecycle.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `616d8ad1a76f02b6494b905549908c1a15e7f812` |
| Message | `feat(platform): implement trial and subscription lifecycle` |

### P3-WP03 — Manual Payment Activation

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

### P3-WP04 — Entitlement Snapshots and Grace Rules

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

### P3-WP05 — Billing Closeout

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
