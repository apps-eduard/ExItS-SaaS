# Architecture documentation

Authoritative architecture notes for ExItS SaaS. Prefer these over ad-hoc chat summaries when they conflict with older drafts.

| Document | Purpose |
|---|---|
| [Client experience boundaries](client-experience-boundaries.md) | **Approved MVP** Mobile vs Web ownership (Platform Admin, Personal, Org Owner essentials, full Org Admin, POS ops) |
| [SaaS scopes, users, boundaries, navigation](saas-scopes-users-boundaries-navigation.md) | Account classes, scopes, and navigation model |
| [Product catalog, entitlement, and role model](product-catalog-entitlement-and-role-model.md) | Catalog, entitlements, product-local roles |
| [User creation flow and account scope rules](user-creation-flow-and-account-scope-rules.md) | Identity and membership creation rules |
| [Organization-scoped staff identities (P19)](../reports/P19-organization-scoped-staff-identities.md) | Staff login `local@ORG######` vs Personal/Owner |
| [P16-WP01 entity/API impact matrix](p16-wp01-entity-api-impact-matrix.md) | Phase 16 entity and API impact |
| [ADR-021 linked customer statements](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | Personal read projection of POS Business Utang; Personal-only monetization (Phase 24) |

Related engineering summaries live under [docs/engineering](../engineering/approved-architecture-summary.md).
