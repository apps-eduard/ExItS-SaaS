# ExItS SaaS Documentation Package Manifest

Markdown documentation files and tracked foundation inventory for the ExItS portfolio.
Broken links: none found in spot-check of presentation paths

## Root foundation (tracked)

.gitignore (also ignores `*.dump` / encrypted backup artifacts)
.cursor/rules/exits-workflow.mdc
.cursor/rules/exits-product-context.mdc
.dockerignore
README.md
CONTRIBUTING.md
SECURITY.md
Start-LocalValidation.md
Reset-LocalValidation.md
Reset-Products-And-Business-Templates.md
Maui-Emulator-Install.md
Maui-PhysicalDevice-Install.md
FILE-MANIFEST.md
global.json
Directory.Build.props
Directory.Packages.props
ExItS.slnx
src/Platform/ExItS.Platform.Domain/ (+ Authorization; Audit; FeatureCode includes `store-suppliers-view` / `store-suppliers-manage`)
src/Platform/ExItS.Platform.Application/ (+ Catalog; Admin portfolio queries; Authorization; Audit; Contracts; Projections; MigrationValidation)
src/Platform/ExItS.Platform.Infrastructure/ (PlatformDbContext, catalog + organization/subscription + payment + entitlement + role-assignment + audit persistence, Admin portfolio read store, Magick.NET shared GlobalProduct WebP pipeline + local/dev filesystem object store, migrations through **`20260817220000_AddGlobalProductImages`**; `Health/PlatformDatabaseReadyHealthCheck`)
src/Platform/ExItS.Platform.Api/ (`/` + `/health` + `/health/ready` + catalog + organizations + subscriptions + payments + entitlements + identity/access + authorization + audit + admin read APIs + org/public-identity + `/api/v1/qr/resolve` + POS device registration-tokens + Platform/merchant GlobalProduct image endpoints; `PlatformAuthz`; Production security pipeline; phase marker `P10-WP08-phase-10-closeout`)
src/Platform/ExItS.Platform.Admin/ (Blazor Web App — Ant Design Blazor shell per ADR-015/ADR-022; canonical browser sign-in; Platform operator console; typed API client; GlobalProduct image preview/upload/replace/remove; themes Light/Dark/System; AdminResources en/fil-PH; no Fluent/Tailwind)
src/Platform/ExItS.Personal.Web/ (Personal Web — Ant Design Blazor presentation over existing Personal APIs; Local Validation :8094; no checkout)
src/Shared/ExItS.Web.UI/ (shared AntDesign browser conventions: theme, culture, page header, pager, host options, handoff helpers; AntDesign 1.6.2)
src/Shared/ExItS.DesignSystem/ (semantic tokens; forms/data/feedback overlays; DesignSystem/Validation/Error resources en/fil-PH; Blazor primitives; `IDensityPreferenceStore`)
src/Shared/ExItS.BackupRestore/ (PostgreSQL logical backup/restore helpers: manifests, SHA-256, retention, AES-GCM protect, restore validation)
src/Shared/ExItS.Deployment/ (pilot/deployment config validation, backup gates, readiness, rollback advisor, Commercial MVP closeout board — P9-WP05/P9-WP06; phase marker `P10-WP08-phase-10-closeout`)
tools/ExItS.BackupRestore.Cli/ (non-interactive backup/verify/restore/encrypt/retention CLI)
tools/ExItS.Deployment.Cli/ (validate-config / backup-gate / readiness / smoke-catalog CLI)
ops/backup/ (PowerShell operators scripts + disabled schedule notes + config.example.env)
ops/deploy/ (pilot deploy orchestration, smoke, pre-deploy backup, env templates)
deploy/docker/ (packaging + local-validation + production compose, Dockerfiles, nginx; local-validation default = DBs only)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Domain/ (POSCustomer + CreditEntry + CreditDueDateChange + Repayment + CatalogProduct + ProductCategory + Supplier + **PurchaseOrder/GoodsReceipt** aggregates; connected PO lifecycle + receiving discrepancies; FIFO aging helpers)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Application/ (+ Auth; Customers; Credit; due dates/overdue; Payments/ledger; Statements/receipts; Catalog; Suppliers; **Purchasing**; ConnectedSuppliers client contracts + `ConnectedPoDisplayStatus` + linked-product delta sync; Commercial/UtangCapabilityPolicy; Reporting batch lookups)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Infrastructure/ (PosDbContext schema `pos`; migrations through **`20260818223000_AddSaleBranchId`**; Magick.NET WebP merchant-override pipeline + local/dev filesystem object store; `Health/PosDatabaseReadyHealthCheck`)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/ (`/health` + `/health/ready` + customers + credit + repayments/ledger + due dates/overdue + statements/receipts + catalog + sales + inventory + expenses + suppliers + purchase-orders/goods-receipts + cashier-shifts + sale-returns + permissions + registers + dashboard/reports; commercial header gates; Production security pipeline; phase marker `P10-WP08-phase-10-closeout`)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.ApiClient/ (+ Platform access client incl. org public-identity, `/api/v1/qr/resolve`, POS device registration-token create/redeem; PosCommercialHeaderHandler; PosCustomerClient; PosSaleClient/PosExpenseClient/**PosPurchaseOrderClient** idempotency headers; PosCatalogClient online-only; PosSupplierClient online-only)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.LocalStore/ (Microsoft.Data.Sqlite schema v9 + generic encrypted offline_operations outbox + BlockedByAccess reclaim + encrypted customer/credit/repayment projections + selective connected-supplier linked products and local PO drafts + product usage/sell-unit offline cache; never a full supplier catalog; **not** part of server backup sets)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ (Android-first MAUI Blazor Hybrid; Customers + credit + repayments + ledger + overdue/due dates + statement/receipt preview/share + catalog/barcode + sales (**multi-unit Sell as checkout**) + inventory + expenses + suppliers + **purchasing hub (Receive stock / POs / discrepancy-aware goods receipts)** + connected supplier request/catalog/linked products/incoming order list/detail + lifecycle actions + **connected buyers + post-accept share prompt + per-buyer shared products/pricing** + **unified org notifications (Read-on-open)** + dashboard/reports; onboarding/auth; sync-status shell; private product-image cache + explicit adopted-template thumbs + queueable offline catalog create (metadata JSON; pending photos as files, never SQLite bytes); offline foundation diagnostics; PosResources en/fil-PH)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Web/ (Organization Web Admin — AntDesign Blazor Server management/reporting per ADR-022; **not a POS checkout client**; unified org notifications + Connected buyers; Local Validation :8093)
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/ (future React + TypeScript Vite host for browser / PWA / later Capacitor; Gate C scaffold + Gate D Phase A static PWA shell on `feat/pos-react-client`; MAUI remains active)
tests/ExItS.Platform.UnitTests/
tests/ExItS.ArchitectureTests/
tests/ExItS.Platform.IntegrationTests/
tests/ExItS.Platform.Admin.UnitTests/
tests/ExItS.DesignSystem.Tests/
tests/ExItS.PinoyBusinessPOS.ApiClient.Tests/
tests/ExItS.PinoyBusinessPOS.Maui.Tests/
tests/ExItS.PinoyBusinessPOS.Web.Tests/
tests/ExItS.Personal.Web.Tests/
tests/ExItS.PinoyBusinessPOS.UnitTests/
tests/ExItS.PinoyBusinessPOS.IntegrationTests/
tests/ExItS.BackupRestore.Tests/
tests/ExItS.Deployment.Tests/

## Documentation (tracked)

docs/cursor/README.md
docs/cursor/completion-report-template.md
docs/cursor/cursor-prompt-template.md
docs/decisions/ADR-010-separate-ui-implementations-platform-and-pos.md
docs/decisions/ADR-011-platform-authority-and-product-local-projections.md
docs/decisions/ADR-012-versioned-platform-contracts-and-local-projections.md
docs/decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md
docs/decisions/ADR-021-linked-customer-statements-and-personal-monetization.md
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
docs/engineering/final-portfolio-boundaries.md
docs/engineering/localization.md
docs/engineering/pos-terminology-guide.md
docs/engineering/pos-branch-inventory-transfers.md
docs/engineering/pos-cashier-cash-count.md
docs/engineering/pos-expiration-aware-inventory.md
docs/engineering/offline-sync-design.md
docs/engineering/product-units-and-inventory-behavior.md
docs/engineering/product-images-and-storefront-availability.md
docs/engineering/phase-02-evidence-matrix.md
docs/engineering/phase-02-readiness-checklist.md
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
docs/Mobile-React/README.md
docs/Mobile-React/documentation-status.md
docs/Mobile-React/decisions.md
docs/Mobile-React/current-state-and-replacement-boundaries.md
docs/Mobile-React/product-surfaces-and-ux.md
docs/Mobile-React/frontend-architecture-and-reuse.md
docs/Mobile-React/pwa-and-capacitor-delivery.md
docs/Mobile-React/offline-sync-auth-and-security.md
docs/Mobile-React/device-and-payment-integration.md
docs/Mobile-React/migration-testing-and-implementation-gates.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-08-final-closeout.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-AMEND-01-auth-connectivity-diagnostics.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-AMEND-02-language-theme-defaults.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-AMEND-03-smart-workspace-product-context.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-APPROVAL-record.md
docs/Mobile-React/Reports/MOBILE-REACT-DOC-MERGE-01-approved-planning-baseline.md
docs/phases/README.md
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
docs/phases/phase-24-linked-customer-statements-and-personal-monetization.md
docs/phases/phase-25-organization-web-admin.md
docs/phases/phase-26-sales-documents-compliance-readiness.md
docs/phases/phase-27-connected-supplier-commerce-and-purchasing.md
docs/phases/phase-28-customer-ordering-pickup-and-delivery.md
docs/phases/phase-29-data-integrity-query-performance-and-database-hardening.md
docs/phases/phase-21-privacy-compliance-and-regulatory-readiness.md
docs/reports/P21-foundation-privacy-compliance-workspace.md
docs/reports/P21-privacy-readiness-visibility-product-status-ui.md
docs/reports/P21-WP01-requirements-and-privacy-inventory.md
docs/reports/P21-WP11-post-phase21-privacy-impact-refresh.md
docs/compliance/post-phase21-privacy-impact-refresh.md
docs/reports/P25-WP01-organization-web-admin-management-center.md
docs/reports/P25-WP02-antdesign-web-standardization-and-host-separation.md
docs/reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md
docs/reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md
docs/reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md
docs/reports/P25-WP06-personal-organization-identity-isolation.md
docs/reports/P25-WP07-sales-buyer-party-isolation.md
docs/reports/P25-WP08-organization-profile-independence.md
docs/reports/P25-WP09-organization-ownership-transfer.md
docs/reports/P26-WP01-sales-document-compliance-readiness-foundation.md
docs/reports/P26-WP02-organization-compliance-education-and-acknowledgment.md
docs/reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md
docs/reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md
docs/reports/P26-WP05-sales-document-compliance-integration-hardening.md
docs/reports/P26-WP06-bir-registration-profile-and-activation-readiness.md
docs/reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md
docs/reports/P27-WP02-connected-po-delivery-and-reliability.md
docs/reports/P27-WP03-supplier-response-synchronization.md
docs/reports/P27-WP04-connected-po-cancellation-and-withdrawal.md
docs/reports/P27-WP05-fulfillment-goods-receipt-and-discrepancies.md
docs/reports/P28-WP01-branch-fulfillment-location-foundation.md
docs/reports/P28-WP02-customer-ordering-stage-b-slice.md
docs/reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md
docs/reports/P28-WP12-multi-branch-customer-commerce-hardening.md
docs/reports/P28-WP13-branch-operational-context-and-owner-switching.md
docs/reports/P28-branch-edit-ux-densification.md
docs/reports/P19-maui-list-load-performance.md
docs/reports/P29-WP01-data-authority-and-schema-consistency.md
docs/reports/P29-WP02-tenant-isolation-and-relational-integrity.md
docs/reports/P29-WP03-financial-and-transaction-integrity.md
docs/reports/P29-WP04-inventory-reservation-concurrency.md
docs/reports/P29-WP05-listbranches-n-plus-one-elimination.md
docs/reports/P29-WP06-reporting-aggregation-performance.md
docs/reports/P29-WP07-customer-order-buyer-indexes.md
docs/reports/P29-WP08-concurrency-load-and-reliability.md
docs/reports/P29-WP09-migration-backup-restore-and-db-operations.md
docs/reports/P29-WP10-phase-29-closeout.md
docs/reports/P29-WP11-database-verification-and-constraint-closeout.md
docs/reports/P29-WP12-electronic-payment-transaction-reliability-hardening.md
docs/reports/P29-WP13-concurrency-and-postgresql-execution-plan-validation.md
docs/reports/P29-WP13-explain-plan-snippets.md
docs/reports/P29-WP14-postgresql-backup-restore-and-recovery-validation.md
docs/runbooks/postgresql-backup-and-restore.md
docs/reports/P29-performance-baseline.md
docs/engineering/data-integrity-query-performance-and-database-hardening.md
docs/engineering/connected-exits-suppliers.md
docs/engineering/organization-branches-and-fulfillment-locations.md
docs/engineering/branch-delivery-pricing.md
docs/engineering/customer-ordering-pickup-and-delivery.md
docs/engineering/purchasing-inventory-ux-mental-model.md
docs/validation/phase-26-owner-validation-checklist.md
docs/compliance/bir-compliance-activation-roadmap.md
docs/compliance/bir-authoritative-source-register.md
docs/engineering/bir-registration-readiness-and-activation.md
docs/reports/personal-organization-identity-isolation.md
docs/reports/organization-profile-independence.md
docs/reports/sales-buyer-party-isolation.md
docs/reports/organization-ownership-transfer.md
docs/engineering/sales-buyer-party-model.md
docs/engineering/organization-profile-independence.md
docs/engineering/organization-ownership-transfer.md
docs/engineering/organization-web-role-and-workflow-matrix.md
docs/engineering/organization-web-ui-responsive-standard.md
docs/validation/organization-web-responsive-owner-checklist.md
docs/reports/P25-org-web-full-responsive-ux-completion.md
docs/reports/P25-owner-organization-management-authority-fix.md
docs/reports/P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md
docs/reports/connected-supplier-connection-request-lifecycle.md
docs/engineering/sales-document-compliance-boundary.md
docs/engineering/platform-controlled-organization-tax-configuration.md
docs/engineering/organization-sales-document-acknowledgment.md
docs/engineering/platform-organization-compliance-eligibility.md
docs/engineering/organization-compliance-profile.md
docs/architecture/personal-organization-identity-boundaries.md
docs/architecture/client-experience-boundaries.md
docs/specs/identity/public-user-id-and-qr.md
docs/decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md
docs/reports/P24-WP01-current-state-and-architecture-contract.md
docs/reports/P24-WP02-customer-link-and-pos-correlation.md
docs/reports/P24-WP03-linked-customer-authorization-contract.md
docs/reports/P24-WP04-lightweight-linked-business-utang-statement.md
docs/reports/P24-WP05-receipt-summary-detail-and-lazy-loading.md
docs/reports/P24-WP06-free-vs-paid-personal-history-entitlement.md
docs/reports/P24-WP07-personal-reward-points-and-redemption.md
docs/reports/P24-WP08-reward-ledger-foundation.md
docs/reports/P24-WP09-ads-abstraction-and-ad-free-entitlement.md
docs/portfolio-progress.md
docs/product/pinoy-business-pos-requirements.md
docs/product/portfolio-vision.md
docs/product/subscriptions-and-billing.md
docs/release-plan.md
docs/reports/README.md
docs/reports/P1-WP01-platform-product-capability-boundary.md
docs/reports/P1-WP02-data-ownership-and-contracts.md
docs/reports/P1-WP04-architecture-approval-closeout.md
docs/reports/P2-WP01-extraction-baseline-and-safety.md
docs/reports/P2-WP02-identity-organization-boundary.md
docs/reports/P2-WP03-products-plans-entitlements.md
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
docs/reports/P7-WP01-sqlite-and-device-identity.md
docs/reports/P7-WP02-offline-queue-and-idempotency.md
docs/reports/P7-WP03-customer-and-credit-sync.md
tests/ExItS.PinoyBusinessPOS.UnitTests/Offline/PaymentOfflineStoreTests.cs
tests/ExItS.PinoyBusinessPOS.IntegrationTests/PosPaymentOfflineIdempotencyApiTests.cs
docs/reports/P7-WP04-payment-sync-and-recovery.md
docs/reports/P7-WP05-offline-closeout.md
docs/reports/P8-WP01-catalog-and-barcode.md
docs/reports/P8-WP02-simple-sales.md
docs/reports/P8-WP03-product-based-utang.md
docs/reports/P8-WP04-basic-inventory.md
docs/reports/product-units-and-inventory-behavior.md
docs/reports/P8-WP05-expenses.md
docs/reports/P8-WP06-dashboard-and-reports.md
docs/reports/P8-WP07-basic-store-closeout.md
docs/reports/P9-WP01-security-and-privacy-hardening.md
docs/reports/P9-WP02-performance-and-reliability.md
docs/reports/P9-WP03-backup-and-restore.md
docs/reports/P9-WP04-accessibility-localization-theme-qa.md
docs/reports/P9-WP05-pilot-and-deployment.md
docs/reports/P9-WP06-commercial-mvp-closeout.md
docs/reports/P10-WP01-scope-ambiguity.md
docs/reports/P10-WP01-suppliers.md
docs/reports/P10-WP02-purchasing.md
docs/reports/P10-WP03-advanced-inventory.md
docs/reports/P10-WP04-cashier-shifts.md
docs/reports/P10-WP05-returns-refunds.md
docs/reports/P10-WP06-advanced-permissions-operational-reports.md
docs/reports/P10-WP07-multiple-registers.md
docs/reports/P10-WP08-phase-10-closeout.md
docs/reports/P11-WP01-web-ui-audit-and-component-inventory.md
docs/reports/P11-WP02-global-web-layout-and-navigation.md
docs/reports/P11-WP03-shared-forms-validation-and-dialogs.md
docs/reports/P11-WP04-shared-tables-lists-cards-and-status-components.md
docs/reports/P11-WP05-shared-reporting-framework.md
docs/reports/P11-WP06-dashboard-and-report-refactoring.md
docs/reports/P11-WP07-localization-theme-accessibility-responsive-qa.md
docs/reports/P11-WP08-phase-11-closeout.md
docs/reports/P12-WP01-platform-product-contract-audit.md
docs/reports/P12-WP02-authoritative-product-foundation-reference.md
docs/phases/phase-11-web-ui-reporting-design-system.md
docs/phases/phase-12-product-foundation-and-bootstrap.md
docs/Product-Foundation/README.md
docs/Product-Foundation/exits-product-foundation-reference.md
docs/Product-Foundation/product-bootstrap-prompt.md
docs/Product-Foundation/Reference-Product/README.md
docs/Product-Foundation/Reference-Product/product-definition.md
docs/Product-Foundation/Reference-Product/architecture.md
docs/Product-Foundation/Reference-Product/security.md
docs/Product-Foundation/Reference-Product/authorization-matrix.md
docs/Product-Foundation/Reference-Product/development-plan.md
docs/Product-Foundation/Reference-Product/roadmap.md
docs/Product-Foundation/Reference-Product/risks-and-decisions.md
docs/Product-Foundation/Reference-Product/FILE-MANIFEST.md
docs/Product-Foundation/Templates/README.md
docs/Product-Foundation/Templates/product-definition.md
docs/Product-Foundation/Templates/architecture.md
docs/Product-Foundation/Templates/security.md
docs/Product-Foundation/Templates/authorization-matrix.md
docs/Product-Foundation/Templates/development-plan.md
docs/Product-Foundation/Templates/roadmap.md
docs/Product-Foundation/Templates/work-package-report.md
docs/Product-Foundation/Templates/risks-and-decisions.md
docs/Product-Foundation/Templates/deployment-notes.md
docs/Product-Foundation/Templates/FILE-MANIFEST.md
docs/Product-Foundation/Templates/product-docs-readme.md
docs/reports/P12-WP03-product-documentation-templates.md
docs/reports/P12-WP04-cursor-product-context-rule.md
docs/reports/P12-WP05-product-bootstrap-prompt.md
docs/reports/P12-WP06-reference-product-dry-run.md
docs/reports/P12-WP07-foundation-hardening-and-closeout.md
docs/reports/P13-WP01-authentication-architecture-and-threat-model.md
docs/reports/P13-WP02-identity-credentials-and-auth-persistence.md
docs/phases/phase-13-production-authentication-and-identity.md
docs/engineering/authentication-architecture.md
docs/engineering/authentication-threat-model.md
docs/reports/PRE-P11-admin-ui-recovery.md
docs/reports/PRE-P11-admin-theme-visual-polish.md
docs/operations/backup-restore/README.md
docs/operations/pilot-and-deployment/README.md
docs/reports/phase-02-extraction-closeout.md
docs/reports/phase-01-architecture-approval.md
docs/risks-and-issues.md

## Not tracked

**/bin/, **/obj/
