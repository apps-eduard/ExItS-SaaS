# ADR-014 — Approve ExItS Portfolio Architecture for Controlled Implementation

[Decisions](README.md) | [Phase 1 approval](../reports/phase-01-architecture-approval.md) | [Approved summary](../engineering/approved-architecture-summary.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-07-29 |
| Work package | P1-WP04 |
| Related | ADR-009–013, ADR-003, ADR-010 |

## Context

Phase 1 produced capability boundaries, contracts, payment MVP rules, extraction sequence, and rollback plans. Implementation agents need explicit approval to begin narrow foundation work without ambiguity or portfolio-boundary violations.

## Decision

1. **Phase 1 architecture is approved** (close with documented risks).
2. **New Platform is built in root Git**; no nested foreign product source tree is permitted without an approved work package.
3. **Product and Platform boundaries** in P1-WP01–02 documents are authoritative.
4. **Local entitlement projections** and **versioned contracts** are required (ADR-011/012).
5. **Platform Admin uses Ant Design Blazor** under the P15 amendment; POS remains native Razor/CSS and DesignSystem (ADR-010/015).
6. **Cash, GCash, and Utang** are MVP POS payment methods; GCash is manual; SaaS payments remain Platform-owned.
7. **Implementation begins with a narrow solution foundation** under **P2-WP01** when authorized.
8. **All later capability work remains work-package controlled**; no wholesale foreign product copy and no large shared library without two consumers.

## Consequences

### Positive

- Clear go-ahead for controlled Phase 2 foundation.
- Preserves portfolio independence and payment/UI decisions.

### Negative

- Open decisions (transport, MFA, stale durations, import timing, etc.) remain; safe defaults apply.
- Empty root remote (R-016) still blocks shared publication until user push.

## Rejected alternatives

- Begin full Platform modules or POS/billing in the first implementation WP.
- Import foreign product source before Platform foundation gates.
- Defer all coding until every OD is closed.
