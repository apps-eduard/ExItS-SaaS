# ReferenceLoan — Product Definition

> **FICTIONAL** P12-WP06 dry run. Contract: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)  
> Not production. No code. Do not invent lending regulation or underwriting policy.

| Field | Value |
|---|---|
| Product name | ReferenceLoan |
| Platform product code | `reference-loan` |
| Docs root (intended for a real product) | `src/Products/ReferenceLoan/Docs/` |
| Docs root (this dry run) | `docs/Product-Foundation/Reference-Product/` |
| Status | Draft — fictional validation only |
| Last updated | 2026-07-31 |

## Purpose and users

- Purpose: Illustrative sample lending-workflow product for foundation validation only
- Target organizations: Fictional internal demo orgs
- Target users / jobs: Internal lending staff and borrowers (illustrative labels only)

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | **DECISION:** R-091 open — do not claim production-secure auth |
| Organizations | Platform | Product would store `OrganizationId` as Guid reference only |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for `reference-loan` only |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent |
| SaaS billing payments | Platform | Never store product operational money here |
| Operational workflows / roles / money | **This product** | |

## Boundaries (checklist)

- [x] Independent product subscription (not shared with other products)
- [x] Separate database `ExItS_ReferenceLoan` / schema `loan`
- [x] No direct Platform table reads; no cross-product FKs
- [x] Product-local roles and grants defined (below)
- [x] Operational money defined separately from SaaS billing
- [x] Trusted org + product context enforced server-side (when implemented)
- [x] PHI / sensitive data: default **none**
- [x] No customer-specific source forks (config only)

## Surfaces

| Surface | Ownership | Notes |
|---|---|---|
| API | Product | Planned product-owned API |
| Web UI | Product | Planned product-owned web |
| Mobile UI | Product | Planned product-owned mobile |
| Reports | Product | Product operational reports only — not Platform SaaS billing |

## Operational money

Product operational money (not Platform SaaS billing): principal, fees, disbursements, and repayments recorded in the ReferenceLoan database only. Illustrative categories — no rates, schedules, or regulatory treatment invented.

## Product-local roles and grants (summary)

| Role | Purpose | Key grants |
|---|---|---|
| LoanOfficer | Day-to-day operational actions | `loan-accounts-view`, `loan-accounts-manage` (illustrative codes) |
| LoanViewer | Read-only operational visibility | `loan-accounts-view` |

Detail: `authorization-matrix.md`. These are **not** POS roles and must not be copied from POS.

## Privacy classification

| Class | Present? | Notes |
|---|---|---|
| PHI | No (default) | Explicitly none |
| PII | Yes | Borrower/staff identity fields — product-owned handling TBD when implemented |
| Financial operational | Yes | Principal/fees/disbursements/repayments in product DB |
| Other sensitive | No | — |

## MVP inclusions

- Documentation baseline for foundation validation
- Illustrative module placeholders only (no feature implementation)

## Explicit exclusions

- Real credit underwriting, collections, or legal compliance design
- Production authentication (R-091)
- Final commercial-state transport (D-P12-03)
- Copying PinoyBusinessPOS entities, roles, or workflows
- Any source projects under `src/Products/ReferenceLoan/`

## Assumptions

- Fictional product used solely to validate Phase 12 templates and bootstrap prompt

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| R-091 | Production authentication approach | Production readiness |
| D-P12-03 | How commercial state reaches the product without Platform table reads | Product commercial gate implementation |
| RL-D-01 | Real lending domain policy (if ever productized) | Any real MVP scope |

## Document links

| Doc | Path |
|---|---|
| Architecture | `architecture.md` |
| Security | `security.md` |
| Authorization | `authorization-matrix.md` |
| Development plan | `development-plan.md` |
| Roadmap | `roadmap.md` |
| Risks / decisions | `risks-and-decisions.md` |
| Manifest | `FILE-MANIFEST.md` |
