# {{PRODUCT_NAME}} — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)
> Replace all `{{…}}`. Unresolved items → `Docs/risks-and-decisions.md`. Do not invent policy.

| Field | Value |
|---|---|
| Product name | {{PRODUCT_NAME}} |
| Platform product code | {{PRODUCT_CODE}} |
| Docs root | `src/Products/{{PRODUCT_NAME}}/Docs/` |
| Status | Draft / Approved |
| Last updated | {{DATE}} |

## Purpose and users

- Purpose: {{PURPOSE}}
- Target organizations: {{TARGET_ORGS}}
- Target users / jobs: {{TARGET_USERS}}

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | **DECISION:** R-091 open — do not claim production-secure auth |
| Organizations | Platform | Product stores `{{ORG_ID_FIELD}}` as Guid reference only |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent |
| SaaS billing payments | Platform | Never store product operational money here |
| Operational workflows / roles / money | **This product** | |

## Boundaries (checklist)

- [ ] Independent product subscription (not shared with other products)
- [ ] Separate database `{{DATABASE_NAME}}` / schema `{{SCHEMA_NAME}}`
- [ ] No direct Platform table reads; no cross-product FKs
- [ ] Product-local roles and grants defined (below)
- [ ] Operational money defined separately from SaaS billing
- [ ] Trusted org + product context enforced server-side
- [ ] PHI / sensitive data: default **none** unless explicitly authorized below
- [ ] No customer-specific source forks (config only)

## Surfaces

| Surface | Ownership | Notes |
|---|---|---|
| API | Product | {{API_NOTES}} |
| Web UI | Product / none | {{WEB_NOTES}} |
| Mobile UI | Product / none | {{MOBILE_NOTES}} |
| Reports | Product | {{REPORT_NOTES}} |

## Operational money

Define what counts as **product operational money** (not Platform SaaS billing):

{{OPERATIONAL_MONEY_DEFINITION}}

## Product-local roles and grants (summary)

| Role | Purpose | Key grants |
|---|---|---|
| {{ROLE_1}} | {{ROLE_1_PURPOSE}} | {{ROLE_1_GRANTS}} |
| {{ROLE_2}} | {{ROLE_2_PURPOSE}} | {{ROLE_2_GRANTS}} |

Detail: `authorization-matrix.md`.

## Privacy classification

| Class | Present? | Notes |
|---|---|---|
| PHI | No (default) / Yes (authorized) | {{PHI_NOTES}} |
| PII | {{PII}} | {{PII_NOTES}} |
| Financial operational | {{FIN}} | {{FIN_NOTES}} |
| Other sensitive | {{OTHER}} | {{OTHER_NOTES}} |

## MVP inclusions

- {{INCLUSION_1}}
- {{INCLUSION_2}}

## Explicit exclusions

- {{EXCLUSION_1}}
- {{EXCLUSION_2}}

## Assumptions

- {{ASSUMPTION_1}}

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| {{DEC_ID}} | {{DEC_QUESTION}} | {{DEC_BLOCKS}} |

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
| Deployment | `deployment-notes.md` (if used) |
