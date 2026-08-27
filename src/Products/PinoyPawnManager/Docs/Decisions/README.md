# Pinoy Pawn Manager — Decisions (ADR Process)

> Authoritative open register: [../risks-and-decisions.md](../risks-and-decisions.md)
> Product README: [../README.md](../README.md)

| Field | Value |
|---|---|
| Status | PPM-01 complete — ADR-001 provisionally approved for implementation |
| Implementation | Scaffold only |
| Last updated | 2026-08-27 |

## Purpose

Architecture Decision Records (ADRs) capture **closed or proposed** product architecture choices with context, options, and consequences. Until an ADR is accepted, treat [../risks-and-decisions.md](../risks-and-decisions.md) `PPM-D-00-XX` items as the working open register.

## Process

1. **Raise** — Identify a decision that blocks design or implementation; assign/link `PPM-D-00-XX` (or later package IDs).
2. **Propose** — Draft ADR with status **PROPOSED** (not Closed).
3. **Review** — Product Owner / architecture review with evidence.
4. **Accept or Reject** — Status becomes **Accepted** or **Rejected**; update risks-and-decisions.md.
5. **Implement** — Only after acceptance (and package authorization).

Do not mark legal/regulatory items Closed without counsel or Product Owner evidence. Prefer stable IDs.

## Status vocabulary

| Status | Meaning |
|---|---|
| PROPOSED | Draft direction; not binding |
| Provisionally Approved for Implementation | Product Owner authorizes scaffold/implementation use; **not** final marketing Closed |
| ACCEPTED | Approved for implementation guidance (may still separate marketing closure) |
| REJECTED | Explicitly not chosen |
| SUPERSEDED | Replaced by a later ADR |

**Closed** / final marketing approval in risks-and-decisions.md requires repository or Product Owner evidence—not inference. Provisional implementation approval is weaker than final marketing Closed.

## ADR index

| ADR | Title | Status |
|---|---|---|
| [ADR-001](ADR-001-product-identity.md) | Product identity (`pinoy-pawn-manager` / `PinoyPawnManager`; DB name still Open) | **Provisionally Approved for Implementation** (PPM-01) — not final marketing Closed |

## Rules

- ADRs do not authorize POS/PLM/BNPL code changes
- ADRs do not claim `LEGAL_AUTHORIZATION_CLAIMED=YES`
- Provisional identity approval authorizes scaffold + Local Validation registration only; **PPM-D-00-04** … **PPM-D-00-20** remain Open unless separately closed

## Related

- [../risks-and-decisions.md](../risks-and-decisions.md) — canonical open register
- [../Phases/README.md](../Phases/README.md)
- [../Validation/PPM-00-readiness-checklist.md](../Validation/PPM-00-readiness-checklist.md)
