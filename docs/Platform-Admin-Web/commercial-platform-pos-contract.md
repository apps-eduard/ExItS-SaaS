# Platform → POS commercial contract (React Admin E2E)

**Status:** AUTHORITATIVE for commercial testing  
**Audit HEAD (Platform Admin / Platform API):** `525bae3633fb7fde1bbc9b855435a05f5f616c09`  
**POS React inspected (read-only):** `feat/pos-react-client` @ `42d487d42c0d9e3e6592cafcb2259c24655dbb23`  
**Companion docs:** [implementation plan](./commercial-subscription-implementation-plan.md) · [E2E matrix](./commercial-e2e-validation-matrix.md) · [audit](./Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md)

Do not describe Development/Testing fallbacks as production security.

---

## 1. Product vs Plan vs POS

```text
Product (pinoy-business-pos)
  └── Plan (starter | growth | pro) + PlanVersion (Published grants)
        └── Organization Subscription (status + plan bind)
              └── EntitlementSnapshot (grants + limits + subscriptionStatus)
                    └── Platform access token / introspect
                          └── POS bearer + PosCommercialAccess
                                └── POS product-local role (separate)
```

Product and Plan **must not** contain operational POS data (sales, stock, customers).

---

## 2. Runtime path (actual)

```text
Platform login (cookie session `.ExItS.Platform.Auth`)
  → set organization (+ optional branch) context
  → POST /api/v1/platform/auth/token
       productCode = pinoy-business-pos
  → EvaluateProductAuthorization
       entitlement snapshot (refresh if stale)
       product-local role grant
       OR OrganizationManagementAuthority (Owner/Admin without selling role)
  → opaque Platform access token
  → POS API Authorization: Bearer
  → PosPlatformBearerMiddleware
       POST /api/v1/platform/auth/introspect
  → PosCommercialAccessMiddleware
  → PosRoleResolutionMiddleware
  → endpoint: CommercialAccessGuard + PosRoleAuth
```

Pipeline order on POS API: bearer → commercial → role.

### Field dictionary

| Field | Where it appears | Meaning |
|---|---|---|
| `ProductAccessAllowed` | Issue DTO + introspect | `true` when `CanOperate` **or** org-management authority |
| `SubscriptionStatus` | **Introspect only** | Snapshot / live evaluate status |
| `EnabledFeatureCodes` | **Introspect only** | Enabled grant code **strings** (no numeric limits) |
| `ProductLocalRoleCode` | Issue + introspect | `Owner` / `Manager` / `Cashier` / `Viewer` (cleared on org-mgmt path) |
| `MappedPosRoleCode` | Issue + introspect | Owner→Owner, Manager→StoreManager, Cashier→Cashier, Viewer→ReportingUser |
| `MembershipRole` | Issue + introspect | e.g. `OrganizationOwner` |
| `OrganizationManagementAuthority` | Issue + introspect | Membership Owner/Admin; **not** CreateSale |

React session-grant types may include optional `featureCodes`, but the **issue DTO does not emit them**. POS API commercial features arrive per-request via introspect. Device **numeric** limits do **not** ride this payload.

---

## 3. Product entry vs continuity (Pinoy Business POS)

`ProductAccessEligibility.CanEnterPinoyBusinessPos`:

| SubscriptionStatus | Product entry |
|---|---|
| Trialing, Active, GracePeriod | Allow |
| PastDue, Cancelled, Expired | Allow only if continuity grants enabled: `customer-credit-view` or `customer-credit-repay` |
| Suspended | **Deny** |
| Unknown | Deny |

Other products: Trialing/Active only. New product-access **grants**: Trialing/Active only.

Composer adjustments (`EntitlementSnapshotComposer.ApplySubscriptionStatusAdjustments`):

- PastDue / Suspended / Cancelled: force `customer-credit-create` **disabled**
- Cancelled: disable all enabled grants except view/repay continuity
- GracePeriod: keeps base grants (not extra-restricted in composer)

---

## 4. Development / Testing vs Production

| Mechanism | When | Behavior | Production? |
|---|---|---|---|
| Commercial headers `X-Pos-Subscription-Status`, `X-Pos-Feature-Grants` | Dev/Testing only | Missing both → `DevelopmentDefault` (Active + full grants). Production ignores headers → Unknown | **No** |
| Bearer grant merge | Dev/Testing **or** `LocalValidation:Enabled && !Production` | Union Platform grants with `DefaultDevelopmentGrants` | **No** |
| Role Dev bootstrap | Dev/Testing | Auto-assign Owner if no mapped role | **No** |
| Role skip without actor | legacy Dev | Role check skipped | **No** |

**How to test TRUE plan enforcement**

1. Do not treat Local Validation default as proof that Starter disabled credit.
2. Prefer a profile where `ShouldMergeDevelopmentGrants` is false (Production-shaped POS API **or** an explicitly authorized test flag — do not invent the flag in Admin packages).
3. If merge cannot be disabled in the current harness, record **TEST HARNESS GAP** and use Platform snapshot + device-capacity APIs as the source of truth for limits (device limits do not use the merged grant list).

Device capacity is a **Platform org API** using live `Plan.MaxActivePosDevices` after resolving an **active-like** POS subscription. It is the correct proof for the 3-device Growth test.

---

## 5. Device limit contract (critical)

```text
Plan.MaxActivePosDevices
  → also mirrored as grant plan-max-active-pos-devices on published version
  → EntitlementSnapshot copies plan-version grants (quantity)
  → POS Device Management does NOT read the token for the number
  → GET Platform pos-devices capacity → { used, allowed }
  → POST register → 409/error PosDeviceCapacityExceeded when used >= allowed
```

`PosOrganizationPlanLimits.ResolveAsync` requires a subscription that `Subscription.IsActiveLike`:

Trialing, Active, GracePeriod, PastDue, **Suspended**.

Therefore:

- Growth with 3 devices: register 1..3 PASS; 4th denied — **PASS TODAY** (Platform + POS React UI) if a Growth subscription exists.
- Upgrade Growth → Pro: capacity reads **current plan**, so Allowed becomes 10 after upgrade **without** token refresh — **PASS TODAY** on backend once upgrade is performed (React Admin cannot perform upgrade today).
- Suspend: POS **selling/entry** denied; device register/capacity may **still succeed** — **BACKEND NUANCE**. Document in tests; do not claim device APIs fail closed on Suspended unless the resolver is changed in a later authorized backend package.

React Platform Admin has **no** org device CRUD. That is acceptable: device management lives in POS React.

---

## 6. Other limits and features

| Concern | Plan column / grant | Enforced where | POS React |
|---|---|---|---|
| Branches | `MaxBranches` / `plan-max-branches` | Platform branch create | capacity UX weaker than devices |
| Staff | `MaxActiveStaff` / `plan-max-active-staff` | Plan-change **preview** warning; **invite-time check not found** | marketing/explore only |
| Business types | `MaxActiveBusinessTypes` / `plan-max-active-business-types` | activation use cases | Personal/org activation |
| Customer credit | booleans → three credit FeatureCodes | POS `UtangCapability` | role UI; features via introspect (Dev merge risk) |
| Advanced reports | `AdvancedReportsEnabled` → `store-advanced-reports` | catalog/snapshot only | no runtime gate found |
| Export | `ExportEnabled` → `store-export` | catalog/snapshot only | no runtime gate found |
| Ordering | `store-customer-ordering` | POS customer-order capabilities | role UI |
| Delivery | `store-delivery-orders` | ManageCustomerOrders needs ordering **and** delivery | branch fulfillment |

MVP seed grants **all** `BasicStoreFeatureCodes` including ordering and delivery on Starter, Growth, and Pro. Feature differentiation for those two codes is **not** plan-based today.

---

## 7. Org management authority vs selling

Owner/Admin may receive `ProductAccessAllowed=true` with `OrganizationManagementAuthority` and **cleared** product-local roles. Comments and phase docs deny CreateSale without a selling role. Commercial E2E for checkout must use a mapped POS role, not org-admin bypass.

---

## 8. Audit events (Platform)

Use these codes when verifying Activity after Admin actions (server-emitted):

| Action | Code |
|---|---|
| Trial started | `platform.subscription.trial_started` |
| Trial converted | `platform.subscription.trial_converted` |
| Paid started | `platform.subscription.paid_started` |
| Activated | `platform.subscription.activated` |
| Grace | `platform.subscription.grace_period_entered` |
| Past due | `platform.subscription.past_due_marked` |
| Suspended | `platform.subscription.suspended` |
| Reactivated | `platform.subscription.reactivated` |
| Cancelled | `platform.subscription.cancelled` |
| Expired | `platform.subscription.expired` |
| Upgraded | `platform.subscription.upgraded` |
| Downgrade scheduled | `platform.subscription.downgrade_scheduled` |
| Pending plan applied | `platform.subscription.pending_plan_applied` |
| Plan change previewed | `platform.subscription.plan_change_previewed` |
| Snapshot generated | `platform.entitlement.snapshot_generated` |
| Snapshot reconciled | `platform.entitlement.snapshot_reconciled` |
| Override created/revoked | `platform.feature_override.created` / `.revoked` |
| Manual payment | `platform.payment.created` / `.confirmed` / `.rejected` / `.voided` |

---

## 9. What React Admin must provide for this contract to be testable

Today: **inspection only**.

Required to run the spine from Admin:

1. Start trial / paid subscribe (PA-COM-04 + 06)
2. Upgrade Growth → Pro (04 + 06)
3. Suspend / reactivate (04)
4. Visible entitlement snapshot after each change (05, or rely on server auto-generate + existing read UI)
5. A documented POS test mode that does not merge Development grants (07)

No POS React changes in PA-COM packages unless a **separate** POS authorization is given.
