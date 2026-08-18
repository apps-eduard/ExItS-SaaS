# Pinoy Loan Manager — Authorization Matrix

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Platform access ≠ product operational permission.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager / `pinoy-loan-manager` (proposed) |
| Status | Draft — role presets recorded; grants **Open / Product Owner Decision Required** (PLM-D-00-06) |
| Implementation present | No |

## Layers

```text
Trusted actor → org context → Platform product access → commercial state → entitlements
  → product-local role → product-local grant → resource / workflow rules
```

**DECISION D-P12-03:** how commercial state reaches the product is unresolved — document provisional approach here without inventing a final Platform transport.

Provisional commercial approach for this product: **none chosen**. Do not copy PinoyBusinessPOS Dev/Testing commercial headers as the Pinoy Loan Manager production design. Until D-P12-03 is closed, any later Dev/Testing gate must be labeled provisional and must fail closed outside approved environments.

## Platform vs product

| Concern | Platform | Product |
|---|---|---|
| System / org admin roles | Yes | Do not grant product ops by implication |
| Product access assignment | Yes | Consumed only |
| Subscription / entitlement | Yes | Enforced |
| Operational roles / grants | No | **Yes — authoritative** when defined |

## Product roles

Default **presets** recorded as product direction. They must eventually be backed by granular product-local grants. Do **not** hard-code authorization directly to role names. Do **not** copy PinoyBusinessPOS grant sets even where display names overlap.

Detail: [Product/lending-operating-model.md](Product/lending-operating-model.md).

| Role code | Display name | Purpose |
|---|---|---|
| *open* | Owner | Full organization-level Loan Manager administration |
| *open* | Manager | Lending operations and supervision |
| *open* | Cashier | Cash custody, float, remittance, office cash operations |
| *open* | Collector | Field collections, assigned borrowers, remittance |

Role **codes** are not assigned. Final grant matrix remains **Open / Product Owner Decision Required**.

## Grants / permissions

**Status: Open / Product Owner Decision Required.** Presets do not substitute for grants.

| Grant code | Description |
|---|---|
| — | Not defined |

## Matrix

| Role | Grant | Resource | Action | Org scope | Concealment | Commercial state required | Special rules |
|---|---|---|---|---|---|---|---|
| Owner / Manager / Cashier / Collector | Not defined | Not defined | Not defined | own-org (required intent) | 404 (Product Foundation default) | **Status: Open / Product Owner Decision Required** | No implemented matrix until PLM-D-00-06 grants are decided |

## Continuity / denied commercial states

| Commercial state | Operational effect |
|---|---|
| Any denied / unknown state | Fail closed. Specific view-only or continuity behavior **Status: Open / Product Owner Decision Required**. Do not invent. |

## Ownership and workflow rules

- Resource/workflow authorization is a required layer. Exact traditional-loan workflow and remaining calculation rules are not defined (PLM-D-00-08).
- Last-owner / bootstrap rules: **Status: Open / Product Owner Decision Required**.
- POS Customer status never grants Loan operational permission.
- Platform Administrator does not automatically receive Loan operational access.
- Platform Admin Web is not the normal UI for managing borrower loans.

Separation-of-duty baseline (intent, not implemented):

- Collector must not approve their own waiver.
- Collector must not approve their own cash variance.
- Collector must not approve their own loan/disbursement authorization.
- Cashier should not normally be the loan approver.
- Owner/Manager may approve according to grants.

## Explicit non-grants

- Platform Administrator does **not** automatically receive unrestricted product operational access
- PinoyBusinessPOS roles do **not** grant Pinoy Loan Manager operations
- A PinoyBusinessPOS subscription does **not** unlock Pinoy Loan Manager
- EX ID / QR resolution does **not** grant a Personal-to-Borrower relationship
