# Pinoy Pawn Manager — Operations

> Product README: [../README.md](../README.md)  
> Roadmap: [../roadmap.md](../roadmap.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning only |
| Implementation | None |
| Last updated | 2026-08-27 |

## Purpose

Sketch **daily operating concerns** for a future PPM deployment. This is planning documentation—not a live runbook, not staff training, and not a claim of regulatory readiness.

`LEGAL_AUTHORIZATION_CLAIMED=NO`

## Daily ops sketch (planning)

| Area | Future operational intent |
|---|---|
| Opening | Confirm online connectivity (ONLINE_ONLY mutations); staff grants; branch/vault access |
| Intake | Identify customer → inspect item → photos → appraisal → offer → acceptance |
| Activation | Take custody → release funds → ticket ACTIVE |
| Intraday custody | Receive/move/locate; discrepancy escalation |
| Payments | Renewal / redemption posting with idempotent retries |
| Release | Separate checklist after payment readiness; identity confirmation |
| Maturity watch | Review matured / approaching maturity queues (rules Open) |
| Unredeemed | Operational classification only; no auto retail handoff |
| Close | Reconcile money intents vs custody exceptions; secure vault |

## Explicit non-goals (now)

- Production SOPs with invented legal notice timelines  
- Cash-drawer procedures copied from POS without **PPM-D-00-17**  
- Offline end-of-day mutation queues for Web  
- Auctioneer scripts presented as ExItS legal process  

## Dependencies on Open decisions

Ops detail cannot finalize until relevant `PPM-D-00-*` items close (interest, maturity/grace, renewal, representative redemption, disposition, retention, licensing). See [../risks-and-decisions.md](../risks-and-decisions.md) and [../Compliance/philippines-regulatory-review.md](../Compliance/philippines-regulatory-review.md).

## Related

- [../Architecture/web-pwa-runtime-policy.md](../Architecture/web-pwa-runtime-policy.md)
- [../Security/custody-security.md](../Security/custody-security.md)
- [../Phases/README.md](../Phases/README.md)
