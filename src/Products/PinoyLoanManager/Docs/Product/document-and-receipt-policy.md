# Pinoy Loan Manager — Document and Receipt Policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Authoritative document types, document identity, receipts, statements, and template versioning. Not a legal-forms pack, print-engine design, or statutory compliance certification.

**Canonical companions:** [loan-documents-and-receipts.md](loan-documents-and-receipts.md), [reporting-kpi-and-aging-policy.md](reporting-kpi-and-aging-policy.md), [notification-and-delivery-policy.md](notification-and-delivery-policy.md), [approval-revision-and-disbursement-readiness-policy.md](approval-revision-and-disbursement-readiness-policy.md), [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md), [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [write-off-and-recovery-policy.md](write-off-and-recovery-policy.md). ADR: [../Decisions/ADR-015-documents-receipts-and-reporting-policy.md](../Decisions/ADR-015-documents-receipts-and-reporting-policy.md).

---

## Terminology

| Term | Meaning |
|---|---|
| Authoritative document | A system-generated artifact with durable identity tied to a source transaction or Loan snapshot |
| Human-readable reference | Organization-scoped display number or code shown to staff and customers |
| Template version | Organization-approved layout/content rules used to render a document type |
| Reprint / copy | A subsequent render of an existing authoritative document; not a new financial event |
| Source of record (SoR) | Posted financial state in the Loan subledger; not print or delivery success |

---

## Authoritative document types (MVP plan)

The following document types are planned for MVP:

| Category | Document types |
|---|---|
| Borrower / origination | Borrower profile summary; Loan Application summary; assessment/approval summary; Loan agreement; disclosure statement; payment schedule |
| Financial transactions | Disbursement receipt; Payment receipt; Cash Refund receipt; Settlement Quote; settlement receipt; principal-prepayment receipt |
| Lifecycle / collections | Restructuring agreement/schedule; Penalty Waiver/Reversal notice; Write-Off/Recovery internal record; collection-case summary (where authorized) |
| Account servicing | Account statement |

Documents not listed here may be added only through an explicit future product decision.

Do **not** claim legal sufficiency of any document (PLM-D-00-11 Open).

---

## Template versioning

- Document templates are **versioned** per Organization and document type.
- Rendered copies preserve:
  - template/policy version
  - Loan or transaction snapshot at issuance
  - generation timestamp and actor/system
- Later template or Loan Product edits must **not** silently rewrite previously issued documents.
- Reproduction of an issued document uses the preserved snapshot and template version.
- Exact document-version persistence store remains an implementation concern; the **behavior** is approved.

---

## Document identity

Every authoritative document or receipt requires:

| Field | Requirement |
|---|---|
| Immutable machine identity | Server-generated; globally unique within the product |
| Organization | Required |
| Branch | Required where applicable |
| Document type | Required |
| Human-readable reference | Server-generated; unique within defined Organization/document-type scope |
| Source transaction/resource | Link to originating Payment, Disbursement, Loan, Settlement Quote, etc. |
| Generated timestamp | UTC instant |
| Generated-by | Actor or system component |
| Template/version | Preserved |
| Status | e.g. Issued, Superseded, Voided (conceptual; not finalized as enum) |
| Integrity metadata | Where later implemented (hash/signature) |

### Human-readable reference rules

- **Server-generated** — not client-assigned
- **Unique** within its defined Organization/document-type scope
- **Immutable** after issuance
- **Reproducible / reprintable** from preserved snapshot

Exact legally required sequential format (e.g. BIR, BSP, or other regulatory numbering) remains subject to legal review (PLM-D-00-11). Do **not** rely only on printed paper as proof of posting.

---

## Receipts for posted financial events

Every **posted** financial event of the following types receives a durable receipt/reference identity:

| Event type | Receipt requirement |
|---|---|
| Payment | Required |
| Disbursement | Required |
| Cash Refund | Required |
| Settlement (full early settlement) | Required |
| Principal Prepayment | Required |
| Recovery Payment | Required |

### Receipt vs financial posting

- Receipt identity is created as part of authoritative posting.
- **Print, render, or delivery failure does not roll back** the financial transaction.
- The posted event remains the SoR regardless of customer-visible delivery.
- A replacement render or reprint is marked as a **copy**, not a new transaction.
- Receipt numbering/format as a legally mandated scheme remains **Open** (PLM-D-00-11); durable identity and human-readable reference are **approved**.

Receipts may later be:

- printed
- displayed in Personal
- downloaded
- delivered via notification channel

See [notification-and-delivery-policy.md](notification-and-delivery-policy.md).

---

## Account statements

A Loan **account statement** must explain component balances and activity. It must **not** display one unexplained total balance.

Required content:

- opening component balances (as-of statement period start)
- transaction history for the period
- principal movements
- finance charge / interest
- fees
- penalties
- waivers and reversals
- payments and allocations
- refunds and credits
- closing component balances
- DPD / collection condition
- next due obligation
- maturity
- settlement information where applicable

Statements are authoritative documents subject to the same identity and versioning rules.

---

## Access and scope

Document generation, view, print, and export follow the same server-authoritative grants and resource scopes as operational screens. See [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md).

Collectors do not receive unrestricted organization-wide document browse by default.

---

## Relationship to prior planning baselines

This policy **supersedes** the open items in [loan-documents-and-receipts.md](loan-documents-and-receipts.md) for:

- authoritative document catalog
- durable receipt identity
- template versioning behavior

The planning baseline doc remains as historical direction; canonical rules are here.

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Document catalog and receipt identity approved for MVP planning | Yes |
| Template versioning and snapshot behavior approved | Yes |
| Legally sufficient forms or mandatory numbering format | **No** (PLM-D-00-11 Open) |
| Print/delivery success equals financial posting | **No** |
| Implemented | **No** |
