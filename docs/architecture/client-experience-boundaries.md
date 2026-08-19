# Client Experience Boundaries

## Status

Approved for MVP.

## Purpose

This document defines which client application owns each ExItS SaaS experience.

The goal is to provide a smooth mobile-first journey while keeping the Web application available for full administration, larger screens, detailed tables, and advanced controls.

The MVP client boundaries are:

| Experience | Primary Client | Additional Client |
|---|---|---|
| Platform Administration | Web only | None |
| Personal Account | Mobile | None for MVP |
| Organization Owner Essentials | Mobile | Web for full control |
| Full Organization Administration | Web | Mobile provides the practical MVP subset |
| POS Product Operations | Mobile | None for MVP |

These boundaries define where features are presented. They do not replace server-side authorization.

---

## 1. Platform Administration

Platform Administration is available only through the Web application.

It is used by authorized Platform Administrators to manage the SaaS platform.

Platform Administration owns:

- platform users;
- platform administrator access;
- organizations;
- product catalog;
- plans;
- subscriptions;
- SaaS payments;
- entitlements;
- platform audit activity;
- controlled support and administrative actions.

Platform Administration must not expose POS operational workflows such as sales, shifts, receipts, inventory transactions, or operational cash.

The Mobile application must not contain Platform Administration screens.

---

## 2. Personal Account

The Personal Account experience is available through the Mobile application for MVP.

Personal Account owns:

- user registration;
- sign-in;
- personal profile;
- personal settings (appearance + logout);
- account security;
- organization selection;
- Start a Business;
- Personal Utang dashboard totals;
- People/Contacts;
- I Lent / I Borrowed;
- Personal Utang invitations;
- viewing businesses associated with the user;
- pending organization invitations (accept; decline deferred);
- launching entitled products.

A personal user may exist without belonging to an organization.

Personal Mobile must not expose POS operational pages until an organization is selected and product access is granted.

Starting a business creates an organization and makes the initiating user its Organization Owner. Contact details may be copied once from Personal into OrganizationProfile; profiles remain independent afterward (no live sync). The same Personal user may own multiple organizations.

After the business is created, the user must continue inside Mobile without being forced to leave the application.

The expected journey is:

```text
Register
→ Start a Business
→ Organization created
→ Trial or subscription activated
→ Organization Owner essentials
→ POS setup
→ Start using POS
```

The Web application must not duplicate Personal Account onboarding during MVP.

---

## 3. Organization Owner Experience

Organization management follows a mobile-first, Web-complete model.

**P28-WP15A:** Canonical capability scopes, Mobile Primary vs exact-branch exposure, and client boundaries are defined in [organization-branch-capability-matrix.md](../engineering/organization-branch-capability-matrix.md). Workspace selection (WP14) does not grant POS permission. Mobile **Primary/Main** workspace is the governance gateway; non-primary Mobile workspace is branch configuration/operations. Organization Web does not require selecting Main to manage the business.

**P28-WP15B:** Mobile operational shell separates branch ops from org governance. Burger **Manage business** (Primary + Owner/Admin only) opens `/manage-business`. Operational surfaces link to `/branch-settings` for the selected branch. Global branch list/create lives under Manage business → Branches. Web unchanged.

The Mobile application should provide every Organization Owner function that can be handled safely and clearly on a mobile device.

The Web application provides full control, detailed administration, larger tables, advanced filters, audit views, and complex management workflows.

### Mobile Organization Owner Essentials

For MVP, Mobile supports organization governance **when the selected workspace branch is Primary/Main** (P28-WP15B):

- burger menu entry **Manage business** → `/manage-business` hub (branches, staff & access, profile, subscription, devices, compliance links);
- compact summary on the hub; child pages lazy-load their own data;
- clear Web reminder for full administration.

From any workspace branch, Mobile supports **branch settings** for the selected location:

- `/branch-settings` → configure this branch (details, address, hours, fulfillment, local devices);
- no create/suspend other branches, change primary, or org-wide role matrix from branch settings.

Operational POS surfaces (dashboard, More, inventory, orders, etc.) no longer expose global branch management or org-wide governance links.

All other Owner essentials remain reachable from the Manage business hub (same APIs as before):

- viewing and editing basic organization profile information (business name, legal/contact/address/locale fields; independent of Personal profile);
- viewing organization status;
- viewing trial or subscription status;
- viewing active product entitlements;
- creating or inviting staff;
- viewing organization members;
- assigning POS Owner, POS Manager, or POS Cashier roles;
- revoking POS product access;
- suspending or removing staff where safely supported;
- launching POS onboarding;
- reviewing and acknowledging current sales-document education as the exact Organization Owner;
- **Start Selling** (POS selling interface mode without changing the POS role);
- switching between organizations;
- receiving clear reminders that Web provides full organization control.

Mobile should not block a newly subscribed Organization Owner from continuing the setup journey.

Where a function is not practical on Mobile, show a message such as:

> For full organization administration, use the Web application.

For longer Web guidance, operators may also see:

> For complete organization administration, advanced settings, detailed audit history, and larger management views, open ExItS in a Web browser.

### Full Organization Administration on Web

The Web application owns the complete Organization Administration experience, including:

- complete organization profile and settings;
- detailed staff and membership management;
- invitation history and status;
- staff suspension and removal;
- organization role management;
- POS product-role assignment and revocation;
- subscription details;
- entitlement details;
- organization-level audit information;
- sales-document education status and Owner acknowledgment;
- detailed tables, filters, exports, and advanced administrative actions.

The Mobile and Web experiences must use the same APIs, authorization rules, organization context, and data source.

Mobile is not a separate simplified data model. It is a smaller presentation of the same authorized organization capabilities.

Organization Administration must not own operational POS data such as sales, shifts, receipts, inventory transactions, refunds, or operational cash.

**P25 update:** Organization Web Admin remains **not a POS checkout client**. It may **read** operational records (sales history, receipts, shifts, inventory movements) for reporting, audit, and investigation, and may **write** management configuration (profile, branches, staff, catalog, inventory adjustments/transfers, devices, settings, **suppliers / Connected ExItS connection requests**) through the same server-authoritative APIs. It must not provide checkout, cart, barcode selling, payment-taking, or cashier sale creation. **Owner and Manager** use Organization Web; **Cashier** is denied the host and uses MAUI only. Organization Owner membership grants **full Organization management authority** (including POS management APIs via `OrganizationManagementAuthority` from membership; commercial entitlement remains feature-level) without automatic POS checkout (`CreateSale`/`EnterPos`) and without Platform operator permissions such as `view_portfolio`. Org Web restores session/Bearer ambient on Blazor circuit inbound activities so Staging Local Validation does not drop auth. Navigation is icon-complete for collapsed sider usability. After normal sign-in, Owner/Administrator memberships route to Organization Web (or the workspace chooser when multiple); Personal-only users route to Personal Web. Development Test User fills username only (manual password). Organization Web management pages share responsive patterns (`OrgAlert` / `OrgLoading` / `OrgEmpty`) documented in [organization-web-ui-responsive-standard.md](../engineering/organization-web-ui-responsive-standard.md) ([completion report](../reports/P25-org-web-full-responsive-ux-completion.md); [owner checklist](../validation/organization-web-responsive-owner-checklist.md)). Browser UI is AntDesign on a dedicated host (`:8093` locally). Personal product browser UI is a dedicated Personal Web host (`:8094`). Platform Admin remains the operator console and canonical sign-in. See [organization-web-role-and-workflow-matrix.md](../engineering/organization-web-role-and-workflow-matrix.md), [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md), [P25-WP02](../reports/P25-WP02-antdesign-web-standardization-and-host-separation.md), [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md), [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md).

**P26 update:** Sale documents shown by POS Mobile and Organization Web are Transaction Summaries for business/customer records. Tax calculation and Owner education acknowledgment do not authorize tax-document issuance. Platform owns the default-off organization capability (TaxDocumentIssuanceEnabled and TaxConfigurationEnabled; see [tax configuration](../engineering/platform-controlled-organization-tax-configuration.md)), versioned acknowledgment history, eligibility lifecycle, and an organization-scoped compliance profile anchor (no invented TIN/BIR fields). Tax settings stay hidden until Platform enables them for an Approved organization. Organization Web is the primary Owner education surface; MAUI presents the same information as a soft setup prompt. Cashiers cannot acknowledge, and sales/sync are not hard-blocked. Public QR identity remains tax/TIN-free and must not expose the compliance profile. Controlled future activation is tracked in [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md). See [sales-document boundary](../engineering/sales-document-compliance-boundary.md), [acknowledgment design](../engineering/organization-sales-document-acknowledgment.md), [eligibility](../engineering/platform-organization-compliance-eligibility.md), and [compliance profile](../engineering/organization-compliance-profile.md).

**Privacy refresh (P21-WP11):** Client surfaces must respect the P25/P26 access matrix (Owner-only ack; cashiers without compliance review detail; no compliance evidence on offline cashier devices). Inventory: [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md). **NPC compliance NOT CLAIMED.**

---

## 4. POS Product Operations

POS Product Operations are available through the Mobile application for MVP.

The POS product owns its operational workflows and data.

POS Mobile owns:

- POS operational onboarding;
- store operational settings;
- register setup;
- product and category management;
- inventory;
- customer management;
- shifts;
- sales;
- receipts;
- refunds and voids;
- operational reports;
- POS product-local roles and permissions enforcement.

Organization-level membership and entitlement data remain owned by the Platform.

The POS product must not directly manage:

- SaaS plans;
- SaaS payments;
- Platform users;
- Platform Administrators;
- organization ownership;
- the authoritative organization membership lifecycle.

Mobile may call Platform APIs to display or update authorized organization information, but POS operational data must remain inside the POS product boundary.

---

## 5. User Experience by Role

### Platform Administrator

Uses the Web application.

Available experience:

- Platform Administration.

Platform Administrators do not automatically receive access to an organization's POS operational data.

### Personal User

Uses the Mobile application.

Available experience:

- registration;
- sign-in;
- profile;
- Start a Business;
- organization selection;
- entitled product launch.

A Personal User without an active organization membership cannot access Organization Administration or POS.

### Organization Owner

Uses Mobile for the normal business journey and practical Organization Owner tasks.

Uses Web when full organization control, detailed administration, advanced settings, large tables, or audit history is required.

The same user may use Mobile POS only when assigned an active POS product-local role.

Organization ownership alone does not automatically grant POS access.

Example:

```text
Organization Owner
+ no POS role
= Organization Owner administration access
+ no POS operational access
```

```text
Organization Owner
+ POS Owner role
= Organization Owner administration access
+ Mobile POS Owner access
```

### Organization Staff

Organization Staff membership alone does not grant POS access.

A Staff member must also have an active POS product-local role.

Example:

```text
Organization Staff
+ no POS role
= no POS access
```

```text
Organization Staff
+ POS Cashier role
= Mobile POS Cashier access
```

---

## 6. POS Product Roles

The MVP POS roles are:

- POS Owner;
- POS Manager;
- POS Cashier.

These roles are product-local roles and are separate from Organization roles.

### Hierarchy

```text
POS Owner ⊇ POS Manager ⊇ POS Cashier
```

- POS Owner includes Manager and Cashier capabilities.
- POS Manager includes Cashier capabilities.
- POS Cashier has selling and own-shift capabilities only (no void/return of completed sales, no operational setup management).

Selecting **Start Selling** does not change the user's POS role. The application switches into a selling interface mode while preserving Owner or Manager identity and capabilities.

### POS Owner

May manage POS operational configuration and product access allowed by the POS product.

Typical access:

- POS setup;
- products;
- inventory;
- registers;
- shifts;
- sales;
- receipts;
- refunds;
- operational reports;
- Start Selling mode without role change.

### POS Manager

May manage daily POS operations.

Typical access:

- products;
- inventory;
- shifts;
- sales;
- approved refunds or voids;
- operational reports;
- Start Selling mode without role change.

### POS Cashier

May perform front-line POS operations.

Typical access:

- start and close own shift;
- use the assigned register;
- create sales;
- accept supported payments;
- issue receipts;
- view permitted sales information.

POS Cashier must not receive Organization Administration access merely because of the POS role.

---

## 6.1 Business creator provisioning

When a user Starts a Business and the POS entitlement becomes active:

```text
Business creator
→ Organization Owner (exactly one for MVP)
→ first POS Owner product-local role
```

Organization Owner status alone never grants POS access. Other organization members receive no POS access until an explicit POS role is assigned.

MVP allows only one Organization Owner per organization. Ownership transfer is supported via Personal QR / PublicUserId to another Personal identity (former owner removed). See [organization-ownership-transfer.md](../engineering/organization-ownership-transfer.md).
---

## 7. Access Rules

POS access requires all of the following:

```text
Active user
+ active organization membership
+ active POS entitlement
+ active POS product-local role
= POS access
```

Access must be denied when any requirement is missing.

Examples:

```text
Active organization membership
+ active POS entitlement
+ no POS role
= POS access denied
```

```text
Suspended organization membership
+ active POS entitlement
+ POS Cashier role
= POS access denied
```

```text
Active organization membership
+ expired POS entitlement
+ POS Owner role
= POS access denied
```

Organization Owner status must not bypass this rule.

---

## 8. Shared Identity

Web and Mobile use the same user identity.

The same account may access different experiences based on:

- account status;
- platform role;
- organization membership;
- organization role;
- product entitlement;
- product-local role.

The client applications must not create a separate identity for the same person.

Authentication may be shared, but authorization must be evaluated independently for every protected operation.

---

## 9. Authorization Enforcement

The API is the authoritative enforcement point.

Client-side navigation and hidden controls are usability features only.

The Web and Mobile applications must not be trusted to enforce permissions by themselves.

Every protected API operation must validate:

- authenticated user;
- account status;
- organization context;
- organization membership;
- entitlement where required;
- relevant platform, organization, or product-local role;
- organization isolation;
- resource ownership or scope.

Unauthorized requests must remain denied even when a client attempts to call the API directly.

Web and Mobile must produce the same authorization outcome for the same user, organization, and operation.

---

## 10. Data and Service Boundaries

Platform data and POS operational data must remain separated.

The Platform owns:

- identity;
- accounts;
- organizations;
- memberships;
- subscriptions;
- SaaS payments;
- entitlements;
- Platform Administration;
- organization administration data;
- platform and organization audit records.

The POS product owns:

- products;
- inventory;
- stores;
- registers;
- shifts;
- sales;
- receipts;
- refunds;
- operational cash;
- POS reports;
- POS product-local permissions.

POS operational money must not be stored as SaaS billing money.

There must be:

- no cross-product database access;
- no cross-database foreign keys;
- no direct POS access to Platform tables;
- no Platform ownership of POS operational transactions.

Integration between Platform and POS must use approved API contracts or controlled synchronization mechanisms.

---

## 11. MVP Navigation

### Web

```text
Web Application
├── Platform Administration
│   └── Platform Administrator
│
└── Full Organization Administration
    └── Organization Owner
        ├── Organization Profile and Settings
        ├── Staff and Invitations
        ├── POS Role Assignments
        ├── Subscription
        ├── Entitlements
        ├── Organization Audit
        └── Advanced Administration
```

### Mobile

Personal and POS shells share one BlazorWebView host. Android system-bar / cutout / navigation-bar insets are applied **once** via `ContentPage.SafeAreaEdges=Container` on `MainPage` (required under .NET 10 edge-to-edge defaults). Shell CSS must not add a second status-bar top spacer.

```text
Mobile Application
├── Personal Account (PersonalShell bottom tabs)
│   ├── Top bar — user display name + avatar/initials (session-derived; no env badge)
│   ├── Home — Personal Utang summary + recent activity (no verbose PageHeader)
│   ├── People
│   ├── I Lent
│   ├── I Borrowed
│   └── More (My QR, invitations, profile, settings, Explore POS, sign out)
│
├── Explore / Confirm business (AuthShell product brand only; no env badge; org created only on confirm)
│
├── Select Organization / Account context (outside Personal home)
│   └── Workspace selection (`/workspace-select`) — Organization + Branch (WP14)
│
├── Manage business (Primary/Main workspace only — WP15B)
│   ├── Burger → `/manage-business` hub
│   ├── Branches (list/create/edit under hub)
│   ├── Staff & access, profile, subscription, devices, compliance
│   └── Open Web for full control reminder
│
├── Branch settings (any selected workspace branch — WP15B)
│   └── `/branch-settings` → local branch configure (not org-wide governance)
│
└── POS Product (PosShell)
    ├── Top bar — active organization name + logo/initials (session-derived; updates on org switch)
    ├── Device registration (self-register or scan org registration code)
    ├── POS Owner (may enter Start Selling mode)
    ├── POS Manager (may enter Start Selling mode)
    └── POS Cashier
```
---

## 12. MVP User Journey

```text
User registers in Mobile
→ user signs in
→ Personal home (Utang-first; Explore POS when ready)
→ user explores Pinoy Business POS plans from catalog
→ user confirms business details (organization created only then)
→ user becomes Organization Owner
→ subscription or trial becomes active
→ POS entitlement is granted
→ creator receives first POS Owner role
→ owner continues in Mobile
→ owner completes basic organization setup
→ owner creates or invites staff
→ owner assigns POS product roles
→ owner launches POS onboarding or Start Selling
→ staff signs in through Mobile
→ Mobile displays the functions allowed by each assigned role
```
At any point where the owner needs advanced organization control, Mobile should provide a clear action or reminder to open the Web application.

The reminder must not interrupt ordinary MVP onboarding or POS operation.

---

## 13. Phase Boundary

### Phase 16

Phase 16 is responsible for:

- Personal registration;
- Start a Business;
- organization creation;
- Organization Owner assignment;
- mobile Organization Owner essentials;
- Web full Organization Administration;
- organization staff management;
- subscription and entitlement activation;
- POS product-role assignment;
- Platform-to-POS access handoff.

Phase 16 ends when an eligible user can launch the POS product without being forced to leave Mobile for ordinary setup tasks.

### Phase 17

Phase 17 is responsible for actual POS operation through Mobile:

- initial POS operational setup;
- default store and register;
- products and inventory;
- cashier shifts;
- cash sale;
- receipt;
- refund or void controls;
- operational reporting.

Phase 17 may use Mobile organization context and role-assignment capabilities delivered by Phase 16, but it must preserve the boundary between organization administration and POS operational data.

### Phase 18

Phase 18 is **Complete (implementation/scope)** for Mobile Personal Account, Organization Owner essentials, role routing, and catalog (Products / Categories) experience. Physical-phone validation was **partial**. Phase 18 is **not** Device Verified and does **not** claim production readiness.

### Phase 19

Phase 19 is **Open** and owns remaining Mobile POS operations and Cashier experience completion:

- Inventory (stock on hand, counts, adjust, transfers, expiration);
- Purchasing hub (Receive stock, purchase orders, goods receipts, suppliers);
- Registers;
- Shift operations;
- Cashier selling experience completion;
- Sales and receipt history;
- Customers;
- Reports, authorization, navigation, and UX hardening;
- end-to-end validation and user closeout checklist.

**Purchasing vs Inventory UX:** Purchasing is the primary entry for buying/receiving goods; Inventory is stock control. User-facing “Receive stock” replaces informal “Direct Stock In” / “Manual Purchase” labels. Canonical detail: [purchasing-inventory-ux-mental-model.md](../engineering/purchasing-inventory-ux-mental-model.md).

**Multi-unit selling:** Checkout supports independent sell-unit prices and base-inventory conversion (e.g. Rice kg ₱55 / Sack ₱2,600). See [product-units-and-inventory-behavior.md](../engineering/product-units-and-inventory-behavior.md).

Phase 19 reuses existing Phase 8–18 APIs and screens. It remains Open until user phone confirmation after WP08. Not Device Verified. Not production-ready. Phase 14 remains separate and unfinished.

---

## 14. Mobile and Web Capability Rule

The approved rule is:

> Put every safe and practical Organization Owner capability in Mobile. Use Web for full control, advanced administration, detailed views, and complex workflows.

A feature should remain Web-only for MVP when it requires one or more of the following:

- large or highly detailed tables;
- complex multi-step administration;
- advanced filters or bulk actions;
- detailed audit investigation;
- exports or print-oriented layouts;
- settings that are risky or difficult to manage safely on a small screen;
- Platform Administrator authority.

When a capability is Web-only, Mobile must:

- explain that the feature is available on Web;
- provide a clear Open Web or Learn More action where practical;
- preserve the user's organization context where technically possible;
- avoid presenting the limitation as an error.

---

## 15. Deferred After MVP

The following are deferred unless separately approved:

- Platform Administration on Mobile;
- full Personal Account experience on Web;
- complete feature parity between Mobile and Web Organization Administration;
- POS Web client;
- advanced bulk organization administration on Mobile;
- large audit investigation views on Mobile;
- multiple branches — **delivered (P28);** branch workspace + capability matrix in [organization-branch-capability-matrix.md](../engineering/organization-branch-capability-matrix.md);
- advanced branch-specific administration — Mobile non-primary branch ops vs Primary governance (WP15A baseline; UI enforcement WP15B+);
- custom product roles;
- offline synchronization;
- advanced cross-client notifications (SignalR); Organization in-app bell for customer-link + Connected Supplier connection requests is live via Platform `OrganizationInAppNotification` (tap → Read; Connected buyers supplier-side list; see [unified-organization-business-notifications.md](../reports/unified-organization-business-notifications.md));
- delegated Organization Administrators beyond the approved MVP role model.

---

## 16. Architectural Decision Summary

The approved MVP direction is:

```text
Platform Administration = Web only
Personal Account = Mobile
Organization Owner Essentials = Mobile
Full Organization Administration = Web
POS Product Operations = Mobile
```

The Organization is a business and authorization boundary, not a separate application.

The Mobile application must support the complete normal onboarding and operational journey without forcing the user to leave immediately after subscribing.

An Organization Owner may use both clients:

```text
Mobile
= normal business journey, practical organization management, and POS operation

Web
= full organization control, advanced administration, and detailed management
```

All authorization remains enforced by the APIs.
