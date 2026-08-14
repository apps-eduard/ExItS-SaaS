# Platform Organization Compliance Eligibility

## Purpose

P26-WP03 adds a Platform-owned compliance review lifecycle for each organization. Eligibility is separate from tax-document issuance capability and from Owner education acknowledgment.

## Aggregate fields

`OrganizationSalesDocumentCapability` (Platform, one row per organization):

| Field | Role |
|---|---|
| `ComplianceEligibilityStatus` | Review lifecycle; default `NotRequested` |
| `TaxDocumentIssuanceEnabled` | Separate bool; default `false` |
| `UpdatedAtUtc` / `UpdatedByActorReference` | Last mutation metadata |

Missing capability rows still mean disabled issuance. Ensuring a row for review does not enable issuance.

## Eligibility statuses

`NotRequested` → `Requested` → `DocumentsRequired` / `UnderReview` → `Approved` / `Rejected`, with `Suspended` and `Revoked` from approved paths. Allowed transitions are enforced in domain (`OrganizationSalesDocumentCapability.TransitionEligibility`).

Any transition into a non-`Approved` status disables issuance.

## Issuance capability vs runtime

- Platform may set `TaxDocumentIssuanceEnabled` only when status is `Approved` and the current Owner has acknowledged `transaction-summary-v1`.
- `TaxDocumentIssuanceRuntime.ImplementationAvailable` remains `false`. Enabling the org flag does not create TaxDocuments.
- Tax calculation settings and plan entitlements never grant eligibility or issuance.

## Authorization

- **Owner request:** `POST /api/v1/platform/organizations/{id}/compliance/request` — exact active Organization Owner.
- **Platform transitions / capability:** `POST .../compliance/transition` and `POST .../compliance/tax-document-capability` — Platform `ManageOrganizations`.
- **Status read:** Platform org view or active organization member.

## Audit

- `platform.organization.compliance.requested`
- `platform.organization.compliance.review_started`
- `platform.organization.compliance.documents_required`
- `platform.organization.compliance.approved`
- `platform.organization.compliance.rejected`
- `platform.organization.compliance.suspended`
- `platform.organization.compliance.revoked`
- `platform.organization.tax_document_capability_enabled`
- `platform.organization.tax_document_capability_disabled`

## UI

- Platform Admin: Organizations detail → Compliance tab (status, education snapshot, transition and issuance controls).
- Organization Web: `/organization/sales-documents` Owner request CTA when status is `NotRequested`, `Rejected`, or `Revoked`.

## Migration

`20260814204603_AddOrganizationComplianceEligibilityStatus` adds `compliance_eligibility_status` with default `NotRequested`. No enabling backfill. No LocalStore change.

## Explicit non-claims

This is not BIR compliance, not TaxDocument issuance, and not Production Ready. See
[sales-document boundary](sales-document-compliance-boundary.md),
[acknowledgment design](organization-sales-document-acknowledgment.md),
[organization compliance profile](organization-compliance-profile.md), and the living
[BIR activation roadmap](../compliance/bir-compliance-activation-roadmap.md).
