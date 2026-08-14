# P26-WP01 — Sales Document and Compliance Readiness Foundation

## Status

**Code Complete / Validation Pending.** Phase 26 remains **OPEN**. Phase 25 also remains **OPEN** with owner validation pending.

**Starting SHA:** `6677421eaac2e70746addd8b75660926ff630431`  
**Feature SHA:** `b616cd3ed7100986d1b8e926634d4ccde581536c`

## Delivered

- Platform organization-scoped `OrganizationSalesDocumentCapability`, repository, default-off queries, and blocked issuance use cases.
- Read-only member/owner API: `GET /api/v1/platform/organizations/{organizationId}/sales-document-capability`.
- Idempotent Platform migration `AddOrganizationSalesDocumentCapabilities`.
- POS `SalesDocumentKind` and `PosSaleDto.DocumentKind`, with all current sales mapped to `TransactionSummary`.
- English/Filipino sales-document localization, online/offline Transaction Summary presentation, and non-authorizing disclaimer.
- Organization Web sales history terminology changed from receipt to Transaction Summary.
- Unit/UI guard coverage for default-off behavior, multi-organization isolation, mapping, rejection, wording, and public identity DTO boundaries.

## Boundaries preserved

- Tax calculation settings remain intact and do not enable tax-document issuance.
- Capability is Platform-controlled, off by default, and independent of plans and feature overrides.
- There is no enable endpoint, owner self-enable, or Platform grant/revoke UI.
- Public QR resolvers expose no TIN/tax fields.
- LocalStore schema/version and offline Transaction Summary behavior are unchanged.
- Ownership transfer preserves capability because the capability key is `OrganizationId`.

## Explicit exclusions

No BIR rules, invoice series, education acknowledgment, capability grant workflow, or TaxDocument generation was implemented. No UI claims BIR compliance.

## Persistence

Migration: `20260814200436_AddOrganizationSalesDocumentCapabilities` / `AddOrganizationSalesDocumentCapabilities`.

The migration uses `CREATE TABLE IF NOT EXISTS`; the table has one row per organization and defaults `tax_document_issuance_enabled` to false.

## Validation evidence

- Platform API Release build: passed, 2 existing obsolete-API warnings, 0 errors.
- Organization Web Release build: passed, 0 warnings, 0 errors.
- Platform unit tests: 899 passed, 0 failed, 0 skipped.
- POS unit tests: 730 passed, 0 failed, 0 skipped.
- P26 sales-document MAUI guards: 2 passed, 0 failed, 0 skipped.
- Complete MAUI guard suite: 409 passed / 3 failed. The failures are in pre-existing protected cash/shift/auth work (`ShiftsPageGuardTests`, `OperationalSetupUiGuardTests`, `MauiFoundationGuardTests`), outside P26-WP01; those files were not changed.
- Complete solution Release build reached the MAUI Android target and was blocked by missing Android SDK (`XA5300`); non-Android P26 projects built successfully.
- Known existing warnings include NU1903 for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

## Risks and open decisions

- TaxDocument remains unavailable until an audited Platform grant path and compliant immutable snapshot model exist.
- Legal/regulatory requirements require qualified review; tracked in [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md) after P26-WP04.
- Owner/browser/device validation remains outstanding for earlier open phases.

## Next

**P26-WP02 — Owner education and acknowledgment UX.** Do not begin P26-WP03 grant administration or any issuance rules as part of WP02.
