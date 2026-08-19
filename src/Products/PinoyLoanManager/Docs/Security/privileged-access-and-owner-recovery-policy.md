# Pinoy Loan Manager — Privileged Access and Owner Recovery Policy

**Status:** Accepted product policy (PLM-DOC-05); not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

First-Owner bootstrap, Owner assignment, last-Owner protection, no self-escalation, and Platform support recovery boundary. Not an API contract or authentication implementation.

**Canonical companions:** [authorization-grant-catalog.md](authorization-grant-catalog.md), [default-role-preset-policy.md](default-role-preset-policy.md), [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). ADR: [../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md](../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md).

---

## First Owner bootstrap

The first `plm.owner` assignment is established during future product activation/onboarding through an approved Platform-to-PLM contract.

The bootstrap subject must be:

- an authenticated Platform user
- an active member/authorized representative of the Organization
- explicitly selected/confirmed for PLM ownership

Platform performs only the bootstrap control-plane action. Platform Admin does **not** thereby receive PLM operational access. Bootstrap must be auditable.

Exact API/transport remains **D-P12-03** / future Platform integration work.

---

## Owner assignment and last-Owner protection

- only `plm.owner` with `plm.owner-assignments.manage` may assign/remove Owner
- Owner assignment/removal is a security-high-risk action
- use distinct approver when another eligible Owner exists
- controlled Owner Override may apply when only one eligible Owner exists (ADR-008)
- every Organization must retain at least one active PLM Owner
- removing, suspending, or expiring the last active Owner is **blocked**
- an Owner cannot remove their own assignment when it would leave no active Owner
- future Organization deactivation follows a separate controlled workflow

---

## Role assignment lifecycle

Every role assignment must conceptually preserve:

- Organization
- role code
- policy version
- assigned user
- scope type and scope references
- effective-from
- optional effective-until
- status
- assigned by
- reason
- created/updated timestamps
- audit history

Conceptual statuses (not finalized enums): Pending, Active, Suspended, Expired, Revoked.

Suspended, expired, or revoked assignments grant **no** access.

---

## No self-escalation

- a user cannot grant themselves a role or broader scope unless they already possess the required role-management authority
- users cannot grant a role/grant they are not authorized to manage
- Manager cannot assign roles
- Cashier and Collector cannot assign roles
- Owner assignment requires `plm.owner-assignments.manage`
- expanding one’s own Owner authority is subject to maker/checker or controlled Owner Override
- client-side role labels never authorize self-escalation

---

## Platform support / emergency Owner recovery

Platform Owner/Admin does **not** automatically receive PLM operational access.

Future emergency Owner recovery may be performed through a dedicated control-plane recovery workflow only when:

- the Organization has no usable active Owner
- identity and organization authority are verified
- reason/evidence is mandatory
- access restoration is limited to assigning/recovering an Owner
- the support operator does **not** gain ordinary PLM operational grants
- the event is time-stamped and audited in Platform and PLM
- the Organization is notified where appropriate

Do not implement the recovery mechanism here.

---

## High-risk action catalog

Classify as high risk:

- Owner role assignment/removal
- Personal identity correction
- Payment Reversal approval
- Disbursement Reversal approval
- Penalty Reversal approval
- Cash Refund approval/payment
- Cash Variance Resolution
- Owner Override
- future Write-Off
- future Recovery adjustment
- future restructuring approval where financial terms change

High-risk actions require: reason; evidence/notes where applicable; actor; approver; original transaction/resource reference; policy/grant version; Organization/Branch; time; channel/device where available; enhanced audit.

**PLM-D-00-13 remains Closed** for maker/checker and controlled Owner Override.

---

## Step-up authentication direction

High-risk actions should require recent/step-up authentication when the Platform production authentication architecture supports it.

Do **not** implement MFA, select token/session mechanism, alter R-091, or invent a temporary bypass. Exact mechanism remains a Platform authentication/security decision.

---

## Audit and denied actions

Successful high-risk actions must be audited.

Security-significant denied attempts should also be auditable when useful, including:

- cross-organization access
- unauthorized role assignment
- self-approval attempt
- out-of-scope borrower/loan lookup
- duplicate financial submission
- invalid Owner Override
- last-Owner removal attempt

Audit logging must avoid unnecessary sensitive-data exposure.

---

## Legal / security boundary

No privileged-access policy is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.** **R-091 Closed for Phase 13 scope.**

---

## Explicit non-goals

- Authentication implementation
- Platform recovery mechanism implementation
- Schema design
