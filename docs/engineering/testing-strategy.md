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
- **P4-WP03 (implemented):** Existing subscription/payment domain + API integration coverage retained; Admin typed-client mutation route tests; architecture guards for lifecycle/payment controls without gateway/card/POS/HealthCare dependencies; no new commercial migration
- **P4-WP04 (implemented):** Platform role-assignment + permission catalog unit tests; audit domain/application tests; PostgreSQL migration apply/rollback/re-apply for `AddPlatformAuthorizationAndAudit`; API authorization + denied-audit integration tests; Admin architecture/localization resource tests; themes/i18n smoke via Admin unit coverage
- **P5-WP05 (implemented):** AuthenticationService sign-in/restore/expiry/logout/org-select; SecureStorage session; Dev/Testing gate; commercial access fail-closed; Maui auth route guards; total Platform root **484** passed (261 unit / 41 architecture / 27 Admin unit / 28 DesignSystem / 17 ApiClient / 26 Maui / 84 integration)
- **P5-WP04 (implemented):** DesignSystem forms/validation/confirm/feedback/responsive-data/money component existence + decimal money + no POS business logic + EN/fil-PH MVP keys; Maui Dev showcase gate + no production-nav link
- **P5-WP03 (implemented):** DesignSystem/Validation/Error + Pos EN↔fil-PH parity and critical-key tests; CultureFormatting unit coverage; Maui hard-coded-string and language-persistence guards
- **P5-WP02 (implemented):** DesignSystem token/density/theme markers + touch-target + no hard-coded page colors; Maui density preference/boot + phone/tablet layout markers
- **P5-WP01 (implemented):** DesignSystem architecture/token/component/localization tests; PosApiClient status classification + offline short-circuit + safe GET retry tests; Maui foundation guards (Android TFM, no Bootstrap/EF/Ant/Tailwind/HC, no sales/sync entry); POS architecture boundary tests

## POS tests

- **P5-WP01 (foundation):** Maui shell/route/localization guards; ApiClient connectivity/result classification; DesignSystem shared primitives
- **P5-WP02 (foundation polish):** density persistence, shell layout markers, reduced-motion/token coverage
- **P5-WP03 (localization):** resource completeness, Tagalog label consistency, CultureFormatting, ApiStatusLocalizer wiring guards
- **P5-WP04 (components):** reusable MVP inventory; responsive table/card CSS; pagination/sort labels; ConfirmDialog loading/Escape; MoneyDisplay decimal; Dev showcase gating
- **P5-WP05 (auth):** Dev/Testing sign-in; secure session clear; production auth blocked; org access deny/allow; no POS operational roles
- Customer profiles (P6-WP01) — organization-isolated; Testcontainers PostgreSQL
- Remarks-based credit (P6-WP02) — append-only entries, derived outstanding, reversal; Testcontainers PostgreSQL
- Repayments and unified ledger (P6-WP03) — overpayment protection, serializable balance checks, ledger read model; Testcontainers PostgreSQL
- Due dates and overdue monitoring (P6-WP04) — append-only due-date history, FIFO aging read model, overdue rules, migration apply/rollback; Testcontainers PostgreSQL
- Statements, repayment receipts, and trial/continuity capability matrix (P6-WP05) — projection statements/receipts (`RCPT-{guid:N}`); `UtangCapabilityPolicy`; Platform POS continuity entry; commercial header gates; OD-07/08/09; Testcontainers where relational; no new receipt migration
- Tenant isolation
- Subscription feature enforcement (P6-WP05 matrix + grants)
- Offline queue and idempotency *(Phase 7)*
- Inventory movements
- Cashier permissions

## UI system tests

- English and Filipino resource completeness
- Light/dark/system theme behavior
- Keyboard/focus and labels
- Compact desktop and touch layouts
- Table empty/loading/error states
- Date field culture formatting
- **P5-WP01:** DesignSystem + PosResources en/fil-PH foundation coverage; theme preference persistence smoke via Maui/Settings paths
- **P5-WP02:** density + theme boot persistence paths; DesignSystem compact touch-target token
- **P5-WP03:** EN↔fil-PH parity; no hard-coded NotFound English; formatting helpers
- **P5-WP04:** form/data/money/confirm markers; showcase unavailable outside Development/Testing

All reports use exact command output; totals are never estimated.
