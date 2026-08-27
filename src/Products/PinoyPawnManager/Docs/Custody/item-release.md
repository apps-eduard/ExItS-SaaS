# Item Release

> Index: [README.md](README.md)  
> Redemption: [../Product/redemption-model.md](../Product/redemption-model.md)  
> States: [custody-state-model.md](custody-state-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

**Item release** is the custody operation that hands a pledged item back to the redeeming party. It is the second half of redemption: **after** (or strictly sequenced with) payment — never instead of payment.

Insider wrong-item release is a primary risk ([PPM-R-00-06](../risks-and-decisions.md)).

---

## Hard rules

| Rule | Value |
|---|---|
| Payment ≠ release | Must record distinct events |
| Release without required payment | **Forbidden** |
| Mandatory biometrics | **No** in foundation |
| Online-only (initial Web/PWA) | **Yes** |
| Representative release | Default **deny** until [PPM-D-00-13](../risks-and-decisions.md) |

---

## Preconditions

1. Ticket in an eligible open state (`ACTIVE` / `MATURED` / policy-allowed).  
2. Redemption payment accepted (machine C) — or supervised same-visit flow that still persists payment before `RELEASED`.  
3. Custody can enter `RELEASE_PENDING`.  
4. Staff has release capability.  
5. Item located; pull movement recorded.

---

## Wrong-item safeguards (planning checklist)

Perform before marking `RELEASED`:

| Check | Intent |
|---|---|
| Ticket number confirmation | Customer/staff re-read |
| Bag / tag scan or code match | Bind physical bag to ticket |
| Description / photo side-by-side | Visual confirm against snapshot |
| Serial / IMEI / hallmark match when present | High-value identifiers |
| Category sanity | Jewelry ticket ≠ phone bag |
| Customer identity per org policy | Not a second auth system; KYC Open |
| Representative docs | Only if [PPM-D-00-13](../risks-and-decisions.md) allows |
| Staff acknowledgment | Actor recorded |
| Optional second staff witness | Dual control Open — no invented ₱ threshold |

Any failed check → **do not release**; open discrepancy if item mismatch/missing ([loss-damage-discrepancy.md](loss-damage-discrepancy.md)).

---

## Biometrics — explicitly non-mandatory

| Topic | Foundation stance |
|---|---|
| Fingerprint / face gate | **Not required** |
| Future optional biometrics | Possible; privacy/[PPM-D-00-19](../risks-and-decisions.md) apply |
| AI face match as sole release authority | **Out of foundation** |

Safeguards rely on ticket↔bag matching, staff process, and audit — not biometric theater.

---

## Release event fields (concepts)

| Concept | Intent |
|---|---|
| Release id | Unique |
| Ticket id / item id / bag id | What was released |
| Payment operation id | Link to financial settle |
| Checklist results | Pass/fail per check |
| Recipient type | Customer / representative (if allowed) |
| Recipient evidence refs | Optional ID capture — minimization |
| Staff actor (+ optional witness) | Who |
| Timestamp | When |
| From location | Counter/staging |
| Notes | Exceptions |

Success transitions custody to `RELEASED` and allows ticket `REDEEMED` → `CLOSED`.

---

## Abort paths

| Situation | Direction |
|---|---|
| Checklist fail | Stay `RELEASE_PENDING` or return `IN_CUSTODY`; incident if needed |
| Customer leaves after paying | Aging `RELEASE_PENDING` report; still not POS stock |
| Wrong bag opened | Incident; do not “fix” by releasing another item silently |

---

## Disposition is not this flow

Handing an item to an auctioneer or Commerce handoff uses **disposition custody states**, not customer `RELEASED` ([../Product/unredeemed-and-disposition-model.md](../Product/unredeemed-and-disposition-model.md)).

---

## Exclusions

- No scanner hardware drivers in PPM-00  
- No mandatory biometric vendor  
- No claim of statutory release-form compliance  
