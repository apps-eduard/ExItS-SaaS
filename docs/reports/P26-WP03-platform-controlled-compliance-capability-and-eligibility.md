# P26-WP03 — Platform-Controlled Compliance Capability and Eligibility

## Status

**Code Complete / Validation Pending.** Phase 26 remains **OPEN**. Phase 25 remains **OPEN**. This work package makes no BIR-compliance claim and does not produce `TaxDocument` records.

**Starting SHA:** `1cfe2b07a51d1adb245f34415ab518c8afe32f89`  
**Feature SHA:** `73b5822c`

## Delivered capability

- Extended `OrganizationSalesDocumentCapability` with `ComplianceEligibilityStatus` (default `NotRequested`) alongside the existing `TaxDocumentIssuanceEnabled` bool.
- Eligibility statuses: `NotRequested`, `Requested`, `DocumentsRequired`, `UnderReview`, `Approved`, `Rejected`, `Suspended`, `Revoked`.
- Owner may request review; only Platform `ManageOrganizations` may transition eligibility or enable/disable issuance.
- Enable issuance requires `Approved` plus current Owner education acknowledgment for `transaction-summary-v1`.
- Suspend, Revoke, and other non-approved transitions force `TaxDocumentIssuanceEnabled=false`.
- `TaxDocumentIssuanceRuntime.ImplementationAvailable = false` — organization enable does **not** produce TaxDocuments.
- Platform audit actions under `platform.organization.compliance.*` plus `tax_document_capability_enabled` / `tax_document_capability_disabled`.
- Platform Admin Organizations page Compliance tab (transition + issuance controls).
- Organization Web Sales Documents page Owner request CTA.
- Tests: `OrganizationComplianceEligibilityTests` (6) plus updated capability/education coverage.

## Persistence and migration

Migration: `20260814204603_AddOrganizationComplianceEligibilityStatus`
(`AddOrganizationComplianceEligibilityStatus`).

Adds `compliance_eligibility_status` (`character varying(64)`, `NOT NULL`, default `'NotRequested'`) to
`platform.organization_sales_document_capabilities`. No backfill enables issuance. LocalStore / POS schema unchanged.

## API and security

| Endpoint | Authority |
|---|---|
| `GET .../compliance-status` | Platform org view **or** active organization member |
| `POST .../compliance/request` | Exact current Organization Owner |
| `POST .../compliance/transition` | Platform `ManageOrganizations` |
| `POST .../compliance/tax-document-capability` | Platform `ManageOrganizations` |

Education acknowledgment remains independent: it never sets eligibility or issuance. Issuance enable reads acknowledgment as a precondition only.

## Explicit exclusions

- No BIR rules, registration, invoice series, numbering, or compliance certification.
- No TaxDocument generation or runtime issuance (`ImplementationAvailable` remains false).
- No plan/entitlement/feature-override path to enable issuance.
- No POS/LocalStore schema change.
- No Production Ready or Device Verified claim.

## Risks and open decisions

- Feature SHA recorded as `73b5822c`. Migration apply/rollback/re-apply on an authorized non-production PostgreSQL database remains pending.
- Phase 25 cash/shift leftovers were intentionally not modified.

## Documentation changed

Phase page, reports index, portfolio progress, implementation summary, sales-document boundary,
acknowledgment design, new eligibility engineering note, and file manifest.

## Next work package

**P26-WP04 — Organization Tax/Compliance Profile & Future Activation Foundation** (delivered separately).  
Living activation playbook: [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md).  
Exact next after WP04: **P26-WP05**.
