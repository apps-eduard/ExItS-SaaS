# Pinoy Buy Now Pay Later — Authorization Matrix

> Grant identifiers are **Open** (BNPL-D-00-18). Do not hard-code authorization to role names. Do not copy POS/PLM/PSP grant sets.

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Status | Planning baseline (BNPL-00) |
| Last updated | 2026-08-27 |
| Implementation present | No |

## Access layers

| Layer | Owner | Purpose |
|---|---|---|
| Platform account / session | Platform | Authenticated actor |
| Organization membership | Platform | Org membership context |
| Product entitlement | Platform | BNPL subscription/commercial gate |
| Branch access | Org / product policy | Scope financed sales and ops to allowed branches |
| BNPL role / grant | BNPL product-local | Operational permission |
| Customer ownership / visibility | BNPL + Platform contracts | Customer sees own plans; staff see org-scoped plans |

## Role presets (intent — identifiers open)

| Preset | Purpose | Typical grant areas (intent) |
|---|---|---|
| Owner | Org-level BNPL administration | Config, staff grants, audit, settlement overview |
| Manager | Operations supervision | Approvals within policy, reports, overdue oversight |
| BNPL Approver | Eligibility / offer approval | Approve/decline applications; not necessarily settlement |
| Sales / Cashier | Originate financed purchase | Create request, attach cart/sale intent; limited approval |
| Collector / Support | Follow-up and repayment capture | Collections queue, record repayments within policy |
| Reporting / Read-only | Analytics | Read reports; no mutations |

These are **presets**, not final codes. Catalog: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md).

## Capability matrix (intent)

| Capability | Owner | Manager | Approver | Sales | Collector | Read-only |
|---|---|---|---|---|---|---|
| Configure BNPL product settings | Y | limited | N | N | N | N |
| Create financing application | Y | Y | Y | Y | N | N |
| Approve / decline | Y | Y* | Y | N* | N | N |
| Activate after commerce sale (system) | system | system | system | system | — | — |
| Record repayment | Y | Y | N | limited | Y | N |
| View overdue / collections | Y | Y | limited | N | Y | Y |
| Merchant settlement ops | Y | Y* | N | N | N | N |
| View audit | Y | Y | limited | N | limited | limited |
| Customer self-service (future) | — | — | — | — | — | own plans |

\*Subject to policy (Open decisions). “system” means server workflow after successful commerce sale — not a human grant to invent ACTIVE state without sale.

## Least privilege

- Deny by default
- Separate “create application” from “approve”
- Separate “record repayment” from “settle merchant”
- Branch-scoped staff must not see other branches’ plans unless grant allows
- Customer/Personal actors never receive organization staff grants

## Intersection with Commerce

POS cashiers may initiate Path A (POS first) only if both:

1. POS operational permission to checkout, and  
2. BNPL grant to create financing request (or a documented bridge grant)

BNPL must not rely on “POS role alone” for financing approval.
