# P26-WP04 — Organization Tax/Compliance Profile and Activation Foundation

## Status

**Code Complete / Validation Pending.** Phase 26 remains **OPEN**. Phase 25 remains **OPEN**. This work package makes no BIR-compliance claim and does not produce `TaxDocument` records.

**Starting SHA:** `22fd8209305c87c16d4adb636652d7b6a1f31f77`  
**Feature SHA:** *pending commit*

Canonical living playbook (not this report):
[docs/compliance/bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md).

## Delivered capability

- Created organization-scoped `OrganizationComplianceProfile` **anchor** (organization id, created/updated timestamps, optional actor reference).
- No invented TIN, BIR registration, invoice series, or other speculative regulatory fields.
- `GetOrganizationComplianceProfile` reads existing `OrganizationProfile` identity fields plus sales-document capability status; it does **not** enable TaxDocument.
- `EnsureOrganizationComplianceProfile` creates the anchor when missing (Platform `ManageOrganizations`).
- Read API available to Platform org viewers or active organization members; ensure remains Platform manage-only.
- Ownership transfer preserves the org-scoped profile (keyed by `OrganizationId`, not Owner user id).
- Public QR / public organization identity contracts remain free of compliance profile fields.
- Living BIR activation roadmap with CURRENT/FUTURE state, incomplete checklist (**UNCONFIRMED / FUTURE**), requirements table structure, support flow, evidence deferral, branch extension point, and historical snapshot invariant.
- Unit coverage: org isolation, no TIN/BIR DTO invention, ensure idempotency without enabling TaxDocument, ownership-transfer preservation of profile keying, public identity property guards.

## Persistence and migration

Migration: `20260814205652_AddOrganizationComplianceProfiles`
(`AddOrganizationComplianceProfiles`).

Creates `platform.organization_compliance_profiles` with:

- `organization_id` (PK, FK → `platform.organizations`, cascade delete)
- `created_at_utc`
- `updated_at_utc`
- `updated_by_actor_reference` (nullable)

No backfill enables issuance. No LocalStore / POS schema change.

## API and security

| Endpoint | Authority |
|---|---|
| `GET /api/v1/platform/organizations/{id}/compliance-profile` | Platform org view **or** active organization member |
| `POST /api/v1/platform/organizations/{id}/compliance-profile/ensure` | Platform `ManageOrganizations` |

Combined DTO surfaces legal/registered address identity from `OrganizationProfile`, eligibility/issuance flags from capability, `DocumentMode=TransactionSummary`, and snapshot guidance. It does not invent taxpayer identity fields.

## Explicit exclusions

- No BIR rules, accreditation completion, invoice series, numbering, or TaxDocument generation.
- No enablement of `TaxDocumentIssuanceRuntime.ImplementationAvailable` (remains `false`).
- No fake document verification or public compliance-document URLs.
- No invented branch compliance rules (extension point documented only).
- No POS/LocalStore schema change.
- No Production Ready, Device Verified, or Browser Verified claim.
- Phase 25 cash/shift leftovers intentionally untouched.

## Risks and open decisions

- Feature SHA pending commit; hash record follows the focused feature/docs commit.
- Migration apply/rollback/re-apply on an authorized non-production PostgreSQL database remains pending.
- Regulatory requirements remain **UNCONFIRMED** until ExItS accreditation/registration sources are recorded in the roadmap table.
- Owner/device/browser validation remains outstanding.

## Documentation changed

- Created roadmap, this report, and [organization-compliance-profile.md](../engineering/organization-compliance-profile.md).
- Updated phase page, reports index, portfolio progress, implementation summary, sales-document boundary, eligibility note, WP01–WP03 cross-references as needed, FILE-MANIFEST, and client-experience / identity boundary notes where they mention compliance.

## Next work package

**P26-WP05 — Validation, operational evidence, and phase closeout decision.**  
P26-WP05 is **not** automatic phase closeout. Phase 26 remains **OPEN** until the Owner explicitly decides.
