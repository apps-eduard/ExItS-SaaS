# Pinoy Pawn Manager — Decisions (ADR Process)

> Authoritative open register: [../risks-and-decisions.md](../risks-and-decisions.md)  
> Product README: [../README.md](../README.md)

| Field | Value |
|---|---|
| Status | PPM-00 documentation foundation |
| Implementation | None |
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
| ACCEPTED | Approved for implementation guidance |
| REJECTED | Explicitly not chosen |
| SUPERSEDED | Replaced by a later ADR |

**Closed** in risks-and-decisions.md requires repository or Product Owner evidence—not inference.

## ADR index

| ADR | Title | Status |
|---|---|---|
| [ADR-001](ADR-001-product-identity.md) | Product identity (`pinoy-pawn-manager` / `PinoyPawnManager` / `ExItS_PinoyPawnManager`) | **PROPOSED** |

## Rules

- ADRs do not authorize POS/PLM/BNPL code changes  
- ADRs do not claim `LEGAL_AUTHORIZATION_CLAIMED=YES`  
- PPM-00 may add PROPOSED ADRs only; no implementation from ADRs alone  

## Related

- [../risks-and-decisions.md](../risks-and-decisions.md) — canonical open register  
- [../Phases/README.md](../Phases/README.md)  
- [../Validation/PPM-00-readiness-checklist.md](../Validation/PPM-00-readiness-checklist.md)  
