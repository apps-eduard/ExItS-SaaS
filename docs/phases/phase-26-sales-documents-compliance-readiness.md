# Phase 26 — Sales Documents and Compliance Readiness

## Status

**OPEN** — P26-WP01 implemented; validation pending. This is not a phase closeout. Phase 25 remains **OPEN** with owner validation pending.

## Goal

Establish a truthful sales-document boundary before any jurisdiction-specific tax-document work. ExItS continues to use one Sale engine. Existing and new sales produce a **Transaction Summary**; `TaxDocument` is a future document kind and is unavailable.

## Work packages

| Work package | Scope | Status |
|---|---|---|
| P26-WP01 | Sales-document kinds, organization capability foundation, safe wording, read API | Code Complete / Validation Pending |
| P26-WP02 | Owner education and acknowledgment UX | Not started — exact next work package |
| P26-WP03 | Platform-controlled grant/revoke administration and audit | Not started |
| P26-WP04 | Tax-document snapshot, numbering, and jurisdiction rules | Not started |
| P26-WP05 | Validation, operational evidence, and phase closeout decision | Not started |

## Invariants

- Tax calculation settings (`TaxRatePercent`, `TaxPricingMode`) do not authorize tax-document issuance.
- The capability is Platform-controlled and organization-scoped, not a plan entitlement or commercial feature override.
- Missing capability state means `TaxDocumentIssuanceEnabled=false`.
- Ownership transfer preserves the capability because it belongs to the organization, not its owner.
- Historical and offline sales remain Transaction Summaries without a LocalStore schema-version change.
- A future TaxDocument must snapshot its document kind and compliance facts; it must not reinterpret historical sales.
- Public QR and organization identity contracts expose no TIN or tax/compliance fields.

## Explicit exclusions

WP01 does not implement BIR rules, invoice series, tax-document issuance, capability grant/revoke UI, grant workflow, or education acknowledgment. No ExItS UI claims BIR compliance.

See [engineering boundary](../engineering/sales-document-compliance-boundary.md) and [P26-WP01 report](../reports/P26-WP01-sales-document-compliance-readiness-foundation.md).
