# Pinoy Loan Manager — Role and Grant Baseline

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Product-local, **server-authoritative** authorization. Grant names below are **planning categories**, not final code constants.

Companions: [../authorization-matrix.md](../authorization-matrix.md), [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md), [../Product/reversal-refund-and-correction-policy.md](../Product/reversal-refund-and-correction-policy.md), [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md), [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md). ADR: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md).

---

## Authorization principle

Platform product access / entitlement alone does **not** grant operational Loan Manager permissions.

```text
Authenticated Actor
+ Trusted Organization Context
+ Platform Product Access
+ Allowed Commercial State
+ Required Entitlement
+ Active PLM Product Role
+ Required PLM Grant
+ Resource / Branch / Workflow Scope
= Authorized Operational Action
```

No client-only authorization. No hard-coded logic such as `if Role == "Manager" then allow everything`.

Roles are **default presets** backed by **granular grants**. Do **not** implement implicit role hierarchy (Manager does not automatically inherit Collector).

---

## Default role presets

Organization-scoped PLM roles. Do **not** add more default roles in this package. Custom roles are future work.

| Preset | Focus |
|---|---|
| **Owner** | Highest organization-level PLM administration |
| **Manager** | Lending operations and supervision |
| **Cashier** | Physical cash custody and office financial operations |
| **Collector** | Limited field operations on assigned work |

These are **not** ExItS Platform Owner / Platform Admin.

---

## Owner

Planning baseline:

- organization PLM administration
- operational configuration
- staff / PLM role management
- Quick Loan template management
- publishing control
- loan approval according to grants
- high-risk exception approval
- penalty waiver approval
- cash variance resolution
- reporting
- audit visibility
- operational oversight

Owner is **not**:

- ExItS Platform Owner
- automatically authorized to Platform administration
- allowed to bypass server authorization / audit

Owner activity remains auditable.

---

## Manager

Planning baseline:

- borrower review
- loan / request review
- approve / reject loans according to grant
- manage collector assignments
- supervise collections
- approve collection exceptions where granted
- approve penalty waivers where granted
- review cash variances
- operational reports
- loan / collection oversight
- authorized reversals according to grant

Manager should **not** automatically receive:

- organization ownership transfer
- Platform administration
- unrestricted SaaS billing administration
- hidden bypass permissions

---

## Cashier

Planning baseline:

- open authorized cashier session
- record opening cash
- receive / add authorized cash
- issue collector opening float
- issue collector additional float
- office loan disbursement
- office payment receipt
- receive collector partial remittance
- receive collector end-of-day remittance
- count received cash
- perform reconciliation
- submit / record variance
- close cashier session according to policy

Cashier must **not** normally:

- approve Loan Requests
- create / publish Quick Loan templates
- approve their own high-risk cash variance resolution
- approve Collector penalty waivers
- silently edit posted payments / disbursements

Detail: [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md).

---

## Collector

Planning baseline:

- view assigned borrower / customer work
- view assigned collections
- view assigned approved disbursements
- collect authorized payments
- record payment
- issue / receive system receipt information
- record failed collection attempt
- record missed-payment reason
- request collection exception where applicable
- request correction / reversal where applicable
- release an approved field disbursement when authorized
- maintain collector cash accountability
- make partial / end-of-day remittance
- submit end-of-day cash

Collector must **not**:

- approve their own loan
- approve their own disbursement authorization
- approve their own penalty waiver
- approve their own collection exception where approval is required
- resolve their own cash variance
- change Quick Loan financial terms
- change an existing Loan’s financial terms
- delete financial history
- view unrestricted organization-wide financial data without grant

---

## Conceptual grant catalog

Planning names only. Not final identifiers.

| Area | Conceptual grants |
|---|---|
| Borrower | View, Create, Update |
| LoanApplication / QuickLoanRequest | View, Create, Submit, Review |
| LoanApproval | Approve, Reject |
| QuickLoanTemplate | View, Manage, Publish |
| Loan | View, ViewFinancials |
| Disbursement | Office, Field, View, ReverseRequest, ReverseApprove |
| Payment | OfficePost, FieldPost, View, ReverseRequest, ReverseApprove |
| Collection | ViewAssigned, RecordAttempt, ManageAssignments |
| CollectionException | Request, Approve, DeclareOrganizationWide |
| PenaltyWaiver | Request, Approve |
| CashSession | Open, ViewOwn, ViewBranch, Close |
| CollectorFloat | Issue, Receive, View |
| Remittance | Submit, Receive, Reconcile |
| CashVariance | View, Resolve |
| Configuration | View, Manage |
| Staff | View, ManageRoles |
| Reports | ViewOperational, ViewFinancial |
| Audit | View |

---

## Default role / grant intent

| Preset | Intent |
|---|---|
| **Owner** | Broad organization PLM grants |
| **Manager** | Broad operational grants; **not** ownership-level administration by default |
| **Cashier** | Cash / disbursement / payment / reconciliation grants only as appropriate |
| **Collector** | Assigned field-operation grants only |

A role preset simply contains **explicit** grants. Matrix: [../authorization-matrix.md](../authorization-matrix.md).

---

## Branch / resource scope

Authorization must eventually support resource scope:

- Organization scope
- Branch scope
- Assigned Collector scope
- Assigned Borrower / Loan work
- Own cash session

A Collector should **not** automatically see every borrower in every branch. A Cashier may be restricted to their assigned branch / cash session. Manager / Owner access may be broader according to grants. Schema is **not** designed here.

---

## Separation of duties

**Loan approval** and **cash disbursement** are separate authorities.

```text
Manager approves Loan
        ↓
Loan = Awaiting Disbursement
        ↓
Cashier or authorized Collector releases money
```

Approval alone never proves cash was released.

Collector cannot self-approve a Loan. Cashier should **not** normally approve the Loan they will disburse.

---

## Small-organization practicality

Do **not** require every organization to employ four different humans.

A small organization may assign multiple role presets / grants to the **same** trusted user where allowed. Example: Owner may also perform Manager / Cashier functions.

However:

- every action remains individually authorized
- actor is recorded
- audit remains visible
- high-risk self-approval restrictions may still apply where explicitly required

Do **not** fake separation of duties merely by changing screen labels.

**PLM-D-00-13 Closed.** For high-risk financial actions, the requester normally cannot approve their own action when another eligible approver exists. Collector never self-approves a high-risk action. Cashier never resolves their own Cashier variance and never approves their own Payment Reversal or Cash Refund.

When an organization has only one eligible high-authority user, a controlled **Owner Override** may be permitted only when:

- the actor has Owner preset plus the required explicit override grant
- no other eligible approver exists
- reason and evidence/notes are mandatory
- the action is prominently classified as Owner Override
- enhanced audit is written
- the action appears in a mandatory subsequent-review report
- override use is visible in financial/audit reporting

Owner Override is not available to Collector, Cashier-only user, or Manager without the explicit Owner Override grant. Exact grant identifiers remain PLM-D-00-06 / later documentation.

Canonical: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md).

---

## Platform boundary

Platform Owner / Platform Admin do **not** automatically receive PLM operational grants.

| Owner | Owns |
|---|---|
| Platform | SaaS subscription, entitlement, Platform billing, Platform usage billing |
| Pinoy Loan Manager | Lending operations, Loan ledger, borrower data, cashier/collector cash, disbursement, collections, penalties, remittance, operational audit |

No direct cross-database access. Commercial-state transport remains **D-P12-03**.

---

## Audit requirements

High-risk operations must eventually preserve: actor, organization, branch, time, action, target resource, amount where relevant, reason where required, approval actor where applicable, correlation / reference, original transaction reference for reversal, device / channel where useful.

High-risk examples: loan approval, disbursement, payment, reversal, penalty waiver, collector float, remittance, cash variance resolution, cash refund, collection exception declaration, future write-off/recovery.

---

## Explicit non-goals

- Final grant identifiers or code constants
- Implicit role hierarchy
- Client-only authorization
- Custom-role design
- Schema / endpoints / UI
- Legal-compliance claims

No role, grant, or workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.
