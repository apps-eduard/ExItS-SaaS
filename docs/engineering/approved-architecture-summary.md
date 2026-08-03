# Approved Architecture Summary

[Home](../index.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Authorization](authorization-matrix.md) | [Entitlement states](entitlement-state-matrix.md)

**Version:** 2.0  
**Status:** Authoritative implementation entry point  
**Current phase:** Phase 16 — Implementation Complete, Under Validation  
**Current work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Final closeout:** P16-WP12 — Not Started  
**Last reconciled:** 2026-08-03

---

## 1. Current implementation focus

Phase 16 web validation must stabilize these Platform, Personal, and Organization flows before any MAUI reconciliation:

1. Plan pricing
2. Trial-first business onboarding
3. Local Validation fake SaaS payments
4. Trial-to-paid conversion
5. Organization subscription upgrade and downgrade
6. Product entitlement and provisioning
7. Personal **Start a Business**
8. Organization staff invitation and product-role assignment
9. Scope-bound sessions and account-creation boundaries
10. Clean Local Validation reset and end-to-end user acceptance

Do not begin MAUI contract reconciliation until the web and API contracts above are stable and accepted.

---

## 2. Permanent platform model

```text
User Identity
├── Platform Account Profile
├── Personal Account Profile
└── Organization Account Profile
```

A person may have more than one explicit account profile, but every authenticated session is bound to exactly one account class and allowed scope.

```text
Platform session
→ Platform APIs only

Personal session
→ Personal APIs only

Organization session
→ Organization and entitled Product APIs only
```

Account profiles must never be inferred from unrelated records.

---

## 3. Commercial hierarchy

```text
Platform Product
→ Plan
→ Organization Subscription
→ Organization Product Entitlement
→ Organization Product Instance
→ Organization Product Role Assignment
```

Permanent rules:

- Plans belong to a Platform Product.
- Subscriptions belong to an Organization, never directly to a Personal Account.
- Entitlement enables a Product for an Organization.
- Product Role authorizes a person inside that Product.
- Organization membership alone does not grant Product access.
- Product operational data remains in the Product boundary.

---

## 4. Personal to business journey

The user-facing journey may be described as upgrading from personal use, but implementation must not convert or delete the Personal Account.

```text
Verified Personal Account
→ Start a Business
→ create explicit Organization Account Profile
→ create Organization
→ assign Owner membership in that Organization only
→ select Product and Plan
→ start trial
→ enable entitlement
→ provision Product Instance
→ assign POS Owner role explicitly
→ issue or select Organization-scoped session
```

The Personal Account remains available and separate.

Personal Utang migration is optional, selective, previewed, idempotent, consent-aware, and audited. It is not part of automatic business onboarding.

---

## 5. Trial and pricing decision

Pinoy Business POS uses configurable Platform-owned Plans.

Local Validation defaults:

| Plan | Monthly | Annual | Branches | Active staff | Trial |
|---|---:|---:|---:|---:|---|
| Starter | PHP 299.00 | PHP 2,990.00 | 1 | 3 | No |
| Business | PHP 699.00 | PHP 6,990.00 | 3 | 15 | 14 days |
| Pro | PHP 1,499.00 | PHP 14,990.00 | 10 | 50 | No |

These prices are Local Validation defaults and remain configurable.

The approved onboarding trial is the **Business Plan 14-day trial**. Personal Utang is a free Personal feature and is not a separate three-calendar-month SaaS trial.

Existing documentation or tests that still treat the commercial POS trial as three calendar months must be reconciled.

---

## 6. Subscription pricing

A Plan stores current offer pricing:

```text
MonthlyPrice
AnnualPrice
CurrencyCode
DisplayOrder
TrialAllowed
DefaultTrialDays
```

A Subscription stores an agreed-price snapshot:

```text
BillingCycle
AgreedPrice
CurrencyCode
PriceEffectiveFrom
CurrentPeriodStart
CurrentPeriodEnd
```

Changing Plan pricing must not silently reprice existing Subscriptions.

---

## 7. Local Validation fake payment provider

The Platform implements:

```text
IPaymentProvider
└── LocalValidationPaymentProvider
```

It simulates:

- successful payment
- declined payment
- pending payment
- failed payment
- refund
- successful renewal
- failed renewal

Rules:

- available only in Local Validation
- clearly marked as test data
- no card-number collection
- idempotent event processing
- audited transitions
- impossible to enable in Production
- Production startup fails closed when configured with the Local Validation provider

SaaS payments are Platform-owned and remain separate from POS retail payments and POS credit repayments.

---

## 8. Subscription lifecycle

Supported states:

```text
Trialing
Active
Past Due
Suspended
Cancelled
Expired
```

Core behavior:

| State | Entitlement behavior |
|---|---|
| Trialing | Enabled |
| Active | Enabled |
| Past Due | Enabled only during configured grace policy |
| Suspended | Product-protected writes blocked; policy-defined continuity only |
| Cancelled | No new paid operations; retained history according to policy |
| Expired | No new paid operations; retained data and continuity according to policy |

No lifecycle transition may delete Product operational data automatically.

---

## 9. Plan changes

Upgrade:

- preview price and feature differences
- require confirmation
- payment when required
- normally effective after successful payment
- store new agreed-price snapshot
- generate new entitlement revision
- preserve all Product data
- audit old/new Plan, price, actor, reason, and effective date

Downgrade:

- preview lost features and lower limits
- detect current usage conflicts
- normally schedule at period end or explicit future date
- never delete staff, branches, customers, transactions, or Product data
- retain existing over-limit data
- block new activity that would further exceed the new limit
- generate new entitlement revision when effective
- audit the request and application

---

## 10. Authorization formula

```text
Active User Identity
+ correct Account Profile and scope
+ active Organization membership
+ valid Subscription
+ enabled Product Entitlement
+ provisioned Product Instance
+ active Product-local role
+ permission for requested operation
= Product access
```

Navigation visibility is not authorization. Every API and domain operation must enforce its own checks.

---

## 11. Technology and ownership

| Surface | Technology / ownership |
|---|---|
| Platform Admin | Blazor Web App + Ant Design Blazor |
| Personal web | Web client using Personal scope |
| Organization web | Web client using Organization scope |
| Pinoy Business POS | Product APIs, Product database, MAUI Blazor Hybrid |
| Platform database | Identity, profiles, Organizations, Plans, Subscriptions, SaaS Payments, Entitlements, Platform audit |
| POS database | `ExItS_PinoyBusinessPOS`, schema `pos`, Product operational truth |

No cross-database foreign keys. Products consume stable IDs and versioned projections.

---

## 12. Validation order

1. Implement and stabilize pricing, trial, fake payments, upgrade/downgrade, and Start a Business.
2. Reset Local Validation data while retaining only the two approved Platform users and stable reference/catalog data.
3. Register a Personal user through the public flow.
4. Activate through Mailpit.
5. Start a Business using a Business Plan trial.
6. Verify Organization creation, Owner membership, entitlement, provisioning, and POS Owner assignment.
7. Convert trial to paid using fake payment.
8. Upgrade, then schedule downgrade.
9. Invite Organization employees and assign Product roles separately.
10. Validate payment failure, renewal failure/recovery, expiry, tenant isolation, and session boundaries.
11. Obtain explicit user acceptance.
12. Only then audit and reconcile MAUI.

---

## 13. Approved validation identities after reset

Retain only:

| Name | Account class | Role |
|---|---|---|
| Olivia Mendoza | Platform | Platform Administrator |
| Rafael Torres | Platform | Platform Support |

All Personal and Organization identities used for final validation must be created through the real user-facing flows.

---

## 14. Current status rule

Keep these statuses until validation and explicit acceptance are complete:

```text
Phase 16 — Implementation Complete, Under Validation
P16-WP11 — In Progress
P16-WP12 — Not Started
```

Do not declare production readiness from Local Validation success alone.
