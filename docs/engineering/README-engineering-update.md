# Phase 16 Engineering Documentation Update Package

Replace the canonical files in `docs/engineering/` with the files in this package.

## Replace in place

```text
docs/engineering/approved-architecture-summary.md
docs/engineering/platform-product-capability-boundary.md
docs/engineering/platform-product-contracts.md
docs/engineering/entitlement-state-matrix.md
docs/engineering/authorization-matrix.md
docs/engineering/data-ownership.md
docs/engineering/platform-product-contract-matrix.md
```

Do not keep duplicate `-v2`, `-old`, or `-copy` files inside the active engineering folder. Git preserves the previous versions.

## Architecture files that must also be current

```text
docs/architecture/saas-scopes-users-boundaries-navigation.md
docs/architecture/user-creation-flow-and-account-scope-rules.md
docs/architecture/product-catalog-entitlement-and-role-model.md
```

## Additional repository records to update after implementation

These are project-status records, not included as replacements in this package because they must contain actual implementation evidence and commit SHAs:

```text
docs/phase-progress.md
docs/reports/P16-WP11-*.md
```

Record actual migrations, tests, browser validation, Local Validation reset results, Production fake-provider guard result, defects, and commit hashes.

## Important reconciliation decisions encoded here

- Business Plan trial is 14 days.
- Personal Utang is free Personal functionality, not a three-calendar-month SaaS trial.
- Personal user-facing “upgrade” means Start a Business; the Subscription belongs to the new Organization.
- Plans have monthly/annual PHP pricing.
- Subscription stores agreed-price snapshots.
- LocalValidationPaymentProvider is test-only and fails closed in Production.
- Upgrade is normally immediate after successful payment.
- Downgrade is scheduled and non-destructive.
- Web and API contracts must be stabilized before MAUI reconciliation.
