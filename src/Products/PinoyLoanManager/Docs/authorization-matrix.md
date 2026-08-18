# Pinoy Loan Manager — Authorization Matrix

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Platform access ≠ product operational permission.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager / `pinoy-loan-manager` (proposed) |
| Status | Draft — role presets and grant **intent** recorded; identifiers **Open** (PLM-D-00-06) |
| Implementation present | No |

Planning catalog: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md).

## Layers

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

No client-only authorization. No `if Role == "Manager" then allow everything`. Roles are **default presets** backed by **explicit grants**. No implicit role hierarchy.

**DECISION D-P12-03:** commercial-state transport remains unresolved. Provisional approach: **none chosen**. Do not copy PinoyBusinessPOS Dev/Testing commercial headers as production design. Any later Dev/Testing gate must fail closed outside approved environments.

## Platform vs product

| Concern | Platform | Product |
|---|---|---|
| System / org admin roles | Yes | Do not grant product ops by implication |
| Product access assignment | Yes | Consumed only |
| Subscription / entitlement | Yes | Enforced |
| Operational roles / grants | No | **Yes — authoritative** |

Platform Owner / Platform Admin do **not** automatically receive PLM operational grants.

## Product roles

Organization-scoped PLM **presets**. Role **codes** are not assigned. Custom roles are future work.

| Role code | Display name | Purpose |
|---|---|---|
| *open* | Owner | Organization-level PLM administration |
| *open* | Manager | Lending operations and supervision |
| *open* | Cashier | Cash custody, float, remittance, office cash |
| *open* | Collector | Assigned field collections and remittance |

## Grants / permissions

Planning **categories**, not final identifiers. Full catalog: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md).

## Planning matrix legend

| Mark | Meaning |
|---|---|
| **Allow** | Allowed by default preset (still requires the corresponding grant + scope at runtime) |
| **Deny** | Not allowed by default preset |
| **Scope** | Allowed only with assignment / branch / session scope |
| **Open** | Future decision |

## Default preset intent (planning)

| Capability | Owner | Manager | Cashier | Collector |
|---|---|---|---|---|
| Staff / PLM role management | Allow | Deny | Deny | Deny |
| Configuration manage | Allow | Deny | Deny | Deny |
| Configuration view | Allow | Allow | Scope | Deny |
| Quick Loan template manage / publish | Allow | Deny | Deny | Deny |
| Quick Loan template view | Allow | Allow | Deny | Deny |
| Borrower group manage | Allow | Allow | Deny | Deny |
| Personal / Borrower link request | Allow | Allow | Deny | Deny |
| Borrower create / update | Allow | Allow | Deny | Deny |
| Borrower view | Allow | Allow | Scope | Scope |
| Application / request review | Allow | Allow | Deny | Deny |
| Loan approve / reject | Allow | Allow | Deny | Deny |
| Loan view financials | Allow | Allow | Scope | Scope |
| Office disbursement | Allow | Deny | Allow | Deny |
| Field disbursement execute | Allow | Deny | Deny | Scope |
| Disbursement authorize (create approval) | Allow | Allow | Deny | Deny |
| Office payment post | Allow | Deny | Allow | Deny |
| Field payment post | Allow | Deny | Deny | Scope |
| Collection assignments manage | Allow | Allow | Deny | Deny |
| Collection attempt record | Allow | Allow | Deny | Scope |
| Collection exception request | Allow | Allow | Deny | Scope |
| Collection exception approve | Allow | Allow | Deny | Deny |
| Organization-wide exception declare | Allow | Allow | Deny | Deny |
| Penalty waiver request | Allow | Allow | Deny | Scope |
| Penalty waiver approve | Allow | Allow | Deny | Deny |
| Cash session open / close | Allow | Deny | Allow | Deny |
| Collector float issue | Allow | Deny | Allow | Deny |
| Collector float receive | Allow | Deny | Deny | Scope |
| Remittance submit | Allow | Deny | Deny | Scope |
| Remittance receive / reconcile | Allow | Deny | Allow | Deny |
| Cash variance view | Allow | Allow | Allow | Scope |
| Cash variance resolve | Allow | Allow | Deny | Deny |
| Payment / disbursement reverse request | Allow | Allow | Allow | Scope |
| Reverse approve | Allow | Allow | Deny | Deny |
| Reports operational | Allow | Allow | Scope | Scope |
| Reports financial | Allow | Allow | Deny | Deny |
| Audit view | Allow | Allow | Scope | Deny |
| Unrestricted org-wide financial browse | Allow | Allow | Deny | Deny |
| Platform administration | Deny | Deny | Deny | Deny |
| Silent edit / delete of posted history | Deny | Deny | Deny | Deny |
| Self-approve own Loan | Deny | Open | Deny | Deny |
| Self-approve own waiver | Deny | Open | Deny | Deny |
| Self-resolve own cash variance | Deny | Open | Deny | Deny |

**Open** on Owner/Manager self-approval: whether two distinct humans are required for **all** organization sizes remains a product-owner decision. High-risk self-approval restrictions may still apply where explicitly required. Do **not** fake separation of duties with screen labels.

Collector **Scope** = assigned borrowers / loans / disbursement tasks / own cash accountability only.

Cashier **Scope** = assigned branch / own cash session unless a broader grant is given.

## Continuity / denied commercial states

| Commercial state | Operational effect |
|---|---|
| Any denied / unknown state | Fail closed. Specific view-only or continuity behavior **Open**. |

## Ownership and workflow rules

- Last-owner / bootstrap rules: **Open**.
- POS Customer status never grants Loan operational permission.
- Platform Admin Web is not the normal UI for managing borrower loans.
- Approval and disbursement are separate authorities.
- Small organizations may assign multiple presets to one person; each action remains individually authorized and audited.

## Explicit non-grants

- Platform Administrator does **not** automatically receive unrestricted product operational access
- PinoyBusinessPOS roles do **not** grant Pinoy Loan Manager operations
- A PinoyBusinessPOS subscription does **not** unlock Pinoy Loan Manager
- EX ID / QR resolution does **not** grant a Personal-to-Borrower relationship
- Decline or unlink does **not** authorize Borrower deletion
- Role name alone does **not** authorize an action without the corresponding grant and scope
