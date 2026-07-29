# Phase 2 — Platform Extraction and HealthCare Reconnection

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-01-platform-boundary.md) | [Next](phase-03-billing-entitlements.md) | [Phase 2 readiness](../engineering/phase-02-readiness-checklist.md) | [Approved architecture](../engineering/approved-architecture-summary.md)

## Objective

Extract/adapt generic Platform capabilities while preserving and reconnecting HealthCare.

## Phase 1 prerequisite

Phase 1 is **Close with documented risks** (P1-WP04). Architecture approved for controlled implementation (ADR-014). Do **not** begin Phase 2 until P1-WP04 is accepted and this WP is explicitly authorized.

## Work packages

### P2-WP01 — Extraction Baseline Tag and Safety Checks

Status: **Ready for Review**

#### Goal

Establish a **narrow root repository and solution foundation** plus baseline tag and HealthCare freeze safety checks before identity or product work.

#### Outcomes delivered

- Annotated local tag `phase-1-approved` → `01ab65b511721d5dd2173188bc6d962a5feea803`.
- Root `ExItS.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`.
- Platform Domain / Application / Infrastructure / Api (+ markers only).
- Unit + architecture tests (dependency + HealthCare freeze safety).
- API `/` and `/health` without database (port 5288).
- Release restore/build/test: **11/0/0**.

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

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Do not begin until P2-WP01 is accepted.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P2-WP03 — Products, Plans and Entitlement Foundation

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
