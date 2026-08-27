# Pinoy Pawn Manager — Web / PWA Runtime Policy

> Architecture index: [README.md](README.md)  
> Parent overview: [../architecture.md](../architecture.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Initial runtime policy | **ONLINE_ONLY** |

## Verdict

Initial PPM Organization Web / PWA is **ONLINE_ONLY** for financial and custody mutations.

| Mutation class | Initial policy |
|---|---|
| Fund / principal release | ONLINE_ONLY |
| Renewal / redemption payments | ONLINE_ONLY |
| Custody receive / move / release | ONLINE_ONLY |
| Appraisal create/approve (ops) | ONLINE_ONLY |
| Ticket activate / disposition marks | ONLINE_ONLY |
| Static asset caching (shell) | May cache installable shell only |
| Offline mutation outbox | **Not** in initial Web scope |

## Rationale (planning)

- Money and physical custody errors are high severity under ambiguous offline replay.  
- Idempotency and reconciliation assume server authority while online.  
- Portfolio Personal Web online-only lessons apply by analogy; PPM does not copy POS offline grant models.

## Allowed vs forbidden (initial)

| Allowed | Forbidden (initial) |
|---|---|
| Installable PWA that requires network for ops | Queuing fund release while offline |
| Read-only cached chrome / static UI assets | Local authoritative custody state writes |
| Clear “you are offline” blocking mutations | Silent outbox drain that posts money later |
| Server-authoritative quotes and tickets | Client-invented ticket numbers as authority |

## Future offline / native

MAUI or native offline capability is **deferred**. Any offline financial/custody mutation requires a separate architecture decision and must not be assumed by PPM-00 docs.

## Honesty

- Installable ≠ offline-capable for pawn money/custody.  
- Do not market initial PPM Web as “full offline pawnshop.”  
- `LEGAL_AUTHORIZATION_CLAIMED=NO` remains independent of runtime policy.

## Related

- [idempotency-and-reconciliation.md](idempotency-and-reconciliation.md)
- [api-contract-boundary.md](api-contract-boundary.md)
- [../Operations/README.md](../Operations/README.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
