# P16-WP05 — Personal Utang Core

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `156cd0188abcecc80dd83d30784c0421b01843f7` (after P16-WP04 tip-hash) |
| Feature commit | *(recorded after commit)* |
| Date | 2026-08-02 |

## Scope completed

- Domain: `PersonalContact`, `PersonalDebtRelationship`, `PersonalUtangEntry` (loan/payment/adjustment append model) with aggregate `version` + PostgreSQL `xmin`.
- Application use cases: create contact, create relationship, record entries, list I Lent / I Borrowed, balance, history.
- Dashboard aggregates wired to live contact/relationship balances (WP04 stubs replaced).
- API under `/api/v1/personal/utang/*` (Personal scope only).
- Authorization: participant user or owned contact required; unrelated users receive `403` + `application.personal.utang.unauthorized` with no visibility.
- Audit events for contact create, relationship create, entry record.
- EF migration `AddPersonalUtang` + repositories in Platform DB (`platform` schema). Settings table from WP04 (`AddPersonalAccountSettings`) unchanged.
- Phase 16 seed extension: `InitializePhase16PersonalUtangSeed` chained from `InitializePhase16AccountSeed` for `personal.user1` / `personal.user2` (non-Production).
- Integration/unit tests: balances, `409` stale version, unauthorized denial.

## Files changed (high level)

- Domain: `PersonalContact`, `PersonalDebtRelationship`, `PersonalUtangEntry`, enums, error codes, audit actions
- Application: `PersonalUtangUseCases.cs`, `InitializePhase16PersonalUtangSeed.cs`, dashboard aggregates, account seed wiring
- Infrastructure: personal records, repositories, DbContext, migration `AddPersonalUtang`
- API: utang routes in `PersonalEndpoints.cs`, DI, API result mapping
- Tests: `ApiPersonalUtangTests`, `PersonalUtangDomainTests`

## Schema and migration changes

Migration `AddPersonalUtang` (settings already present from WP04):

| Table | Purpose |
|---|---|
| `platform.personal_contacts` | Owner-scoped contacts (linked/unlinked) |
| `platform.personal_debt_relationships` | Creditor/debtor sides (user or contact), balance, due date |
| `platform.personal_utang_entries` | Append-only loan/payment/adjustment history |

Check constraints enforce exactly one user or contact per side. No cross-DB FKs. WP03 `organization_context_preferences` and WP04 `personal_account_settings` remain intact.

## API routes

| Method | Route | Notes |
|---|---|---|
| POST | `/api/v1/personal/utang/contacts` | Create contact |
| GET | `/api/v1/personal/utang/contacts` | List owned contacts |
| POST | `/api/v1/personal/utang/relationships` | Create relationship (+ optional initial loan) |
| GET | `/api/v1/personal/utang/relationships/lent` | Creditor-side perspective |
| GET | `/api/v1/personal/utang/relationships/borrowed` | Debtor-side perspective |
| GET | `/api/v1/personal/utang/relationships/{id}` | Detail (authorized participants only) |
| GET | `/api/v1/personal/utang/relationships/{id}/balance` | Current balance |
| GET | `/api/v1/personal/utang/relationships/{id}/history` | Entry history |
| POST | `/api/v1/personal/utang/relationships/{id}/entries` | Loan/payment/adjustment (`expectedVersion`) |

## Business rules enforced

- At least one relationship side belongs to the authenticated Personal account (user or owned contact).
- Balance = sum of signed entry deltas; payments decrease balance.
- Stale `expectedVersion` → `409 application.concurrency_conflict`.
- Personal Utang ≠ POS Business Credit (Platform DB only; separate domain).

## Audit coverage

- `platform.personal.contact.created`
- `platform.personal.utang_relationship.created`
- `platform.personal.utang_entry.recorded`

## Seed-data changes

`InitializePhase16PersonalUtangSeed` (idempotent, chained from Phase 16 account seed):

- `personal.user1` lends ₱500 to unlinked contact “Seed Coworker Ana”
- `personal.user1` borrows ₱200 from `personal.user2`

Never runs in Production.

## Tests added

- `ApiPersonalUtangTests` — lifecycle balances, unauthorized `403`, stale version `409`
- `PersonalUtangDomainTests` — loan/payment reconciliation, concurrency domain error

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **322 passed**, 0 failed, 0 skipped
- Platform integration: **159 passed**, 0 failed, 0 skipped
- Build: Platform API Release — 0 warnings, 0 errors

## Explicit exclusions

- Invitations/linking/reminders (P16-WP06)
- Due-date reminder scheduling (P16-WP06)
- Business Utang migration (P16-WP08)
- Mobile navigation UI
- P16-WP03/WP04 SHAs and settings migration unchanged

## Explicit next work package

**P16-WP06** — Invitations, Linking, Reminders, and Notifications.

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**. Seeds never run in Production.
