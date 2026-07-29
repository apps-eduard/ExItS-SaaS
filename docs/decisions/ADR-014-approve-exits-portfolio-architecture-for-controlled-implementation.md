# ADR-014 — Approve ExItS Portfolio Architecture for Controlled Implementation

[Decisions](README.md) | [Phase 1 approval](../reports/phase-01-architecture-approval.md) | [Approved summary](../engineering/approved-architecture-summary.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-07-29 |
| Work package | P1-WP04 |
| Related | ADR-009–013, ADR-003, ADR-010 |

## Context

Phase 1 produced capability boundaries, contracts, payment MVP rules, extraction sequence, and rollback plans. Implementation agents need an explicit approval to begin narrow foundation work without ambiguity or HealthCare destabilization.

## Decision

1. **Phase 1 architecture is approved** (close with documented risks).
2. **New Platform is built in root Git**; HealthCare remains frozen/ignored until an approved reconnection WP.
3. **Product and Platform boundaries** in P1-WP01–02 documents are authoritative.
4. **Local entitlement projections** and **versioned contracts** are required (ADR-011/012).
5. **New Platform Admin and POS use native Razor/CSS** (no Ant, no Tailwind); HC Staff retains Ant (ADR-010).
6. **Cash, GCash, and Utang** are MVP POS payment methods; GCash is manual; SaaS payments remain Platform-owned.
7. **Implementation begins with a narrow solution foundation** under **P2-WP01** when authorized.
8. **All later capability work remains work-package controlled**; no wholesale HC copy; no mega shared libraries without two consumers.

## Consequences

### Positive

- Clear go-ahead for controlled Phase 2 foundation.
- Preserves HC MVP and payment/UI decisions.

### Negative

- Open decisions (transport, MFA, stale durations, import timing, etc.) remain; safe defaults apply.
- Empty root remote (R-016) still blocks shared publication until user push.

## Rejected alternatives

- Begin full Platform modules or POS/billing in the first implementation WP.
- Import or modify HealthCare before Platform foundation gates.
- Defer all coding until every OD is closed.
