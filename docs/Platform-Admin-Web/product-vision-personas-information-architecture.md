# Platform Admin Web — Product Vision, Personas, and Information Architecture

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-02  
**Branch:** `docs/platform-admin-web-v2`

---

## 1. Product Vision

### 1.1 Application Identity

The future Platform Admin Web is the **ExItS Platform SaaS Control Center**.

Its job is to administer the shared SaaS control plane: identity, organizations, catalog, plans, subscriptions, entitlements, billing, Platform roles, privacy compliance, and audit.

It is not a product-operational console. POS checkout, inventory operations, cash management, loan processing, collections, and all other product-domain workflows belong to their respective product applications.

### 1.2 Measurable Goals

| Goal | Meaning |
|---|---|
| Clear | Every screen communicates its purpose, current state, and available actions without ambiguity. Labels, headings, and empty states use precise language. |
| Fast | Pages load and respond quickly. Long operations show progress. Navigation transitions are instant or near-instant. |
| Data-dense when appropriate | List and detail views present the information operators need without hiding it behind unnecessary clicks, while avoiding overwhelming low-frequency users. |
| Discoverable | Features and entities are findable through navigation, search, and contextual links without requiring prior memorization. |
| Keyboard-friendly | All primary workflows are operable via keyboard. Focus management is correct. Tab order is logical. |
| Accessible | Meets WCAG 2.1 AA. Screen reader support, sufficient contrast, resizable text, no information conveyed by color alone. |
| Responsive | Usable on desktop and tablet. The primary target is desktop (wide viewport); tablet is supported but not optimized for mobile-phone form factors. |
| Safe for high-impact administration | Destructive or irreversible actions require explicit confirmation. Bulk operations show preview and count. Role and permission changes are audited. |
| Consistent across Platform capabilities | Navigation patterns, list/detail layout, form behavior, error handling, and empty states follow shared conventions. |
| Excellent loading, error, and empty states | Every data-loading surface has a loading indicator, a meaningful error message on failure, and a helpful empty state when no data exists. |

---

## 2. UX Personas

These are UX personas describing how different people use the SaaS Control Center. They are **not** new authorization roles. Authorization is governed by the existing Platform roles and permissions defined in the authorization matrix.

### 2.1 Existing Platform Authorization Roles (reference)

From the authorization matrix (`docs/engineering/authorization-matrix.md`):

- **Platform Administrator** — full Platform permission set
- **Platform Support** — view-oriented permissions with limited management authority

Optional future roles noted in the authorization matrix (not yet defined):

- Billing Administrator
- Platform Auditor
- Platform Operations

These are authorization constructs. The UX personas below describe usage patterns and design priorities for people who may hold one or more of these roles.

### 2.2 Persona: Platform Administrator

A person responsible for the overall configuration and health of the SaaS platform.

| Attribute | Detail |
|---|---|
| Primary goals | Ensure all organizations have correct subscriptions and entitlements; manage the product catalog and plans; onboard and manage Platform staff; configure Platform roles and permissions; maintain privacy compliance posture |
| Frequent tasks | Review organization list and drill into details; manage product catalog (business types, categories, products, templates, imports); create/edit plans and plan versions; manage subscriptions and entitlements; invite and manage Platform staff; assign Platform roles |
| Sensitive tasks | Modify Platform roles and permissions; manage privacy compliance records; extend or override trials; manage entitlement overrides; suspend or deactivate accounts |
| Information needed | Organization list with subscription status; product catalog state; Platform staff list with role assignments; audit trail for recent changes; privacy compliance dashboard |
| UX risks | Accidental bulk changes to subscriptions or entitlements; modifying roles without understanding downstream effects; overlooking audit requirements for sensitive changes |
| Authorization dependency | Requires `Platform Administrator` role. Most permissions are available. Privacy compliance management requires `platform.permission.manage_privacy_compliance`. |

### 2.3 Persona: Operations / Support Operator

A person who handles day-to-day support requests and operational monitoring without full administrative authority.

| Attribute | Detail |
|---|---|
| Primary goals | Quickly find and review organization, user, and subscription information to resolve support requests; monitor Platform health indicators; escalate issues that require administrator authority |
| Frequent tasks | Search for organizations and users; view subscription and entitlement status; review audit logs for recent activity; view organization membership and product access; check account status |
| Sensitive tasks | Initiating support sessions (when authorized); viewing user account details that may contain contact information |
| Information needed | Organization detail with branches, members, subscriptions, and entitlements; user account detail with membership history; audit log filtered by entity or time range |
| UX risks | Inability to find the right entity quickly; confusion between view-only and actionable items; accidentally navigating to a page that requires permissions they lack |
| Authorization dependency | Requires `Platform Support` role (or future `Platform Operations` if defined). View permissions are broadly available; management actions are denied or limited. |

### 2.4 Persona: Commercial / Billing Operator

A person focused on subscription lifecycle, billing, plan management, and commercial health.

| Attribute | Detail |
|---|---|
| Primary goals | Ensure organizations are on correct plans with valid subscriptions; manage billing records and payment status; handle trial extensions and plan changes; monitor commercial KPIs |
| Frequent tasks | Review subscription list filtered by status (active, past-due, trialing, suspended); process manual payment records; review entitlement overrides; manage plan catalog |
| Sensitive tasks | Extending trials; applying entitlement overrides; recording manual payments; changing an organization's subscription plan |
| Information needed | Subscription list with commercial state; payment history; entitlement status per organization; plan catalog with version history |
| UX risks | Applying changes to the wrong organization; extending trials without proper justification; misunderstanding plan version effects on existing subscribers |
| Authorization dependency | Depends on specific permissions: `platform.subscriptions.manage`, `platform.plans.manage`, `platform.entitlements.manage`, `platform.test-payments.*` (Local Validation only). May be fulfilled by `Platform Administrator` or a future `Billing Administrator` role. |

### 2.5 Persona: Security / Governance Operator

A person responsible for audit review, privacy compliance, and security governance.

| Attribute | Detail |
|---|---|
| Primary goals | Review and maintain privacy compliance artifacts; monitor audit logs for anomalies; ensure Platform security policies are followed; maintain compliance documentation |
| Frequent tasks | Review privacy compliance overview and category status; manage compliance documents, systems records, PIAs, data inventory, retention policies, incidents, and vendor records; review audit logs filtered by action type or entity; review DPO/NPC records |
| Sensitive tasks | Managing privacy compliance records; reviewing audit entries that may reference user actions; accessing privacy evidence artifacts |
| Information needed | Privacy compliance dashboard with category-level status; audit log with filtering and export; compliance document inventory; incident and vendor records |
| UX risks | Missing critical compliance gaps due to poor dashboard design; overwhelming audit log without effective filtering; confusion between Platform-level and product-level compliance boundaries |
| Authorization dependency | Requires `platform.permission.view_privacy_compliance` (and `manage_privacy_compliance` for mutations). Audit view requires `platform.audit.view`. May be fulfilled by `Platform Administrator` or a future `Platform Auditor` role. |

---

## 3. Information Architecture

### 3.1 Primary Navigation Structure

The SaaS Control Center organizes Platform capabilities into a small set of top-level navigation groups. This structure is derived from the existing Admin navigation (`AdminNav.razor`) and refined for clarity.

| Primary Group | Contains | Permission Dependency |
|---|---|---|
| **Dashboard** | Platform overview / landing page | Authenticated Platform session |
| **Accounts** | All accounts, Platform staff, organization accounts, personal accounts, accounts needing review | `platform.accounts.view`, `platform.platform-staff.manage` |
| **Commercial** | Organizations, products, plans, personal features, subscriptions, entitlements, payments, test payments (Local Validation) | `platform.organizations.view`, `platform.catalog.manage`, `platform.plans.manage`, `platform.subscriptions.*`, `platform.entitlements.*`, `platform.test-payments.*` |
| **Product Catalog** | Global business types, categories, products, templates, imports | `platform.catalog.manage` (via `ViewGlobalCatalog`) |
| **Privacy & Compliance** | Overview, documents, systems, PIAs, data inventory, retention, incidents, vendors, DPO/NPC, evidence | `platform.permission.view_privacy_compliance` |
| **Operations** | Platform roles & permissions, organization memberships, audit logs | `platform.platform-staff.manage`, `platform.accounts.security-manage`, `platform.audit.view` |
| **Settings** | Organization settings (context-selected), branding (context-selected) | `platform.organizations.view` or `platform.accounts.security-manage` (requires selected organization context) |

Navigation groups that have no permitted items for the current user are hidden entirely.

### 3.2 Organization Drill-Down Architecture

When navigating into a specific organization, the SaaS Control Center provides a detail view with contextual tabs. These tabs represent Platform-owned information about the organization.

| Tab | Content | Notes |
|---|---|---|
| **Overview** | Organization profile, status, creation date, key metrics | Landing tab for organization detail |
| **Branches** | Branch list, branch detail, location, hours | Platform-managed organization structure |
| **People / Memberships** | Organization members, roles, invitations | Platform membership management; does not include product-local role assignment |
| **Products / Access** | Enabled products, product access grants, product entitlements | Platform product access; does not include product-operational configuration |
| **Subscription** | Current subscription, plan, status, trial dates, plan change history | Platform commercial state |
| **Entitlements** | Active entitlements, overrides, feature access | Platform entitlement management |
| **Billing** | Payment records, billing history | Platform SaaS billing; does not include product-operational financial records |
| **Activity / Audit** | Organization-scoped audit log entries | Filtered view of Platform audit for this organization |

### 3.3 Explicit Exclusions from Platform Admin

The following operational workflows belong to their respective product applications and must not appear in the SaaS Control Center:

- POS checkout and sales transactions
- POS inventory operations (stock counts, receiving, adjustments)
- POS cash management (cash register, cash drops, payouts)
- POS product-local role management (Cashier, Store Manager grants within POS)
- POS reporting dashboards (sales reports, inventory reports)
- Loan application processing
- Loan approval and disbursement
- Collections and payment collection
- Loan payments and settlement
- Any other product-domain operational workflow

Platform Admin may display summary or status information about a product (e.g., "POS entitlement: active") but must not host operational product workflows.

### 3.4 Account-Class Shell Boundaries

The current Admin supports three shell modes based on the authenticated user's account class:

| Shell Mode | Navigation | Purpose |
|---|---|---|
| Platform | Full Platform navigation (§3.1) | SaaS administration |
| Organization | Handoff to Organization Web; workspace selection | Organization context; operational work happens in Organization Web or product apps |
| Personal | Handoff to Personal Web; workspace selection | Personal context; operational work happens in Personal Web |

The future SaaS Control Center focuses on the **Platform shell**. Organization and Personal shells exist only as transition surfaces (workspace selection, handoff links). The SaaS Control Center does not replicate Organization Web or Personal Web functionality.

---

## 4. Navigation Principles

### 4.1 Entity-First Navigation

Navigation is organized around entities (organizations, users, products, subscriptions) rather than actions. Users navigate to an entity first, then act on it.

### 4.2 Breadcrumbs

Every page below the top-level navigation displays breadcrumbs showing the navigation path. Breadcrumbs are clickable and support direct return to any ancestor level.

### 4.3 Global Navigation

The primary navigation sidebar is always visible (collapsible on desktop, drawer on mobile). It provides consistent access to all top-level groups regardless of current page context.

### 4.4 Organization Detail Context

When viewing an organization's detail, the organization context is established and maintained across all tabs within that organization's detail view. Navigating away from the organization detail clears the organization-specific context.

### 4.5 Product Detail Context

Product detail pages (catalog products, plans) follow the same entity-first pattern. Product context is local to the detail view.

### 4.6 Global Search

A global search surface allows operators to find organizations, users, products, and subscriptions by name, email, or identifier without navigating through the menu hierarchy first.

### 4.7 Back-Navigation Behavior

Browser back/forward navigation works correctly. Each meaningful state (list view, detail view, tab selection) has its own URL. Navigating back returns to the previous state without data loss in read-only views.

### 4.8 Direct-Link and Bookmark Support

Every entity detail page and filtered list view has a stable, shareable URL. Operators can bookmark or share links to specific organizations, users, or audit entries. Opening a direct link restores the correct navigation context and breadcrumbs.

### 4.9 Permission-Aware Navigation

Navigation items that the current user cannot access are hidden (not shown as disabled). However, hiding a navigation item is not a security boundary. The server enforces authorization independently. If a user navigates directly to a URL they lack permission for, they receive a clear "not authorized" message rather than a generic 404.

### 4.10 Route-Not-Found Behavior

Navigating to a URL that does not match any defined route displays a clear "page not found" message with navigation to return to the dashboard. This is distinct from the authorization denial described above.

### 4.11 No Security Decisions Based on Hidden Navigation

Navigation visibility is a UX convenience. All authorization decisions are made server-side. The SaaS Control Center must not rely on hidden navigation items as a security control. This principle is consistent with the authorization matrix: "Navigation visibility is not authorization."
