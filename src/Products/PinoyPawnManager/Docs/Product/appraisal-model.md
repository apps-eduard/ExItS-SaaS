# Appraisal Model

> Index: [README.md](README.md)  
> Related: [pledged-item-model.md](pledged-item-model.md), [loan-release-model.md](loan-release-model.md), [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

Appraisal records the shop’s **assessed value** of a pledged item at a point in time. It is **not** the loan principal. PPM foundation uses **manual appraisal only** — **no AI valuation** ([PPM-D-00-06](../risks-and-decisions.md)).

---

## Appraisal vs principal

| Concept | Meaning |
|---|---|
| **Appraised value** | Staff judgment of item value for lending decisions; evidence-backed |
| **Loan principal** | Amount actually offered/released to the customer |
| Relationship | Principal ≤ policy relative to appraisal — **no fixed % in product** ([PPM-D-00-07](../risks-and-decisions.md)) |

```text
AppraisedValue  ──(policy Open)──►  OfferedPrincipal  ──(release)──►  ReleasedPrincipal
```

Never auto-derive principal as a hard-coded LTV percentage in foundation docs or code. Calculators, if later added, must be configurable and auditable.

---

## Manual appraisal — planning concepts

| Concept | Intent |
|---|---|
| `AppraisalId` | PPM-owned id |
| Linked `PledgedItemId` | Subject of appraisal |
| `OrganizationId` / `BranchId` | Scope |
| Appraised amount + currency | Explicit amount; currency policy follows org (Open) |
| Method notes | How value was judged (manual notes) |
| Condition factors | What reduced/increased judgment |
| Photos / evidence refs | May reuse intake + add appraisal-specific |
| Appraiser staff id | Platform user Guid |
| Optional supervisor review | [PPM-D-00-06](../risks-and-decisions.md) Open direction: optional second set of eyes |
| Timestamps | Created / superseded |
| Status | e.g. current vs superseded (planning labels) |

---

## History is required

| Rule | Intent |
|---|---|
| Appraisals are versioned | New appraisal **supersedes**; prior rows remain |
| Binding snapshot | Ticket stores the appraisal id/amount used at acceptance |
| Silent overwrite forbidden | Changing amount after `ACTIVE` without a controlled event is prohibited |
| Config drift | Later fee or gold-price board changes do not rewrite past appraisals |

Dispute and audit scenarios depend on being able to answer: *Who appraised what, when, at what amount, with what notes/photos?*

---

## Placement in state machine A

| State | Appraisal role |
|---|---|
| `DRAFT` | May be incomplete |
| `APPRAISED` | At least one current appraisal exists |
| `OFFERED` / `ACCEPTED` | Offer references appraisal snapshot |
| `ACTIVE`+ | Historical; re-appraisal for renewal is a **new** appraisal event if needed |

See [pawn-transaction-model.md](pawn-transaction-model.md).

---

## Explicit exclusions — AI and automation

| Excluded in foundation | Reason |
|---|---|
| AI / ML valuation engines | [PPM-D-00-06](../risks-and-decisions.md); accuracy and liability |
| Scraping live bullion APIs as silent auto-appraisal | May assist UX later; must not silently set appraised value without staff commit |
| Automatic principal = f(appraisal) with fixed % | [PPM-D-00-07](../risks-and-decisions.md) |
| “Market price” presented as legal truth | **LEGAL_AUTHORIZATION_CLAIMED=NO** |

Staff remain accountable for the recorded appraised value.

---

## Supervisor / dual control (Open)

Direction in the register: manual appraisal + notes/photos; **optional supervisor**. Until closed:

- Single-appraiser path is allowed in planning  
- High-value thresholds for mandatory supervisor are **not invented** as ₱ law  

If dual control is enabled later, both actions are audited.

---

## Online-only

Recording or superseding appraisals that feed offers/tickets is an **ONLINE-ONLY** mutation on the initial Web/PWA surface.

---

## Related decisions

| ID | Topic |
|---|---|
| [PPM-D-00-05](../risks-and-decisions.md) | Categories affecting appraisal forms |
| [PPM-D-00-06](../risks-and-decisions.md) | Appraisal methodology |
| [PPM-D-00-07](../risks-and-decisions.md) | Loan-to-appraisal policy |
| [PPM-D-00-19](../risks-and-decisions.md) | Evidence retention |

---

## Exclusions

- No appraisal engine implementation  
- No statutory PH valuation rules claimed  
- No silent edit of historical appraisal amounts  
