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

Status: **Complete**

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
| Hash | `6e866d710afc09c8ddca1893a720073dc8dadfa1` (foundation); correction `10f99c5f549d2161c1f033be6223ccc26a6e0eaf` |
| Message | `feat(platform): add products plans entitlement foundation`; `fix(platform): keep trial duration configurable` |

### P2-WP04 — HealthCare Contract Adaptation

Status: **Complete**

#### Goal

Create Platform-side versioned contracts, projection apply rules, and HealthCare adapter interfaces for future reconnection — without modifying or importing HealthCare.

#### Outcomes delivered

- `ContractEnvelope` / `ContractVersion` + identity, membership, org-mapping, product-access, subscription, entitlement projections.
- Projection checkpoint / apply outcomes / applicability evaluator (idempotency, ordering, gap, conflict).
- HealthCare delivery + reconciliation interfaces (no implementation/transport).
- Architecture + unit tests; API phase marker `P2-WP04-healthcare-contract-adaptation`.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No HealthCare edits/import/DB/auth changes; no messaging/EF/business API routes/persistence.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (104/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P2-WP04 report](../reports/P2-WP04-healthcare-contract-adaptation.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `3b660956bfe5f5ce522c09c9cd19c614fb6602ef` (foundation); follow-up `eb9fdfeab80e1e602587b810b91cb79e71e491e4` |
| Message | `feat(platform): add healthcare contract adaptation boundary`; `fix(platform): scope HealthCare gitignore to repo root` |

### P2-WP05 — Regression and Migration Validation

Status: **Complete**

#### Goal

Validate Platform regression coverage and HealthCare mapping dry-run readiness without modifying HealthCare or performing a real migration; publish existing remote history first.

#### Outcomes delivered

- Published `main` and `phase-1-approved` to `origin` (no force).
- Migration batch + identity/organization/membership mapping candidates.
- Preflight validator, compatibility report, simulation service, rollback-readiness models (no executor).
- Architecture + unit tests; API phase marker `P2-WP05-regression-migration-validation`.
- HealthCare remains frozen.

#### Explicit exclusions (honored)

- No HealthCare edits/import/DB/auth; no EF/SQL/real migration/transport/persistence/migration APIs.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (121/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Remote publication verified; P2-WP05 commit pushed after validation.

#### Artifacts

| Artifact | Path |
|---|---|
| Report | [P2-WP05 report](../reports/P2-WP05-regression-and-migration-validation.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `e001f3d654cc91976bfd2d4de890c8244ce04c7e` |
| Message | `test(platform): add regression and migration validation` |

### P2-WP06 — Extraction Closeout

Status: **Ready for Review**

#### Goal

Close Phase 2 by reconciling evidence from P2-WP01–05, validating freeze/build/tests, classifying exit criteria, and identifying the next phase — documentation only.

#### Outcomes delivered

- Phase 2 closeout report + evidence matrix + P2-WP06 completion report.
- Recommendation: **Close with documented non-blocking risks**.
- Next phase/WP identified: Phase 3 / P3-WP01.
- Root validation: 121/0/0; API healthy on 5288; HealthCare frozen.
- No source changes.

#### Explicit exclusions (honored)

- No HealthCare edits; no implementation expansion; no auth/persistence/cutover claims.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (121/0/0).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below).
- [x] Working tree clean (after hash-record).
- [x] HealthCare freeze verified.
- [x] Validated commits pushed to `origin/main`.

#### Artifacts

| Artifact | Path |
|---|---|
| Phase closeout | [phase-02-extraction-closeout.md](../reports/phase-02-extraction-closeout.md) |
| WP report | [P2-WP06 report](../reports/P2-WP06-extraction-closeout.md) |
| Evidence matrix | [phase-02-evidence-matrix.md](../engineering/phase-02-evidence-matrix.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `95039665d604e1d56435214b62ae039da0608742` |
| Message | `docs(platform): close phase 2 extraction` |

## Phase exit criteria

- [x] Every work package is complete or explicitly deferred.
- [x] Risks and decisions are recorded.
- [x] Required regression/security tests pass (Platform root; HC 1102 not rerun by design).
- [x] Next phase is explicitly identified (Phase 3 / P3-WP01; start requires separate authorization).

**Phase recommendation:** Close with documented non-blocking risks. See [phase-02-extraction-closeout.md](../reports/phase-02-extraction-closeout.md).
