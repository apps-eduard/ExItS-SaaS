# ExITS SaaS Documentation Package Manifest

Markdown documentation files plus root Platform foundation through Phase 4 Platform Admin (P4-WP03 subscriptions/payments/trials).
Internal links checked: spot-check P4-WP03
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
src/Platform/ExItS.Platform.Domain/
src/Platform/ExItS.Platform.Application/ (+ Catalog; Admin portfolio queries; Contracts; Projections; MigrationValidation; Integration/HealthCare)
src/Platform/ExItS.Platform.Infrastructure/ (PlatformDbContext, catalog + organization/subscription + payment + entitlement persistence, Admin portfolio read store, migrations)
src/Platform/ExItS.Platform.Api/ (`/` + `/health` + catalog + organizations + subscriptions + payments + entitlements + admin read APIs)
src/Platform/ExItS.Platform.Admin/ (Blazor Web App — native CSS Platform Admin; typed API client; portfolio + users/memberships/product-access + subscription/payment/trial mutation views)
tests/ExItS.Platform.UnitTests/
tests/ExItS.ArchitectureTests/
tests/ExItS.Platform.IntegrationTests/
tests/ExItS.Platform.Admin.UnitTests/

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
