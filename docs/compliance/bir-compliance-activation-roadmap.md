# BIR Compliance Activation Roadmap

> Living playbook for controlled future TaxDocument activation. **Not** a work-package completion report.  
> Update this file when accreditation/registration sources confirm requirements. Do not invent regulatory facts.

[Phase 26](../phases/phase-26-sales-documents-compliance-readiness.md) ·
[Authoritative source register](bir-authoritative-source-register.md) ·
[Registration readiness design](../engineering/bir-registration-readiness-and-activation.md) ·
[Sales-document boundary](../engineering/sales-document-compliance-boundary.md) ·
[Eligibility](../engineering/platform-organization-compliance-eligibility.md) ·
[Compliance profile](../engineering/organization-compliance-profile.md) ·
[P26-WP01](../reports/P26-WP01-sales-document-compliance-readiness-foundation.md) ·
[P26-WP02](../reports/P26-WP02-organization-compliance-education-and-acknowledgment.md) ·
[P26-WP03](../reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md) ·
[P26-WP04](../reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md) ·
[P26-WP05](../reports/P26-WP05-sales-document-compliance-integration-hardening.md) ·
[P26-WP06](../reports/P26-WP06-bir-registration-profile-and-activation-readiness.md) ·
[Owner validation checklist](../validation/phase-26-owner-validation-checklist.md)

**Status markers:** `UNCONFIRMED` · `FUTURE` · `CONFIRMED` · `IMPLEMENTED` · `VALIDATED` · `Activated`

---

## Disclaimer

The architecture is prepared for controlled activation, but final TaxDocument issuance remains dependent on implementation and validation of requirements confirmed during ExItS accreditation/registration.

Additional engineering, document-format, numbering, reporting, evidence, and validation work may be required after those requirements are confirmed. Activation is not a single configuration toggle.

ExItS does **not** claim BIR Compliant / BIR Certified / BIR Accredited status.

---

## CONFIRMED

| Item | Evidence | Notes |
|---|---|---|
| Official BIR site exists | [SRC-BIR-001](bir-authoritative-source-register.md) | Entry point only |
| eAccReg portal exists for CRM/POS / PTU-related workflows | [SRC-BIR-002](bir-authoritative-source-register.md) | Not an ExItS accreditation claim |
| CAS registration vs PTU distinction is a known RMC 5-2021 concept | [SRC-BIR-003](bir-authoritative-source-register.md) | Secondary summaries; prefer official RMC PDF |
| EIS certification / PTT are valid **reference types** | [SRC-BIR-004](bir-authoritative-source-register.md) | Types only |

---

## IMPLEMENTED (engineering)

| Item | WP | Notes |
|---|---|---|
| Sales document kinds + Transaction Summary boundary | WP01 | TaxDocument unavailable |
| Owner education acknowledgment | WP02 | Soft gate |
| Compliance eligibility lifecycle + issuance capability flags | WP03 | Platform-controlled |
| Organization compliance profile anchor | WP04 | Extended in WP06 |
| Integration hardening + wording guards | WP05 | |
| Registered taxpayer + MaskedTin | WP06 | Full TIN never on DTOs |
| Branch compliance profiles | WP06 | Separate from org profile |
| Compliance registration records + Platform Accept/Reject for readiness | WP06 | Not BIR certification |
| Readiness evaluator + GET/submit readiness APIs | WP06 | Runtime block while `ImplementationAvailable=false` |
| Org Web Tax & Compliance page | WP06 | Owner/Manager view; Owner mutate |
| Platform Admin readiness / registration review UI | WP06 | Issuance enable stays blocked while runtime unavailable |
| MAUI Owner compact readiness summary | WP06 | Deep-link note to Org Web |

---

## VALIDATED

| Item | Status |
|---|---|
| Owner / device / browser validation of WP06 UI | **Not validated** (pending) |
| Regulatory / DPO confirmation of required fields | **Not validated** |
| Migration apply/rollback on authorized non-production DB | Pending for WP06 migration |

---

## UNCONFIRMED

| Item | Notes |
|---|---|
| Exact ExItS accreditation/registration package contents | Do not invent |
| Invoice / OR / SI layout for TaxDocument | |
| Numbering / series rules | |
| Reporting / transmission obligations applicability | |
| Fiscal memory / grand total memory | |
| MIN association rules | Warned as FUTURE in evaluator |

---

## FUTURE

| Item | Notes |
|---|---|
| TaxDocument issuance runtime (`ImplementationAvailable=true`) | **Must stay false** until authorized WP |
| Evidence object storage with private URLs | Catalog placeholders only |
| Automatic enable of issuance after readiness | Never auto-enable |
| Phase 26 closeout | Remains OPEN |

---

## CURRENT STATE

| Concern | Current truth |
|---|---|
| Sales document kind | **Transaction Summary** for current and historical sales |
| Tax calculation | Allowed via POS settings when Tax Configuration enabled; does **not** authorize tax documents |
| TaxDocument | Unavailable (`TaxDocumentIssuanceRuntime.ImplementationAvailable = false`) |
| Issuance capability | Organization-scoped, Platform-controlled, **default off** |
| Compliance eligibility | Platform review lifecycle (WP03); default `NotRequested` |
| Compliance profile | Org taxpayer fields + branch profiles + registration records (WP06) |
| Owner education | Soft acknowledgment (`transaction-summary-v1`) |
| Public identity / QR | Identity-only; no TIN / compliance exposure |
| BIR claim | **None** |

---

## Controlled activation flow (future)

1. ExItS completes applicable registration / accreditation work as required by **Confirmed** sources.
2. Organization Owner completes taxpayer / branch / registration readiness.
3. Platform AcceptForReadiness on required registrations.
4. Eligibility Approved + Owner education acknowledged.
5. Technical TaxDocument runtime implemented and validated.
6. Platform enables `TaxDocumentIssuanceEnabled` only when preconditions hold.
7. TaxDocument issuance available only when runtime **and** all conditions are met.

---

## Related engineering docs

| Doc | Role |
|---|---|
| [bir-authoritative-source-register.md](bir-authoritative-source-register.md) | Source IDs and non-confirmations |
| [bir-registration-readiness-and-activation.md](../engineering/bir-registration-readiness-and-activation.md) | WP06 design |
| [sales-document-compliance-boundary.md](../engineering/sales-document-compliance-boundary.md) | Document kind boundary |
| [organization-compliance-profile.md](../engineering/organization-compliance-profile.md) | Profile + TIN privacy |
| [platform-organization-compliance-eligibility.md](../engineering/platform-organization-compliance-eligibility.md) | Eligibility / issuance |
