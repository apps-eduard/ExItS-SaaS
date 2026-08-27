# Pawn Ticket and Agreement

> Index: [README.md](README.md)  
> State machine: [pawn-transaction-model.md](pawn-transaction-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

The **pawn ticket / agreement** is the binding operational record of disclosed terms, parties, collateral identity, appraisal snapshot, principal, charges (method Open), and maturity. Historical ticket content is **immutable evidence** — configuration drift must not silently rewrite it.

This document does **not** claim that a PPM ticket satisfies Philippine pawnshop ticketing statutes. **LEGAL_AUTHORIZATION_CLAIMED=NO** ([PPM-D-00-20](../risks-and-decisions.md)).

---

## Ticket vs transaction state

| Concept | Role |
|---|---|
| Pawn transaction (machine A) | Lifecycle state (`DRAFT` … `CLOSED`) |
| Ticket / agreement snapshot | Point-in-time disclosed contract facts used while obligation is open and afterward for audit |

Typically the durable snapshot is created at **accept** or immediately before **activation** (`ACCEPTED` → `ACTIVE`). Drafts may exist without a customer-facing ticket number.

---

## Snapshot fields (planning concepts)

These are **fields/concepts**, not an implemented schema:

### Parties and scope

| Concept | Intent |
|---|---|
| Ticket number / display id | Human-facing reference (format Open) |
| `OrganizationId` / `BranchId` | Issuing/holding org and branch |
| Customer reference id + **customer display snapshot** | Name/contact as of issuance |
| Staff issuer id | Who created/accepted |

### Collateral identity snapshot

| Concept | Intent |
|---|---|
| Pledged item id | Link |
| Category snapshot | Label at time of ticket |
| Description snapshot | Frozen text |
| Identifying attributes snapshot | Serial/IMEI/etc. as recorded |
| Evidence refs snapshot | Photo/document ids retained |

### Value and money terms

| Concept | Intent |
|---|---|
| Appraisal id + **appraised amount snapshot** | From [appraisal-model.md](appraisal-model.md) |
| Principal amount | Offered/accepted loan amount |
| Currency | Explicit |
| Contractual charges disclosure | Method Open ([PPM-D-00-08](../risks-and-decisions.md)) — store what was disclosed, not invented rates |
| Fees disclosed at issuance | Whatever policy later defines; snapshot the numbers shown |

### Dates

| Concept | Intent |
|---|---|
| Agreement / issuance datetime | Explicit |
| Maturity datetime | Explicit ([maturity-model.md](maturity-model.md), [PPM-D-00-09](../risks-and-decisions.md)) |
| Time zone / business-date basis | **Open** — do not invent |

### Renewals

Renewals create **new maturity (and possibly charge) snapshots** linked to the same ticket lineage or a renewal child record — policy Open ([renewal-model.md](renewal-model.md)). Prior snapshots remain.

---

## Immutability of history

| Allowed | Forbidden |
|---|---|
| Append renewal addenda / new snapshots | Overwrite principal on an `ACTIVE` ticket without audited correction event |
| Append payment and custody events | Rewrite customer name on old tickets when CRM name changes |
| Void/cancel with reason before activation | Delete ticket rows to “clean” reports |
| Controlled correction events that reference prior values | Silent admin SQL edits of evidence |

If a disclosed charge schedule is later found wrong, correction is a **new audited event**, not a quiet mutate. Print/PDF regenerations must pull from snapshot store, not live fee config.

---

## Disclosure and acceptance

Planning intent:

1. Staff prepare offer from appraisal + policy.  
2. System presents disclosure (UI later).  
3. Customer acceptance recorded (`OFFERED` → `ACCEPTED`).  
4. Snapshot sealed.  
5. Funds release + custody commitment → `ACTIVE`.

Electronic vs paper printing requirements for PH pawnshops are **compliance Open** — software may support print later without claiming statutory form compliance.

---

## Relationship to payment and release

The ticket states the obligation. It does **not** by itself move money or custody:

- Money: [loan-release-model.md](loan-release-model.md), redemption/renewal payments  
- Custody: [../Custody/](../Custody/README.md)  
- Redemption: payment then separate release ([redemption-model.md](redemption-model.md))

---

## Not PLM / POS artifacts

| Do not | Why |
|---|---|
| Reuse PLM loan / installment entities as tickets | Different domain ([PPM-R-00-01](../risks-and-decisions.md)) |
| Treat ticket as POS sales receipt | No retail sale while pledged |
| Store SaaS subscription invoice as pawn ticket | Platform billing ≠ pawn money |

---

## Online-only

Issuing, accepting, and sealing ticket snapshots that enable funds release are **ONLINE-ONLY** mutations on the initial Web/PWA.

---

## Exclusions

- No claim of PH-prescribed ticket form compliance  
- No interest-rate schedule invented here  
- No implemented ticket printer integration in PPM-00  
