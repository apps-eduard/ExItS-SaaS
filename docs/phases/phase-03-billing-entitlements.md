# Phase 3 — Portfolio Billing, Plans and Entitlements

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-02-platform-extraction.md) | [Next](phase-04-platform-admin.md) | [Phase 2 closeout](../reports/phase-02-extraction-closeout.md)

## Objective

Implement portfolio products, plans, trials, payments and resilient entitlement propagation.

## Phase 2 prerequisite

Phase 2 is **Close with documented risks** (P2-WP06). Domain foundations for catalog/subscriptions/entitlements exist. **P3-WP01** adds catalog persistence and API. SaaS payment collection and HealthCare cutover remain out of scope until later WPs. Do **not** begin a Phase 3 WP until explicitly authorized.

## First work package

**P3-WP01–P3-WP05** Complete. Phase 3 closed with documented risks.

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

Status: **Complete** (accepted)

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

Status: **Complete** (accepted)

#### Goal

Implement persistent Platform SaaS manual payment records, confirmation lifecycle, duplicate-reference detection, subscription activation linkage, void/reversal, and development-stage API — without payment gateway, webhook, QR, or automatic verification.

#### Outcomes delivered

- `platform.saas_payments`; migration `AddManualSaaSPayments`.
- SaaSPayment aggregate with PendingConfirmation → Confirmed → Voided lifecycle; PendingConfirmation → Rejected.
- Duplicate-reference partial unique index (method + normalized_reference + organization_id).
- ConfirmPaymentAndActivateSubscription: atomic confirm + subscription activation.
- APIs under `/api/v1/platform/payments` (development-stage, unauthenticated).
- 251 root tests passing; isolated PostgreSQL apply/rollback/re-apply validated.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No payment gateway, webhook, QR, card storage, GCash API, automatic verification.
- No invoices, tax, discount, proration, credit notes, reconciliation engine.
- No POS/Utang/retail/HealthCare payment entities.
- No authentication — actor references accept plain strings (production blocker).

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (251/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commit pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P3-WP03 report](../reports/P3-WP03-manual-payment-activation.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `934c1d6a5f3a1a980748e9effb04345c801e8c37` |
| Message | `feat(platform): implement manual payment activation` |

### P3-WP04 — Entitlement Snapshots and Grace Rules

Status: **Complete** (accepted)

#### Goal

Persist authoritative Platform entitlement snapshots and feature overrides with grace/past-due/suspended/cancelled/expired composition rules — without product delivery.

#### Outcomes delivered

- `platform.feature_overrides`, `entitlement_snapshots`, `entitlement_snapshot_grants`; migration `AddEntitlementSnapshotsAndOverrides`.
- Monotonic snapshot versioning per organization+product; immutable history; reconcile creates new versions.
- Composer status rules including Expired Utang view/repay/create grants; PastDue/Suspended fail-closed on create.
- Provisional `IEntitlementRefreshPolicy` (24h); R-022 remains open.
- APIs under entitlements and feature-overrides routes (development-stage, unauthenticated).
- 301 root tests; isolated PostgreSQL apply/rollback/re-apply validated.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No product delivery, broker, outbox, Hangfire, HealthCare/POS projection tables.
- No Admin UI, authentication, payment-gateway changes.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (301/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commit pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P3-WP04 report](../reports/P3-WP04-entitlement-snapshots-and-grace-rules.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `44dc236a8aab38cce7071d957aa560470911a4db` |
| Message | `feat(platform): persist entitlement snapshots and grace rules` |

### P3-WP05 — Billing Closeout

Status: **Complete**

#### Goal

Close Phase 3 by validating the commercial foundation (catalog, subscriptions, manual payments, entitlements), migration chain, E2E scenario, documentation, and risk dispositions — without starting Phase 4.

#### Outcomes delivered

- Full migration chain apply / stepwise rollback / re-apply validated (13 platform tables).
- Deterministic E2E commercial API scenario (`Phase3CommercialCloseoutTests`).
- Phase marker `P3-WP05-billing-closeout`.
- API and database inventories recorded.
- Risk dispositions evidence-based; authentication/delivery/R-022/R-035 remain open.
- 302 root tests passing.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No Auth, Admin UI, delivery, gateway, invoice, Hangfire, POS, HealthCare implementation.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (302/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commit pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P3-WP05 report](../reports/P3-WP05-billing-closeout.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `da08fcd8fa6583e8b451514f89c0d195b9dd876e` |
| Message | `docs(platform): close Phase 3 billing foundation` |

## Phase exit criteria

- [x] Every work package is complete or explicitly deferred.
- [x] Risks and decisions are recorded.
- [x] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
