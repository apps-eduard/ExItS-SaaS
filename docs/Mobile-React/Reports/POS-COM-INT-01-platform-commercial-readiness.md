# POS-COM-INT-01 — Platform Commercial Integration Readiness

**Package:** POS-COM-INT-01  
**Branch:** `feat/pos-react-client`  
**Scope:** POS / Personal / Organization side only (no Platform Admin Web changes)  
**Status:** AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW

---

## 1. Platform → POS runtime commercial path

```
Platform login / session
  → organization context (Platform API)
  → access token (Bearer)
  → POS API: PosPlatformBearerMiddleware
       POST {PlatformAuth:BaseUrl}/api/v1/platform/auth/introspect
       Platform EvaluateProductAuthorization / entitlement snapshot
  → HttpContext.Items (UserId, OrganizationId, ProductAccessAllowed, SubscriptionStatus, EnabledFeatureCodes,
     ProductLocalRoleCode, MappedPosRoleCode, MembershipRole, OrganizationManagementAuthority)
  → PosCommercialAccess bound on IPosCommercialAccessAccessor
  → PosCommercialAccessMiddleware (header fallback in Dev/Testing only)
  → PosRoleResolutionMiddleware
  → Endpoint: PosCommercialScope.TryAuthorize(UtangCapability)
  → POS React: pos-capabilities.ts (UI hints from session grant; server authoritative)
```

### Authoritative code

| Stage | Location |
|-------|----------|
| Product authorization + entitlement snapshot | `src/Platform/ExItS.Platform.Application/Access/AccessUseCases.cs` (`EvaluateProductAuthorization`) |
| Token introspection | `src/Platform/ExItS.Platform.Application/Identity/AccessTokenUseCases.cs` |
| Bearer middleware | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosPlatformBearerMiddleware.cs` |
| Commercial accessor | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosCommercialAccess.cs` |
| Capability policy | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Application/Commercial/UtangCapabilityPolicy.cs` |
| React capability hints | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/src/access/pos-capabilities.ts` |
| React HTTP | Bearer token only — **no** `X-Pos-Subscription-Status` / `X-Pos-Feature-Grants` in production paths |

### Introspection fields verified

- `ProductAccessAllowed`
- `SubscriptionStatus`
- `EnabledFeatureCodes`
- `ProductLocalRoleCode`
- `MappedPosRoleCode`
- `MembershipRole`
- `OrganizationManagementAuthority`

---

## 2. Development fallback vs strict validation

### Default development / local validation (unchanged convenience)

When `CommercialValidation:Strict` is **false** (default):

1. **Bearer path** (`PosPlatformBearerMiddleware`): merges `UtangCapabilityPolicy.MergeWithDevelopmentDefaults` in Development/Testing or when `LocalValidation:Enabled` (non-Production).
2. **Missing subscription status** on introspection: upgraded to `Active` when grant merge is enabled.
3. **Header path** (`PosCommercialScope.BindFromRequest`): missing `X-Pos-*` headers in Development/Testing → `PosCommercialAccess.DevelopmentDefault` (Active + broad grant set).

This can make Starter-like orgs appear to support features the real Platform entitlement does not grant.

### Strict commercial validation mode (new)

**Config:** `CommercialValidation:Strict=true`  
**Helper:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosCommercialValidation.cs`

When strict in Development / Testing / Local Validation:

- Does **not** merge development default grants on bearer introspection.
- Does **not** default missing subscription status to `Active`.
- Does **not** use `DevelopmentDefault` when commercial headers are missing — uses `Unknown` (fail closed).
- Bearer with empty grants or missing status → `PosCommercialAccess.Unknown`.

**Production:** unchanged — already fail-closed via introspection; commercial dev headers ignored.

**Example (local strict testing):**

```json
"CommercialValidation": {
  "Strict": true
}
```

Documented default in `appsettings.Development.json` with `"Strict": false`.

---

## 3. Subscription status matrix

Policy: `UtangCapabilityPolicy.CanEnter`, `IsFullCommercialState`, `IsSuspended`.

| Status | POS entry / read (when granted) | Financial / catalog mutations |
|--------|----------------------------------|-------------------------------|
| Trialing | Allowed (`CanEnter`) | Allowed when `IsFullCommercialState` + feature grant |
| Active | Allowed | Allowed when grant present |
| GracePeriod | Allowed | Allowed when grant present |
| PastDue | Continuity read paths | New credit / mutations blocked (`IsFullCommercialState` false) |
| Cancelled | Continuity read paths | Mutations blocked |
| Expired | Continuity read paths | New credit blocked; repay/reverse per existing continuity rules |
| Suspended | **Blocked** (`CanEnter` false) | **Blocked** |

Tests: `PosStatementReceiptAndCommercialApiTests`, `PosSaleApiTests`, `PosReportingApiTests`, `PosCommercialIntegrationReadinessTests`.

---

## 4. Feature enforcement matrix

| Capability | Platform provides value? | POS API enforces? | React hides/disables? | Server fail-closed? | Test exists? |
|------------|---------------------------|-------------------|----------------------|---------------------|--------------|
| Customer credit / Utang | Yes — `customer-credit-*` feature codes from entitlement snapshot | Yes — `UtangCapabilityPolicy` + sale/credit endpoints | Partial — `pos-capabilities.ts` + checkout UX | Yes | Yes — `PosProductBasedUtangApiTests`, `PosCommercialIntegrationReadinessTests` |
| Advanced reports | Plan flag on Platform (`AdvancedReportsEnabled`) — **no separate POS feature code yet** | Partial — only `store-reports-view` grant; no advanced-only split | Explore POS UI only | N/A for advanced split | Partial |
| Export | Plan flag (`ExportEnabled`) — file export deferred | **No dedicated export endpoint enforcement yet** | Deferred footnote (Maui parity) | N/A | **BLOCKED** — export generation not implemented |
| Customer ordering | Yes — `store-customer-ordering` | Yes — seller capability + order endpoints | React capability helpers | Yes (non-Testing) | **GAP** — `PosSellerCustomerOrderingCapability` returns all-true in `Testing` env |
| Delivery | Yes — `store-delivery-orders` | Yes — paired with ordering capability | React capability helpers | Yes (non-Testing) | Same Testing bypass gap |

---

## 5. Numeric plan limits

Limits originate on Platform plan version (`maxActivePosDevices`, `maxBranches`, `maxActiveStaff`, `maxActiveBusinessTypes`) → subscription entitlement snapshot → Platform `RegisterCurrentDevice` / org setup use cases.

POS React consumes device capacity via Platform API (`getPosDeviceCapacity`) — **not hardcoded Starter/Growth/Pro**.

| Limit | Platform | POS React display | Server registration enforcement | Tests |
|-------|----------|-------------------|--------------------------------|-------|
| max POS devices | `PosOrganizationPlanLimits` | `OrgPosDevicesPage`, `DeviceRegisterPage` | Platform `RegisterCurrentDevice` | `RegisterCurrentDeviceCapacityTests`, `PosDeviceConcurrentRegistrationIntegrationTests`, new 3-device integration test |
| max branches | Platform org branch use cases | Branch UI | Platform | Org setup integration tests |
| max active staff | Platform staff invites | Staff UI | Platform | Platform unit tests |
| max business types | Platform | Business type UI | Platform | `OrganizationBusinessTypeEntitlementEnforcementTests` |

Expected Starter/Growth/Pro device counts (1 / 3 / 10) are **Platform plan configuration** — POS reads effective `allowed` from Platform.

---

## 6. Cross-org security

- Organization scope on every POS mutation via `X-Pos-Organization-Id` (dev) or bearer-bound org (production).
- Cross-org data access returns `404` / scope denial — not commercial leakage.
- Commercial grants are per-request; org A headers/token never authorize org B resources.
- Device capacity counted per Platform organization — org A limit never includes org B devices.

Test: `PosCommercialIntegrationReadinessTests.Cross_org_commercial_headers_do_not_authorize_other_org` + existing customer/credit cross-org tests.

---

## 7. Session / cache refresh

| Mechanism | Behavior |
|-----------|----------|
| Bearer introspection | Per POS API request when Authorization header present |
| Introspection cache | 45s in-memory TTL per token hash (`PlatformTokenIntrospectionClient`) |
| React session grant | Stored in `pos-session-grant`; refreshed on login, org context change, token refresh flows in `platform-auth-client.ts` |
| Workspace switch | Selecting a new org re-runs Platform org context + session grant fetch — commercial state tied to new org token/context |
| Plan change while POS open | Next introspection after cache TTL (≤45s) or manual reload/re-login picks up new Platform entitlement; React UI hints refresh when session grant refetched |

No polling added. Full Platform Admin → POS E2E refresh: **READY_FOR_PLATFORM_ADMIN_E2E**.

---

## 8. Platform Admin E2E dependencies

| Scenario | Status |
|----------|--------|
| Platform Admin sets Growth → 3 devices → POS login → register 3 → 4th blocked | **READY_FOR_PLATFORM_ADMIN_E2E** (Platform registration tests prove limit; POS uses Platform capacity API) |
| Upgrade to Pro → more devices | **READY_FOR_PLATFORM_ADMIN_E2E** |
| Suspend subscription → POS mutations blocked | **READY_FOR_PLATFORM_ADMIN_E2E** (POS enforcement proven via header/bearer tests) |
| Reactivate → access restored | **READY_FOR_PLATFORM_ADMIN_E2E** |
| Platform Admin commercial UI configuration | **BLOCKED_BY_PLATFORM_ADMIN** (Agent 2 scope) |

---

## 9. Tests added / referenced

### New

- `tests/ExItS.PinoyBusinessPOS.UnitTests/Api/PosCommercialValidationTests.cs`
- `tests/ExItS.PinoyBusinessPOS.IntegrationTests/PosCommercialIntegrationReadinessTests.cs`
- `tests/ExItS.Platform.IntegrationTests/PosDeviceConcurrentRegistrationIntegrationTests.cs` — `Growth_like_three_device_plan_blocks_fourth_registration`

### Existing regression (unchanged)

- Bearer/production hardening: `PosProductionHardeningApiTests`
- Commercial continuity: `PosStatementReceiptAndCommercialApiTests`
- Utang/cash/GCash: `PosSaleApiTests`, `PosProductBasedUtangApiTests`
- Reports grant: `PosReportingApiTests`
- Device concurrency: Platform integration + unit capacity tests
- RMAP-21 offline: Vitest offline suite

---

## 10. Known gaps / blockers

1. **Export / advanced reports split** — Platform plan flags exist; POS has no separate `store-reports-export` / advanced feature codes or export API enforcement yet.
2. **Customer ordering Testing bypass** — `PosSellerCustomerOrderingCapability` forces enable in `Testing` environment; strict entitlement tests for ordering must run against Staging/Local Validation or non-Testing harness.
3. **Platform Admin E2E** — Full cross-app flow awaits Agent 2 commercial management UI + shared test fixtures.

---

## 11. Files changed (POS-COM-INT-01)

- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosCommercialValidation.cs` (new)
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosPlatformBearerMiddleware.cs`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Common/PosCommercialAccess.cs`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/appsettings.Development.json`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/src/features/checkout/checkout-sale-errors.ts`
- `tests/ExItS.PinoyBusinessPOS.UnitTests/Api/PosCommercialValidationTests.cs` (new)
- `tests/ExItS.PinoyBusinessPOS.IntegrationTests/PosCommercialIntegrationReadinessTests.cs` (new)
- `tests/ExItS.Platform.IntegrationTests/PosDeviceConcurrentRegistrationIntegrationTests.cs`
- `docs/Mobile-React/Reports/POS-COM-INT-01-platform-commercial-readiness.md` (this file)

**PLATFORM_ADMIN_REACT_MODIFIED=NO**
