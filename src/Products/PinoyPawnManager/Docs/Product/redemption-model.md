# Redemption Model

> Index: [README.md](README.md)  
> Custody release: [../Custody/item-release.md](../Custody/item-release.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

**Redemption** settles the pawn obligation so the customer can recover the pledged item. PPM treats this as **two coordinated but separate steps**:

1. **Payment** (financial machine C) — accept amount due  
2. **Physical release** (custody machine B) — hand over the correct item  

**Payment completion ≠ physical release** ([PPM-R-00-03](../risks-and-decisions.md)).

---

## Canonical redemption flow

```text
ACTIVE or MATURED (or policy-allowed later state)
→ Staff quotes amount due (charges method Open — PPM-D-00-08)
→ Customer pays (idempotent payment op)
→ Custody → RELEASE_PENDING (item pulled / verified)
→ Release checklist + wrong-item safeguards
→ Custody → RELEASED
→ Machine A → REDEEMED → CLOSED
```

Never jump `ACTIVE` → `REDEEMED` solely because a payment row exists.

---

## Payment step

| Concept | Intent |
|---|---|
| Quote | Principal outstanding + contractual charges as disclosed/policy (Open) |
| Partial payments | [PPM-D-00-12](../risks-and-decisions.md) Open — default require full amount until decided |
| Channels | Cash / others; cash integration Open ([PPM-D-00-17](../risks-and-decisions.md)) |
| Idempotency | Mandatory |
| Receipt | PPM payment fact; not POS sale |

Quoted amounts must not invent statutory PH pawn interest. Store and show policy-configured figures only when those configs exist.

---

## Release step (separate)

After payment (or in tightly supervised same-counter workflow that still records **two events**):

| Check | Intent |
|---|---|
| Ticket ↔ item match | Bag/tag/id verification |
| Customer / representative policy | [PPM-D-00-13](../risks-and-decisions.md) — default deny third party |
| Location pull | Movement audit from vault bin to counter |
| Condition note | Optional; discrepancies → incident flow |
| Staff acknowledgment | Who released |

Detail: [../Custody/item-release.md](../Custody/item-release.md). Biometrics are **not** mandatory in foundation.

---

## State coupling (recommended)

| Payment status | Custody status | Machine A |
|---|---|---|
| Not paid | `IN_CUSTODY` | `ACTIVE` / `MATURED` |
| Paid | `RELEASE_PENDING` | Still open until release (planning: avoid premature `REDEEMED`) |
| Paid | `RELEASED` | `REDEEMED` → `CLOSED` |

If product UX combines screens, persistence must still store distinct payment and release events for audit and dispute.

---

## Authorized representatives — OPEN

[PPM-D-00-13](../risks-and-decisions.md): whether a third party may redeem.

Safe default: **deny**. If later allowed, require documented authorization evidence and the same payment-then-release split.

---

## Failure modes

| Failure | Direction |
|---|---|
| Payment succeeds, release blocked (wrong item / missing) | Keep funds recorded; custody discrepancy; do not mark `REDEEMED` |
| Release attempted without payment | Forbidden |
| Double payment | Idempotent reject / refund ops procedure |
| Customer paid but abandoned pickup | Ops hold policy Open; item not POS stock |

---

## Online-only

Redemption payment and custody release mutations are **ONLINE-ONLY** on initial Web/PWA. No offline “mark redeemed” queue.

---

## What redemption is not

| Not | Why |
|---|---|
| Renewal | Renewal keeps custody ([renewal-model.md](renewal-model.md)) |
| POS return / refund | Different product |
| Automatic ownership to shop | That is disposition territory and legal Open |

---

## Exclusions

- No implemented tender UI  
- No invented redemption interest formula  
- No mandatory biometric gate  
