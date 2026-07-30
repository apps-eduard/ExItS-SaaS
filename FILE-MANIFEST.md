# ExITS SaaS Documentation Package Manifest

Markdown documentation files plus root Platform foundation through Phase 6 Utang MVP closeout (P6-WP06; Phase 6 complete with documented risks).
Internal links checked: spot-check P6-WP06
Broken links: none found in spot-check of new paths

## Root foundation (tracked)

.gitignore (nested product only: `/HealthCare/`)
.cursor/rules/exits-workflow.mdc
README.md
FILE-MANIFEST.md
global.json
Directory.Build.props
Directory.Packages.props
ExItS.slnx
src/Platform/ExItS.Platform.Domain/ (+ Authorization; Audit)
src/Platform/ExItS.Platform.Application/ (+ Catalog; Admin portfolio queries; Authorization; Audit; Contracts; Projections; MigrationValidation; Integration/HealthCare)
src/Platform/ExItS.Platform.Infrastructure/ (PlatformDbContext, catalog + organization/subscription + payment + entitlement + role-assignment + audit persistence, Admin portfolio read store, migrations including `AddPlatformAuthorizationAndAudit`)
src/Platform/ExItS.Platform.Api/ (`/` + `/health` + catalog + organizations + subscriptions + payments + entitlements + identity/access + authorization + audit + admin read APIs; `PlatformAuthz`; phase marker retained from P5)
src/Platform/ExItS.Platform.Admin/ (Blazor Web App — redesigned native CSS shell; typed API client; portfolio + users/memberships/product-access + subscription/payment/trial + audit views; themes; AdminResources en/fil-PH)
src/Shared/ExItS.DesignSystem/ (semantic tokens; forms/data/feedback overlays; DesignSystem/Validation/Error resources en/fil-PH; Blazor primitives; `IDensityPreferenceStore`)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Domain/ (POSCustomer + CreditEntry + CreditDueDateChange + Repayment aggregates; FIFO aging helpers)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Application/ (+ Auth; Customers; Credit; due dates/overdue; Payments/ledger; Statements/receipts; Commercial/UtangCapabilityPolicy)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Infrastructure/ (PosDbContext schema `pos`; migrations `AddPosCustomers`, `AddPosCreditEntries`, `AddPosRepayments`, `AddPosCreditDueDates`; no receipt migration)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/ (`/health` + customers + credit + repayments/ledger + due dates/overdue + statements/receipts; commercial header gates; phase marker `P6-WP06-utang-mvp-closeout`)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.ApiClient/ (+ Platform access client; PosCommercialHeaderHandler; PosCustomerClient with credit/repayment/ledger/due-date/overdue/statement/receipt methods)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.LocalStore/ (Microsoft.Data.Sqlite foundation + generic encrypted offline_operations outbox — P7-WP01/P7-WP02)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ (Android-first MAUI Blazor Hybrid; Customers + credit + repayments + ledger + overdue/due dates + statement/receipt preview/share; onboarding/auth; sync-status shell; offline foundation diagnostics; PosResources en/fil-PH)
tests/ExItS.Platform.UnitTests/
tests/ExItS.ArchitectureTests/
tests/ExItS.Platform.IntegrationTests/
tests/ExItS.Platform.Admin.UnitTests/
tests/ExItS.DesignSystem.Tests/
tests/ExItS.PinoyBusinessPOS.ApiClient.Tests/
tests/ExItS.PinoyBusinessPOS.Maui.Tests/
tests/ExItS.PinoyBusinessPOS.UnitTests/
tests/ExItS.PinoyBusinessPOS.IntegrationTests/

## Documentation (tracked)

docs/cursor/README.md
docs/cursor/completion-report-template.md
docs/cursor/cursor-prompt-template.md
docs/cursor/first-cursor-command.md
docs/decisions/ADR-010-separate-ui-implementations-platform-and-pos.md
docs/decisions/ADR-011-platform-authority-and-product-local-projections.md
docs/decisions/ADR-012-versioned-platform-contracts-and-local-projections.md
docs/decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md
docs/decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md
docs/decisions/README.md
docs/engineering/architecture.md
docs/engineering/approved-architecture-summary.md
docs/engineering/admin-terminology-guide.md
docs/engineering/authorization-matrix.md
docs/engineering/capability-ownership-matrix.md
docs/engineering/data-authority-matrix.md
docs/engineering/data-classification-matrix.md
docs/engineering/data-ownership.md
docs/engineering/development-environment.md
docs/engineering/development-standards.md
docs/engineering/entitlement-state-matrix.md
docs/engineering/extraction-rollback-plan.md
docs/engineering/final-portfolio-boundaries.md
docs/engineering/implementation-gate-matrix.md
docs/engineering/localization.md
docs/engineering/pos-terminology-guide.md
docs/engineering/offline-sync-design.md
docs/engineering/phase-02-evidence-matrix.md
docs/engineering/phase-02-readiness-checklist.md
docs/engineering/platform-extraction-risk-matrix.md
docs/engineering/platform-product-capability-boundary.md
docs/engineering/platform-product-contract-matrix.md
docs/engineering/platform-product-contracts.md
docs/engineering/repository-boundaries.md
docs/engineering/reusable-component-catalog.md
docs/engineering/security.md
docs/engineering/testing-strategy.md
docs/engineering/theme-system.md
docs/engineering/ui-design-system.md
docs/index.md
docs/phases/README.md
docs/phases/phase-00-healthcare-assessment.md
docs/phases/phase-01-platform-boundary.md
docs/phases/phase-02-platform-extraction.md
docs/phases/phase-03-billing-entitlements.md
docs/phases/phase-04-platform-admin.md
docs/phases/phase-05-pos-maui-foundation.md
docs/phases/phase-06-utang-mvp.md
docs/phases/phase-07-offline-sync.md
docs/phases/phase-08-basic-store.md
docs/phases/phase-09-mvp-hardening.md
docs/phases/phase-10-full-pos.md
docs/portfolio-progress.md
docs/product/pinoy-business-pos-requirements.md
docs/product/portfolio-vision.md
docs/product/subscriptions-and-billing.md
docs/release-plan.md
docs/reports/README.md
docs/reports/P0-WP01-completion.md
docs/reports/P0-WP01-healthcare-reuse-assessment.md
docs/reports/P0-WP02-baseline-runtime-map.md
docs/reports/P0-WP03-ui-reuse-review.md
docs/reports/P0-WP04-assessment-closeout.md
docs/reports/P1-WP01-platform-product-capability-boundary.md
docs/reports/P1-WP02-data-ownership-and-contracts.md
docs/reports/P1-WP03-extraction-sequence-and-rollback.md
docs/reports/P1-WP04-architecture-approval-closeout.md
docs/reports/P2-WP01-extraction-baseline-and-safety.md
docs/reports/P2-WP02-identity-organization-boundary.md
docs/reports/P2-WP03-products-plans-entitlements.md
docs/reports/P2-WP04-healthcare-contract-adaptation.md
docs/reports/P2-WP05-regression-and-migration-validation.md
docs/reports/P2-WP06-extraction-closeout.md
docs/reports/P3-WP01-product-and-plan-catalog.md
docs/reports/P3-WP02-trials-and-subscription-lifecycle.md
docs/reports/P3-WP03-manual-payment-activation.md
docs/reports/P3-WP04-entitlement-snapshots-and-grace-rules.md
docs/reports/P3-WP05-billing-closeout.md
docs/reports/P4-WP01-portfolio-navigation-and-product-views.md
docs/reports/P4-WP02-organizations-users-and-product-access.md
docs/reports/P4-WP03-subscriptions-payments-and-trials.md
docs/reports/P4-WP04-audit-authorization-and-closeout.md
docs/reports/P5-WP01-maui-solution-and-api-client.md
docs/reports/P5-WP02-native-ui-tokens-themes-and-compact-layout.md
docs/reports/P5-WP03-english-and-filipino-localization.md
docs/reports/P5-WP04-reusable-mvp-components.md
docs/reports/P5-WP05-authentication-onboarding-and-closeout.md
docs/reports/P6-WP01-customers.md
docs/reports/P6-WP02-remarks-based-credit.md
docs/reports/P6-WP03-payments-and-ledger.md
docs/reports/P6-WP04-due-dates-and-overdue-monitoring.md
docs/reports/P6-WP05-statements-receipts-and-trial-rules.md
docs/reports/P6-WP06-utang-mvp-closeout.md
docs/reports/phase-02-extraction-closeout.md
docs/reports/phase-00-final-assessment-and-recommendation.md
docs/reports/phase-01-architecture-approval.md
docs/reuse/extraction-rules.md
docs/reuse/extraction-sequence.md
docs/reuse/healthcare-reuse-assessment.md
docs/reuse/healthcare-runtime-baseline.md
docs/reuse/healthcare-ui-reuse-assessment.md
docs/reuse/reuse-classification-matrix.md
docs/risks-and-issues.md

## Not tracked

HealthCare/ (ignored nested repository)
**/bin/, **/obj/
