# Products, Plans, Trials and Billing

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md) | [Contracts](../engineering/platform-product-contracts.md) | [Entitlement states](../engineering/entitlement-state-matrix.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md)

## Platform product catalog

- legacy product
- PinoyBusinessPOS

## PinoyBusinessPOS plans

### Utang Trial

**Three calendar months** from trial start (UTC). Intended future policy:

```text
Trial expiration = trial start timestamp plus three calendar months
```

The generic Platform `TrialDefinition` is **configurable** (positive duration supplied by configuration/application input). **Ninety days is not an approved substitute** for three calendar months. End-of-month behavior (e.g. start 31 January) remains **undecided** and must be confirmed in a later catalog/configuration work package — do not guess.

After expiry, existing balances remain visible and payments on existing debt remain allowed via **Cash** or **GCash** (manual). New credit is blocked until activation. Detailed allowed/blocked matrix and open post-expiry UX questions: [platform-product-contracts.md §9](../engineering/platform-product-contracts.md). MVP payment methods: [pinoy-business-pos-requirements.md](pinoy-business-pos-requirements.md).

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

**P2-WP03:** Domain models exist for Product, Plan, PlanVersion, FeatureDefinition, TrialDefinition, Subscription, FeatureOverride, and EntitlementSnapshot. PaymentTransaction / Invoice / payment collection are **not** implemented.

**P3-WP02:** PlatformOrganization (minimal) and Subscription are **persisted**. Commercial `ActivateSubscription` does **not** collect or verify payment. Invoices/GCash/payment gateways remain out of scope.

**P3-WP03:** Manual SaaS payment records are **persisted** (`platform.saas_payments`). Manual confirmation lifecycle (PendingConfirmation → Confirmed → Voided; PendingConfirmation → Rejected). Confirmed payment can atomically activate a subscription. Duplicate-reference detection enforced. No payment gateway, webhook, QR, card storage, or automatic verification. Payment amount reconciliation against catalog price is deferred.

**P3-WP04:** Feature overrides and immutable entitlement snapshots are **persisted**. Composition applies plan/trial grants, overrides, and subscription-state restrictions (including Expired Utang view/repay/create). Snapshot versions are monotonic per organization+product. Refresh-by uses a provisional 24h policy (R-022 open). **No product delivery** — legacy product/POS local projections remain future work.

**P3-WP05:** Phase 3 commercial foundation **closed with documented risks**. End-to-end catalog → trial → payment activation → snapshots → grace/past-due → terminal/expired Utang path validated. Production auth, delivery, invoices, and gateways remain out of scope.

**P4-WP03:** Platform Admin exposes subscription lifecycle, trial start, and manual SaaS payment workflows over the same Phase 3 persistence and APIs. No new commercial migration. Manual confirmation remains operator judgment (not provider verification). R-035 calendar-month EOM remains open.

## Availability rule

Product APIs use locally stored entitlement snapshots (versioned, time-bounded, fail-safe, with grace and audit). A sale or offline credit must not fail solely because the central Platform service is temporarily unavailable.

PinoyBusinessPOS plans apply to the retail product for Philippine SME stores (initial focus: Sari-Sari and mini groceries; architecture remains generic for broader retail).
