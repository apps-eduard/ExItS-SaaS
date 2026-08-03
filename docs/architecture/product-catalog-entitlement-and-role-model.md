# Product Catalog, Entitlement, Instance, and Role Model

> **Status:** Authoritative architecture and security reference  
> **Applies to:** ExITS Platform, Organizations, and Products  
> **Current phase:** Phase 16 — P16-WP11 validation and stabilization

---

## 1. Executive Summary

ExITS must keep these four concepts separate:

1. **Platform Product**
2. **Organization Product Entitlement**
3. **Organization Product Instance**
4. **Organization Product Role Assignment**

Permanent rule:

> **Entitlement enables a Product for an Organization. Product Role authorizes a person inside that Product.**

Example:

```text
Pinoy Business POS
= Platform Product

ABC Sari-Sari Store may use Pinoy Business POS
= Organization Product Entitlement

ABC's stores, inventory, sales, customers, settings, and reports
= Organization Product Instance

Carlo Reyes is Cashier in ABC's POS
= Organization Product Role Assignment
```

---

## 2. Core Hierarchy

```text
ExITS Platform
└─ Platform Product Catalog
   ├─ Pinoy Business POS
   └─ Loan Management System

Organization
└─ Product Entitlements
   ├─ Pinoy Business POS: Enabled
   └─ Loan Management System: Disabled

Organization Product Instance
└─ Organization-specific Product data and configuration

Organization Staff
└─ Product Role Assignments
   ├─ POS Owner
   ├─ Store Manager
   ├─ Cashier
   └─ Reporting User
```

---

## 3. Platform Product

A **Platform Product** is a Product defined and owned by ExITS.

Examples:

- Pinoy Business POS
- Loan Management System
- Future ExITS Products

Recommended entity:

```text
PlatformProduct
```

Recommended fields:

```text
ProductId
ProductKey
DisplayName
Description
Status
DefaultRoute
CreatedAt
UpdatedAt
```

Example:

```text
ProductKey: pinoy-business-pos
DisplayName: Pinoy Business POS
```

Only the Platform may:

- create a Product
- rename a Product
- change its stable key
- activate or retire it
- define Product roles and capabilities

Organizations must not create or rename Platform Products.

Internal key:

```text
pinoy-business-pos
```

User-facing name:

```text
Pinoy Business POS
```

Do not display the internal key as the Product name.

---

## 3A. Platform Plan and Organization Subscription

Commercial packaging sits between Product and Entitlement:

```text
Platform Product
→ has Plans
→ Organization has a Subscription to one Plan
→ Subscription controls commercial state
→ Subscription enables or suspends the Organization Product Entitlement
```

### Plan

A **Plan** is a Platform-owned commercial package for one Product.

Recommended fields:

```text
PlanId
ProductId
PlanKey
DisplayName
Description
Status
MaxBranches
MaxActiveStaff
CustomerCreditEnabled
AdvancedReportsEnabled
ExportEnabled
TrialAllowed
DefaultTrialDays
SortOrder
MonthlyPrice
AnnualPrice
CurrencyCode
CreatedAt
UpdatedAt
```

`SortOrder` controls display/presentation order only. `MonthlyPrice`, `AnnualPrice`, and `CurrencyCode` are catalog defaults for new commercial actions; they do not retroactively rewrite existing subscription snapshots.

Statuses:

```text
Draft
Active
Inactive
Retired
```

Rules:

- `PlanKey` is unique per Product and immutable after creation
- Product association is fixed after create
- only `Active` plans accept new subscriptions
- `Retired` plans keep existing subscriptions but reject new enrollments
- do not hard-delete a Plan that has subscription history (retire instead)

### MVP Pinoy Business POS plans

| PlanKey | Display name | Max branches | Max active staff | Customer credit | Advanced reports | Export | Sort |
|---|---|---|---|---|---|---|---|
| `starter` | Starter | 1 | 3 | Disabled | Disabled | Disabled | 10 |
| `business` | Business | 3 | 15 | Enabled | Enabled | Enabled | 20 |
| `pro` | Pro | 10 | 50 | Enabled | Enabled | Enabled | 30 |

Business default trial: **14 days** when `TrialAllowed` is enabled.

Catalog pricing (MVP POS, PHP):

| PlanKey | Monthly | Annual | Currency |
|---|---|---|---|
| `starter` | catalog default | catalog default | PHP |
| `business` | catalog default | catalog default | PHP |
| `pro` | catalog default | catalog default | PHP |

### Subscription commercial snapshot

Each subscription stores the commercial terms agreed at enrollment or plan change:

```text
BillingCycle (Monthly | Annual)
AgreedPrice
CurrencyCode
PriceEffectiveFromUtc
CurrentPeriodStartUtc / CurrentPeriodEndUtc
PendingPlanId / PendingPlanEffectiveAtUtc (scheduled downgrades)
```

Rules:

- `AgreedPrice` is a snapshot; editing Plan catalog prices does not change existing subscriptions.
- Upgrades apply immediately (trial upgrades skip payment; paid upgrades require provider confirmation).
- Downgrades schedule `PendingPlan*` for period end; no Product Instance data is deleted.
- `ApplyDuePendingPlanChanges` applies scheduled downgrades when effective.

### Local Validation test payments

Local Validation exposes **Test payment** simulation only when `LocalValidation:Enabled` is true and the host is not Production. Simulations (`Succeeded`, `Declined`, `Pending`, `Failed`, `RenewalSucceeded`, `RenewalFailed`, `Refunded`) drive `IPaymentProvider` without card data. Production uses `Payments:Provider=None` (or a real provider later); `LocalValidation` is fail-closed in Production.

### Subscription

A **Subscription** is one Organization’s enrollment in a Plan. It controls commercial eligibility only.

It must **not**:

- assign Product roles
- create Organization membership
- create Account Class
- grant every staff member Product access

Statuses:

```text
Trialing
Active
Past Due
Suspended
Cancelled
Expired
```

Trial rules:

- trial belongs to the Subscription; Plan defines whether trial is allowed
- one trial per Organization per Product
- no automatic repeated trial
- trial expiration must not delete Organization or Product Instance data

### Subscription → entitlement lifecycle

| Subscription status | Entitlement effect |
|---|---|
| Trialing / Active | Entitlement Enabled |
| Past Due | May remain Enabled during configured grace |
| Suspended / Cancelled / Expired | Entitlement Suspended or Disabled per policy |

Product Instance data and Product role assignments remain stored; launch is denied when entitlement is not enabled.

### Platform Admin UX

- **Commercial → Plans**: Plan list/create/view/edit/activate/deactivate/retire with human-readable Product selection (`Pinoy Business POS`); columns include Monthly Price, Annual Price, Currency, Trial, Limits, Sort Order
- **Commercial → Subscriptions**: list columns use Organization name, Product display name, Plan display name, Agreed Price, Billing Cycle, Currency, pending plan (not GUIDs or Product keys as primary values); technical IDs remain in advanced details
- **Organization → Commercial**: current plan, available plans (upgrade/downgrade designation), subscription summary, Test Payments (Local Validation only)
- **Personal → Start a Business**: organization details + plan/billing/trial/pay-now; does not assign a Plan to the Personal account

---

## 4. Organization Product Entitlement

An **Organization Product Entitlement** means an Organization is allowed to use a Platform Product.

Recommended entity:

```text
OrganizationProductEntitlement
```

Example:

```text
ABC Sari-Sari Store
→ Pinoy Business POS
→ Enabled
```

Recommended fields:

```text
OrganizationProductEntitlementId
OrganizationId
ProductId
Status
Plan
EnabledAt
DisabledAt
ProvisioningStatus
CreatedAt
UpdatedAt
```

Recommended status values:

```text
Pending
Enabled
Suspended
Disabled
Expired
Provisioning Failed
```

Rules:

- entitlement enables the Product for the Organization
- entitlement does not grant every staff member access
- entitlement does not create Product-local roles
- entitlement does not change Account Class
- one entitlement per Organization and Product

Recommended uniqueness rule:

```text
UNIQUE (OrganizationId, ProductId)
```

---

## 5. Organization Product Instance

An **Organization Product Instance** is the Organization-specific operational workspace for a Product.

Recommended generic entity:

```text
OrganizationProductInstance
```

A Product may use a more specific aggregate:

```text
PosOrganization
LoanOrganization
```

For Pinoy Business POS, the Product Instance may contain or reference:

- branches
- terminals
- inventory
- products
- categories
- customers
- sales
- receipts
- cash sessions
- reports
- Product settings

For a Loan Management System, it may contain or reference:

- borrowers
- loan products
- collectors
- schedules
- payments
- arrears
- reports
- loan settings

Rules:

- belongs to one Organization
- belongs to one Platform Product
- contains Organization-specific operational data
- must be isolated from other Organizations
- must not redefine the Platform Product
- must not create duplicate Product records

Provisioning flow:

```text
Entitlement Enabled
→ Product provisioning starts
→ Organization Product Instance created or activated
→ Product becomes launchable
```

---

## 6. Organization Product Role Assignment

An **Organization Product Role Assignment** authorizes an Organization Staff member inside a specific Product.

Recommended entity:

```text
OrganizationProductRoleAssignment
```

Example:

```text
Carlo Reyes
→ ABC Sari-Sari Store
→ Pinoy Business POS
→ Cashier
```

Approved POS roles:

- POS Owner
- Store Manager
- Cashier
- Reporting User

A Product role assignment requires:

- active User Identity
- active Organization Account
- active Organization membership
- enabled Product entitlement
- provisioned Product Instance
- valid Product role
- authorized assigner

A Product role must not:

- create an entitlement
- create Organization membership
- change Account Class
- create Personal or Platform profiles
- grant access to another Organization
- authorize Platform administration

Recommended fields:

```text
OrganizationProductRoleAssignmentId
OrganizationId
ProductId
UserId
RoleKey
Status
AssignedAt
AssignedBy
RevokedAt
RevokedBy
```

---

## 7. Organization Role versus Product Role

These are separate authorization layers.

Organization roles:

- Owner
- Staff

Product roles for POS:

- POS Owner
- Store Manager
- Cashier
- Reporting User

Correct examples:

```text
Maria Santos
Account Class: Organization
Organization Role: Owner
POS Role: POS Owner
```

```text
Carlo Reyes
Account Class: Organization
Organization Role: Staff
POS Role: Cashier
```

Incorrect:

```text
Organization Role: Cashier
```

Incorrect:

```text
Account Class: POS Owner
```

---

## 8. Account Class versus Product Access

Approved Account Classes:

- Platform
- Organization
- Personal

Account Class does not determine Product access by itself.

Correct access rule:

```text
Active User Identity
+ Organization Account
+ Active Organization Membership
+ Enabled Organization Product Entitlement
+ Active Product Role Assignment
= Product access
```

---

## 9. Product Access Decision

A user may launch a Product only when all required checks pass:

```text
1. Is the User Identity active?
2. Is the session in Organization Scope?
3. Is the Organization membership active?
4. Is the Product entitlement enabled?
5. Is the Product instance provisioned and active?
6. Does the user have an active Product role?
7. Does the role permit the requested action?
```

Deny access when any required condition fails.

Navigation visibility is not authorization. Server-side enforcement is mandatory.

---

## 10. Platform Admin UI Terminology

Recommended Platform Admin sections:

```text
Product Catalog
Organization Entitlements
Product Provisioning
Product Role Definitions
```

Avoid ambiguous labels such as:

```text
Products
User Products
Organization Products
Product Access
```

unless the context is unmistakable.

### Product Catalog

Shows Platform-owned Products:

- Pinoy Business POS
- Loan Management System

### Organization Entitlements

Shows which Organizations may use which Products.

### Product Provisioning

Shows whether the Organization Product Instance is ready.

---

## 11. Organization UI Terminology

For Organization Owners, use:

```text
Enabled Products
```

or:

```text
Organization Products
```

Each Product card should show:

```text
Pinoy Business POS
Entitlement: Enabled
Provisioning: Ready
Your role: POS Owner
[Open Product]
[Manage Staff Access]
```

Display name:

```text
Pinoy Business POS
```

Internal key (never used as the card title):

```text
pinoy-business-pos
```

**Manage Staff Access** uses human-readable dropdowns only:

- Select Staff Member
- Select Product
- Select Product Role (POS Owner, Store Manager, Cashier, Reporting User)
- optional reason

Do not require User ID GUID, Product code, entitlement ID, or raw role key input in normal Organization UI.

Organization Owners may:

- view enabled Products
- open provisioned Products
- assign permitted Product roles
- revoke or suspend Product access

Organization Owners must not:

- rename the Platform Product
- change the Product key
- create a new Platform Product
- modify another Organization's entitlement
- use Platform commercial grant forms as the routine staff Product-role path

For Organization Staff, use:

```text
My Products
```

or:

```text
Available Products
```

Only show Products where:

```text
Organization entitlement is enabled
+ Product instance is active
+ staff membership is active
+ Product role is active
```

Technical denial codes (for example `product_local_role_missing`) must be replaced with friendly messages such as:

```text
You do not have a role assigned for this Product.
```

### Organization API field separation

Enabled Products / My Products responses expose separate fields:

```text
ProductId
ProductKey
ProductDisplayName
EntitlementStatus
ProvisioningStatus
OrganizationRole
ProductRole
CanLaunch
DenialReasonCode
DenialReasonDisplay
```

Do not overload a single generic `Role` or `Status` property.

---

## 12. “My Products” Rule

If the UI uses `My Products`, define it as:

> Products available to the signed-in user in the active Organization Scope.

A Product must appear only once.

Deduplicate by stable Product ID or Product Key.

Do not duplicate because of:

- multiple Product roles
- multiple grants
- multiple branch assignments
- repeated joins
- repeated entitlement rows

Display:

```text
Pinoy Business POS
```

Do not display:

```text
pinoy-business-pos
```

---

## 13. Data Ownership and Database Boundaries

### Platform Database

May own:

- Platform Product Catalog
- Organizations
- Organization Product Entitlements
- provisioning metadata
- Product-role definition metadata
- cross-product audit references

### Product Database

Should own Product-operational data.

For Pinoy Business POS:

```text
Database: ExItS_PinoyBusinessPOS
Schema: pos
```

Examples of POS-owned data:

- inventory
- products
- sales
- receipts
- cash sessions
- customers
- business credit
- Product settings
- Product-local audit

Boundary rule:

> **Platform owns entitlement and provisioning intent. Product owns operational truth.**

---

## 14. Naming Standards

Use these names consistently:

```text
PlatformProduct
OrganizationProductEntitlement
OrganizationProductInstance
OrganizationProductRoleAssignment
```

Avoid overloaded names such as:

```text
Product
OrganizationProduct
UserProduct
ProductAccess
Role
Status
```

unless the bounded context makes the meaning unambiguous.

---

## 15. Recommended API Naming

Platform APIs:

```text
/api/v1/platform/products
/api/v1/platform/organizations/{organizationId}/entitlements
/api/v1/platform/organizations/{organizationId}/product-provisioning
```

Organization APIs:

```text
/api/v1/organizations/{organizationId}/enabled-products
/api/v1/organizations/{organizationId}/products/{productKey}/staff-access
```

Product APIs:

```text
/api/v1/pos/...
```

Do not mix Platform Product Catalog APIs with Product-operational APIs.

---

## 16. Audit Requirements

Audit at least:

- Platform Product created
- Platform Product renamed
- Product activated or retired
- Organization entitlement enabled
- Organization entitlement suspended
- Product provisioning started
- Product provisioning completed
- Product provisioning failed
- Product role assigned
- Product role changed
- Product role revoked
- Product launch denied because entitlement is missing
- Product launch denied because Product role is missing

Audit records should include:

- actor
- Organization ID
- Product ID or Product Key
- target User ID where applicable
- previous state
- new state
- timestamp
- reason where required

---

## 17. Invalid Models

### Invalid: Product Role Creates Entitlement

```text
Assign Cashier role
→ automatically enable POS
```

Reason: role assignment must not create entitlement.

### Invalid: Entitlement Grants Everyone Access

```text
Enable POS
→ all Organization Staff can use POS
```

Reason: Product access requires explicit Product role assignment.

### Invalid: Organization Creates Platform Product

```text
ABC creates a Product named ABC POS
```

Reason: Platform Products are Platform-owned.

### Invalid: Duplicate Product per Organization

```text
Pinoy Business POS
Pinoy Business POS
```

Reason: multiple joins or grants must not duplicate the Product.

### Invalid: Product Role Used as Organization Role

```text
Organization Role: Cashier
```

Reason: Cashier is a POS Product role.

### Invalid: Internal Key Used as Display Name

```text
pinoy-business-pos
```

Reason: internal keys are not user-facing names.

---

## 18. Example: Pinoy Business POS

Platform Product:

```text
ProductKey: pinoy-business-pos
DisplayName: Pinoy Business POS
```

ABC entitlement:

```text
Organization: ABC Sari-Sari Store
Product: Pinoy Business POS
Entitlement: Enabled
```

ABC Product Instance:

```text
ABC POS workspace
- branches
- inventory
- products
- sales
- customers
- settings
```

ABC Product roles:

```text
Maria Santos
Organization Role: Owner
POS Role: POS Owner

Carlo Reyes
Organization Role: Staff
POS Role: Cashier
```

Maria and Carlo may access ABC's POS according to their Product roles.

They must not access XYZ's POS.

---

## 19. Example: Loan Management System

Platform Product:

```text
ProductKey: loan-management-system
DisplayName: Loan Management System
```

Organization entitlement:

```text
Organization: ABC Lending
Product: Loan Management System
Entitlement: Enabled
```

Organization Product Instance:

```text
ABC Lending workspace
- borrowers
- loan products
- collectors
- schedules
- payments
- arrears
- reports
```

Possible Loan Product roles:

```text
Loan Owner
Manager
Cashier
Collector
Reporting User
```

These are Product roles, not Organization roles and not Account Classes.

---

## 20. Acceptance Criteria

This model is correctly implemented only when:

- Platform Products exist once in the Product Catalog
- Product keys and display names are separate
- Organizations receive entitlements, not duplicate Product definitions
- Product instances are Organization-isolated
- Product roles do not create entitlements
- entitlements do not automatically grant user access
- Organization roles remain Owner and Staff
- POS roles remain POS Owner, Store Manager, Cashier, and Reporting User
- Account Classes remain Platform, Organization, and Personal
- My Products displays each Product once
- Pinoy Business POS displays with the correct user-facing name
- server-side checks verify entitlement and Product role
- Product-operational data remains in the Product boundary
- cross-Organization Product access is denied

---

## 21. Quick Reference

```text
Platform Product
= What ExITS sells or provides

Organization Product Entitlement
= Whether an Organization may use the Product

Organization Product Instance
= The Organization's Product workspace and data

Organization Product Role Assignment
= What a staff member may do inside the Product
```

Permanent rule:

> **Entitlement enables a Product for an Organization. Product Role authorizes a person inside that Product.**

---

## 22. Required Reference

Before changing Product Catalog, entitlement, provisioning, Product navigation, or Product roles, read:

```text
docs/architecture/product-catalog-entitlement-and-role-model.md
```

Also reference this decision from:

```text
docs/architecture.md
docs/security.md
docs/phase-progress.md
docs/reports/P16-WP11-*.md
```
