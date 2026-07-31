# {{PRODUCT_NAME}} — Deployment Notes

> Template: P12-WP03. **Documentation only** until a packaging WP is authorized.  
> Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Status | Not started / Pilot / … |

## Images

| Artifact | Name | Versioning |
|---|---|---|
| Product API (or app) | {{IMAGE_NAME}} | Independently versioned; immutable tags |
| Platform | Separate images | Do not bake this product into Platform image |

- [ ] No customer-specific source forks — configuration only
- [ ] Deploy only when organization is licensed/subscribed for {{PRODUCT_CODE}}

## Database

| Item | Value |
|---|---|
| Database | {{DATABASE_NAME}} |
| Schema | {{SCHEMA_NAME}} |
| Migrations | {{MIGRATION_PROCESS}} — never silent `Migrate()` on production startup paths |

## Persistent storage / volumes

{{STORAGE_NOTES}}

## Configuration and secrets

| Key / area | Source | Notes |
|---|---|---|
| {{CONFIG_KEY}} | env / secret store | {{CONFIG_NOTES}} |

## Health checks

| Endpoint / probe | Meaning |
|---|---|
| {{HEALTH}} | liveness / readiness |

## Backup / restore

- Independent of Platform DB backups
- {{BACKUP_RESTORE_NOTES}}

## Upgrade / rollback

| Action | Procedure |
|---|---|
| Upgrade | {{UPGRADE}} |
| Rollback | {{ROLLBACK}} — compatibility constraints: {{COMPAT}} |

## Environment limitations

- Production TLS / readiness: follow portfolio risks — do not claim Production-ready without evidence
- Auth: R-091 open unless closed
- {{ENV_LIMIT_1}}
