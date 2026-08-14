# Phase 26 — Owner Validation Checklist

> **Status:** Owner Validation Pending. Phase 26 remains **OPEN**.  
> Do not mark items complete until the Owner personally validates.  
> This checklist is **not** phase closeout. Not Device / Browser / Production Ready.

Related: [phase page](../phases/phase-26-sales-documents-compliance-readiness.md) ·
[P26-WP05 report](../reports/P26-WP05-sales-document-compliance-integration-hardening.md) ·
[BIR activation roadmap](../compliance/bir-compliance-activation-roadmap.md)

## Surfaces

- [ ] Organization Web — Sales Documents education / status / request review
- [ ] Platform Admin — Compliance eligibility transitions and issuance controls
- [ ] MAUI Owner — soft setup / education prompt (non-blocking)
- [ ] MAUI cashier — read-only message; cannot acknowledge; sales unblocked

## Cross-cutting scenarios

- [ ] Multi-org switch — compliance/education state does not leak across organizations
- [ ] Ownership transfer — org capability/profile preserved; incoming Owner must re-acknowledge education
- [ ] Tax settings (`TaxRatePercent` / `TaxPricingMode`) do not authorize TaxDocument issuance
- [ ] Education acknowledgment never enables eligibility or `TaxDocumentIssuanceEnabled`
- [ ] Platform compliance state — eligibility / issuance / profile read correctly; TaxDocument still unavailable
- [ ] Transaction Summary print / share — denial wording present; no BIR-compliance claim
- [ ] Offline — sales remain Transaction Summaries; no per-sale compliance gate
- [ ] Privacy / Public QR — no TIN, compliance profile, or tax-document capability exposure
- [ ] Migration startup — additive WP01–WP04 migrations apply safely; no auto-enable; LocalStore unchanged

## Explicit non-claims

- TaxDocument: **NOT IMPLEMENTED / NOT AVAILABLE**
- No BIR compliance or accreditation claim
- Phase 25 remains **OPEN** (independent)
- Phase 26 remains **OPEN** after checklist use until Owner decides otherwise
