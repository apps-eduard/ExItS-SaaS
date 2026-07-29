# Phase 2 — Platform Extraction and HealthCare Reconnection

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-01-platform-boundary.md) | [Next](phase-03-billing-entitlements.md) | [Phase 2 readiness](../engineering/phase-02-readiness-checklist.md) | [Approved architecture](../engineering/approved-architecture-summary.md)

## Objective

Extract/adapt generic Platform capabilities while preserving and reconnecting HealthCare.

## Phase 1 prerequisite

Phase 1 is **Close with documented risks** (P1-WP04). Architecture approved for controlled implementation (ADR-014). Do **not** begin Phase 2 until P1-WP04 is accepted and this WP is explicitly authorized.

## Work packages

### P2-WP01 — Extraction Baseline Tag and Safety Checks

Status: **Complete**

#### Goal

Establish a **narrow root repository and solution foundation** plus baseline tag and HealthCare freeze safety checks before identity or product work.

#### Outcomes delivered

- Annotated local tag `phase-1-approved` → `01ab65b511721d5dd2173188bc6d962a5feea803`.
- Root `ExItS.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`.
- Platform Domain / Application / Infrastructure / Api (+ markers only).
- Unit + architecture tests (dependency + HealthCare freeze safety).
- API `/` and `/health` without database (port 5288).
- Release restore/build/test: **11/0/0** at closeout (superseded by later WPs).

#### Explicit exclusions (honored)

- HealthCare unmodified / not in solution / not referenced.
- No identity, billing, entitlements, Admin UI, POS, shared projects, EF/migrations.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (11/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.

#### Artifacts

| Artifact | Path |
|---|---|
| Solution | [ExItS.slnx](../../ExItS.slnx) |
| Report | [P2-WP01 report](../reports/P2-WP01-extraction-baseline-and-safety.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `4827b7f3ff2cba161df749dd47507f16171ff8da` |
| Message | `chore(platform): establish root solution foundation` |

### P2-WP02 — Shared Identity and Organization Boundary

Status: **Complete**

#### Goal

Create the first Platform domain and application boundary for global users, platform organizations, memberships, platform organization roles, product-access concepts, statuses, stable IDs, invariants, contracts, and tests — without authentication or persistence.

#### Outcomes delivered

- Strongly typed `PlatformUserId`, `PlatformOrganizationId`, `OrganizationMembershipId`.
- Aggregates: `PlatformUser`, `PlatformOrganization`, `OrganizationMembership`.
- Statuses, `OrganizationRole`, `ProductCode`, minimal `ProductAccess`.
- Application repositories + use cases; `DomainException` + `ApplicationResult`.
- Unit + expanded architecture tests (**61/0/0** at closeout; superseded by later totals).
- API still `/` and `/health` only (port 5288); no DB.

#### Explicit exclusions (honored)

- No login/JWT/Identity/EF/Npgsql/migrations/business API/catalog/plans/subs/entitlements/Admin UI/POS/HealthCare changes.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (61/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P2-WP02 report](../reports/P2-WP02-identity-organization-boundary.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `49f8ae81f9c5b8a60307ac1d9eb67eab8d2f45ba` |
| Message | `feat(platform): add identity organization domain boundary` |

### P2-WP03 — Products, Plans and Entitlement Foundation

Status: **Ready for Review**

#### Goal

Create Platform domain/application foundation for products, features, plans, plan versions, trials, subscriptions, entitlements, overrides, and snapshots — without persistence or payment processing.

#### Outcomes delivered

- Catalog aggregates + `FeatureCode` / `FeatureDefinition` / `PlanVersion` immutability.
- Subscription lifecycle + `EntitlementSnapshotComposer`.
- POS Utang trial expiry represented via view/repay/create feature codes; trial duration is **configurable** (three-calendar-month POS policy documented; **not** hard-coded as 90 days).
- Application repos + use cases; tests updated after trial-duration correction.
- API still `/` + `/health` only.
#### Explicit exclusions (honored)

- No EF/Npgsql/auth/billing invoices/payment/GCash/Admin UI/product APIs/brokers/POS/HealthCare changes.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (82/0/0 foundation; **86/0/0** after trial-duration correction).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P2-WP03 report](../reports/P2-WP03-products-plans-entitlements.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `6e866d710afc09c8ddca1893a720073dc8dadfa1` |
| Message | `feat(platform): add products plans entitlement foundation` |

### P2-WP04 — HealthCare Contract Adaptation

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

### P2-WP05 — Regression and Migration Validation

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

### P2-WP06 — Extraction Closeout

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
