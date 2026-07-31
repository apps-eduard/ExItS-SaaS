# {{PRODUCT_NAME}} — Architecture

> Template: P12-WP03. Do not duplicate the foundation; link it.  
> Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} / {{PRODUCT_CODE}} |
| Database | {{DATABASE_NAME}} / schema {{SCHEMA_NAME}} |
| Status | Draft / Approved |

## System context

```text
[Actors] → Platform (identity, org, subscription, entitlements, SaaS billing)
                ↓ commercial access (contract — see D-P12-03; do not invent)
         {{PRODUCT_NAME}} API / UI
                ↓
         {{DATABASE_NAME}} (product only)
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
| {{MODULE_1}} | {{MODULE_1_RESP}} | {{MODULE_1_NOTES}} |
| {{MODULE_2}} | {{MODULE_2_RESP}} | {{MODULE_2_NOTES}} |

## Data ownership

| Data | SoR | Cross-boundary |
|---|---|---|
| Platform Org / User ids | Platform | Guid only — no FK |
| Product operational entities | Product DB | Never in Platform DB |
| Commercial subscription state | Platform | Via approved contract only |

## Organization isolation

- Server derives/validates org context; do not trust client org ids as authority alone.
- Cross-org access: conceal ({{CONCEALMENT_BEHAVIOR}}, e.g. 404).
- No shared operational DB with other products.

## Isolation rules (non-negotiable)

- [ ] No cross-product FKs
- [ ] No direct Platform table reads from this product
- [ ] No Platform reads of this product’s operational tables
- [ ] No shared authoritative operational database

## External integrations

| System | Direction | Contract | Notes |
|---|---|---|---|
| {{INTEGRATION_1}} | in / out / both | {{CONTRACT}} | {{NOTES}} |

## Deployment boundary

| Artifact | Name / notes |
|---|---|
| Product image | {{IMAGE_NAME}} (independently versioned) |
| Platform images | Separate — do not fork per customer |
| Persistent DB | {{DATABASE_NAME}} |
| Config | Environment / secrets — not source forks |

Detail: `deployment-notes.md` when packaging begins.

## Observability and background work

| Concern | Approach |
|---|---|
| Logging / correlation | {{LOGGING}} |
| Metrics / health | {{HEALTH}} |
| Background jobs | {{JOBS}} — product-owned workers only; no shared Hangfire DB with other products |

## Explicit non-goals

- {{ARCH_EXCLUSION_1}}
