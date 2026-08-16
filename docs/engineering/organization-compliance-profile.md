# Organization Compliance Profile

## Purpose

Organization-scoped **compliance profile** for ExItS registration readiness and future TaxDocument activation. Confirmed fields are added only with explicit work packages. Canonical playbook: [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md). Design: [bir-registration-readiness-and-activation.md](bir-registration-readiness-and-activation.md).

## What exists today (P26-WP04 + P26-WP06)

`OrganizationComplianceProfile` (Platform, one row per organization):

| Field | Role |
|---|---|
| `OrganizationId` | Organization scope key (not Owner / Personal user) |
| `RegisteredTaxpayerName` | Registered taxpayer / business name for readiness |
| `TinNormalized` | 9-digit TIN (Platform persistence only) |
| `MaskedTin` | Derived display (`***-***-123`) — **only** TIN form on DTOs |
| `SetupStatus` | Org readiness lifecycle (`NotConfigured` … `ActivationBlocked` / `Activated`) |
| `CreatedAtUtc` / `UpdatedAtUtc` / `UpdatedByActorReference` | Audit metadata |

Business address identity continues to live on `OrganizationProfile`. Tax calculation remains on POS `OperationalSetup`. Eligibility and issuance flags remain on `OrganizationSalesDocumentCapability`.

### Branch compliance profiles (separate)

`BranchComplianceProfile` is **branch-scoped** and does not replace the org profile:

| Field | Role |
|---|---|
| `OrganizationBranchId` | Branch key |
| `BirBranchCode` | Branch code for readiness |
| `SetupStatus` | Branch subset of setup statuses |
| `Notes` | Optional operational notes |

### Registration records

`ComplianceRegistrationRecord` stores organization-provided registration evidence references (PTU, CAS registration, EIS types, Other) with Platform Accept/Reject for **ExItS readiness** — not BIR certification.

`GetOrganizationComplianceProfile` combines:

- profile fields above (masked TIN only)
- `OrganizationProfile` identity fields already stored
- capability eligibility / issuance / tax-configuration flags
- `TaxDocumentIssuanceRuntime.ImplementationAvailable` (currently `false`)
- `DocumentMode = TransactionSummary`
- snapshot guidance for future TaxDocument issuance

## TIN privacy

- Full TIN is never returned on application/API DTOs.
- Authorized org/Platform clients may see `MaskedTin` only.
- Public QR / public organization identity contracts must not expose TIN or compliance profile fields.
- Classification: **RESTRICTED COMPLIANCE**. See [post-phase21 privacy refresh](../compliance/post-phase21-privacy-impact-refresh.md).

## What remains deliberately absent

- No TaxDocument generation or runtime enablement
- No fake evidence verification or public document URLs
- No Personal-profile storage of business regulatory identity
- No cashier UI for TIN / registration detail

## Ownership and transfer

The profile belongs to the organization. Ownership transfer changes membership authority only; it must not move, clear, or rewrite the org-scoped profile with the new Owner’s Personal identity.

## Authorization

- **GET** `.../compliance-profile` / readiness / registrations — Platform organization view **or** active organization member.
- **Mutations** (taxpayer, branch profile, registrations, submit readiness) — Organization **Owner** or Platform `ManageOrganizations`.
- **Registration review (Accept/Reject)** — Platform `ManageOrganizations` only.
- **POST** `.../compliance-profile/ensure` — Platform `ManageOrganizations` only.

## Migrations

- `20260814205652_AddOrganizationComplianceProfiles` — anchor
- `20260816110906_AddBirRegistrationReadinessProfiles` — taxpayer/TIN, branch profiles, registration records

No enabling backfill. LocalStore unchanged.

## Historical snapshot invariant

Future TaxDocument issuance must snapshot seller compliance facts at issuance time. Later organization profile changes must not rewrite historical documents. Current and historical sales remain Transaction Summaries while TaxDocument remains unavailable.

## Explicit non-claims

This is not BIR compliance, not TaxDocument issuance, and not Production Ready. See
[sales-document boundary](sales-document-compliance-boundary.md) and
[activation roadmap](../compliance/bir-compliance-activation-roadmap.md).
