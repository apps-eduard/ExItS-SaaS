# ReferenceLoan — Architecture

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | ReferenceLoan / `reference-loan` |
| Database | `ExItS_ReferenceLoan` / schema `loan` |
| Status | Draft — fictional validation only |

## System context

```text
[Actors] → Platform (identity, org, subscription, entitlements, SaaS billing)
                ↓ commercial access (contract — see D-P12-03; do not invent)
         ReferenceLoan API / UI
                ↓
         ExItS_ReferenceLoan (product only)
```

## Responsibility boundary

| Area | Platform | This product |
|---|---|---|
| Identity / future prod auth | Yes (R-091 open) | Consume trusted actor only |
| Org membership | Yes | Guid reference + isolation |
| Subscription / entitlements | Yes | Enforce; no Platform table reads |
| SaaS payments | Yes | No |
| Domain workflows | No | Yes |
| Product roles / grants | No | Yes |
| Operational money | No | Yes |
| Product DB / migrations | No | Yes |

## Product modules

| Module | Responsibility | Notes |
|---|---|---|
| Accounts | Illustrative account records | Placeholder only — no schema invented beyond naming |
| Money movements | Disbursements / repayments / fees | Operational money in product DB |

## Data ownership

| Data | SoR | Cross-boundary |
|---|---|---|
| Platform Org / User ids | Platform | Guid only — no FK |
| Product operational entities | Product DB | Never in Platform DB |
| Commercial subscription state | Platform | Via approved contract only |

## Organization isolation

- Server derives/validates org context; do not trust client org ids as authority alone.
- Cross-org access: conceal (404).
- No shared operational DB with other products (including POS).

## Isolation rules (non-negotiable)

- [x] No cross-product FKs
- [x] No direct Platform table reads from this product
- [x] No Platform reads of this product’s operational tables
- [x] No shared authoritative operational database

## External integrations

| System | Direction | Contract | Notes |
|---|---|---|---|
| None | — | — | No external integrations in this dry run |

## Deployment boundary

| Artifact | Name / notes |
|---|---|
| Product image | `exits-reference-loan` (independently versioned; not built in this WP) |
| Platform images | Separate — do not fork per customer |
| Persistent DB | `ExItS_ReferenceLoan` |
| Config | Environment / secrets — not source forks |

## Observability and background work

| Concern | Approach |
|---|---|
| Logging / correlation | Correlation ids; no secret/PII dumps |
| Metrics / health | Product-owned probes when implemented |
| Background jobs | Product-owned workers only; no shared Hangfire DB with other products |

## Explicit non-goals

- Implementing lending features, migrations, or APIs in Phase 12
- Copying POS sales/inventory/utang models
