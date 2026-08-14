# BIR Compliance Activation Roadmap

> Living playbook for controlled future TaxDocument activation. **Not** a work-package completion report.  
> Update this file when accreditation/registration sources confirm requirements. Do not invent regulatory facts.

[Phase 26](../phases/phase-26-sales-documents-compliance-readiness.md) ·
[Sales-document boundary](../engineering/sales-document-compliance-boundary.md) ·
[Eligibility](../engineering/platform-organization-compliance-eligibility.md) ·
[Compliance profile](../engineering/organization-compliance-profile.md) ·
[P26-WP01](../reports/P26-WP01-sales-document-compliance-readiness-foundation.md) ·
[P26-WP02](../reports/P26-WP02-organization-compliance-education-and-acknowledgment.md) ·
[P26-WP03](../reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md) ·
[P26-WP04](../reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md) ·
[P26-WP05](../reports/P26-WP05-sales-document-compliance-integration-hardening.md) ·
[Owner validation checklist](../validation/phase-26-owner-validation-checklist.md)

**Status markers:** `UNCONFIRMED` · `FUTURE` · `Confirmed` · `Implemented` · `Validated` · `Activated`

---

## Disclaimer

The architecture is prepared for controlled activation, but final TaxDocument issuance remains dependent on implementation and validation of requirements confirmed during ExItS accreditation/registration.

Additional engineering, document-format, numbering, reporting, evidence, and validation work may be required after those requirements are confirmed. Activation is not a single configuration toggle.

---

## CURRENT STATE

| Concern | Current truth |
|---|---|
| Sales document kind | **Transaction Summary** for current and historical sales |
| Tax calculation | Allowed via POS `TaxRatePercent` / `TaxPricingMode`; does **not** authorize tax documents |
| TaxDocument | Unavailable (`TaxDocumentIssuanceRuntime.ImplementationAvailable = false`) |
| Issuance capability | Organization-scoped, Platform-controlled, **default off** |
| Compliance eligibility | Platform review lifecycle (WP03); default `NotRequested` |
| Compliance profile | Organization-scoped **anchor** only (WP04); no invented TIN/BIR fields |
| Owner education | Soft acknowledgment (`transaction-summary-v1`); independent of eligibility/issuance |
| Public identity / QR | Identity-only; no TIN, compliance profile, or evidence exposure |
| BIR claim | **None.** ExItS does not claim BIR-authorized invoicing |

Engineering foundations already delivered:

- [P26-WP01](../reports/P26-WP01-sales-document-compliance-readiness-foundation.md) — document kinds, capability foundation, safe wording
- [P26-WP02](../reports/P26-WP02-organization-compliance-education-and-acknowledgment.md) — Owner education acknowledgment
- [P26-WP03](../reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md) — eligibility lifecycle and issuance capability controls
- [P26-WP04](../reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md) — profile anchor + this roadmap

---

## FUTURE STATE — Controlled activation flow

Intended sequence once requirements are confirmed and implemented (all steps required; none alone enables TaxDocument):

1. ExItS completes applicable registration / accreditation / certification work as required by confirmed sources.
2. Organization Owner requests BIR-enabled ExItS support.
3. Support / compliance intake receives the request.
4. Required Organization evidence and information are gathered (**FUTURE** evidence handling).
5. Organization eligibility is reviewed under Platform authority (WP03 lifecycle).
6. Organization compliance profile is completed with confirmed fields only.
7. Platform approval is recorded.
8. Technical readiness is validated (formats, numbering, reporting, runtime, tests).
9. Platform enables organization `TaxDocumentIssuanceEnabled` only when preconditions hold.
10. TaxDocument issuance becomes available **only when** runtime implementation is available **and** all required conditions above are met.

Support does not bypass Platform authority. Payment, plan entitlement, Owner role, POS role, tax settings, Business QR, and uploaded documents alone never authorize TaxDocument issuance.

---

## Incomplete activation checklist

Intentional gaps remain. Items marked **UNCONFIRMED / FUTURE** are not product claims.

| Done | Item | Marker |
|---|---|---|
| [ ] | ExItS regulatory / accreditation requirements confirmed | **UNCONFIRMED / FUTURE** |
| [ ] | Authoritative regulatory source recorded | **UNCONFIRMED / FUTURE** |
| [ ] | Organization eligibility confirmed | **FUTURE** (lifecycle exists; activation not complete) |
| [ ] | Organization documents verified | **UNCONFIRMED / FUTURE** (no fake verification) |
| [ ] | Tax / compliance profile complete | **FUTURE** (anchor only; confirmed fields not yet defined) |
| [ ] | Branch requirements confirmed | **UNCONFIRMED / FUTURE** |
| [ ] | Required registration / reference values configured | **UNCONFIRMED / FUTURE** |
| [ ] | Document format requirements implemented | **UNCONFIRMED / FUTURE** |
| [ ] | Numbering / series requirements implemented | **UNCONFIRMED / FUTURE** |
| [ ] | Reporting / transmission requirements implemented if applicable | **UNCONFIRMED / FUTURE** |
| [ ] | Technical validation completed | **FUTURE** |
| [ ] | Platform approval recorded | **FUTURE** |
| [ ] | Issuance capability enabled | **FUTURE** (flag exists; runtime unavailable) |
| [ ] | Owner / device / browser validation completed | **FUTURE** |

Do not invent required documents or field lists here. When a source is confirmed, add a requirements-table row and update the checklist marker.

---

## Requirements table

Populate only confirmed facts. Example rows below are placeholders marked **Unconfirmed** — not regulatory conclusions.

| Requirement | Source | Authority | Effective date | Required for ExItS? | Required per Organization? | Implementation status | Validation evidence | Notes |
|---|---|---|---|---|---|---|---|---|
| Seller taxpayer identity fields for TaxDocument snapshot | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | Do not invent TIN schema until source recorded |
| Document format / layout for TaxDocument | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | Transaction Summary is not a TaxDocument |
| Numbering / series rules | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | No series generator in product |
| Reporting / transmission (if any) | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | Applicability unknown |
| Branch-level compliance configuration | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | See branch extension point |
| Organization evidence package contents | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | *Unconfirmed* | Unconfirmed | — | Evidence handling deferred |

**Status vocabulary for Implementation status:** `Unconfirmed` → `Confirmed` → `Implemented` → `Validated` → `Activated`.

---

## Support flow

```text
Organization Owner
  → requests BIR-enabled ExItS support
  → Support receives request
  → evidence / information requested
  → review
  → compliance profile completion (confirmed fields only)
  → Platform approval
  → technical readiness validation
  → issuance activation (Platform-controlled)
```

- Owner may request; Owner may **not** self-approve or self-enable TaxDocument.
- Support gathers and routes; Support does **not** directly bypass Platform `ManageOrganizations` authority.
- Eligibility transitions and issuance enable remain Platform-controlled (see WP03).

---

## Evidence and document handling (**deferred**)

- No public URLs for compliance uploads.
- No fake document verification in product.
- Do not build a large document-management system until a confirmed need and secure storage approach exist.
- If secure file infrastructure is added later, integrate it behind authorized Platform/Support paths only.
- Audit events may record that evidence was requested/received; they must not store secret document contents.

---

## Branch extension point

Future compliance may be branch-specific. The current organization-scoped profile and capability do **not** automatically prove that every branch is ready for TaxDocument issuance.

Extension point (not implemented):

- Keep organization-level eligibility/capability as the Platform control plane.
- Add branch-scoped configuration only after branch requirements are **Confirmed** in the table above.
- Do not invent branch registration fields or assume one org record covers every branch.

---

## Historical snapshot invariant (future TaxDocument)

When TaxDocument issuance is eventually implemented:

- Capture an immutable issuance-time snapshot of document kind and then-required seller compliance facts.
- Organization profile changes after issuance must **not** rewrite historical TaxDocuments.
- Historical and offline sales remain Transaction Summaries until an explicit, validated TaxDocument issuance path creates a new document kind.
- Never reclassify historical sales solely because tax settings or profile fields changed.

See [sales-document boundary](../engineering/sales-document-compliance-boundary.md) and [organization compliance profile](../engineering/organization-compliance-profile.md).

---

## Sensitive data boundary

Tax / compliance profile data is **not** Public Business QR data. Public resolvers must not expose TIN, registration evidence, approval references, document-review details, uploaded files, or internal reviewer notes.

---

## Related engineering docs

| Doc | Role |
|---|---|
| [sales-document-compliance-boundary.md](../engineering/sales-document-compliance-boundary.md) | Document kind and authority boundary |
| [organization-sales-document-acknowledgment.md](../engineering/organization-sales-document-acknowledgment.md) | Owner education (WP02) |
| [platform-organization-compliance-eligibility.md](../engineering/platform-organization-compliance-eligibility.md) | Eligibility / issuance (WP03) |
| [organization-compliance-profile.md](../engineering/organization-compliance-profile.md) | Profile anchor (WP04) |
