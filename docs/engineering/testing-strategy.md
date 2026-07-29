# Testing Strategy

[Home](../index.md) | [Dashboard](../portfolio-progress.md)

## Reuse/extraction tests

- Baseline HealthCare build and tests before extraction
- Architecture dependency tests
- Contract tests between Platform and HealthCare
- HealthCare regression tests after each extraction step
- Migration and rollback verification

## Platform tests

- Authentication/session *(not yet — deferred past P2-WP02)*
- Organization isolation
- Product subscription lifecycle
- Entitlement snapshot/version/idempotency
- Platform role authorization
- Billing audit
- **P2-WP02 (implemented):** domain ID/value-object tests; Platform User / Organization / Membership transition tests; ProductCode tests; application use-case tests with in-memory doubles; architecture layer, freeze, forbidden-type, and no-generic-repository tests
- **P2-WP03 (implemented):** Product/Plan/PlanVersion/Trial/Subscription/Override/Snapshot tests; entitlement composer (override precedence, trial expiry view/repay vs create); commercial use-case tests; published plan-version immutability architecture check
- **P2-WP04 (implemented):** contract envelope/version tests; projection idempotency/ordering/conflict/gap; security shape reflection tests; HealthCare adapter interface architecture tests; no messaging/EF dependencies
- **P2-WP05 (implemented):** migration preflight/simulation/rollback-readiness unit tests; identity/org/membership duplicate & conflict detection; architecture checks for no SQL/migration routes; Integration/HealthCare remains tracked
- **P3-WP01 (implemented):** EF Core/Npgsql catalog persistence; Testcontainers PostgreSQL integration tests; catalog API tests; architecture rules for EF placement and no auto-migrate
- **P3-WP02 (implemented):** Organization + subscription persistence; active-like uniqueness; lifecycle API/integration tests; no payment tables
- **P4-WP01 (implemented):** Platform Admin unit/architecture guards (no Infrastructure/EF/Ant/Tailwind; no deferred commercial mutation controls); typed API client tests; Admin portfolio read API integration tests; Admin UI runtime smoke
- **P4-WP02 (implemented):** Platform user/membership/product-access unit tests; effective-access evaluation; PostgreSQL migration apply/rollback/re-apply; identity/access API integration tests; Admin guards for no product-local role selectors / no login screens

## POS tests

- Customer credit/payment ledger
- Tenant isolation
- Subscription feature enforcement
- Offline queue and idempotency
- Inventory movements
- Cashier permissions

## UI system tests

- English and Filipino resource completeness
- Light/dark/system theme behavior
- Keyboard/focus and labels
- Compact desktop and touch layouts
- Table empty/loading/error states
- Date field culture formatting

All reports use exact command output; totals are never estimated.
