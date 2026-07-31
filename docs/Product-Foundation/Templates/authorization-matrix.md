# {{PRODUCT_NAME}} — Authorization Matrix

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)
> Platform access ≠ product operational permission.

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} / {{PRODUCT_CODE}} |
| Status | Draft / Approved |

## Layers

```text
Trusted actor → org context → Platform product access → commercial state → entitlements
  → product-local role → product-local grant → resource / workflow rules
```

**DECISION D-P12-03:** how commercial state reaches the product is unresolved — document provisional approach here without inventing a final Platform transport.

Provisional commercial approach for this product: {{COMMERCIAL_TRANSPORT_NOTE}}

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
| {{ROLE_CODE}} | {{ROLE_NAME}} | {{ROLE_PURPOSE}} |

## Grants / permissions

| Grant code | Description |
|---|---|
| {{GRANT_CODE}} | {{GRANT_DESC}} |

## Matrix

| Role | Grant | Resource | Action | Org scope | Concealment | Commercial state required | Special rules |
|---|---|---|---|---|---|---|---|
| {{ROLE}} | {{GRANT}} | {{RESOURCE}} | {{ACTION}} | own-org / … | 404 / 403 / … | {{COMMERCIAL}} | {{SPECIAL}} |

## Continuity / denied commercial states

| Commercial state | Operational effect |
|---|---|
| {{STATE}} | deny / view-only / … — {{EFFECT_NOTES}} |

## Ownership and workflow rules

- {{OWNERSHIP_RULE_1}}
- Last-owner / bootstrap rules: {{BOOTSTRAP_RULES}}

## Explicit non-grants

- Platform Administrator does **not** automatically receive unrestricted product operational access
- {{NON_GRANT_1}}
