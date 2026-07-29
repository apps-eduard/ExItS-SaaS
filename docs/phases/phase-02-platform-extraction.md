# Phase 2 — Platform Extraction and HealthCare Reconnection

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-01-platform-boundary.md) | [Next](phase-03-billing-entitlements.md) | [Phase 2 readiness](../engineering/phase-02-readiness-checklist.md) | [Approved architecture](../engineering/approved-architecture-summary.md)

## Objective

Extract/adapt generic Platform capabilities while preserving and reconnecting HealthCare.

## Phase 1 prerequisite

Phase 1 is **Close with documented risks** (P1-WP04). Architecture approved for controlled implementation (ADR-014). Do **not** begin Phase 2 until P1-WP04 is accepted and this WP is explicitly authorized.

## Work packages

### P2-WP01 — Extraction Baseline Tag and Safety Checks

Status: **Not Started** (first approved Phase 2 WP after Phase 1 closeout)

#### Goal

Establish a **narrow root repository and solution foundation** plus baseline tag and HealthCare freeze safety checks before identity or product work.

#### Expected outcomes (when authorized)

- Baseline tag / safety checklist evidence for root and HealthCare freeze.
- Root solution skeleton and initial Platform/test project structure (buildable).
- Build conventions and dependency direction rules.
- Architecture tests that fail on prohibited project references.
- Confirm `git ls-files HealthCare` empty and ignore remains intact.

#### Explicit exclusions

- Do not modify or import HealthCare.
- Do not implement full Platform modules (identity, billing, entitlements, Admin UI).
- Do not implement PinoyBusinessPOS, GCash, offline sync, or HC adapters.
- Do not create a complete UI component library.
- Do not perform database migration or cutover.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.
- [ ] HealthCare freeze verified.

### P2-WP02 — Shared Identity and Organization Boundary

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
