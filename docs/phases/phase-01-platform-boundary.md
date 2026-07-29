# Phase 1 — Platform Boundary and Architecture

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-00-healthcare-assessment.md) | [Next](phase-02-platform-extraction.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md)

## Objective

Approve the target Platform/product boundary, data ownership, contracts and extraction sequence.

## Phase 0 prerequisite

Phase 0 is **Complete with documented risks** (P0-WP04 accepted as closeout direction).

## Work packages

### P1-WP01 — Platform vs Product Capability Boundary

Status: **Ready for Review**

#### Required outcomes

- Authoritative Platform vs product capability boundary.
- Capability ownership matrix and data authority matrix.
- ADR-011 Platform Authority and Product-Local Projections.
- Reconcile Phase 0 decisions without rewriting correct docs.
- Markdown-only; HealthCare frozen; no application folders.

#### Definition of Done

- [x] Approved outcomes complete (docs).
- [x] Applicable validation (Markdown-only, freeze, links) with evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (see below / report).
- [x] Working tree clean (after hash-record commit).

#### Artifacts

| Artifact | Path |
|---|---|
| Capability boundary | [platform-product-capability-boundary.md](../engineering/platform-product-capability-boundary.md) |
| Ownership matrix | [capability-ownership-matrix.md](../engineering/capability-ownership-matrix.md) |
| Data authority | [data-authority-matrix.md](../engineering/data-authority-matrix.md) |
| ADR-011 | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) |
| Report | [P1-WP01 report](../reports/P1-WP01-platform-product-capability-boundary.md) |

#### Commit

| Field | Value |
|---|---|
| Hash | `b6a3133732f6d29c68159447eb1ca43ea0b1212b` |
| Message | `docs(architecture): define platform product boundaries` |

### P1-WP02 — Data Ownership and Contracts

Status: Not Started

#### Required outcomes

- Deepen data ownership and versioned contract specifications from P1-WP01 authority matrices.
- Do not begin until P1-WP01 is accepted.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P1-WP03 — Extraction Sequence and Rollback Plan

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

### P1-WP04 — Architecture Approval Closeout

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
