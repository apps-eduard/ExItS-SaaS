# Pinoy Loan Manager — Collections Case and Promise to Pay Policy

**Status:** Accepted product policy (PLM-DOC-06); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Promise to Pay (PTP), Collection Case lifecycle, collection conduct boundaries, and linkage to restructuring/Write-Off. Not legal sufficiency or harassment regulation approval.

**Canonical companions:** [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [restructuring-and-hardship-policy.md](restructuring-and-hardship-policy.md), [write-off-and-recovery-policy.md](write-off-and-recovery-policy.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md). Authorization: [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md). ADR: [../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md](../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md).

---

## Promise to Pay (PTP)

A **Promise to Pay** records a borrower commitment to pay by a stated date. It is operational collection metadata, not a financial transaction.

A PTP records:

- Borrower
- Loan
- promised amount
- promised date
- source/contact channel
- Collector or Manager actor
- notes
- status
- follow-up date

### Conceptual statuses

- Open
- Kept
- Partially Kept
- Broken
- Cancelled
- Superseded

### PTP boundaries

PTP:

- does **not** change the schedule
- does **not** change maturity
- does **not** waive penalties
- does **not** mark a Loan Current
- does **not** count as Payment
- does **not** replace a restructuring agreement

Collectors may record attempts and request PTP-related exceptions per grant catalog. Approval of exceptions remains with Owner/Manager.

---

## Collection Case

A **Collection Case** may be opened for:

- Past Due
- Matured Past Due
- Broken PTP
- hardship/restructuring review
- Written-Off recovery

A Collection Case may contain:

- assigned Collector/Manager
- contact attempts
- PTP records
- exception requests
- hardship request
- escalation status
- documents/notes
- next action
- resolution

Collection Case history is auditable.

Collection notes must **not** become editable financial history. High-risk financial corrections follow reversal/waiver workflows (ADR-008).

---

## Collection conduct boundary (Product requirements)

The product must support organization-authorized collection operations while avoiding abusive features:

- respectful contact recording
- organization-authorized channels only
- contact attempts recorded factually
- no fabricated contact
- no access to unrelated product data (POS, other lenders)
- no disclosure of debt to unrelated third parties through the system
- no harassment features
- no public shame lists
- no automatic contact of all phone contacts
- no continuous surveillance
- communications subject to future legal/compliance review (PLM-D-00-11)

Do **not** claim legal sufficiency.

---

## Grants and scope

| Action | Typical grant | Scope |
|---|---|---|
| View assigned collection work | `plm.collections.view-assigned` | Assigned Work |
| Record contact/visit attempt | `plm.collections.record-attempt` | Assigned Work |
| Manage assignments/routes | `plm.collection-assignments.manage` | Organization/Branch |
| Request exception | `plm.collection-exceptions.request` | Assigned Work |
| Approve exception | `plm.collection-exceptions.approve` | Organization/Branch |
| Declare collection suspension | `plm.collection-exceptions.declare` | Organization/Branch |

Data minimization for Collectors: [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md).

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| PTP and Collection Case product rules are approved | Yes |
| Legally compliant collections practice | **No** (PLM-D-00-11 Open) |
| Implemented | **No** |
