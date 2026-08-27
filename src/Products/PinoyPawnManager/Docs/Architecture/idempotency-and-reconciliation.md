# Pinoy Pawn Manager — Idempotency and Reconciliation

> Architecture index: [README.md](README.md)  
> Security: [../Security/custody-security.md](../Security/custody-security.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Related risk | **PPM-R-00-05** duplicate money release; **PPM-R-00-03** payment≠release |

## Intent

Financial operations and high-risk custody transitions must be **safe under retry**. Network failures, double-clicks, and client reconnects must not create duplicate principal releases, duplicate redemption postings, or ambiguous physical-release states.

**Payment completion does not alone release the item.** Physical release needs extra safeguards beyond payment idempotency.

## Operations requiring idempotency (planning)

| Operation | Duplicate harm | Notes |
|---|---|---|
| Fund / principal release | Double cash out | Highest financial severity |
| Renewal payment | Double charge / wrong maturity | |
| Redemption payment | Double charge / false settled state | |
| Disposition handoff | Duplicate Commerce intake | **PPM-D-00-15** |
| Custody move (selected) | Conflicting locations | Prefer strong sequencing + audit |
| Physical item release | Wrong-item / premature release | **Extra safeguards** (below) |

## Duplicate-prevention model (planning)

When implemented:

1. Client or server supplies a stable **idempotency key** per intended business operation.  
2. Server records key + outcome; retries return the same outcome without re-applying side effects.  
3. Keys are scoped to Organization (and typically Branch / ticket / payment intent).  
4. Reconciliation reports surface orphaned intents, stuck states, and mismatched money vs custody.

Exact key format, TTL, and storage close at implementation—not invented here as API law.

## Payment ≠ physical release

| Machine | Success means |
|---|---|
| Payment / financial | Money posted; obligation updated |
| Custody / release | Item physically verified and released to authorized recipient |

A successful redemption **payment** may make release **eligible**; it must not silently set custody to **RELEASED**.

## Extra safeguards for physical release

Beyond payment idempotency, release planning requires:

| Safeguard | Intent |
|---|---|
| Release readiness check | Payment/policy conditions satisfied |
| Item identity confirmation | Ticket/item/location match (serial/photo/bin as configured) |
| Recipient authorization | Customer or policy-approved representative (**PPM-D-00-13** Open; default deny representative until decided) |
| Actor grant | `ppm.item.release` (planning label) |
| Confirmation step | Explicit staff confirmation; dual control when policy requires |
| Append-only audit | Who released what, when, to whom, with which evidence |
| No retry auto-release | Idempotent “already released” response must not re-open vault flow carelessly |

## Reconciliation themes (planning)

- Payments posted without matching ticket state  
- Tickets ACTIVE without custody receive  
- Redemption paid without release eligibility or with overdue release  
- Duplicate idempotency conflicts  
- Disposition handoff sent without Commerce ack (future)

## Non-goals

- Inventing payment-gateway specifics  
- Claiming accounting GL completeness  
- Offline mutation outbox for initial Web (see [web-pwa-runtime-policy.md](web-pwa-runtime-policy.md))

## Related

- [web-pwa-runtime-policy.md](web-pwa-runtime-policy.md)
- [../Security/audit-and-history.md](../Security/audit-and-history.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
