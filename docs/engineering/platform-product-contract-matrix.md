# Platform–Product Contract Matrix

[Contracts](platform-product-contracts.md) | [Data ownership](data-ownership.md) | [Entitlement states](entitlement-state-matrix.md)

**Version:** 2.0  
**Status:** Authoritative implementation index  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Contract ownership matrix

| Contract family | Publisher / authority | Consumer | Direction | Idempotent | Contains Product operational data? |
|---|---|---|---|---:|---:|
| Identity/Profile | Platform | Products | Platform → Product | Yes | No |
| Organization/Membership | Platform | Products | Platform → Product | Yes | No |
| Product Catalog | Platform | Web/Product clients | Platform → Consumer | Yes | No |
| Plan/Pricing | Platform | Personal/Organization web | Platform → Consumer | Yes | No |
| Subscription | Platform | Web and Products | Platform → Consumer | Yes | No |
| SaaS Payment Result | Platform payment adapter | Platform application | Provider → Platform | Yes | No |
| Plan Change | Platform | Web and Products | Platform → Consumer | Yes | No |
| Entitlement | Platform | Products | Platform → Product | Yes | No |
| Provisioning Request | Platform | Product | Platform → Product | Yes | No |
| Provisioning Result | Product | Platform | Product → Platform | Yes | No |
| Product Role Assignment | Product | Product clients | Product-local | Yes | No commercial authority |
| POS operational contracts | POS | POS clients | Product-local | Yes as required | Yes, Product only |

---

## 2. Required contract families

### Identity/Profile

Minimum:

```text
PlatformUserId
AccountProfileId
AccountClass
AllowedScope
IdentityStatus
ProfileStatus
Version
EffectiveAtUtc
```

### Organization/Membership

Minimum:

```text
PlatformOrganizationId
OrganizationDisplayName
OrganizationStatus
OrganizationMembershipId
MembershipStatus
OrganizationRole
Version
EffectiveAtUtc
```

### Plan/Pricing

Minimum:

```text
ProductId
ProductKey
PlanId
PlanKey
PlanVersion
DisplayName
MonthlyPrice
AnnualPrice
CurrencyCode
DisplayOrder
TrialAllowed
DefaultTrialDays
FeatureLimits
Status
```

### Subscription

Minimum:

```text
SubscriptionId
PlatformOrganizationId
ProductId
PlanId
PlanVersion
SubscriptionStatus
BillingCycle
AgreedPrice
CurrencyCode
TrialStartAtUtc
TrialEndAtUtc
CurrentPeriodStartAtUtc
CurrentPeriodEndAtUtc
GracePeriodEndAtUtc
PendingPlanId
PendingPlanEffectiveAtUtc
Version
```

### SaaS Payment Result

Minimum:

```text
SaaSPaymentId
SubscriptionId
PlatformOrganizationId
Provider
ProviderReference
IdempotencyKey
Purpose
Amount
CurrencyCode
Status
IsTest
FailureCode
OccurredAtUtc
Version
```

### Entitlement

Minimum:

```text
EntitlementId
PlatformOrganizationId
ProductId
SubscriptionId
PlanId
PlanVersion
EntitlementRevision
SubscriptionStatus
EntitlementStatus
ProvisioningStatus
FeatureGrants
Limits
EffectiveAtUtc
ExpiresAtUtc
RefreshByUtc
EventId
Version
```

### Provisioning

Request:

```text
ProvisioningRequestId
PlatformOrganizationId
ProductId
SubscriptionId
EntitlementId
RequestedOwnerUserId
IdempotencyKey
RequestedAtUtc
```

Result:

```text
ProvisioningRequestId
ProductInstanceId
Status
ProductOwnerRoleAssignmentId
FailureCode
Retryable
CompletedAtUtc
Version
```

---

## 3. Event index

| Event | Trigger | Consumer behavior |
|---|---|---|
| `IdentityProfileChangedV1` | profile/status changed | apply latest version; clear invalid sessions/caches |
| `OrganizationMembershipChangedV1` | membership/role changed | apply latest version; deny removed/suspended membership |
| `SubscriptionChangedV1` | trial, active, past due, suspended, cancelled, expired | recompute/publish entitlement |
| `SaaSPaymentResultRecordedV1` | payment result recorded | transition subscription once using idempotency |
| `PlanChangeRequestedV1` | upgrade/downgrade requested | retain pending change and audit |
| `PlanChangeAppliedV1` | effective change applied | update Plan snapshot and entitlement |
| `EntitlementChangedV1` | entitlement revision generated | Product applies monotonic revision |
| `ProductProvisioningRequestedV1` | entitlement enables new Product | Product provisions workspace |
| `ProductProvisioningCompletedV1` | workspace ready | Platform marks provisioning ready |
| `ProductProvisioningFailedV1` | provisioning fails | Platform exposes recoverable failure |

---

## 4. Idempotency and ordering

All mutation/event contracts include:

```text
IdempotencyKey
EventId
SourceVersion
OccurredAtUtc
CorrelationId
CausationId
```

Consumers must:

- ignore exact duplicates
- reject or ignore older versions
- reconcile version gaps
- avoid duplicate payment, period extension, Plan change, Entitlement revision, Organization creation, or Product provisioning
- record processing outcome for diagnostics

---

## 5. Compatibility

- Additive fields are preferred.
- Unknown optional fields are ignored safely.
- Unknown enum values fail closed for authorization.
- Breaking semantic changes require a new major contract version.
- Contract changes require producer and consumer tests.
- MAUI reconciliation is deferred until Phase 16 web/API contracts are stable and accepted.

---

## 6. Security classification

Commercial contracts may contain:

- stable identifiers
- Organization display name
- Plan and price information
- Subscription state
- Payment status and non-secret provider reference
- Entitlement grants and limits

They must not contain:

- passwords
- refresh tokens
- MFA secrets
- card data
- payment-provider secrets
- Personal Utang details
- POS customers, sales, inventory, credit transactions, or other operational payloads
