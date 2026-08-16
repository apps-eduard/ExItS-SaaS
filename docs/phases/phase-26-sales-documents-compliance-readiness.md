# Phase 26 — Sales Documents and Compliance Readiness

## Status

**OPEN** — P26-WP01–WP05 Code Complete / Owner Validation Pending. This is not a phase closeout. Phase 25 remains **OPEN** with owner validation pending. Owner validation and future regulatory confirmation remain outstanding.

## Goal

Establish a truthful sales-document boundary before any jurisdiction-specific tax-document work. ExItS continues to use one Sale engine. Existing and new sales produce a **Transaction Summary**; `TaxDocument` is a future document kind and is unavailable (`TaxDocumentIssuanceRuntime.ImplementationAvailable = false`).

## Work packages

| Work package | Scope | Status |
|---|---|---|
| P26-WP01 | Sales-document kinds, organization capability foundation, safe wording, read API | Code Complete / Validation Pending |
| P26-WP02 | Owner education, versioned acknowledgment, and soft setup prompt | Code Complete / Validation Pending |
| P26-WP03 | Platform-controlled compliance eligibility, grant/revoke administration, and audit | Code Complete / Validation Pending |
| P26-WP04 | Organization Tax/Compliance Profile & Future Activation Foundation | Code Complete / Validation Pending |
| P26-WP05 | Integration Hardening & Validation Readiness | Code Complete / Owner Validation Pending |

## Invariants

- Tax calculation settings (`TaxRatePercent`, `TaxPricingMode`) do not authorize tax-document issuance.
- `TaxConfigurationEnabled` is Platform-controlled and distinct from eligibility and from stored tax values.
- Tax configuration is hidden by default. Compliance eligibility alone does not enable it. Platform Admin explicitly enables Tax Configuration for an eligible (`Approved`) organization. Owner/Manager may configure tax only after enablement. Enablement is not regulatory certification.
- Compliance eligibility and `TaxDocumentIssuanceEnabled` / `TaxConfigurationEnabled` are Platform-controlled and organization-scoped, not plan entitlements or commercial feature overrides.
- Missing capability state means issuance and tax configuration are disabled; eligibility defaults to `NotRequested` when a row is ensured.
- Ownership transfer preserves the capability and the org-scoped compliance profile because they belong to the organization, not its owner.
- Historical and offline sales remain Transaction Summaries without a LocalStore schema-version change. Disabling tax configuration later does not rewrite completed sale tax snapshots.
- A future TaxDocument must snapshot its document kind and compliance facts; it must not reinterpret historical sales.
- Public QR and organization identity contracts expose no TIN or tax/compliance fields.
- Current education version is `transaction-summary-v1`; only the exact current active Owner may acknowledge.
- Ownership transfer and future version changes retain historical rows and require the current Owner to act.
- Education is a soft prompt only; checkout, sales, sync, and offline operation remain available.
- Acknowledgment never mutates eligibility, `TaxDocumentIssuanceEnabled`, or `TaxConfigurationEnabled`; enable issuance separately requires Approved eligibility plus current Owner acknowledgment; enable tax configuration requires Approved eligibility only.
- Organization enable of issuance does not produce TaxDocuments while runtime implementation remains unavailable.
- Compliance profile anchor stores no invented TIN/BIR fields; confirmed requirements are tracked in the activation roadmap.

## Explicit exclusions

WP01–WP05 do not implement BIR rules, invoice series, TaxDocument generation, or compliance certification. No ExItS UI claims BIR compliance. WP05 does not close Phase 26.

Privacy inventory for education acknowledgment, eligibility, profile, and future evidence principles: [P21-WP11](../reports/P21-WP11-post-phase21-privacy-impact-refresh.md) / [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md) (Phase 21 remains **OPEN**; NPC compliance **not claimed**).

See [engineering boundary](../engineering/sales-document-compliance-boundary.md),
[acknowledgment design](../engineering/organization-sales-document-acknowledgment.md),
[compliance eligibility](../engineering/platform-organization-compliance-eligibility.md),
[organization compliance profile](../engineering/organization-compliance-profile.md),
[BIR activation roadmap](../compliance/bir-compliance-activation-roadmap.md),
[owner validation checklist](../validation/phase-26-owner-validation-checklist.md),
[P26-WP01 report](../reports/P26-WP01-sales-document-compliance-readiness-foundation.md),
[P26-WP02 report](../reports/P26-WP02-organization-compliance-education-and-acknowledgment.md),
[P26-WP03 report](../reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md),
[P26-WP04 report](../reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md), and
[P26-WP05 report](../reports/P26-WP05-sales-document-compliance-integration-hardening.md).
