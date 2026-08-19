# Pinoy Loan Manager — Resource Scope and Data Minimization Policy

**Status:** Accepted product policy (PLM-DOC-05); not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

Scope types, server-side filtering, and role-based data minimization. Not a persistence schema or security-production certification.

**Canonical companions:** [authorization-grant-catalog.md](authorization-grant-catalog.md), [default-role-preset-policy.md](default-role-preset-policy.md), [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md). ADR: [../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md](../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md).

---

## Scope types

| Scope | Meaning |
|---|---|
| **Organization** | All authorized resources inside one Organization |
| **Branch** | Only resources assigned/owned by one or more authorized Branches |
| **Assigned Work** | Only explicitly assigned Borrowers, Loans, collections, disbursements, routes, or tasks |
| **Own Session / Accountability** | Only the actor’s current Cashier Session or Collector accountability context |

No scope crosses Organizations.

---

## Default role scopes

| Preset | Scope |
|---|---|
| `plm.owner` | Organization |
| `plm.manager` | Organization or one/multiple Branch scopes, as assigned |
| `plm.cashier` | One or multiple Branch scopes; financial execution additionally requires Own Cashier Session |
| `plm.collector` | One or multiple Branch scopes; Borrower/Loan access additionally requires Assigned Work; cash requires Own Collector Accountability |

Changing a role’s scope is auditable.

---

## Server-side resource filtering

Approve:

- all queries/lists are filtered by trusted Organization and effective scope
- direct resource lookup repeats the same authorization
- possession of a resource ID is not authorization
- cross-tenant lookup should not reveal unnecessary existence information
- use not-found-equivalent behavior where appropriate to prevent tenant data enumeration
- exports/reports apply the same scope rules
- background jobs carry trusted tenant/product/scope context
- audit records preserve Organization and Branch context

---

## Data minimization by role

### Owner / Manager

May view full PLM operational Borrower and Loan information according to scope and grants, including authorized underwriting/supporting documents.

### Cashier

Receives only information required for:

- borrower verification
- office disbursement
- office payment
- settlement/prepayment execution
- refund payment
- remittance/reconciliation

Cashier does **not** receive default access to:

- underwriting assessments
- income analysis
- references
- full borrower document repository
- unrelated internal collection notes

### Collector

Receives the minimum required for assigned field work:

- borrower display identity
- authorized contact/location
- assigned Loan reference
- amount due/past due
- permitted balance summary
- payment/disbursement task
- relevant collection instructions

Collector does **not** receive default access to:

- full identity-document images
- underwriting files
- income details
- unrelated Loans
- other branches
- other Collectors’ routes
- organization-wide reports
- another lender’s data

---

## Commercial state

**D-P12-03 remains Open.**

- unknown/invalid commercial state cannot silently grant write authority
- exact continuity/read-only behavior during suspension or control-plane outage remains a Platform/Product contract decision
- no PLM role or grant bypasses subscription/entitlement enforcement
- PLM authorization documentation does not invent offline license files, headers, leases, or tokens

---

## Legal / security boundary

No scope or minimization rule is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.** **R-091 Closed for Phase 13 scope.**

---

## Explicit non-goals

- Cross-organization scope
- Client-side filtering as authorization
- Schema design
