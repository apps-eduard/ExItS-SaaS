# P16-WP08 — Start a Business and Utang Migration

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `ada6169eb685cc1a7b646b8f153a2df732cfac0b` (after P16-WP07 tip-hash) |
| Feature commit | `cb3f3585e07e6b0865df1a40175b9f5b99a22a78` |
| Date | 2026-08-02 |

## Scope completed

- Start a Business orchestration from Personal session: create Organization, grant Organization Owner, activate Organization Account Profile, switch to Organization-scoped session, provisional POS catalog/trial entitlement snapshot, product access grant, and Platform-recorded POS Owner product-local role grant (separate grants).
- Optional selective Personal Utang → Business Credit migration with preview, confirmation token, idempotency key, destination validation, duplicate prevention, durable batch/item provenance, and archive/retain/mark-transferred source disposition.
- Linked-participant consent required before transferring relationship data involving a registered counterparty.
- Hard guards: no silent full dump; Personal Utang remains personal-owned until migrated; Business Customers never become Organization Staff; Personal session cannot mutate org credit APIs.
- EF migration `AddStartBusinessAndUtangMigration`.
- Unit + integration regression coverage.

## Files changed (high level)

- Domain: migration batch/item, opening balance, product-local role grant; Personal contact archive; relationship archive/transfer + destination provenance fields
- Application: `StartBusinessForPersonalUser`, `PreviewPersonalUtangMigration`, `ExecutePersonalUtangMigration`, repository contracts
- Infrastructure: records/repos, DbContext, migration `AddStartBusinessAndUtangMigration`
- API: `POST /api/v1/personal/start-business`; org utang-migration preview/execute routes; DI; ProblemDetails mappings
- Tests: `StartBusinessAndUtangMigrationDomainTests`, `ApiStartBusinessAndUtangMigrationTests`

## Schema and migration changes

Migration `AddStartBusinessAndUtangMigration`:

| Table | Purpose |
|---|---|
| `platform.personal_utang_migration_batches` | Preview/execute batch, confirmation token, include options, disposition, consent, idempotency |
| `platform.personal_utang_migration_items` | Per-source selection + destination provenance |
| `platform.business_credit_opening_balances` | Org-owned opening credit balance with ADR-020 provenance |
| `platform.product_local_role_grants` | Provisional Platform record of product-local role (e.g. POS Owner) |

Also adds destination/migration columns on `platform.personal_debt_relationships`.

WP03–WP07 tables remain intact. No POS product DB schema changes in this WP (POS Owner is Platform-recorded provisional grant).

## API routes added

| Method | Route | Notes |
|---|---|---|
| POST | `/api/v1/personal/start-business` | Personal session only; returns new Organization session token + separate grant flags |
| POST | `/api/v1/organizations/{orgId}/utang-migrations/preview` | Org Owner; explicit selection required; returns confirmation token |
| POST | `/api/v1/organizations/{orgId}/utang-migrations` | Org Owner; requires batchId + confirmationToken + idempotencyKey |

## Exit criteria

| Criterion | Evidence |
|---|---|
| Organization Owner, POS entitlement, and POS role are separate grants | Start-business DTO flags `organizationOwnerGranted` / `posEntitlementActivated` / `posOwnerRoleGranted` |
| Repeated migration does not duplicate records | Idempotency replay + already-migrated blocked preview → 409 |
| Linked participant data not transferred without consent | Preview blocks; execute → `application.utang_migration.consent_required` |
| Destination Organization owns imported records independently | BusinessCustomer + CreditCustomer + OpeningBalance under destination org |
| Source provenance remains available | Migration items + opening-balance provenance columns |
| Regression suite passes | Unit 337 / Integration 170 |

## Audit coverage

- `platform.business_upgrade.started`
- `platform.business_upgrade.completed`
- `platform.product_local_role.granted`
- `platform.utang_migration.previewed`
- `platform.utang_migration.executed`

## Seed-data changes

None. Start-business ensures a provisional POS trial catalog when plan/trial ids are omitted (dev/testing-safe).

## Tests added

- `StartBusinessAndUtangMigrationDomainTests` — archive/transfer, selection required, role grant separation, provenance
- `ApiStartBusinessAndUtangMigrationTests` — happy path grants, migration + idempotency + duplicate, wrong org, Personal scope denial, consent block

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **337 passed**, 0 failed, 0 skipped
- Platform integration: **170 passed**, 0 failed, 0 skipped
- Build: Platform API Release — 0 warnings, 0 errors

## Explicit exclusions

- Product navigation / product-local role enforcement in POS UI (P16-WP09)
- POS product DB role assignment sync from Platform grant (provisional Platform record only)
- Continuous Personal↔Business ledger sync (prohibited)
- Phase 14 production closeout
- WP03–WP07 feature SHAs unchanged

## Explicit next work package

**P16-WP09** — Product Access and Navigation Integration.

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**. POS Owner grant is Platform-recorded and provisional until WP09 product consumption.
