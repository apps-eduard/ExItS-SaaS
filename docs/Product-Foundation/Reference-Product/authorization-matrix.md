# ReferenceLoan — Authorization Matrix

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)  
> Platform access ≠ product operational permission. Sample roles only — not POS roles.

| Field | Value |
|---|---|
| Product | ReferenceLoan / `reference-loan` |
| Status | Draft — fictional validation only |

## Layers

```text
Trusted actor → org context → Platform product access → commercial state → entitlements
  → product-local role → product-local grant → resource / workflow rules
```

**DECISION D-P12-03:** how commercial state reaches the product is unresolved — document provisional approach here without inventing a final Platform transport.

Provisional commercial approach for this product: Document as open; when implemented, consume Platform commercial facts via an approved contract only — **never** direct Platform EF/SQL. Dev/Testing may use provisional patterns consistent with portfolio honesty rules.

## Platform vs product

| Concern | Platform | Product |
|---|---|---|
| System / org admin roles | Yes | Do not grant product ops by implication |
| Product access assignment | Yes | Consumed only |
| Subscription / entitlement | Yes | Enforced |
| Operational roles / grants | No | **Yes — authoritative** |

## Product roles

| Role code | Display name | Purpose |
|---|---|---|
| LoanOfficer | Loan officer | Operational manage within org |
| LoanViewer | Loan viewer | Read-only within org |

## Grants / permissions

| Grant code | Description |
|---|---|
| `loan-accounts-view` | View illustrative account summaries |
| `loan-accounts-manage` | Mutate illustrative account records (when authorized to implement) |

## Matrix

| Role | Grant | Resource | Action | Org scope | Concealment | Commercial state required | Special rules |
|---|---|---|---|---|---|---|---|
| LoanOfficer | `loan-accounts-view` | accounts | read | own-org | 404 | Active (illustrative) | — |
| LoanOfficer | `loan-accounts-manage` | accounts | write | own-org | 404 | Active (illustrative) | Server-authoritative |
| LoanViewer | `loan-accounts-view` | accounts | read | own-org | 404 | Active (illustrative) | — |

## Continuity / denied commercial states

| Commercial state | Operational effect |
|---|---|
| Suspended / unknown | deny mutations — exact continuity matrix deferred (do not invent) |

## Ownership and workflow rules

- Resources belong to the trusted organization Guid
- Last-owner / bootstrap rules: deferred — record as open if productized

## Explicit non-grants

- Platform Administrator does **not** automatically receive unrestricted product operational access
- No POS Cashier/StoreManager grants apply here
