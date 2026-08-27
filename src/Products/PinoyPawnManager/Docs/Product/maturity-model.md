# Maturity Model

> Index: [README.md](README.md)  
> Related: [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md), [renewal-model.md](renewal-model.md), [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

Maturity is the **agreement’s end-of-term datetime** after which the ticket is classified `MATURED` for operations. This document intentionally **does not** invent Philippine grace periods, default interest, or auction clocks.

**LEGAL_AUTHORIZATION_CLAIMED=NO.**

---

## What is known (planning baseline)

| Baseline | Intent |
|---|---|
| Agreement carries an explicit maturity datetime | Stored on ticket snapshot |
| `ACTIVE` → `MATURED` when “now” ≥ maturity | Operational classification |
| Maturity ≠ ownership transfer | [PPM-D-00-10](../risks-and-decisions.md) |
| Maturity ≠ automatic POS inventory | [PPM-D-00-15](../risks-and-decisions.md) |
| Customer may still redeem/renew after maturity | Subject to **policy + law** — both Open |

---

## Open decisions (do not close in docs alone)

| ID | Question | Safe default until decided |
|---|---|---|
| [PPM-D-00-09](../risks-and-decisions.md) | How maturity is computed; **timezone / business date** | Store explicit maturity datetime; computation rules unset |
| [PPM-D-00-08](../risks-and-decisions.md) | Charges after maturity | Do not invent rates |
| [PPM-D-00-10](../risks-and-decisions.md) | Grace / default process | No auto ownership transfer at maturity |
| [PPM-D-00-14](../risks-and-decisions.md) | Disposition / auction timing | Technical eligibility ≠ legal sale authority |
| [PPM-D-00-20](../risks-and-decisions.md) | Regulatory prerequisites | No licensing claim |

### Timezone / business date — OPEN detail

Agents must **not** assume:

- Manila local midnight truncation as law  
- A fixed N-day grace from maturity as ExItS product truth  
- Calendar-month vs exact-hour maturity without org config  

Until [PPM-D-00-09](../risks-and-decisions.md) closes, prefer storing and comparing **explicit UTC or offset datetimes** recorded at issuance, and treat “business date” as an Open product policy.

---

## Operational effects of `MATURED`

| Area | Effect |
|---|---|
| Machine A | Ticket may enter `MATURED` ([pawn-transaction-model.md](pawn-transaction-model.md)) |
| Custody | Item remains in shop custody |
| Money | Outstanding quote may change per policy (Open) — still no invented statute |
| UX | Staff see matured queues / worklists (reporting) |
| Disposition | Not started solely by maturity |

Jobs that flip `ACTIVE` → `MATURED` are future implementation; in PPM-00 they are described only.

---

## Relationship to renewal and unredeemed

```text
ACTIVE ──(maturity reached)──► MATURED
                │
                ├── renew → RENEWAL_PENDING → ACTIVE (new maturity snapshot)
                ├── redeem → payment + release → REDEEMED
                └── ops classify → UNREDEEMED (policy window Open — not legal guess)
```

See [renewal-model.md](renewal-model.md) and [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md).

---

## Notifications (planning only)

Reminders before/after maturity are product UX ideas, not legal notices. Wording must not claim statutory notice compliance until compliance review closes.

---

## Online-only

Maturity **classification jobs** run server-side when implemented. Staff actions that renew or redeem after maturity remain **ONLINE-ONLY** on initial Web/PWA.

---

## Exclusions

- No PH grace-day table  
- No automatic forfeiture posting  
- No auction date generator presented as law  
