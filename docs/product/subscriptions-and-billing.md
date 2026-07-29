# Products, Plans, Trials and Billing

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md) | [Contracts](../engineering/platform-product-contracts.md) | [Entitlement states](../engineering/entitlement-state-matrix.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md)

## Platform product catalog

- HealthCare
- PinoyBusinessPOS

## PinoyBusinessPOS plans

### Utang Trial

Three months. Customers, remarks-based credit, payments, ledger and overdue monitoring.

After expiry, existing balances remain visible and payments on existing debt remain allowed. New credit is blocked until activation. Detailed allowed/blocked matrix and open post-expiry UX questions: [platform-product-contracts.md §9](../engineering/platform-product-contracts.md).

### Utang

Unlimited normal Utang operations, statements, reminders and reports within documented fair-use limits.

### Basic Store

Everything in Utang plus products, simple sales, barcode, product-based Utang, basic inventory, expenses and basic reports.

### Full POS

Everything in Basic Store plus suppliers, purchasing, advanced inventory, cashier shifts, returns/refunds, advanced permissions and reports.

## Platform entities

- Product
- ProductPlan
- PlanFeature
- PlatformOrganization
- OrganizationProductSubscription
- PaymentTransaction
- Invoice (later)
- EntitlementSnapshot
- OrganizationFeatureOverride

## Availability rule

Product APIs use locally stored entitlement snapshots (versioned, time-bounded, fail-safe, with grace and audit). A sale or offline credit must not fail solely because the central Platform service is temporarily unavailable.

PinoyBusinessPOS plans apply to the retail product for Philippine SME stores (initial focus: Sari-Sari and mini groceries; architecture remains generic for broader retail).
