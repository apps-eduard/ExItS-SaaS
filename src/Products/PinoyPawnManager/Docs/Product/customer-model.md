# Customer Model

> Index: [README.md](README.md)  
> Parent: [../product-definition.md](../product-definition.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

PPM needs a **customer reference** for pawn operations. It does **not** create a second login or authentication system. Platform owns ExItS identity.

---

## Two layers (do not merge)

| Layer | Owner | Role |
|---|---|---|
| **Platform identity** | ExItS Platform | Login, passwords/sessions, Personal vs Organization staff model, org membership |
| **PPM customer reference** | PPM | Operational pawn customer record scoped to an organization: contact facts used in tickets, KYC/evidence references (Open), ticket history linkage |

```text
Platform Person / Personal identity (optional link)
        │  Guid / approved contract only — no Platform table FK
        ▼
PPM CustomerReference (OrganizationId-scoped)
        │
        ├── linked pawn transactions / tickets
        ├── contact / display snapshots at ticket time
        └── KYC / ID evidence references (retention Open)
```

Staff who operate PPM are **Organization staff** (Platform). They are not “PPM customers.”

---

## Identity model alignment

Follow the ExItS identity model:

- **Personal / Owner:** real email login; may own orgs; never receive org **Staff** membership via invite attach.
- **Organization staff:** separate identity; generated org login; contact email is not an authorization key.
- **Platform Admin:** Platform shell only.

PPM must **not**:

- Create product-local usernames/passwords for pawn customers as a parallel auth system  
- Treat contact email as a unique global login across products  
- Attach org Staff membership to a Personal identity as a side effect of pawn KYC  
- Store Platform session secrets in the PPM database  

Portfolio identity honesty (**R-091**, **D-P12-05**) applies when production auth is discussed.

---

## PPM customer reference — planning concepts

These are **concepts**, not implemented schemas:

| Concept | Intent |
|---|---|
| `CustomerReferenceId` | PPM-owned stable id within product DB |
| `OrganizationId` | Guid — owning org (required) |
| Optional `PlatformPersonId` / Personal link | Guid if customer is also an ExItS Personal user; optional |
| Display name | Operational; snapshot into ticket when binding |
| Contact channels | Phone/email as **contact**, not auth keys |
| Preferred branch | Soft preference; custody still uses ticket `BranchId` |
| Notes / flags | Ops notes; not legal conclusions |
| Evidence references | Pointers to ID photos / docs if collected |
| Active / restricted | Org policy flags (e.g. do-not-lend) — policy Open |

Customer **display name changes** after ticket issuance must **not** rewrite historical ticket snapshots ([pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)).

---

## Customer identification at intake

Canonical intake step ([pawn-transaction-model.md](pawn-transaction-model.md)):

1. Staff selects or creates a PPM customer reference for the org.  
2. Optionally links to Platform Personal identity when the person already has one (contract Open).  
3. Captures identifying facts needed for the pawn ticket (minimization — [PPM-R-00-07](../risks-and-decisions.md)).  
4. Proceeds to pledged-item inspection.

Walk-in customers without ExItS Personal accounts are normal. Personal is a **presentation** surface later (ticket status view); PPM remains operational authority.

---

## KYC / identity evidence — OPEN

| Topic | Status | Notes |
|---|---|---|
| Which government IDs are required | **Open** | Do not invent PH statutory ID lists as product law |
| Biometrics | **Not mandatory** in foundation ([../Custody/item-release.md](../Custody/item-release.md)) | Optional future; privacy risk |
| Face match / AI ID OCR | **Out of foundation** | No AI valuation; no assumed AI KYC |
| Retention / deletion | [PPM-D-00-19](../risks-and-decisions.md) Open | No silent purge of ticket-linked evidence |
| Regulatory KYC thresholds | [PPM-D-00-20](../risks-and-decisions.md) Open | **LEGAL_AUTHORIZATION_CLAIMED=NO** |

Until KYC policy is closed, agents should document **evidence reference hooks** and minimization principles, not claim compliance.

---

## Authorized representatives

Whether a third party may redeem or collect an item is **[PPM-D-00-13](../risks-and-decisions.md) Open**.

Safe default until decided:

- Deny representative redemption/release  
- Only the customer on the ticket (verified by org policy) may redeem  

When closed, representative rules must still keep **payment** and **physical release** as separate steps with audit.

---

## Multi-org and privacy

| Rule | Intent |
|---|---|
| Customer references are org-scoped | Org A must not see Org B customers |
| No cross-product customer merge tables | Optional shared Platform Person link only via Guid |
| PHI | Default **none**; health data not a pawn domain default |
| Evidence over-collection | Avoid; see security/privacy docs |

---

## Relationship to other products

| Product | Relationship |
|---|---|
| Platform | Owns auth/identity |
| PLM | Separate borrower profiles if any — **do not reuse** as PPM customer |
| POS | Retail customer/loyalty ≠ pawn customer authority |
| BNPL | Buyer identity for purchase finance ≠ pledged-item pawn customer |
| ExItS Personal | Optional future view of ticket status; not write authority for custody/money |

---

## Online-only note

Creating/updating customer references that participate in financial or custody-bound tickets should follow the initial Web/PWA **ONLINE-ONLY** mutation policy for ticket-affecting changes. Pure contact typo fixes may still be online-first for consistency in PPM-00 planning.

---

## Exclusions

- No second auth provider inside PPM  
- No implemented customer tables  
- No claim that linking Platform Person completes KYC or licensing  
