# POS-COM-INT-02 — Feature Entitlement Contract Closure

**Package:** POS-COM-INT-02  
**Branch:** `feat/pos-react-client`  
**Scope:** POS / Personal / Organization side only  
**Status:** AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW

---

## 1. Corrected Platform feature codes

Canonical Platform codes (see `FeatureCode` in Platform domain):

| Code | Platform constant |
|------|-------------------|
| `store-advanced-reports` | `FeatureCode.StoreAdvancedReports` |
| `store-export` | `FeatureCode.StoreExport` |
| `store-customer-ordering` | `FeatureCode.StoreCustomerOrdering` |
| `store-delivery-orders` | `FeatureCode.StoreDeliveryOrders` |
| `store-reports-view` | basic / classic reports |
| `customer-credit-create` | Utang creation |

POS mirrors these in `PosFeatureCodes` (Application layer). **No duplicate codes invented.**

---

## 2. Final entitlement matrix

| Capability | Platform grant | POS server | React UI hint | Classification |
|------------|----------------|------------|---------------|----------------|
| Customer credit / Utang | `customer-credit-*` | `UtangCapabilityPolicy` + sale/credit endpoints | `canCreateCredit` checks grant | **IMPLEMENTED_AND_ENFORCED** |
| Basic reports | `store-reports-view` | Classic `/reports/sales`, `/utang`, `/inventory`, `/expenses` | `canViewReports` | **IMPLEMENTED_AND_ENFORCED** |
| Advanced / operational reports | `store-advanced-reports` | `/reports/*` operational endpoints via `TryAuthorizeReport` + `ViewAdvancedReports` | `canViewAdvancedReports` + `report-access.ts` | **IMPLEMENTED_AND_ENFORCED** |
| Export | `store-export` | `UtangCapability.ExportData` reserved; **no export API/UI** | `canExportData` reserved | **PRODUCT_FEATURE_NOT_IMPLEMENTED** (entitlement contract ready) |
| Customer ordering | `store-customer-ordering` | `PlaceCustomerOrders` / `ViewCustomerOrders` + strict capability resolver | `canViewCustomerOrders` | **IMPLEMENTED_AND_ENFORCED** |
| Delivery | `store-delivery-orders` | `ResolveAsync` delivery flag + order use cases | `canManageCustomerOrders` | **IMPLEMENTED_AND_ENFORCED** |
| Device limits | Platform plan limits | Platform `RegisterCurrentDevice` | `getPosDeviceCapacity` | **IMPLEMENTED_AND_ENFORCED** (Platform contract) |

---

## 3. Advanced reports split

**Basic (Starter allowed when `store-reports-view` granted):**

- Dashboard (`store-dashboard-view`)
- Classic reports: sales, utang, inventory, expenses

**Advanced (requires `store-advanced-reports`):**

- All operational report endpoints under `/api/v1/pos/reports/overview`, `sales-summary`, `shifts-summary`, inventory/purchasing operational family, etc.

React Reports hub operational sections hidden without `store-advanced-reports`; classic section remains when `store-reports-view` is present.

---

## 4. Export

- Platform plan flag → `store-export` grant on Growth/Pro (`EnsureMvpPosPlans.BuildGrants`).
- Maui/React show deferred export footnote only — **no CSV/PDF/Excel generation**.
- `UtangCapability.ExportData` exists for future endpoints; nothing to authorize today.
- **Not a subscription-enforcement blocker** — classified `PRODUCT_FEATURE_NOT_IMPLEMENTED`.

---

## 5. Strict mode — ordering Testing bypass fix

`PosSellerCustomerOrderingCapability`:

- `CommercialValidation:Strict=false` + `Testing` → legacy all-true convenience (unchanged for unrelated tests).
- `CommercialValidation:Strict=true` + `Testing` → uses `IPosCommercialAccessAccessor` grants (`store-customer-ordering`, `store-delivery-orders`).

Production unchanged.

---

## 6. Plan contract (feature codes only)

MVP plan grants from `MvpPosPlanCatalog` / `EnsureMvpPosPlans.BuildGrants`:

| Plan | Customer credit | Advanced reports | Export |
|------|-----------------|------------------|--------|
| Starter | disabled | disabled | disabled |
| Growth | enabled | enabled | enabled |
| Pro | enabled | enabled | enabled |

Ordering/delivery: granted on all commercial plans via `BasicStoreFeatureCodes` (V1 — no plan differentiation).

POS authorization never branches on plan name.

---

## 7. Commercial error UX

`src/access/pos-commercial-errors.ts`:

- `pos.commercial.capability_denied` → `commercial.notIncludedInPlan`
- `pos.commercial.access_unknown` → `commercial.accessUnavailable`
- Suspended / product access / device capacity mapped to existing keys

---

## 8. Tests added

- `UtangCapabilityPolicyTests` — advanced reports, export, ordering grants
- `PosSellerCustomerOrderingCapabilityTests` — strict vs non-strict Testing bypass
- `PosCommercialIntegrationReadinessTests` — operational vs classic reports, ordering strict, GCash without credit
- `pos-commercial-errors.test.ts`
- COM-INT-01 tests remain green

---

## 9. Platform Admin E2E

| Scenario | Status |
|----------|--------|
| Starter vs Growth report entitlement via Platform Admin | **READY_FOR_PLATFORM_ADMIN_E2E** (strict POS tests simulate grant sets) |
| Export after product feature ships | **PRODUCT_FEATURE_NOT_IMPLEMENTED** |
| Full cross-app suspend/reactivate | **PLATFORM_ADMIN_E2E_PENDING** (Agent 2 UI) |

---

## 10. Files changed

- `UtangCapabilityPolicy.cs` — `StoreAdvancedReports`, `StoreExport`, `ViewAdvancedReports`, `ExportData`
- `ReportingEndpoints.cs` — operational reports require `ViewAdvancedReports`
- `PosSellerCustomerOrderingCapability.cs` — strict mode respects grants in Testing
- `pos-capabilities.ts`, `report-access.ts`, `pos-commercial-errors.ts`
- i18n commercial message keys (en + PH locales)
- Tests + this document
- Updated `POS-COM-INT-01-platform-commercial-readiness.md`

**PLATFORM_ADMIN_REACT_MODIFIED=NO**
