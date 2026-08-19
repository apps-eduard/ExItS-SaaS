# Platform Admin Web — Canonical Navigation Registry

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-FINAL-AMEND-01  
**Branch:** `docs/platform-admin-web-v2`

This is the single canonical navigation registry for the Platform Admin Web (SaaS Control Center). Other documents summarize and cross-reference this file; they do not duplicate the full registry.

---

## Navigation lifecycle states

| State | Meaning | Visual behavior |
|---|---|---|
| `AVAILABLE` | Implemented and backed by verified capability. Enabled when the user is authorized. | Normal interactive item |
| `PLANNED_DISABLED` | Exists in an approved ExItS roadmap/plan but not yet implemented. | Visible but disabled. Small "Planned" badge. Tooltip: "Planned — not available yet." No route to fake/empty implementation. |
| `CONTEXT_REQUIRED` | Capability exists but requires a selected organization or entity context. | Visible but disabled until context is selected. Tooltip explains what must be selected. |
| `DEV_TEST_ONLY` | Visible only in Development/Testing environments. | Completely absent from Production navigation. |
| `UNAUTHORIZED` | User lacks the required permission. | Hidden (not visually disabled). Server remains authoritative. |

---

## Registry columns

| Column | Description |
|---|---|
| ID | Stable `PWEB-NAV-*` identifier |
| Label (EN) | English navigation label |
| Label key (fil-PH) | Filipino localization intent |
| Icon concept | Lucide icon name (conceptual; final icon selected at implementation) |
| Parent section | Navigation group this item belongs to |
| Route concept | Intended route pattern |
| Required permission | Platform permission(s) needed to see/access this item |
| Required context | Entity context required (e.g., selected organization) |
| Capability dependency | `PWEB-CAP-*` ID(s) this item depends on |
| Implementation evidence | Current backend evidence reference |
| Lifecycle state | One of the states defined above |
| Display order | Relative position within the parent section |
| Responsive behavior | Behavior at tablet/narrow breakpoints |
| Notes | Additional context |

---

## Primary sidebar sections

### HOME

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-OVERVIEW` | Overview | Pangkalahatang-tanaw | `layout-dashboard` | `/admin` | Authenticated Platform session | — | `PWEB-CAP-ORG-OVERVIEW-DATA`, `PWEB-CAP-ORG-ACTIVITY-AUDIT` | AdminNav: Dashboard | `AVAILABLE` | 1 | Visible | Landing page / dashboard |

### ORGANIZATIONS

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-ORGANIZATIONS` | Organizations | Mga Organisasyon | `building-2` | `/admin/organizations` | `ViewPortfolio` or `ManageOrganizations` | — | `PWEB-CAP-ORG-LIST` | AdminNav: Organizations | `AVAILABLE` | 1 | Visible | Primary org list entry |

### PEOPLE & ACCESS

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-ALL-ACCOUNTS` | All Accounts | Lahat ng Account | `users` | `/admin/users` | `ManagePlatformUsers` | — | `PWEB-CAP-IDENTITY-LIST` | AdminNav: All Accounts | `AVAILABLE` | 1 | Visible | |
| `PWEB-NAV-PLATFORM-STAFF` | Platform Staff | Staff ng Platform | `shield-check` | `/admin/users?directory=platform` | `ManagePlatformUsers` | — | `PWEB-CAP-IDENTITY-LIST` | AdminNav: Platform Accounts | `AVAILABLE` | 2 | Visible | Filtered view |
| `PWEB-NAV-ORG-ACCOUNTS` | Organization Accounts | Mga Account ng Organisasyon | `building` | `/admin/users?directory=organization` | `ManagePlatformUsers` | — | `PWEB-CAP-IDENTITY-LIST` | AdminNav: Organization Accounts | `AVAILABLE` | 3 | Visible | Filtered view |
| `PWEB-NAV-PERSONAL-ACCOUNTS` | Personal Accounts | Mga Personal na Account | `user` | `/admin/users?directory=personal` | `ManagePlatformUsers` | — | `PWEB-CAP-IDENTITY-LIST` | AdminNav: Personal Accounts | `AVAILABLE` | 4 | Visible | Filtered view |
| `PWEB-NAV-NEEDS-REVIEW` | Needs Review | Kailangang Suriin | `alert-circle` | `/admin/users?status=needs-review` | `ManagePlatformUsers` | — | `PWEB-CAP-IDENTITY-LIST` | AdminNav: Needs Review | `AVAILABLE` | 5 | Visible | Filtered view |
| `PWEB-NAV-MEMBERSHIPS` | Memberships | Mga Kasapi | `user-plus` | `/admin/organization-users` | `ManageMemberships` | — | `PWEB-CAP-MEMBERSHIP-LIST` | AdminNav: Organization Memberships | `AVAILABLE` | 6 | Visible | |
| `PWEB-NAV-ROLES-PERMISSIONS` | Roles & Permissions | Mga Papel at Pahintulot | `key` | `/admin/platform-roles` | `ManagePlatformUsers` | — | `PWEB-CAP-GOVERNANCE-ROLE-LIST` | AdminNav: Platform Roles | `AVAILABLE` | 7 | Visible | |

### PRODUCTS & COMMERCIAL

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-PRODUCTS` | Products | Mga Produkto | `package` | `/admin/products` | `ViewPortfolio` | — | `PWEB-CAP-PRODUCT-LIST` | AdminNav: Products | `AVAILABLE` | 1 | Visible | |
| `PWEB-NAV-PLANS` | Plans & Pricing | Mga Plano at Presyo | `credit-card` | `/admin/plans` | `ViewPortfolio` | — | `PWEB-CAP-PLAN-LIST` | AdminNav: Plans | `AVAILABLE` | 2 | Visible | |
| `PWEB-NAV-SUBSCRIPTIONS` | Subscriptions | Mga Subskripsyon | `repeat` | `/admin/subscriptions` | `ManageSubscriptions` | — | `PWEB-CAP-SUBSCRIPTION-LIST` | AdminNav: Subscriptions | `AVAILABLE` | 3 | Visible | |
| `PWEB-NAV-ENTITLEMENTS` | Entitlements | Mga Karapatan | `check-square` | `/admin/entitlements` | `ManageEntitlementOverrides` or `ManageSubscriptions` or `ViewPortfolio` | — | `PWEB-CAP-ENTITLEMENT-LIST` | AdminNav: Entitlements | `AVAILABLE` | 4 | Visible | |
| `PWEB-NAV-PERSONAL-FEATURES` | Personal Features | Mga Personal na Feature | `star` | `/admin/personal-features` | `ViewPortfolio` | — | `PWEB-CAP-PERSONAL-FEATURE-LIST` | AdminNav: Personal Features | `AVAILABLE` | 5 | Visible | |

### BILLING

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-PAYMENTS` | Payments | Mga Bayad | `receipt` | `/admin/payments` | `ManageManualPayments` | — | `PWEB-CAP-BILLING-LIST` | AdminNav: Payments | `AVAILABLE` | 1 | Visible | Platform SaaS billing only |

### GLOBAL CATALOG

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-BUSINESS-TYPES` | Business Types | Mga Uri ng Negosyo | `tag` | `/admin/catalog/business-types` | `ViewGlobalCatalog` | — | — | AdminNav: Global Business Types | `AVAILABLE` | 1 | Visible | |
| `PWEB-NAV-CATEGORIES` | Categories | Mga Kategorya | `folder` | `/admin/catalog/categories` | `ViewGlobalCatalog` | — | — | AdminNav: Categories | `AVAILABLE` | 2 | Visible | |
| `PWEB-NAV-GLOBAL-PRODUCTS` | Global Products | Mga Pandaigdigang Produkto | `box` | `/admin/catalog/products` | `ViewGlobalCatalog` | — | — | AdminNav: Products (catalog) | `AVAILABLE` | 3 | Visible | |
| `PWEB-NAV-TEMPLATES` | Templates | Mga Template | `file-text` | `/admin/catalog/templates` | `ViewGlobalCatalog` | — | — | AdminNav: Templates | `AVAILABLE` | 4 | Visible | |
| `PWEB-NAV-IMPORTS` | Imports | Mga Import | `upload` | `/admin/catalog/imports` | `ViewGlobalCatalog` | — | — | AdminNav: Imports | `AVAILABLE` | 5 | Visible | |

### GOVERNANCE

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-AUDIT-LOG` | Audit Log | Talaan ng Audit | `scroll-text` | `/admin/audit` | `ViewAuditRecords` | — | `PWEB-CAP-AUDIT-LIST` | AdminNav: Audit Logs | `AVAILABLE` | 1 | Visible | |
| `PWEB-NAV-PRIVACY-COMPLIANCE` | Privacy & Compliance | Privacy at Pagsunod | `shield` | `/admin/privacy-compliance` | `ViewPrivacyCompliance` | — | — | AdminNav: Privacy Compliance | `AVAILABLE` | 2 | Visible | Single primary destination; workspace navigation within (see §Privacy Workspace below) |

### OPERATIONS

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-PLATFORM-HEALTH` | Platform Health | Kalusugan ng Platform | `activity` | `/admin/operations/health` | Platform Administrator | — | `PWEB-CAP-OPS-HEALTH-STATUS` | Health endpoints exist | `AVAILABLE` | 1 | Visible | Basic health/readiness |
| `PWEB-NAV-EVENT-DELIVERY` | Event Delivery | Paghahatid ng Event | `send` | `/admin/operations/events` | Platform Administrator | — | `PWEB-CAP-OPS-EVENT-DELIVERY` | No API route found | `PLANNED_DISABLED` | 2 | Visible | Planned — not available yet |

### SETTINGS

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-PLATFORM-SETTINGS` | Platform Settings | Mga Setting ng Platform | `settings` | `/admin/settings` | Platform Administrator | — | `PWEB-CAP-SETTINGS-PLATFORM-LIST` | No API route found | `PLANNED_DISABLED` | 1 | Visible | Planned — not available yet |

### DEVELOPMENT

| ID | Label (EN) | Label key (fil-PH) | Icon | Route concept | Permission | Context | Capability | Evidence | State | Order | Responsive | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `PWEB-NAV-TEST-PAYMENTS` | Test Payments | Mga Pagsubok na Bayad | `flask-conical` | `/admin/local-validation/test-payments` | `ManageSubscriptions` | — | — | AdminNav: Test Payments | `DEV_TEST_ONLY` | 1 | Visible | Development/Testing only; absent from Production |

---

## Privacy & Compliance workspace navigation

The Privacy & Compliance area uses a single primary sidebar destination (`PWEB-NAV-PRIVACY-COMPLIANCE`) with its own local workspace navigation:

| ID | Label (EN) | Label key (fil-PH) | Route concept | Evidence | State | Order |
|---|---|---|---|---|---|---|
| `PWEB-NAV-PRIVACY-OVERVIEW` | Overview | Pangkalahatang-tanaw | `/admin/privacy-compliance` | AdminNav evidence | `AVAILABLE` | 1 |
| `PWEB-NAV-PRIVACY-DOCUMENTS` | Documents | Mga Dokumento | `/admin/privacy-compliance/documents` | AdminNav evidence | `AVAILABLE` | 2 |
| `PWEB-NAV-PRIVACY-SYSTEMS` | Systems | Mga Sistema | `/admin/privacy-compliance/systems` | AdminNav evidence | `AVAILABLE` | 3 |
| `PWEB-NAV-PRIVACY-PIAS` | PIAs | Mga PIA | `/admin/privacy-compliance/pias` | AdminNav evidence | `AVAILABLE` | 4 |
| `PWEB-NAV-PRIVACY-DATA-INVENTORY` | Data Inventory | Imbentaryo ng Data | `/admin/privacy-compliance/data-inventory` | AdminNav evidence | `AVAILABLE` | 5 |
| `PWEB-NAV-PRIVACY-RETENTION` | Retention | Pagpapanatili | `/admin/privacy-compliance/retention` | AdminNav evidence | `AVAILABLE` | 6 |
| `PWEB-NAV-PRIVACY-INCIDENTS` | Incidents | Mga Insidente | `/admin/privacy-compliance/incidents` | AdminNav evidence | `AVAILABLE` | 7 |
| `PWEB-NAV-PRIVACY-VENDORS` | Vendors | Mga Vendor | `/admin/privacy-compliance/vendors` | AdminNav evidence | `AVAILABLE` | 8 |
| `PWEB-NAV-PRIVACY-DPO-NPC` | DPO / NPC | DPO / NPC | `/admin/privacy-compliance/dpo-npc` | AdminNav evidence | `AVAILABLE` | 9 |
| `PWEB-NAV-PRIVACY-EVIDENCE` | Evidence | Ebidensya | `/admin/privacy-compliance/evidence` | AdminNav evidence | `AVAILABLE` | 10 |

---

## Organization workspace navigation

When navigating into a specific organization, the detail view uses workspace-local navigation:

| ID | Label (EN) | Label key (fil-PH) | Route concept | Capability dependency | Evidence | State | Order |
|---|---|---|---|---|---|---|---|
| `PWEB-NAV-ORG-OVERVIEW` | Overview | Pangkalahatang-tanaw | `/admin/organizations/{id}` | `PWEB-CAP-ORG-OVERVIEW-DATA` | Org detail endpoint | `AVAILABLE` | 1 |
| `PWEB-NAV-ORG-BRANCHES` | Branches | Mga Sangay | `/admin/organizations/{id}/branches` | `PWEB-CAP-BRANCH-LIST` | Branch list endpoint | `AVAILABLE` | 2 |
| `PWEB-NAV-ORG-PEOPLE` | People / Memberships | Mga Tao / Kasapi | `/admin/organizations/{id}/people` | `PWEB-CAP-MEMBERSHIP-LIST` | Membership endpoint | `AVAILABLE` | 3 |
| `PWEB-NAV-ORG-PRODUCTS` | Products / Access | Mga Produkto / Access | `/admin/organizations/{id}/products` | `PWEB-CAP-ORG-PRODUCT-ACCESS` | Commercial summary | `AVAILABLE` | 4 |
| `PWEB-NAV-ORG-SUBSCRIPTION` | Subscription | Subskripsyon | `/admin/organizations/{id}/subscription` | `PWEB-CAP-ORG-SUBSCRIPTION-COMMERCIAL` | Org subscriptions | `AVAILABLE` | 5 |
| `PWEB-NAV-ORG-ENTITLEMENTS` | Entitlements | Mga Karapatan | `/admin/organizations/{id}/entitlements` | `PWEB-CAP-ORG-ENTITLEMENTS` | Entitlement snapshots | `AVAILABLE` | 6 |
| `PWEB-NAV-ORG-BILLING` | Billing | Pagsingil | `/admin/organizations/{id}/billing` | `PWEB-CAP-ORG-BILLING-RECORDS` | Payment endpoint | `AVAILABLE` | 7 |
| `PWEB-NAV-ORG-BRANDING` | Branding | Pagba-brand | `/admin/organizations/{id}/branding` | `PWEB-CAP-SETTINGS-ORG-MANAGE` | Branding endpoint | `AVAILABLE` | 8 |
| `PWEB-NAV-ORG-ACTIVITY` | Activity / Audit | Aktibidad / Audit | `/admin/organizations/{id}/activity` | `PWEB-CAP-ORG-ACTIVITY-AUDIT` | Org audit endpoint | `AVAILABLE` | 9 |
| `PWEB-NAV-ORG-SETTINGS` | Settings | Mga Setting | `/admin/organizations/{id}/settings` | `PWEB-CAP-SETTINGS-ORG-LIST` | Org profile endpoint | `AVAILABLE` | 10 |
