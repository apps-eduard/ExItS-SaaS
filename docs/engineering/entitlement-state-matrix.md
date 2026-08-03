# Entitlement Projection State Matrix

[Contracts](platform-product-contracts.md) | [Capability boundary](platform-product-capability-boundary.md) | [Authorization](authorization-matrix.md)

**Version:** 2.0  
**Status:** Authoritative  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Commercial state to entitlement behavior

| Subscription state | Entitlement | Product entry | New paid writes | Continuity / historical access | Required action |
|---|---|---|---|---|---|
| Trialing | Enabled | Allow | Allow per trial Plan grants | Allow | Convert before trial end |
| Active | Enabled | Allow | Allow per Plan grants | Allow | Renew normally |
| PastDue, within grace | Enabled with grace facts | Allow | Restrict according to grace policy | Allow | Recover payment |
| PastDue, grace expired | Suspended | Limited or deny according to Product policy | Deny | Retained history according to policy | Recover payment |
| Suspended | Suspended | Deny normal operation | Deny | Administrative recovery only unless explicit continuity rule | Resolve suspension |
| Cancelled | Disabled or Suspended | Historical/continuity only | Deny | Retain data | Reactivate with explicit policy |
| Expired | Expired or Suspended | Historical/continuity only | Deny | Retain data | Subscribe |
| Missing/Invalid | Not trusted | Deny | Deny | Safe metadata only | Initialize or reconcile |

No state automatically deletes Product data, Product roles, or Product Instance history.

---

## 2. Projection trust states

| Projection state | Trusted? | Reads | Writes | Refresh behavior | Audit |
|---|---:|---|---|---|---|
| Current | Yes | According to grants | According to grants | Background refresh before `RefreshByUtc` | Normal Product audit |
| Refresh due | Yes | According to grants | According to grants, subject to risk policy | Refresh promptly | Log refresh attempt |
| Temporarily stale | Conditional | Existing data may continue under policy | Fail closed for risky or newly paid operations unless explicitly permitted | Retry when Platform reachable | Log stale use and reason |
| Grace | Yes | Allow | According to grace grants | Surface billing recovery | Log grace enforcement |
| Suspended | Status trusted | Limited or deny | Deny protected writes | Refresh and expose recovery path | Audit blocked attempts |
| Expired | Status trusted | Historical/continuity according to Product policy | Deny new paid writes | Refresh and expose subscribe path | Audit blocked attempts |
| Invalid | No | Safe metadata only | Deny | Reconcile | Alert and audit |
| Unsupported version | No | Safe metadata only | Deny | Upgrade consumer or reconcile | Alert administrators |
| Reconciliation required | Partial | Prefer read-only | Deny risky writes | Pull authoritative snapshot | Audit reconciliation |
| Never initialized | No | No paid Product access | Deny | Initialize from Platform | Audit first initialization |

---

## 3. Trial lifecycle

Current approved commercial onboarding trial:

```text
Plan: Business
Duration: 14 days
Owner: Platform Subscription
```

Start:

```text
Start a Business
→ Subscription Trialing
→ TrialStartAtUtc / TrialEndAtUtc
→ Entitlement Enabled
→ Provisioning Ready
```

Conversion:

```text
Trialing
→ successful fake or real SaaS payment
→ Active
→ CurrentPeriodStartAtUtc / CurrentPeriodEndAtUtc
→ Entitlement remains Enabled
→ EntitlementRevision increases
```

Expiry:

```text
Trialing
→ TrialEnd reached
→ Expired
→ new paid writes blocked
→ Product data retained
```

A second trial for the same Organization and Product is denied unless an authorized, audited exception policy is implemented.

---

## 4. Renewal lifecycle

```text
Active
→ renewal succeeds
→ extend current period
→ remain Active
→ publish new Entitlement revision
```

```text
Active
→ renewal fails
→ PastDue
→ set GracePeriodEndAtUtc
→ publish grace entitlement
```

```text
PastDue
→ recovery payment succeeds
→ Active
→ clear past-due/grace state
→ extend period
→ publish new Entitlement revision
```

```text
PastDue
→ grace expires
→ Suspended
→ deny protected writes
→ retain Product data
```

---

## 5. Upgrade lifecycle

```text
Active or Trialing
→ request upgrade
→ preview target Plan and price
→ payment succeeds when required
→ apply target Plan
→ snapshot agreed price
→ publish new Entitlement revision
→ preserve Product data
```

Payment decline or failure:

```text
upgrade payment fails
→ current Plan remains authoritative
→ no entitlement expansion
→ failed Payment retained and audited
```

Duplicate payment event:

```text
same provider event received again
→ no duplicate Plan change
→ no duplicate period extension
→ no duplicate Entitlement revision for the same transition
```

---

## 6. Downgrade lifecycle

```text
Active
→ request downgrade
→ calculate usage conflicts
→ store PendingPlanId and PendingPlanEffectiveAtUtc
→ current Plan remains active until effective date
```

At effective date:

```text
apply target Plan
→ snapshot target agreed price
→ publish new Entitlement revision
→ retain existing over-limit data
→ block new actions that further exceed target limits
```

Never automatically:

- delete branches
- remove staff
- delete customers
- delete transactions
- delete Product roles
- purge Product Instance data

---

## 7. Product continuity principles

Pinoy Business POS may provide policy-defined continuity for existing financial obligations after cancellation or expiry, but continuity must be explicitly represented by Product feature grants.

Default categories:

| Category | Default |
|---|---|
| Create new sale, credit, branch, staff, or other paid expansion | Fail closed |
| View existing history and balances | May continue when explicitly granted |
| Accept repayment of existing Organization-owned debt | May continue when explicitly granted |
| Refund or reversal | Product-specific permission and audit required |
| Administrative elevation | Fail closed |
| Missing/unknown entitlement | Fail closed |

Product entry alone does not grant operations. Every protected operation evaluates Product-local role and effective feature grants.

---

## 8. Offline rules

- A device may operate from the last trusted projection only within explicit freshness policy.
- Missing, invalid, unsupported, or never-initialized projection fails closed.
- Offline queued mutations must be reauthorized before synchronization.
- A queued write created during valid access must not sync after the authoritative state forbids it.
- Suspension, expiry, or downgrade does not delete the local database.
- Organization switch or sign-out clears session-bound caches and in-memory carts.
- Cross-Organization queued work is never replayed into another Organization.
