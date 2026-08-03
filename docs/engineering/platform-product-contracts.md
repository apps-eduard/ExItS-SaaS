# Platform–Product Contracts

[Architecture summary](approved-architecture-summary.md) | [Capability boundary](platform-product-capability-boundary.md) | [Entitlement states](entitlement-state-matrix.md) | [Contract matrix](platform-product-contract-matrix.md)

**Version:** 2.0  
**Status:** Authoritative contract policy  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Purpose

Define stable, versioned, product-neutral contracts between ExITS Platform and Products for identity, Organization context, commercial state, entitlement, provisioning, and Product access.

This document defines contract policy and minimum shapes. Concrete DTO names may follow existing code conventions, but must preserve these semantics.

---

## 2. Contract principles

1. Platform is system of record for identity, profiles, Organizations, memberships, catalog, Plans, Subscriptions, SaaS Payments, and Entitlements.
2. Products own operational data and operational authorization.
3. Cross-boundary references use stable IDs only.
4. No cross-database foreign keys.
5. Contracts are versioned and additive by default.
6. Consumers assume at-least-once delivery and implement idempotency.
7. Duplicate and out-of-order events must not duplicate state transitions.
8. Sensitive Product payloads never enter commercial contracts.
9. SaaS Payment is not Retail Payment or Credit Repayment.
10. Product operations do not synchronously query Platform for every transaction.

---

## 3. Stable identifiers

Minimum identifiers:

```text
PlatformUserId
AccountProfileId
PlatformOrganizationId
OrganizationMembershipId
ProductId
ProductKey
PlanId
PlanKey
PlanVersion
SubscriptionId
SaaSPaymentId
EntitlementId
EntitlementRevision
ProductInstanceId
EventId
IdempotencyKey
CorrelationId
CausationId
```

Identifiers are immutable and never reused after retirement or deletion.

---

## 4. Identity and profile projection

Minimum fields:

```text
PlatformUserId
DisplayName
NormalizedEmail, only when needed
IdentityStatus
AccountProfileId
AccountClass
AllowedScope
ProfileStatus
IdentityVersion
EffectiveAtUtc
LastSynchronizedAtUtc
```

Account classes:

```text
Platform
Personal
Organization
```

A Product must not infer account class from Organization membership, Entitlement, Product role, or application settings.

Must not project:

- password hashes
- refresh-token hashes
- MFA secrets
- reset or activation tokens
- unnecessary login history
- unrelated Personal data

---

## 5. Organization and membership projection

Minimum fields:

```text
PlatformOrganizationId
OrganizationDisplayName
OrganizationStatus
OrganizationMembershipId
MembershipStatus
OrganizationRole
ProjectionVersion
EffectiveAtUtc
LastSynchronizedAtUtc
```

User-facing Organization roles:

```text
Owner
Staff
```

Organization membership does not grant Product operational permission.

---

## 6. Plan contract

Minimum Plan fields:

```text
PlanId
ProductId
ProductKey
PlanKey
DisplayName
Description
Status
MonthlyPrice
AnnualPrice
CurrencyCode
DisplayOrder
TrialAllowed
DefaultTrialDays
MaxBranches
MaxActiveStaff
FeatureDefinitions
PlanVersion
CreatedAtUtc
UpdatedAtUtc
```

Rules:

- money uses decimal-compatible representations
- currency uses ISO 4217 code
- DisplayOrder is independent from price
- only Active Plans accept new Subscriptions
- retired Plans retain historical references
- PlanKey is immutable after publication
- Plan price changes do not silently alter existing Subscription snapshots

Current Local Validation defaults:

| PlanKey | Monthly | Annual | Currency | Trial |
|---|---:|---:|---|---|
| starter | 299.00 | 2,990.00 | PHP | No |
| business | 699.00 | 6,990.00 | PHP | 14 days |
| pro | 1,499.00 | 14,990.00 | PHP | No |

---

## 7. Subscription contract

Minimum fields:

```text
SubscriptionId
PlatformOrganizationId
ProductId
ProductKey
PlanId
PlanKey
PlanVersion
SubscriptionStatus
BillingCycle
AgreedPrice
CurrencyCode
PriceEffectiveFromUtc
TrialStartAtUtc
TrialEndAtUtc
CurrentPeriodStartAtUtc
CurrentPeriodEndAtUtc
GracePeriodEndAtUtc
PendingPlanId
PendingPlanKey
PendingPlanEffectiveAtUtc
CancellationEffectiveAtUtc
Version
CreatedAtUtc
UpdatedAtUtc
```

Billing cycles:

```text
Monthly
Annual
```

Statuses:

```text
Trialing
Active
PastDue
Suspended
Cancelled
Expired
```

The Subscription belongs to an Organization. It must not belong directly to a Personal Account.

---

## 8. Trial policy

Approved onboarding behavior:

```text
Business Plan
→ 14-day trial
→ one trial per Organization per Product
→ no payment record required to start
→ Entitlement Enabled
→ Product Instance provisioned
```

Trial conversion:

```text
Trialing
→ Subscribe Now
→ SaaS payment succeeds
→ Active
→ CurrentPeriodStart/End set
→ agreed-price snapshot retained
→ Entitlement remains Enabled with new revision
```

Trial expiry:

```text
Trialing
→ TrialEnd reached without conversion
→ Expired
→ paid writes blocked according to policy
→ Product data retained
```

Personal Utang is free Personal functionality and is not represented as a three-calendar-month Organization Subscription trial.

---

## 9. SaaS payment contract

Minimum fields:

```text
SaaSPaymentId
PlatformOrganizationId
SubscriptionId
Provider
ProviderReference
IdempotencyKey
PaymentPurpose
Amount
CurrencyCode
PaymentStatus
IsTest
FailureCode
FailureMessage
CreatedAtUtc
UpdatedAtUtc
OccurredAtUtc
Version
```

Payment purposes:

```text
InitialActivation
TrialConversion
Upgrade
Renewal
Refund
AdministrativeAdjustment
```

Payment statuses:

```text
Pending
Succeeded
Declined
Failed
Refunded
```

Local Validation provider:

```text
Provider = LocalValidation
IsTest = true
ProviderReference = lvp_pay_000001
```

Rules:

- no card details are collected
- provider events are idempotent
- duplicate success events do not duplicate period extension or Plan changes
- provider is configuration-gated
- Production startup rejects LocalValidation provider configuration
- every payment result and resulting Subscription transition is audited

---

## 10. Plan-change contract

### Upgrade request

Minimum fields:

```text
PlanChangeRequestId
SubscriptionId
CurrentPlanId
TargetPlanId
ChangeType = Upgrade
RequestedByUserId
RequestedAtUtc
EffectiveAtUtc
Reason
PricePreview
CurrencyCode
IdempotencyKey
Status
Version
```

Upgrade behavior:

- validate target Plan belongs to the same Product
- validate target Plan is active
- preview features, limits, and price
- require payment when applicable
- apply only after successful payment
- normally effective immediately
- snapshot new agreed price
- generate new Entitlement revision
- preserve Product data

### Downgrade request

Additional fields:

```text
UsageConflicts
PendingPlanEffectiveAtUtc
```

Downgrade behavior:

- preview lost features and limits
- calculate usage conflicts
- normally schedule at period end or explicit future date
- retain existing over-limit data
- block additional activity that would further exceed limits
- never delete Product operational data automatically
- apply idempotently at the effective time

---

## 11. Entitlement projection

Minimum fields:

```text
EntitlementId
PlatformOrganizationId
ProductId
ProductKey
SubscriptionId
PlanId
PlanKey
PlanVersion
EntitlementRevision
SchemaVersion
SubscriptionStatus
EntitlementStatus
ProvisioningStatus
FeatureCode
Enabled
LimitValue
EffectiveAtUtc
ExpiresAtUtc
RefreshByUtc
GracePeriodEndAtUtc
GeneratedAtUtc
EventId
CorrelationId
```

Entitlement statuses:

```text
Pending
Enabled
Suspended
Disabled
Expired
ProvisioningFailed
```

Provisioning statuses:

```text
NotStarted
Pending
Provisioning
Ready
Failed
Suspended
```

A Product role is not included as an Entitlement grant.

---

## 12. Product access response

Organization-facing Product discovery should expose separate fields:

```text
ProductId
ProductKey
ProductDisplayName
SubscriptionStatus
PlanDisplayName
EntitlementStatus
ProvisioningStatus
OrganizationRole
ProductRole
CanLaunch
DenialReasonCode
DenialReasonDisplay
EntitlementRevision
```

Do not overload a generic `Role` or `Status` field.

Denial displays must be friendly. Technical codes may be logged or returned as structured metadata but not shown as raw user-facing messages.

---

## 13. Start a Business orchestration contract

Minimum request:

```text
UserIdentityId, resolved from authenticated Personal session
OrganizationName
OrganizationDetails
ProductId or ProductKey
PlanId
BillingCycle
StartMode = Trial | PayNow
IdempotencyKey
AcceptedTermsVersion
```

Minimum orchestration state:

```text
OnboardingRequestId
OrganizationProfileStatus
OrganizationCreationStatus
OwnerMembershipStatus
SubscriptionStatus
PaymentStatus
EntitlementStatus
ProvisioningStatus
ProductOwnerRoleStatus
OverallStatus
FailureCode
Retryable
Version
```

Rules:

- Personal session initiates the operation
- Organization assignment comes from server-created state
- operation is transactional where possible
- partially completed steps are recoverable
- retry is idempotent
- the Personal profile remains unchanged
- Organization session/profile selection is explicit
- no automatic Personal Utang migration occurs

---

## 14. Event families

Recommended versioned event families:

```text
IdentityProfileChangedV1
OrganizationMembershipChangedV1
SubscriptionChangedV1
SaaSPaymentResultRecordedV1
PlanChangeRequestedV1
PlanChangeAppliedV1
EntitlementChangedV1
ProductProvisioningRequestedV1
ProductProvisioningCompletedV1
ProductProvisioningFailedV1
```

Each event includes:

```text
EventId
EventVersion
OccurredAtUtc
CorrelationId
CausationId
IdempotencyKey
SourceVersion
```

---

## 15. Failure and continuity behavior

| Situation | Required behavior |
|---|---|
| Platform temporarily unavailable | Product may use a still-trusted local projection within explicit freshness policy |
| Projection never initialized | Fail closed |
| Projection invalid or unsupported | Fail closed |
| Duplicate event | No duplicate transition |
| Out-of-order event | Ignore or reconcile using version |
| Trial expired | Block new paid operations; retain data |
| Renewal failed | PastDue; apply configured grace |
| Grace expired | Suspend protected operations |
| Downgrade over limit | Retain existing data; block additional over-limit creation |
| Product DB unavailable | Product outage; Platform commercial records remain authoritative |

---

## 16. Production restrictions

Production must not enable:

- LocalValidationPaymentProvider
- Local Validation seed identities
- reset/reseed endpoints
- quick login
- development authorization headers
- fixed local passwords
- `10.0.2.2` development endpoints as production configuration
