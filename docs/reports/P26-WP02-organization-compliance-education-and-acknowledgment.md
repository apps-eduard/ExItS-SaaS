# P26-WP02 — Organization Compliance Education and Acknowledgment

## Status

**Code Complete / Validation Pending.** Phase 26 remains **OPEN**. Phase 25 remains **OPEN**. This work package makes no BIR-compliance claim and does not enable `TaxDocument`.

**Starting SHA:** `cb1239385d42161f854792bc0228444c781541ce`  
**Feature SHA:** `40830d66`

## Delivered capability

- Added versioned, organization/Owner-scoped acknowledgment history with current version `transaction-summary-v1`.
- Added active-member status read and exact-current-Owner acknowledgment APIs.
- Added idempotent persistence and audit action `platform.organization.sales_document_education_acknowledged`.
- Added Organization Web Owner education at `/organization/sales-documents`.
- Added MAUI education at `/sales-document-education`, an Owner setup soft prompt, and a More entry.
- Added English and Filipino education copy.
- Added ownership-transfer, version-change, isolation, authorization, idempotency, document-kind, and capability-boundary tests.

## Gate decision

The selected gate is **soft**. An Owner who has not acknowledged is prompted in Organization Web and before MAUI setup continues. A failed status read does not block setup. Checkout, sale creation, synchronization, and offline operation are never hard-blocked. Cashiers receive an Owner-required message and no acknowledgment control.

## Persistence and migration

Migration: `20260814202131_AddOrganizationSalesDocumentAcknowledgments`
(`AddOrganizationSalesDocumentAcknowledgments`).

The Platform table retains historical Owner/version rows and uniquely indexes
`(organization_id, user_id, version)`. Ownership transfer does not rewrite or delete
the former Owner's acknowledgment. No fake backfill is created. No POS database or
LocalStore migration is involved.

## API and security

- GET education status: active organization member in trusted organization context.
- POST acknowledgment: authenticated actor is resolved server-side and must be the exact active `OrganizationOwner`.
- Platform Administrator has no acknowledge-as-Owner bypass.
- The mutation writes an append-only Platform audit record on first acknowledgment.
- The use case reads but never writes `TaxDocumentIssuanceEnabled`.

## Validation evidence

- Platform API Release build: passed, 2 pre-existing obsolete-API warnings / 0 errors.
- Organization Web Release build: passed, 0 warnings / 0 errors.
- Platform unit tests: 906 passed, 0 failed, 0 skipped.
- MAUI guard tests: 411 passed, 3 failed due to pre-existing cash/shift/auth guard mismatches outside P26-WP02.
- Android MAUI build: blocked locally because the Android SDK directory is unavailable (`XA5300`); existing SQLite package advisory `NU1903` also remains.
- EF model/snapshot check: no pending model changes.
- Complete solution Release build: 42 warnings / 1 error; the only error is the same missing Android SDK (`XA5300`).

## Explicit exclusions

- No BIR rules, registration, authorization, invoicing, numbering, series, or compliance certification.
- No tax-document capability grant/revoke.
- No hard block on sales, checkout, sync, or offline operation.
- No Platform Admin acknowledgment impersonation.
- No Platform Admin status UI.
- No rewrite of historical acknowledgment rows.

## Risks and open decisions

- Physical-device validation remains pending.
- Migration apply/rollback/re-apply against an authorized non-production PostgreSQL database remains pending.
- Existing Phase 25 cash/shift leftovers and their guard failures were intentionally not modified.

## Documentation changed

The phase page, reports index, portfolio progress, implementation summary,
sales-document boundary, client-experience boundary, engineering acknowledgment
reference, and file manifest now describe P26-WP02.

## Next work package

**P26-WP03 — Platform-controlled grant/revoke administration and audit** (delivered separately).  
Later foundation: [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md) (P26-WP04).
P26-WP03 must remain separate from Owner acknowledgment and must not infer authority
from education state.
