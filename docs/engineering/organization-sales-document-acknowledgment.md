# Organization Sales-Document Acknowledgment

## Purpose

P26-WP02 records that the current active Organization Owner reviewed the current sales-document education. It is education state only: acknowledgment never grants compliance eligibility, never enables tax-document issuance, and remains independent of the P26-WP03 Platform eligibility lifecycle.

## Version and aggregate

`SalesDocumentEducationVersions.Current` is `transaction-summary-v1`.

Platform owns append-only `OrganizationSalesDocumentAcknowledgment` rows:

- `Id`
- `OrganizationId`
- `UserId` (the acknowledging Owner)
- `Version`
- `AcknowledgedAtUtc`
- optional `ContentKey`

The database uniquely constrains `(OrganizationId, UserId, Version)`. Repeating the same acknowledgment is idempotent. No row is fabricated or backfilled.

## Current status

Status is evaluated against the current active `OrganizationOwner` membership, not merely the caller:

- the current Owner has a row for the current version: acknowledged;
- no matching row: Owner action required;
- a non-owner active member may read status but cannot acknowledge.

Ownership transfer retains former-owner rows as history. The incoming Owner must acknowledge the current version independently. A future version change similarly requires a new row while preserving earlier versions.

## API and authorization

- `GET /api/v1/platform/organizations/{organizationId}/sales-document-education`
  - active organization member;
  - reports current-Owner status and the read-only capability snapshot.
- `POST /api/v1/platform/organizations/{organizationId}/sales-document-education/acknowledge`
  - exact current active Organization Owner;
  - actor comes from authenticated context, never request body;
  - writes `platform.organization.sales_document_education_acknowledged` on first insert.

The response remains `DocumentMode=TransactionSummary`,
`TransactionSummaryAvailable=true`, and reflects the independently controlled
capability snapshot (`ComplianceEligibilityStatus` / `TaxDocumentIssuanceEnabled`).
This use case never mutates eligibility or issuance. Platform may later require a
current-Owner acknowledgment as a precondition before enabling issuance; that gate
lives on the capability use case, not on acknowledgment itself.

## Soft-gate behavior

Organization Web exposes the Owner page at `/organization/sales-documents`.
MAUI exposes `/sales-document-education`. When the MAUI start route reaches setup
and the Owner still needs to act, navigation prompts with the education page first.
Failure to read education status fails open to normal setup.

This is deliberately a soft gate:

- checkout, sales, synchronization, and offline operation are not blocked;
- Cashier and other staff see a friendly Owner-required message and no checkbox;
- acknowledgment is not a compliance certification and does not set eligibility status;
- the UI does not claim BIR compliance.

Compliance review request and Platform eligibility transitions are separate surfaces; see
[platform organization compliance eligibility](platform-organization-compliance-eligibility.md).

## Migration

`AddOrganizationSalesDocumentAcknowledgments` uses PostgreSQL
`CREATE TABLE IF NOT EXISTS` and idempotent indexes. It adds no POS or LocalStore
schema and does not move operational sale data into Platform.
