# Organization Compliance Profile

## Purpose

P26-WP04 introduces an organization-scoped **compliance profile anchor** and a living activation playbook. The profile prepares Platform data ownership for future confirmed tax/compliance fields without inventing BIR requirements.

Canonical playbook: [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md).

## What exists today

`OrganizationComplianceProfile` (Platform, one row per organization):

| Field | Role |
|---|---|
| `OrganizationId` | Organization scope key (not Owner / Personal user) |
| `CreatedAtUtc` | Anchor creation |
| `UpdatedAtUtc` | Last touch |
| `UpdatedByActorReference` | Optional Platform actor reference |

Business identity currently continues to live on `OrganizationProfile` (legal name, registered address lines, city/region/postal/country). Tax calculation remains on POS `OperationalSetup`. Eligibility and issuance remain on `OrganizationSalesDocumentCapability`.

`GetOrganizationComplianceProfile` combines:

- profile initialization timestamps (if the anchor exists)
- `OrganizationProfile` identity fields already stored
- capability eligibility / `TaxDocumentIssuanceEnabled`
- `TaxDocumentIssuanceRuntime.ImplementationAvailable` (currently `false`)
- `DocumentMode = TransactionSummary`
- snapshot guidance for future TaxDocument issuance

Missing anchors do not invent regulatory identity. Ensure creates the empty anchor only; it does not enable TaxDocument.

## What is deliberately absent

- No TIN / BIR registration / permit / series fields invented “just in case”
- No TaxDocument generation or runtime enablement
- No public QR exposure of the profile
- No fake evidence verification or public document URLs
- No Personal-profile storage of business regulatory identity

## Ownership and transfer

The profile belongs to the organization. Ownership transfer changes membership authority only; it must not move, clear, or rewrite the org-scoped profile with the new Owner’s Personal identity.

## Authorization

- **GET** `.../compliance-profile` — Platform organization view **or** active organization member.
- **POST** `.../compliance-profile/ensure` — Platform `ManageOrganizations` only.

Sensitive management of future confirmed regulatory fields (when added) remains Platform-controlled. See [platform-organization-compliance-eligibility.md](platform-organization-compliance-eligibility.md).

## Migration

`20260814205652_AddOrganizationComplianceProfiles` creates `platform.organization_compliance_profiles`. No enabling backfill. LocalStore unchanged.

## Branch extension point

Organization-level profile/capability does not automatically determine every branch’s future regulatory configuration. Branch-scoped fields may be added only after requirements are confirmed in the [activation roadmap](../compliance/bir-compliance-activation-roadmap.md). Do not invent branch rules in this package.

## Historical snapshot invariant

Future TaxDocument issuance must snapshot seller compliance facts at issuance time. Later organization profile changes must not rewrite historical documents. Current and historical sales remain Transaction Summaries while TaxDocument remains unavailable.

## Explicit non-claims

This is not BIR compliance, not TaxDocument issuance, and not Production Ready. See
[sales-document boundary](sales-document-compliance-boundary.md) and
[activation roadmap](../compliance/bir-compliance-activation-roadmap.md).
