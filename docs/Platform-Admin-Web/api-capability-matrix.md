# Platform Admin Web — API Capability Matrix (DOC-09)

**Status:** Evidence-based backend audit (documentation only)

## Audit evidence anchor

- `API_AUDIT_MAIN_SHA = 618a7b61711a2baee5a1589bd49bbd3312eb4eec` (from `git rev-parse origin/main`)
- Audit scope: **routes/endpoints and authorization requirements** found in `src/Platform/ExItS.Platform.Api` (with confirmation for DTOs/types in `src/Platform/ExItS.Platform.Application` / Admin client where helpful).

## Capability requirements covered

All `PWEB-CAP-*` requirements extracted from:
- `Screens/core-administration-screens.md` (DOC-06)
- `Screens/commercial-and-product-screens.md` (DOC-07)
- `Screens/governance-operations-settings-screens.md` (DOC-08)

Total capability IDs: **75**.

## Classification meanings (must be evidence-based)

- `EXISTS`: required capability is available via an appropriate API / contract in `origin/main`
- `PARTIAL`: capability exists but does not fully satisfy the screen requirement (e.g., dev-only, missing listing context, missing filters)
- `APPLICATION-ONLY`: internal application capability exists but is not exposed via browser-safe API contract
- `MISSING`: no verified capability in `origin/main` API evidence
- `NOT-REQUIRED`: evidence review determined UI requirement unnecessary after backend evidence
- `EXTERNAL/DEFERRED`: capability depends on another approved future Platform integration
- `UNKNOWN`: evidence insufficient (no guessing)

## Matrix (all 63 capabilities)

### Core Administration (DOC-06)

#### `PWEB-CAP-ORG-LIST`
- Screen: Organizations List
- Need: list Platform organizations (paged, filterable)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanListOrganizationsAsync` -> `PlatformPermission.ViewPortfolio` or `PlatformPermission.ManageOrganizations`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationEndpoints.cs`
- Pagination/filter support: `page`, `pageSize`, `status`, `search`, `sortBy`, `sortDesc`
- Audit requirement: read-only (no audit write in endpoint)
- Gap owner / priority: Platform API / High
- Notes: list supports server-side sorting + search.

#### `PWEB-CAP-ORG-GET`
- Screen: Organization Workspace bootstrap / Detail
- Need: fetch a single organization record
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync` -> includes `PlatformPermission.ViewPortfolio` (or `ManageOrganizations`) or trusted active membership
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: no cross-org disclosure; orgAuthz enforces access.

#### `PWEB-CAP-ORG-CREATE`
- Screen: Organizations List (optional create)
- Need: create/register a new Platform organization
- Required operation: create
- Status: **PARTIAL**
- Verified API route (method): `POST /api/v1/platform/organizations`
- Relevant auth requirement: `PlatformPermission.ManageOrganizations` (plus environment gate)
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / Medium
- Notes: endpoint exists but **runtime organization creation disabled outside `Testing`** (`ApplicationErrorCodes.RuntimeOrganizationCreationDisabled`).

#### `PWEB-CAP-ORG-OVERVIEW-DATA`
- Screen: Organization Workspace / Overview tab
- Need: overview profile/status (including commercial context for the org)
- Required operation: read overview data
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/admin/organizations/{organizationId}/commercial-summary`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync` (via `AdminEndpoints`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Admin/AdminEndpoints.cs` (+ DTO shape in `src/Platform/ExItS.Platform.Application/Admin/AdminPortfolioModels.cs`)
- Pagination/filter support: N/A (single org payload)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: returned payload includes `subscriptions`, `payments`, and `latestEntitlements` (supports overview/status without exposing secrets).

#### `PWEB-CAP-ORG-ACTIVITY-AUDIT`
- Screen: Organization Workspace / Activity/Audit tab
- Need: org-scoped governance audit timeline
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/audit`
- Relevant auth requirement: `PlatformMembershipAuthz.EnsureCanViewOrganizationAuditAsync` -> `PlatformPermission.ViewAuditRecords` (+ membership/owner path)
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationAuditEndpoints.cs`
- Pagination/filter support: `fromUtc`, `toUtc`, `actor`, `action`, `targetType`, `outcome`, `branchId`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: branchId is mapped to `targetType=OrganizationBranch`.

#### `PWEB-CAP-BRANCH-LIST`
- Screen: Branches Administration tab
- Need: list branches for an organization
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/branches`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/BranchAndDeviceEndpoints.cs`
- Pagination/filter support: (signature shows no explicit paging parameters; list details depend on `ListBranches` use case)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: listing requires a Platform-authenticated actor; rejects if actor PlatformUserId missing.

#### `PWEB-CAP-IDENTITY-LIST`
- Screen: Platform Users / Identity Administration list
- Need: list Platform user identities
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/users`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/IdentityEndpoints.cs`
- Pagination/filter support: `status`, `search`, `directory`, `sortBy`, `sortDesc`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-IDENTITY-GET`
- Screen: Platform Users / Identity Administration detail
- Need: fetch a single Platform identity
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/users/{userId}`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/IdentityEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-IDENTITY-ROLE-ASSIGNMENTS`
- Screen: Platform Users / Identity Administration detail (role assignments)
- Need: view role assignments for a given Platform user
- Required operation: list role assignments
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/authorization/assignments?platformUserId={...}`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Authorization/AuthorizationEndpoints.cs`
- Pagination/filter support: `page`, `pageSize`, optional filters (`platformUserId`, `role`, `organizationId`, `status`)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ORGANIZATION-INVITATIONS-LIST`
- Screen: Organization Workspace / People/Memberships
- Need: list organization staff invitations (sanitized; never returns accept tokens)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/invitations`
- Relevant auth requirement: `PlatformMembershipAuthz.EnsureCanManageMembershipsAsync` -> `PlatformPermission.ManageMemberships`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/InvitationEndpoints.cs`
- Pagination/filter support: `status`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: list response explicitly nulls `AcceptToken` values.

#### `PWEB-CAP-MEMBERSHIP-LIST`
- Screen: Organization Workspace / People/Memberships
- Need: list active members (paged)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/members`
- Relevant auth requirement: `PlatformMembershipAuthz.EnsureCanManageMembershipsAsync` -> `PlatformPermission.ManageMemberships`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/MembershipEndpoints.cs`
- Pagination/filter support: `status`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-MEMBERSHIP-INVITE`
- Screen: Organization Workspace / People/Memberships (optional invite)
- Need: invite staff to an organization
- Required operation: create invitation
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/organizations/{organizationId}/invitations`
- Relevant auth requirement: `PlatformMembershipAuthz.EnsureCanManageMembershipsAsync` -> `PlatformPermission.ManageMemberships`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/InvitationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `membershipAuthz.Inner.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / High
- Notes: invite creation enforces Organization Owner/Staff role constraints.

#### `PWEB-CAP-MEMBERSHIP-REVOKE`
- Screen: Organization Workspace / People/Memberships (optional revoke)
- Need: revoke a membership (or cancel a staff invitation via revoke path)
- Required operation: revoke
- Status: **EXISTS**
- Verified API route(s) (method): `POST /api/v1/platform/memberships/{membershipId}/revoke` (membership revoke) and `POST /api/v1/platform/invitations/{invitationId}/revoke` (invite revoke)
- Relevant auth requirement: `PlatformMembershipAuthz.EnsureCanManageMembershipsAsync` -> `PlatformPermission.ManageMemberships`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/MembershipEndpoints.cs` and `src/Platform/ExItS.Platform.Api/Organizations/InvitationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `membershipAuthz.Inner.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ORG-PRODUCT-ACCESS`
- Screen: Organization Workspace / Products/Access tab (product access + entitlement-driven access summary)
- Need: product access summary for the org (entitlement-driven)
- Required operation: read product access summary
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/admin/organizations/{organizationId}/commercial-summary`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync` (includes `PlatformPermission.ViewPortfolio` or trusted membership)
- Evidence source: `src/Platform/ExItS.Platform.Api/Admin/AdminEndpoints.cs` (+ DTO mapping in `src/Platform/ExItS.Platform.Application/Admin/AdminPortfolioModels.cs`)
- Pagination/filter support: N/A (single org payload)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: uses `latestEntitlements` (includes `productCode` + subscription status). No product operational workflow data is included.

#### `PWEB-CAP-ORG-SUBSCRIPTION-COMMERCIAL`
- Screen: Organization Workspace / Subscription tab
- Need: organization subscription commercial state
- Required operation: list subscriptions
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/subscriptions`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync`
- Evidence source: `src/Platform/ExItS.Platform.Api/Subscriptions/SubscriptionEndpoints.cs`
- Pagination/filter support: `status`, `search`, `isTrial`, `planId`, `productCode`, `sortBy`, `sortDesc`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ORG-ENTITLEMENTS`
- Screen: Organization Workspace / Entitlements tab
- Need: entitlement snapshot history/details for org/product
- Required operation: list entitlements
- Status: **EXISTS**
- Verified API route(s) (method):
  - `GET /api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots`
  - (detail access exists via snapshot-specific routes; see `PWEB-CAP-ENTITLEMENT-GET`)
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync`
- Evidence source: `src/Platform/ExItS.Platform.Api/Entitlements/EntitlementEndpoints.cs`
- Pagination/filter support: `page`, `pageSize` (history list)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ORG-BILLING-RECORDS`
- Screen: Organization Workspace / Billing tab
- Need: organization manual SaaS payment records
- Required operation: list billing records
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}/payments`
- Relevant auth requirement: `PlatformPermission.ManageManualPayments`
- Evidence source: `src/Platform/ExItS.Platform.Api/Payments/PaymentEndpoints.cs`
- Pagination/filter support: `status`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

### Commercial/Product Admin (DOC-07)

#### `PWEB-CAP-PRODUCT-LIST`
- Screen: Product Catalog
- Need: list catalog products (platform control-plane metadata)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/catalog/products`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: `status`, `search`, `sortBy`, `sortDesc`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PRODUCT-GET`
- Screen: Product Detail
- Need: product metadata by id
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/catalog/products/{id:guid}`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PRODUCT-CREATE`
- Screen: Product Catalog (optional; create/register)
- Need: create a new catalog product
- Required operation: create
- Status: **PARTIAL**
- Verified API route (method): `POST /api/v1/platform/catalog/products`
- Relevant auth requirement: `PlatformPermission.ManageCatalog` (plus environment gate)
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / Medium
- Notes: endpoint succeeds only in `Testing` environment; returns `RuntimeProductCreationDisabled` otherwise.

#### `PWEB-CAP-PRODUCT-MANAGE`
- Screen: Product Catalog / Product Detail (activate/deactivate/retire)
- Need: change product lifecycle state
- Required operation: manage lifecycle
- Status: **EXISTS**
- Verified API route(s) (method):
  - `POST /api/v1/platform/catalog/products/{id}/activate`
  - `POST /api/v1/platform/catalog/products/{id}/deactivate`
  - `POST /api/v1/platform/catalog/products/{id}/retire`
- Relevant auth requirement: `PlatformPermission.ManageCatalog`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `PlatformAuthz.AuditSucceededAsync` for lifecycle updates
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PLAN-LIST`
- Screen: Plans / Pricing Administration
- Need: list plans for a product
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/catalog/products/{productCode}/plans`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: none in route signature (relies on internal query)
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PLAN-GET`
- Screen: Plans / Pricing Administration
- Need: get plan details
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/catalog/products/{productCode}/plans/{planId:guid}`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PLAN-CREATE`
- Screen: Plans / Pricing Administration
- Need: create a new plan
- Required operation: create
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/catalog/products/{productCode}/plans`
- Relevant auth requirement: `PlatformPermission.ManageCatalog`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-PLAN-MANAGE`
- Screen: Plans / Pricing Administration
- Need: activate/deactivate/retire plan and manage plan versions (draft/publish)
- Required operation: manage lifecycle + versions
- Status: **EXISTS**
- Verified API route(s) (method):
  - Lifecycle: `POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate|deactivate|retire`
  - Versions: `GET /api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions`, `GET /.../{versionNumber}`, `POST /.../versions/draft`, `POST /.../versions/{versionNumber}/publish`, and feature grants management via `PUT /.../versions/{versionNumber}/feature-grants/{featureCode}`
- Relevant auth requirement: `PlatformPermission.ManageCatalog` (for mutations) and `ViewPortfolio` (for reads)
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: versions list uses no explicit paging in route signature (version lookup by number)
- Audit requirement: audited for all relevant mutations
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-TRIAL-LIST`
- Screen: Plans / Pricing Administration
- Need: list trial definitions for a product
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/catalog/products/{productCode}/trials`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: none in route signature
- Audit requirement: read-only
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-TRIAL-MANAGE`
- Screen: Plans / Pricing Administration
- Need: retire/create trial definitions
- Required operation: manage (create + retire)
- Status: **EXISTS**
- Verified API route(s) (method):
  - `POST /api/v1/platform/catalog/products/{productCode}/trials` (create)
  - `POST /api/v1/platform/catalog/products/{productCode}/trials/{trialId}/retire`
- Relevant auth requirement: `PlatformPermission.ManageCatalog` (for mutations), `ViewPortfolio` for list
- Evidence source: `src/Platform/ExItS.Platform.Api/Catalog/CatalogEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited on success
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-SUBSCRIPTION-LIST`
- Screen: Subscriptions Administration
- Need: list subscriptions (filterable)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/subscriptions`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Subscriptions/SubscriptionEndpoints.cs`
- Pagination/filter support: `organizationId`, `productCode`, `status`, `isTrial`, `planId`, `sortBy`, `sortDesc`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-SUBSCRIPTION-GET`
- Screen: Subscriptions Administration
- Need: get a single subscription detail
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/subscriptions/{subscriptionId:guid}`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync(subscription.OrganizationId)`
- Evidence source: `src/Platform/ExItS.Platform.Api/Subscriptions/SubscriptionEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-SUBSCRIPTION-MANAGE`
- Screen: Subscriptions Administration
- Need: change subscription state
- Required operation: manage lifecycle state
- Status: **EXISTS**
- Verified API route(s) (method) (top-level subscription lifecycle):
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/activate`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/grace-period`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/past-due`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/suspend`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/reactivate`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/cancel`
  - `POST /api/v1/platform/subscriptions/{subscriptionId}/expire`
- Relevant auth requirement: mutations enforce `PlatformPermission.ManageSubscriptions` (see endpoint header)
- Evidence source: `src/Platform/ExItS.Platform.Api/Subscriptions/SubscriptionEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `AuditSubscriptionSuccessAsync` on success
- Gap owner / priority: Platform API / High
- Notes: plan/version changes are modeled by terminating and creating a new subscription, not mutating plan fields in-place (design constraint in endpoint header).

#### `PWEB-CAP-SUBSCRIPTION-PLAN-CHANGE`
- Screen: Subscriptions Administration
- Need: upgrade/downgrade/schedule plan change (plan-change preview + apply)
- Required operation: manage plan change lifecycle
- Status: **EXISTS**
- Verified API route(s) (method) (org-scoped):
  - `POST /api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/upgrade`
  - `POST /api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/downgrade`
  - `POST /api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/convert-trial`
  - `GET /api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/plan-change-preview`
  - `POST /api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/apply-pending-plan`
- Relevant auth requirement: org commercial mutations require `PlatformOrganizationAuthz.EnsureCanManageOrganizationCommercialAsync` (uses `PlatformPermission.ManageSubscriptions` or trusted organization owner)
- Evidence source: `src/Platform/ExItS.Platform.Api/Subscriptions/SubscriptionEndpoints.cs`
- Pagination/filter support: preview is single payload; upgrade/downgrade actions are mutations
- Audit requirement: audited on success (subscription mutation helpers call audit on successful use case execution)
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ENTITLEMENT-LIST`
- Screen: Entitlements Administration (org/product)
- Need: list entitlement snapshot history
- Required operation: list
- Status: **EXISTS**
- Verified API route (method):
  - `GET /api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync(organizationId)`
- Evidence source: `src/Platform/ExItS.Platform.Api/Entitlements/EntitlementEndpoints.cs`
- Pagination/filter support: `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ENTITLEMENT-GET`
- Screen: Entitlements Administration (org/product)
- Need: get single entitlement snapshot detail
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/entitlements/snapshots/{snapshotId:guid}`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync(snapshot.OrganizationId)`
- Evidence source: `src/Platform/ExItS.Platform.Api/Entitlements/EntitlementEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-ENTITLEMENT-OVERRIDE`
- Screen: Entitlements Administration (feature overrides)
- Need: grant/revoke feature overrides (commercial feature access overrides)
- Required operation: create + revoke
- Status: **EXISTS**
- Verified API route(s) (method):
  - `POST /api/v1/platform/organizations/{organizationId}/products/{productCode}/feature-overrides` (create)
  - `POST /api/v1/platform/feature-overrides/{overrideId:guid}/revoke` (revoke)
- Relevant auth requirement: `PlatformPermission.ManageEntitlementOverrides`
- Evidence source: `src/Platform/ExItS.Platform.Api/Entitlements/EntitlementEndpoints.cs`
- Pagination/filter support: list endpoint exists too (`GET .../feature-overrides`) but not required by the capability ID
- Audit requirement: audited on successful overrides and revocations
- Gap owner / priority: Platform API / High
- Notes: override DTOs are organization/product scoped.

#### `PWEB-CAP-BILLING-LIST`
- Screen: Billing / Invoice Administration (Platform SaaS billing)
- Need: list manual SaaS payment records
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/payments`
- Relevant auth requirement: `PlatformPermission.ManageManualPayments`
- Evidence source: `src/Platform/ExItS.Platform.Api/Payments/PaymentEndpoints.cs`
- Pagination/filter support: `status`, `productCode`, `reference`, `organizationId`, `method`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High
- Notes: endpoint requires at least one supported filter (status/productCode/orgId/reference constraints).

#### `PWEB-CAP-BILLING-GET`
- Screen: Billing / Invoice Administration
- Need: get manual SaaS payment detail
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/payments/{paymentId:guid}`
- Relevant auth requirement: `PlatformPermission.ManageManualPayments`
- Evidence source: `src/Platform/ExItS.Platform.Api/Payments/PaymentEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-BILLING-RECORD`
- Screen: Billing / Invoice Administration
- Need: record manual SaaS payment
- Required operation: create manual payment
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/payments/manual`
- Relevant auth requirement: `PlatformPermission.ManageManualPayments`
- Evidence source: `src/Platform/ExItS.Platform.Api/Payments/PaymentEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync` on success
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-BILLING-CONFIRM`
- Screen: Billing / Invoice Administration
- Need: confirm/reject/void SaaS payment
- Required operation: payment lifecycle transition
- Status: **EXISTS**
- Verified API route(s) (method):
  - `POST /api/v1/platform/payments/{paymentId}/confirm`
  - `POST /api/v1/platform/payments/{paymentId}/reject`
  - `POST /api/v1/platform/payments/{paymentId}/void`
- Relevant auth requirement: `PlatformPermission.ManageManualPayments` (enforced by payment mutation guard helper)
- Evidence source: `src/Platform/ExItS.Platform.Api/Payments/PaymentEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited (audit action codes for confirm/reject/void)
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-PERSONAL-FEATURE-LIST`
- Screen: Personal features (Platform-managed personal feature definitions)
- Need: list personal feature definitions (admin)
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/personal/features`
- Relevant auth requirement: `PlatformPermission.ViewPortfolio`
- Evidence source: `src/Platform/ExItS.Platform.Api/Personal/PersonalFeatureAdminEndpoints.cs`
- Pagination/filter support: N/A (route signature has no paging)
- Audit requirement: read-only
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-PERSONAL-FEATURE-MANAGE`
- Screen: Personal features
- Need: update personal feature definition
- Required operation: manage
- Status: **EXISTS**
- Verified API route (method): `PATCH /api/v1/platform/personal/features/{featureCode}`
- Relevant auth requirement: `PlatformPermission.ManageCatalog`
- Evidence source: `src/Platform/ExItS.Platform.Api/Personal/PersonalFeatureAdminEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited on success (`PlatformAuditActions.PersonalFeatureDefinitionUpdated`)
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-USAGE-LIST`
- Screen: Usage / Metering Administration (DOC-08 optional future)
- Need: list billable usage events (usage-based billing inputs)
- Required operation: list usage events
- Status: **EXTERNAL/DEFERRED**
- Verified API route (method): N/A (no matching Platform usage/metering list endpoints found)
- Relevant auth requirement: N/A (route missing)
- Evidence source: `src/Platform/ExItS.Platform.Api` (no `/usage`/`meter` API routes); plus DOC-07 explicitly references D-P12-03 unresolved contract transport
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner: External/deferred Platform integration
- Implementation priority: High once D-P12-03 is closed
- Notes:
  - This DOC does not invent usage-event transport.
  - D-P12-03 remains open; see DOC-07 usage/metering and D-P12-03 references in Platform Foundation docs.

#### `PWEB-CAP-USAGE-CORRECT`
- Screen: Usage / Metering Administration
- Need: correct/void a usage event
- Required operation: usage correction
- Status: **EXTERNAL/DEFERRED**
- Verified API route (method): N/A (no matching usage correction endpoints found)
- Relevant auth requirement: N/A (route missing)
- Evidence source: same as `PWEB-CAP-USAGE-LIST`
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner: External/deferred Platform integration
- Implementation priority: High once D-P12-03 is closed

### Governance / Operations / Settings (DOC-08)

#### `PWEB-CAP-AUDIT-LIST`
- Screen: Audit Explorer (global)
- Need: list global audit records
- Required operation: list
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/audit`
- Relevant auth requirement: `PlatformPermission.ViewAuditRecords`
- Evidence source: `src/Platform/ExItS.Platform.Api/Audit/AuditEndpoints.cs`
- Pagination/filter support: `fromUtc`, `toUtc`, `actor`, `actorType`, `action`, `targetType`, `targetId`, `organizationId`, `productCode`, `outcome`, `correlationId`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUDIT-GET`
- Screen: Audit Explorer (global)
- Need: get single audit record detail
- Required operation: get
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/audit/{auditId:guid}`
- Relevant auth requirement: `PlatformPermission.ViewAuditRecords`
- Evidence source: `src/Platform/ExItS.Platform.Api/Audit/AuditEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUDIT-EXPORT`
- Screen: Audit Explorer (global) export
- Need: export filtered audit records
- Required operation: export
- Status: **MISSING**
- Verified API route (method): N/A (no audit export endpoints found)
- Relevant auth requirement: N/A
- Evidence source: `src/Platform/ExItS.Platform.Api/Audit/AuditEndpoints.cs` (read-only immutable endpoints only)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / High
- Notes: export can be implemented only after an approved API contract exists; DOC-09 does not propose one.

#### `PWEB-CAP-AUTH-CREDENTIAL-RESET`
- Screen: Identity / Authentication Administration
- Need: reset a Platform user's credentials (set password)
- Required operation: credential reset
- Status: **EXISTS**
- Verified API route (method): `PUT /api/v1/platform/users/{userId}/credentials/password`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/CredentialEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync(PlatformUserPasswordSet)` on success
- Gap owner / priority: Platform API / High
- Notes: password value is not recorded (hash only); endpoint returns success DTO without leaking secret.

#### `PWEB-CAP-AUTH-LOCKOUT-CLEAR`
- Screen: Identity / Authentication Administration
- Need: clear account lockout
- Required operation: lockout clear
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/users/{userId}/credentials/unlock`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/CredentialEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via `authz.AuditSucceededAsync(PlatformUserCredentialUnlocked)` on success
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUTH-MFA-STATUS`
- Screen: Identity / Authentication Administration
- Need: view MFA readiness status
- Required operation: read MFA status
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: API contains MFA step-up usage but no `GET/POST` route for MFA readiness status
  - `src/Platform/ExItS.Platform.Api/Identity/IdentityEndpoints.cs` (step-up fields) and `src/Platform/ExItS.Platform.Api/Identity/CredentialEndpoints.cs` (no MFA status routes)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-AUTH-EXTERNAL-LIST`
- Screen: Identity / Authentication Administration
- Need: list external login providers linked to a user
- Required operation: list linked providers
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/ExternalAuthEndpoints.cs` (only challenge/complete flows; no user-linked provider list routes)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-AUTH-SESSION-LIST`
- Screen: Identity / Authentication Administration
- Need: list active browser sessions for a user
- Required operation: list sessions
- Status: **MISSING**
- Verified API route (method): N/A (no `/auth/sessions` or equivalent list endpoints found)
- Relevant auth requirement: N/A
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs` (only current-user session and token grant ops exist)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUTH-SESSION-REVOKE`
- Screen: Identity / Authentication Administration
- Need: revoke a browser session for a user
- Required operation: session revoke
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: same as `PWEB-CAP-AUTH-SESSION-LIST` (no admin session revoke route found)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUTH-TOKEN-LIST`
- Screen: Identity / Authentication Administration
- Need: list active access tokens for a user
- Required operation: list access tokens
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs` (token issuance, bind, introspect, revoke; no list endpoints)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-AUTH-TOKEN-REVOKE`
- Screen: Identity / Authentication Administration
- Need: revoke an access token
- Required operation: revoke token
- Status: **PARTIAL**
- Verified API route (method): `POST /api/v1/platform/auth/token/revoke`
- Relevant auth requirement: endpoint is `AllowAnonymous()` and revokes based on extracted bearer token (`ExtractBearerToken(http)`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: no explicit `PlatformAuthz.AuditSucceededAsync` observed on this revoke handler (relies on token ops plumbing)
- Gap owner / priority: Platform API / Medium
- Notes: admin screen requirement expects revocation by token selection; current endpoint revokes based on provided bearer token value (cannot be satisfied from a secure “no token display” UI model).

#### `PWEB-CAP-GOVERNANCE-ROLE-LIST`
- Screen: Access / Governance
- Need: list Platform role assignments (with filters)
- Required operation: list role assignments
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/authorization/assignments`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Authorization/AuthorizationEndpoints.cs`
- Pagination/filter support: `platformUserId`, `role`, `organizationId`, `status`, `page`, `pageSize`
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-GOVERNANCE-ROLE-ASSIGN`
- Screen: Access / Governance
- Need: assign a Platform role to a Platform user identity
- Required operation: assign role
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/authorization/assignments`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Authorization/AuthorizationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: assignment writes audit records (assignment/revoke use cases write audit themselves; handler comment confirms)
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-GOVERNANCE-ROLE-REVOKE`
- Screen: Access / Governance
- Need: revoke a Platform role assignment
- Required operation: revoke role
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/authorization/assignments/{id}/revoke`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Authorization/AuthorizationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: revoke writes audit records (handler comment indicates revoke use case performs audit)
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-GOVERNANCE-PERMISSION-VIEW`
- Screen: Access / Governance
- Need: view effective permissions for a user/role (resolved from assignments)
- Required operation: get effective permissions
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/authorization/users/{userId}/effective-permissions`
- Relevant auth requirement: `PlatformPermission.ManagePlatformUsers`
- Evidence source: `src/Platform/ExItS.Platform.Api/Authorization/AuthorizationEndpoints.cs`
- Pagination/filter support: optional `organizationId`; single payload result
- Audit requirement: read-only
- Gap owner / priority: Platform API / High

#### `PWEB-CAP-OPS-HEALTH-STATUS`
- Screen: Platform Operations
- Need: view Platform health/readiness status
- Required operation: read health
- Status: **EXISTS**
- Verified API route (method):
  - `GET /health`
  - `GET /health/ready`
- Relevant auth requirement: none (HealthChecks endpoint does not appear permission-gated in route mapping)
- Evidence source: `src/Platform/ExItS.Platform.Api/Common/PlatformHealthEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: read-only
- Gap owner / priority: Platform API / Medium
- Notes: endpoint scope is infrastructure health; must not include secrets.

#### `PWEB-CAP-OPS-EVENT-DELIVERY`
- Screen: Platform Operations
- Need: event delivery status (queue depth/retry/failures)
- Required operation: read event delivery status
- Status: **MISSING**
- Verified API route (method): N/A (no platform event-delivery / delivery-queue APIs found)
- Relevant auth requirement: N/A
- Evidence source: API search (no `/events`/event-delivery routes in `src/Platform/ExItS.Platform.Api`)
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-SETTINGS-PLATFORM-LIST`
- Screen: Platform Settings (global)
- Need: list Platform global settings
- Required operation: list
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: no matching Platform global settings endpoints found in `src/Platform/ExItS.Platform.Api`
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-SETTINGS-PLATFORM-MANAGE`
- Screen: Platform Settings (global)
- Need: update Platform global settings
- Required operation: manage
- Status: **MISSING**
- Verified API route (method): N/A
- Relevant auth requirement: N/A
- Evidence source: same as `PWEB-CAP-SETTINGS-PLATFORM-LIST`
- Pagination/filter support: N/A
- Audit requirement: N/A
- Gap owner / priority: Platform API / Medium

#### `PWEB-CAP-SETTINGS-ORG-LIST`
- Screen: Platform Settings (organization-scoped settings in org context)
- Need: list org-scoped settings
- Required operation: list
- Status: **PARTIAL**
- Verified API route (method): `GET /api/v1/platform/organizations/{organizationId}`
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanViewOrganizationAsync`
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationEndpoints.cs`
- Pagination/filter support: N/A (single payload)
- Audit requirement: read-only
- Gap owner / priority: Platform API / Medium
- Notes: endpoint exposes org profile/branding fields; matrix does not verify a broader “settings set” distinct from organization profile.

#### `PWEB-CAP-SETTINGS-ORG-MANAGE`
- Screen: Platform Settings (organization-scoped settings)
- Need: update org-scoped settings (branding and platform-editable profile fields)
- Required operation: manage
- Status: **EXISTS**
- Verified API route(s) (method):
  - `PUT /api/v1/platform/organizations/{organizationId}/branding`
  - `PUT /api/v1/platform/organizations/{organizationId}` (profile/platform fields)
- Relevant auth requirement: `PlatformOrganizationAuthz.EnsureCanEditOrganizationProfileAsync` / orgAuthz edit paths
- Evidence source: `src/Platform/ExItS.Platform.Api/Organizations/OrganizationEndpoints.cs`
- Pagination/filter support: N/A
- Audit requirement: audited via organization mutation authz paths (`PlatformAuditActions.OrganizationUpdated` / branching audit writers where applicable)
- Gap owner / priority: Platform API / High

### Authentication Screens (AMEND-01)

#### `PWEB-CAP-AUTH-LOGIN`
- Screen: Sign In
- Need: authenticate with credentials (email + password)
- Required operation: login
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/login`
- Relevant auth requirement: AllowAnonymous, rate-limited (`auth-login`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`
- Notes: sets session cookie on success.

#### `PWEB-CAP-AUTH-LOGOUT`
- Screen: Application shell (sign out)
- Need: invalidate session and clear cookie
- Required operation: logout
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/logout`
- Relevant auth requirement: AllowAnonymous
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-ME`
- Screen: Session validation / shell bootstrap
- Need: validate/renew session, return auth state
- Required operation: session check
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/auth/me`
- Relevant auth requirement: AllowAnonymous
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-FORGOT-PASSWORD`
- Screen: Forgot Password
- Need: request password reset email
- Required operation: initiate reset
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/forgot-password`
- Relevant auth requirement: AllowAnonymous, rate-limited (`auth-password-reset`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-RESET-PASSWORD`
- Screen: Reset Password
- Need: reset password with token
- Required operation: complete reset
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/reset-password`
- Relevant auth requirement: AllowAnonymous, rate-limited (`auth-password-reset`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-REGISTER`
- Screen: Create Account / Register
- Need: register personal account
- Required operation: register
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/register`
- Relevant auth requirement: AllowAnonymous, rate-limited (`auth-password-reset`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-ACTIVATE`
- Screen: Account Activation
- Need: activate personal account with token + password
- Required operation: activate
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/activate-account`
- Relevant auth requirement: AllowAnonymous, rate-limited
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-EXTERNAL-CHALLENGE`
- Screen: Sign In (social auth)
- Need: initiate external login (Google/Facebook)
- Required operation: OAuth challenge redirect
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/auth/external/{provider}/challenge`
- Relevant auth requirement: AllowAnonymous, rate-limited (`auth-login`)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/ExternalAuthEndpoints.cs`
- Notes: Google and Facebook supported when configured with ClientId/ClientSecret.

#### `PWEB-CAP-AUTH-EXTERNAL-COMPLETE`
- Screen: Sign In (social auth callback)
- Need: complete external login
- Required operation: OAuth callback
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/auth/external/{provider}/complete`
- Relevant auth requirement: AllowAnonymous
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/ExternalAuthEndpoints.cs`

#### `PWEB-CAP-AUTH-CHANGE-PASSWORD`
- Screen: Account settings (change password)
- Need: change password (authenticated)
- Required operation: password change
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/change-password`
- Relevant auth requirement: Authenticated (session claim)
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-ACCOUNT-PROFILES`
- Screen: Account profile selection
- Need: list account profiles for user
- Required operation: list profiles
- Status: **EXISTS**
- Verified API route (method): `GET /api/v1/platform/auth/account-profiles`
- Relevant auth requirement: Authenticated
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

#### `PWEB-CAP-AUTH-PROFILE-SELECT`
- Screen: Account profile selection
- Need: select account profile, set session
- Required operation: profile select
- Status: **EXISTS**
- Verified API route (method): `POST /api/v1/platform/auth/account-profiles/select`
- Relevant auth requirement: Authenticated
- Evidence source: `src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs`

## Backend gap summary (prioritized; evidence-based)

A. Reuse as-is (backed by verified routes): **61 capabilities**
- Products/catalog: list/get and plan/trial management exist (DOC-07)
- Commercial config: subscriptions/plan change, entitlements/snapshots + feature overrides, manual SaaS payments, and org-scoped views exist (DOC-07 + DOC-06)
- Audit read APIs exist (global + org-scoped) (DOC-06 + DOC-08)
- Platform role governance APIs exist (DOC-08)
- Personal feature admin endpoints exist (DOC-07)
- Health/readiness endpoints exist for platform operations (DOC-08)

B. Small API exposure / extension (minimal contract work): **3 capabilities**
- `PWEB-CAP-PRODUCT-CREATE` (dev/testing-only create gate): requires product ownership tooling or promotion path if/when needed
- `PWEB-CAP-ORG-CREATE` (dev/testing-only create gate): requires production creation path if/when authorized
- `PWEB-CAP-AUTH-TOKEN-REVOKE` (revokes based on bearer token value; cannot be used from a “no token display” admin UI model without a different contract)

C. Genuine Platform backend capability missing: **9 capabilities**
- Audit export: `PWEB-CAP-AUDIT-EXPORT`
- Identity admin visibility: session listing/revoke, token listing, MFA readiness, linked external provider listing
- Platform operations: event delivery status/retry read APIs
- Platform global settings list/manage

D. External/deferred dependency (approved future integration): **2 capabilities**
- `PWEB-CAP-USAGE-LIST`, `PWEB-CAP-USAGE-CORRECT`
- Dependencies referenced in docs:
  - **D-P12-03** commercial-state/access-context/event transport (unresolved)
  - **PLM-D-00-04** generic cross-product relationship model (dependency referenced in Platform product model; not redefined here)

E. UI-only / no backend work: **0**

## React-admin exposure gaps vs current Blazor Admin

The current Blazor Admin calls Platform API via server-side `PlatformApiClient` (HTTP requests), not via direct internal assembly calls. However, **some admin operations require sensitive bearer values** that a browser React app cannot safely render or collect under the “no secret/token display” constraint:

- `PWEB-CAP-AUTH-TOKEN-REVOKE`: current API contract revokes based on extracted bearer token value; there is no token listing endpoint and the UI model cannot display/round-trip token strings.

Other missing capabilities (session list/revoke, token list, MFA readiness, external provider list) are blocked by absent API routes, not by UI-only constraints.

## Contract dependencies explicitly reflected

- `PLM-D-00-04` reflected: **Yes** (referenced only as a dependency in DOC-09 notes for usage-based billing/event transport modeling; not redefined)
- `D-P12-03` reflected: **Yes** (usage/metering capabilities are deferred due to unresolved transport)

