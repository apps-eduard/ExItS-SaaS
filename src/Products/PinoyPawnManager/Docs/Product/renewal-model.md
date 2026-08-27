# Renewal Model

> Index: [README.md](README.md)  
> Related: [maturity-model.md](maturity-model.md), [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md), [pawn-transaction-model.md](pawn-transaction-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

A **renewal** (extension) continues an open pawn obligation by accepting updated terms and payment (as policy requires), producing a **new maturity snapshot**, without releasing the pledged item.

Renewals are **allowed subject to policy** and are **not unlimited by assumption** ([PPM-D-00-11](../risks-and-decisions.md)).

---

## What renewal is / is not

| Is | Is not |
|---|---|
| Continuation of custody hold | Physical release to customer |
| New disclosed maturity (and possibly charges) | Silent extension without acceptance |
| Payment event (machine C) when fees due | Unlimited free rollovers |
| Audited addendum / child snapshot | Rewrite of original issuance history |

---

## Conceptual flow

```text
ACTIVE or MATURED
→ Staff initiates renewal; system quotes amount due (policy Open)
→ Customer accepts renewal disclosure
→ State → RENEWAL_PENDING
→ Payment collected (idempotent); partials Open [PPM-D-00-12]
→ New maturity (+ charge) snapshot appended
→ State → ACTIVE
→ Custody unchanged (still IN_CUSTODY)
```

If payment fails or is abandoned, return to prior open state (`ACTIVE` / `MATURED`) per policy — do not leave money and state inconsistent.

---

## Limits — not unlimited

Until [PPM-D-00-11](../risks-and-decisions.md) closes, safe planning defaults:

| Default | Intent |
|---|---|
| Explicit acceptance required | No auto-renew |
| Explicit payment when charges due | No free infinite roll |
| Max renewal count / max term | **Unset** — do not invent statutory caps; org config later |
| Re-appraisal on renewal | Optional; if done, new appraisal history row |

Product may later configure caps; agents must not hard-code “always allow” or “PH law says N renewals.”

---

## Money

| Topic | Status |
|---|---|
| What amount is due to renew | Depends on [PPM-D-00-08](../risks-and-decisions.md) Open |
| Partial renewal payments | [PPM-D-00-12](../risks-and-decisions.md) Open — default **full required amount** until decided |
| Idempotency | Required ([PPM-R-00-05](../risks-and-decisions.md)) |

Renewal payment ≠ redemption payment. Paying renewal fees must **not** trigger item release.

---

## Snapshots and history

Each completed renewal should append:

- Prior maturity  
- New maturity  
- Amount paid / charges applied as disclosed  
- Staff and timestamps  
- Optional new appraisal reference  

Original ticket issuance snapshot remains immutable ([pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)).

---

## Authorization and runtime

- Capability-gated staff actions (renew initiate / accept payment)  
- Initial Web/PWA: **ONLINE-ONLY**  
- No offline queue for renewal money  

---

## Failure / edge cases (planning)

| Case | Direction |
|---|---|
| Double-submit payment | Idempotent — one success |
| Item missing in vault during renewal | Block; open discrepancy ([../Custody/loss-damage-discrepancy.md](../Custody/loss-damage-discrepancy.md)) |
| Customer wants renew + partial redeem | Open / likely unsupported until [PPM-D-00-12](../risks-and-decisions.md) |
| After `UNREDEEMED` | Only if policy still allows ([PPM-D-00-10](../risks-and-decisions.md)) |

---

## Exclusions

- No invented interest tables for renewals  
- No automatic renewal jobs without acceptance  
- No custody state change to `RELEASED` on renew  
