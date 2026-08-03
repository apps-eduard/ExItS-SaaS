# Platform–Product Capability Boundary

[Architecture summary](approved-architecture-summary.md) | [Contracts](platform-product-contracts.md) | [Authorization](authorization-matrix.md) | [Data ownership](data-ownership.md)

**Version:** 2.0  
**Status:** Authoritative  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Purpose

Define the permanent ownership and security boundary between the ExITS Platform and each Product so implementation does not:

- place Product operational logic in Platform
- duplicate commercial ownership inside Products
- grant Product permissions from Platform membership alone
- mix SaaS payments with retail or credit payments
- create synchronous Platform coupling for every Product operation
- leak Personal or tenant data across scopes

---

## 2. Core boundary principles

1. One authoritative owner per capability.
2. Platform owns identity, account profiles, Organizations, Plans, Subscriptions, SaaS Payments, and Entitlements.
3. Products own operational data, operational workflows, Product-local roles, and Product-local audit.
4. Products may store versioned Platform projections; projection is not ownership.
5. No cross-database foreign keys.
6. No shared DbContext or shared mutable Product entities.
7. Product access and Product operational permission are separate.
8. Platform Administrator does not automatically receive tenant operational access.
9. SaaS Payment, POS Retail Payment, and POS Credit Repayment are separate concepts.
10. Client-supplied Organization IDs are never authoritative.

---

## 3. Platform ownership

The Platform is system of record for:

- User Identity
- credentials, verification, activation, suspension, sessions, and revocation
- Platform, Personal, and Organization Account Profiles
- Organizations
- Organization memberships and Organization roles
- Product catalog
- Plans and Plan pricing
- trial policy
- Organization Subscriptions
- SaaS Payments
- subscription lifecycle
- Product Entitlements
- entitlement revisions and overrides
- Product provisioning intent and status metadata
- Platform Admin
- Platform audit, security events, and support operations
- trial, payment, renewal, and suspension notifications

The Platform must not own POS sales, inventory, Utang transactions, Product customers, cash sessions, or Product operational roles.

---

## 4. Personal ownership

Personal scope owns:

- Personal profile settings
- Personal contacts
- Personal Utang relationships
- personal reminders
- personal notification preferences
- Personal-to-business onboarding initiation

A Personal Account does not directly own:

- an Organization Subscription
- a Product Entitlement
- an Organization membership
- a POS role
- tenant operational data

When a Personal user selects **Start a Business**, the Platform explicitly creates an Organization Account Profile and a new Organization. The Personal profile remains separate.

---

## 5. Organization ownership

An Organization owns or controls:

- Organization profile
- Organization memberships
- Organization role assignments
- subscription selection and approved Plan-change requests
- tenant settings
- Organization-scoped audit references
- Product Instances associated with that Organization

Organization roles for the current model are user-facing:

```text
Owner
Staff
```

Organization role is not Product role.

---

## 6. Product ownership

Pinoy Business POS owns:

- POS business/operational profile
- stores and branches
- registers and cash sessions
- Product-local staff profiles where needed
- POS Product roles and permissions
- customers
- Personal-to-business imported customer records after explicit migration
- catalog and barcodes
- inventory and stock movements
- sales, returns, and retail payments
- Customer Credit / Utang and repayments
- expenses, purchasing, reports
- offline database and synchronization state
- Product-local audit

Authoritative Product database:

```text
Database: ExItS_PinoyBusinessPOS
Schema: pos
```

---

## 7. Commercial hierarchy

```text
Platform Product
→ Plan
→ Organization Subscription
→ Organization Product Entitlement
→ Organization Product Instance
→ Organization Product Role Assignment
```

Responsibilities:

| Concept | Owner | Purpose |
|---|---|---|
| Platform Product | Platform | Defines what ExITS offers |
| Plan | Platform | Defines price, features, and limits |
| Subscription | Platform | Commercial enrollment of one Organization in one Plan |
| SaaS Payment | Platform | Payment for ExITS software service |
| Entitlement | Platform | Commercial permission for an Organization to use a Product |
| Product Instance | Product, provisioned from Platform intent | Organization-specific workspace and operational data |
| Product Role | Product | Authorizes a staff member inside the Product |

Permanent rule:

> Entitlement enables a Product for an Organization. Product Role authorizes a person inside that Product.

---

## 8. Plan and trial boundary

The Platform owns Plan pricing and trial policy.

Current Local Validation defaults:

- Starter: PHP 299 monthly / PHP 2,990 annual
- Business: PHP 699 monthly / PHP 6,990 annual
- Pro: PHP 1,499 monthly / PHP 14,990 annual
- Business trial: 14 days

Prices and trial duration remain configurable.

Personal Utang is a free Personal capability. It is not a separate three-calendar-month Platform Subscription trial.

---

## 9. Payment boundary

```text
Platform SaaS Payment
→ Organization pays ExITS for software access

POS Retail Payment
→ store customer pays the Organization for a sale

POS Credit Repayment
→ customer pays an existing Organization-owned credit balance
```

These use separate:

- entities
- services
- permissions
- audit trails
- idempotency keys
- provider integrations
- reporting

`LocalValidationPaymentProvider` exists only for Platform SaaS payment testing and must never be reused for retail checkout.

---

## 10. Start a Business boundary

Platform and web onboarding own:

```text
Personal user initiates
→ Organization profile creation
→ Organization creation
→ Owner membership
→ Plan selection
→ trial or payment
→ Subscription
→ Entitlement
→ provisioning intent
→ explicit POS Owner role bootstrap
```

The POS MAUI application does not own:

- Personal registration
- Organization creation
- Plan purchase
- SaaS payment
- subscription upgrade/downgrade

MAUI consumes the resulting Organization/Product access contracts after web stabilization.

---

## 11. Authorization boundary

Platform determines:

- identity status
- account class and scope
- active Organization membership
- Subscription and Entitlement state
- whether the Organization may enter the Product

Product determines:

- Product-local role
- operation-level permission
- branch/register scope
- Product-specific continuity rules

A Platform commercial grant never creates an operational Product role automatically, except an explicitly designed, transactional first-owner bootstrap during Start a Business.

---

## 12. Offline and projection boundary

Products use local projections so normal operations do not synchronously call Platform on every transaction.

A projection may contain:

- PlatformOrganizationId
- ProductKey
- SubscriptionId
- PlanKey and PlanVersion
- SubscriptionStatus
- EntitlementRevision
- feature grants and numeric limits
- EffectiveAt
- ExpiresAt or RefreshBy
- grace and suspension facts
- source event and correlation identifiers

Rules:

- duplicate and out-of-order updates are idempotent
- missing, invalid, or unsupported projections fail closed
- temporarily stale projections follow explicit policy
- suspension or expiry does not delete Product data
- offline queued writes must not synchronize when the authoritative commercial state forbids them

---

## 13. Support boundary

Platform support access to tenant operational data is not ordinary scope switching.

A Support Session must be:

- explicit
- Organization-specific
- time-limited
- reason-required
- read-only by default
- prominently displayed
- fully audited
- independently revocable

Support Session implementation remains separate from normal Platform, Personal, and Organization sessions.

---

## 14. Prohibited models

- Personal Account directly owns an Organization Subscription
- Organization membership automatically grants POS access
- Entitlement automatically grants all staff access
- Product role creates an Entitlement
- Platform Admin directly operates POS as ordinary staff
- Platform SaaS Payment is stored as POS Retail Payment
- Product operational data is stored in Platform
- Product directly updates Platform commercial tables
- automatic Personal-to-business ledger synchronization
- automatic deletion of Product data after downgrade, cancellation, or expiry
