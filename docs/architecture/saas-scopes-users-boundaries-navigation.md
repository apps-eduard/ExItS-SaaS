# SaaS Platform Architecture
## Scopes, Accounts, Boundaries, Navigation, and Product Evolution

**Version:** 1.5  
**Status:** Accepted for Phase 16 implementation (2026-08-02)  
**Recommended project path:** `docs/architecture/saas-scopes-users-boundaries-navigation.md`

> **Security decision**
>
> Platform, Organization, and Personal access are separate security domains.
> A person may have more than one account profile, but every session is bound to exactly one account class and one allowed scope.

---

# Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Core Design Principles](#2-core-design-principles)
3. [Identity and Account Model](#3-identity-and-account-model)
4. [Scope Overview](#4-scope-overview)
5. [Platform Scope](#5-platform-scope)
6. [Personal Scope](#6-personal-scope)
7. [Organization Scope](#7-organization-scope)
8. [Product Navigation](#8-product-navigation)
9. [User Types, Relationships, and Roles](#9-user-types-relationships-and-roles)
10. [Permissions Model](#10-permissions-model)
11. [Account Selection and Organization Switching](#11-account-selection-and-organization-switching)
12. [Personal-to-Organization Upgrade and Migration](#12-personal-to-organization-upgrade-and-migration)
13. [Invitation Types](#13-invitation-types)
14. [Product Upgrade Journey](#14-product-upgrade-journey)
15. [Data Ownership and Isolation](#15-data-ownership-and-isolation)
16. [Lifecycle Rules](#16-lifecycle-rules)
17. [Technical Enforcement Notes](#17-technical-enforcement-notes)
18. [Implementation Work Packages](#18-implementation-work-packages)
19. [Acceptance Criteria](#19-acceptance-criteria)
20. [Changelog](#20-changelog)

---

# 1. Executive Summary

The platform supports three independent security scopes:

| Scope | Owner | Main Purpose |
|---|---|---|
| **Platform** | SaaS vendor | Operate and govern the SaaS platform |
| **Personal** | Individual | Use personal tools without an organization |
| **Organization** | Tenant or business | Run business operations and subscribed products |

A single person may use more than one scope, but the scopes do not share sessions or permissions.

```text
One verified person
├── Personal Account Profile
├── Organization Account Profile
└── Platform Account Profile
```

Each account profile has its own session:

```text
Personal session
→ Personal APIs only

Organization session
→ Organization and product APIs only

Platform session
→ Platform APIs only
```

A Platform account never becomes an Organization user through ordinary navigation.  
An Organization account never enters Platform Administration.  
A Personal account never operates business products directly.

The main product journey is:

```text
Free Personal Utang
→ Start a Business
→ Create Organization
→ Become Organization Owner
→ Activate POS
→ Receive POS role
→ Optionally migrate selected Utang data
→ Use Business Credit and Loan Management
```

---

# 2. Core Design Principles

## 2.1 Security

- One verified person may have multiple account profiles.
- Each account profile belongs to exactly one account class.
- Each session is bound to exactly one account class and scope.
- Platform, Personal, and Organization permissions never inherit from one another.
- Organization data never crosses tenant boundaries.
- Product access requires both entitlement and product-local authorization.
- Client-side navigation is never the security boundary; APIs and domain logic enforce authorization.

## 2.2 Product

- Products are modular.
- Only enabled products appear.
- Product navigation belongs to the product.
- Common Organization navigation remains small and product-neutral.
- Personal Utang is the free acquisition feature.
- Business Credit and Loan Management are the advanced organization features.
- Personal-to-organization migration is explicit, selective, and auditable.
- No automatic two-way synchronization exists between personal and business ledgers.

## 2.3 Identity and Roles

- Identity is not permission.
- Account class is not permission.
- Persona is not permission.
- Organization membership is not product access.
- Product entitlement is not product-user authorization.
- Lender and Borrower are relationship roles, not global roles.
- Manager, Cashier, and similar operational roles are normally product-local.

---

# 3. Identity and Account Model

## 3.1 User Identity

A **User Identity** is the verified human identity used for authentication and account recovery.

Typical fields:

```text
UserIdentity
- Id
- DisplayName
- Email
- Phone
- PublicUserId (immutable ExItS ID, e.g. EX-4827-1936)
- VerificationStatus
- SecurityStatus
```

The User Identity is not itself a business, organization, or Platform role.

**Public ExItS ID / QR:** Platform owns an immutable public identifier separate from UUID, email, phone, and username. QR payloads use only `exits://user/v1/{PublicUserId}`. Exact-match resolve is authorized and rate-limited; scanning never auto-creates memberships, customers, or roles. See [public-user-id-and-qr](../specs/identity/public-user-id-and-qr.md).

## 3.2 Account Classes

Every account profile has one class:

```text
Platform Account
Organization Account
Personal Account
```

### Platform Account

Used only by authorized SaaS vendor personnel.

```text
Allowed scope: Platform
```

### Organization Account

Used only for organization and product operations.

```text
Allowed scope: Organization
```

An Organization Account may belong to multiple organizations.

### Personal Account

Used only for personal products.

```text
Allowed scope: Personal
```

## 3.3 Why Profiles Are Separate

This model reduces the impact of account compromise.

```text
Compromised Personal session
≠ access to Organization APIs

Compromised Organization session
≠ access to Platform Administration

Compromised Platform session
≠ ordinary tenant operational access
```

## 3.4 Platform Account Directory

Recommended Platform Administration views:

```text
Accounts
├── All Accounts
├── Platform Accounts
├── Organization Accounts
├── Personal Accounts
└── Requires Assignment
```

Definitions:

| View | Meaning |
|---|---|
| **All Accounts** | Every account profile |
| **Platform Accounts** | Account class is Platform |
| **Organization Accounts** | Account class is Organization |
| **Personal Accounts** | Account class is Personal |
| **Requires Assignment** | Incomplete, pending, orphaned, migration, or recovery cases requiring action |

Do not use **Personal Product Users** as a primary account category.

Do not treat valid Personal Accounts as unassigned.

---

# 4. Scope Overview

| Aspect | Platform Scope | Personal Scope | Organization Scope |
|---|---|---|---|
| Owner | SaaS vendor | Individual | Tenant or business |
| Account class | Platform Account | Personal Account | Organization Account |
| Requires organization | No | No | Yes |
| Main purpose | Platform operations | Personal tools | Business operations |
| Data isolation | Global control plane | Per person or relationship | Per organization |
| Multi-organization switching | No | No | Yes |
| Product model | Platform administration | Personal mini-features | Subscribed business products |
| Role model | Platform RBAC | Personal and relationship permissions | Organization RBAC + product-local RBAC |

---

# 5. Platform Scope

## 5.1 Purpose

Platform Scope is the SaaS control plane.

It owns and manages:

- global identity
- account profiles
- organizations
- product catalog
- features and modules
- plans
- subscriptions
- entitlements
- Platform billing
- Platform configuration
- monitoring
- security administration
- Platform audit
- approved support operations

Platform Scope does not own tenant operational business data.

## 5.2 Recommended Platform Roles

| Role | Typical Responsibility |
|---|---|
| **Platform Administrator** | Highest Platform authority |
| **Billing Administrator** | Plans, subscriptions, invoices, billing |
| **Platform Support** | Support-safe diagnostics and controlled support sessions |
| **Platform Operations** | Monitoring, jobs, infrastructure operations |
| **Platform Auditor** | Read-only audit and compliance access |

Optional later roles:

- Security Administrator
- Integration Administrator
- Developer Operator

Use **Platform Administrator** as the highest MVP role. Avoid duplicating it with a second role called Super Admin unless the distinction is formally defined.

## 5.3 Platform Boundaries

Platform users may, according to permission:

- manage Platform accounts
- activate, suspend, or disable accounts
- manage organizations as Platform records
- manage products, plans, subscriptions, and entitlements
- manage Platform roles and permissions
- configure Platform settings and integrations
- view monitoring, logs, and audit trails
- start an approved Support Session

Platform users must not automatically:

- create POS sales
- modify organization inventory
- process pawn transactions
- alter organization-owned customer transactions
- read personal Utang records
- assign product-local roles
- enter an organization as an ordinary staff member
- access tenant operational data outside a Support Session

## 5.4 Support Session

A Support Session is not normal scope switching.

It must be:

- permission-gated
- organization-specific
- time-limited
- reason-required
- read-only by default
- prominently displayed
- fully audited
- revocable
- incapable of silently changing record ownership or authorship

Write access should require an explicit elevated support permission and, where appropriate, approval.

## 5.5 Platform Navigation

```text
Platform Administration

Dashboard

Accounts
├── All Accounts
├── Platform Accounts
├── Organization Accounts
├── Personal Accounts
└── Requires Assignment

Organizations
├── All Organizations
├── Invitations
└── Organization Access

Catalog
├── Products
├── Features
├── Modules
└── Versions

Commercial
├── Plans
├── Subscriptions
├── Entitlements
├── Coupons
└── Platform Billing

Operations
├── Support Center
├── Support Sessions
├── Background Jobs
└── Notifications

Monitoring
├── Health
├── Metrics
├── Logs
└── Audit Logs

Security
├── Roles & Permissions
├── Policies
├── API Credentials
└── Security Events

Configuration
├── Platform Settings
├── Email Templates
├── Storage
├── Integrations
└── Webhooks

Developer
├── API Explorer
├── OpenAPI
└── System Events
```

Only implemented and authorized items should appear.

---

# 6. Personal Scope

## 6.1 Purpose

Personal Scope belongs to an individual.

No organization is required.

Initial personal product:

- Personal Utang Tracker

Possible later personal features:

- personal budget
- personal expenses
- personal reminders
- personal contacts

## 6.2 Personal Account

Use **Personal Account** as the account classification.

The following are personas, not roles:

- Freelancer
- Student
- Individual
- Employee
- Store owner

## 6.3 Personal Capabilities

A Personal Account may:

- create personal contacts
- track money lent
- track money borrowed
- record payments and adjustments
- set due dates
- create reminders
- invite another Personal Account
- link participants after explicit acceptance
- view shared balances and history
- receive notifications
- start the business onboarding process

A Personal Account must not:

- manage an organization from a Personal session
- manage Organization staff
- access POS, Inventory, Pawnshop, or other business APIs
- see organization data
- access unrelated personal records
- convert personal debt into business credit automatically

## 6.3a Personal Offline / Local-First (Mobile)

Personal Utang local-first offline (grant scope, path-isolated SQLite, outbox sync) is documented in
[P19-personal-scope-offline-operability](../reports/P19-personal-scope-offline-operability.md).
Personal local databases must remain separate from Organization POS local databases; invitations /
public-id resolve / start-business remain online-required.

## 6.4 Personal Utang Relationship Roles

Do not create permanent global Lender or Borrower roles.

```text
Debt A
- Eduard: Lender
- Juan: Borrower

Debt B
- Eduard: Borrower
- Maria: Lender
```

The same person may be a lender in one relationship and borrower in another.

## 6.5 Personal Navigation

```text
Personal (bottom tabs)
├── Home — summary totals + recent activity
├── People — contacts list / detail / create
├── I Lent — lent relationships list / create / detail
├── I Borrowed — borrowed relationships list / create / detail
└── More
    ├── Utang invitations
    ├── Profile
    ├── Settings (Account context switcher when orgs exist)
    ├── Explore Pinoy Business POS → plan catalog → confirm business details
    └── Sign out
```

Personal home does **not** list organizations, show Account context, or expose Start a Business / primary feature buttons (those live in tabs / More).
Organization / product switching for existing members is available from Settings and Organization Select.
POS operational routes stay outside PersonalShell and require organization + entitlement.

Payments / Reminders / History remain hidden from primary UI until implemented.

Target Personal Utang navigation (deferred on Mobile):

```text
Utang Tracker
├── People
├── I Lent
├── I Borrowed
├── Invitations
├── Payments
├── Reminders
└── History
```

---

# 7. Organization Scope

## 7.1 Purpose

Organization Scope belongs to a tenant or business.

Examples:

- sari-sari store
- mini grocery
- pawnshop
- restaurant
- clinic
- service company

The organization owns:

- organization profile
- Organization staff relationships
- branches
- business customers
- organization settings
- organization audit
- organization-owned product data

## 7.2 Organization People

### Organization Staff

People who work for or administer the organization.

Examples:

- Organization Owner
- Organization Administrator
- Organization Member
- Finance Officer
- Branch Coordinator

### Business Customers

People who have a commercial relationship with the organization.

Examples:

- Customer
- Credit Customer
- Linked Customer App User

A Business Customer is never automatically Organization Staff.

## 7.3 Organization Roles

Recommended built-in Organization roles:

- Organization Owner
- Organization Administrator
- Organization Member

Possible custom Organization roles:

- Finance Officer
- Customer Service
- Branch Coordinator
- Store Supervisor

Manager, Supervisor, Cashier, Inventory Clerk, and POS Viewer should normally be product-local roles.

## 7.4 Organization Boundaries

Authorized Organization staff may:

- manage organization profile and branding
- manage Organization staff
- send and revoke staff invitations
- manage Organization roles
- manage branches where supported
- view subscription and enabled products
- launch subscribed products
- manage customers through the owning product
- view organization-scoped audit records

Organization users must not:

- access another organization without membership
- access Platform Administration
- manage Platform accounts or roles
- access unrelated personal data
- gain product permissions from Organization membership alone

## 7.5 Organization Navigation

```text
Organization Administration

Dashboard

People
├── Staff
├── Staff Invitations
└── Roles & Permissions

Business
├── Branches
├── Customers
└── Customer Link Requests

Products
├── My Products
└── Product Access

Commercial
├── Current Plan
├── Subscription
└── Billing History

Administration
├── Organization Profile
├── Branding
├── Settings
├── Audit Log
└── API Credentials

Support
├── Tickets
└── Contact Support
```

Sales, Inventory, Pawnshop, Healthcare, and similar operations belong inside their respective products.

---

# 8. Product Navigation

## 8.1 Product Authorization Rule

```text
Organization entitlement
+ active Organization membership
+ active product-local role
= product access
```

Entitlement alone does not authorize a user.

## 8.2 POS

```text
POS
├── Dashboard
├── Sales
├── Registers
├── Customers
├── Products
├── Discounts
├── Returns
├── Reports
└── POS Staff & Roles
```

Typical roles:

- POS Owner
- POS Manager
- POS Cashier
- POS Viewer

## 8.3 Inventory

```text
Inventory
├── Products
├── Warehouses
├── Stock
├── Stock Movements
├── Transfers
├── Purchase Orders
├── Suppliers
├── Reports
└── Inventory Staff & Roles
```

Inventory and supplier management belong primarily in the Inventory product. POS may surface limited stock information required for selling.

## 8.4 Pawnshop

```text
Pawnshop
├── Dashboard
├── Customers
├── Pawns
├── Renewals
├── Redemptions
├── Auctions
├── Payments
├── Reports
└── Staff & Roles
```

## 8.5 Personal Utang

```text
Personal Utang
├── People
├── I Lent
├── I Borrowed
├── Invitations
├── Payments
├── Reminders
└── History
```

## 8.6 Business Credit and Loans

```text
Business Credit & Loans
├── Customers
├── Credit Accounts
├── Loans
├── Transactions
├── Payments
├── Collections
├── Reminders
├── Statements
├── Reports
└── Staff & Roles
```

## 8.7 Shared Ledger Components

Personal Utang and Business Credit may reuse:

- calculation libraries
- validation rules
- UI components
- ledger abstractions
- reporting primitives

They must not share:

- mutable records
- tenant tables
- authorization context
- ownership
- audit scope
- active balances

```text
Shared Credit Ledger Components
├── Personal Utang Mode
└── Organization Business Credit Mode
```

---

# 9. User Types, Relationships, and Roles

| Type | Domain | Meaning |
|---|---|---|
| User Identity | Global | Verified human identity |
| Personal Account | Personal | Personal products only |
| Organization Account | Organization | Organization and product operations |
| Platform Account | Platform | SaaS vendor administration only |
| Organization Owner | Organization | Highest authority in one organization |
| Organization Administrator | Organization | Organization administration |
| Organization Member | Organization | Basic staff relationship |
| Business Customer | Organization relationship | Customer of one organization |
| Credit Customer | Organization relationship | Customer with organization-owned credit |
| Linked Customer App User | Organization relationship | User linked to one customer record |
| Product Owner | Product | Highest product-local authority |
| Product Manager | Product | Product operational management |
| Product Staff / Cashier | Product | Daily product operations |
| Product Viewer | Product | Read-only product access |
| Guest / Invitee | Pending | Invitation review only |

## 9.1 Guest and Invitee

A Guest or Invitee:

- may view invitation details
- may accept or decline
- has no operational access
- has no active role before acceptance
- leaves the pending state after acceptance, decline, expiry, or revocation

---

# 10. Permissions Model

## 10.1 Authorization Formula

```text
Authorization
= Account Class
+ Allowed Scope
+ Account Status
+ Active Relationship
+ Active Role
+ Product Entitlement
+ Product-local Role
+ Resource Ownership
+ Tenant Isolation
+ Operation Policy
```

## 10.2 Examples

### Platform

```text
platform.organizations.manage
```

Requires an active Platform Account and Platform role containing the permission.

### Organization

```text
organization.staff.manage
```

Requires:

- active Organization Account
- active membership in the current organization
- Organization role containing the permission

### POS

```text
product.pos.sales.create
```

Requires:

- active Organization Account
- active membership
- active POS entitlement
- active POS role with sales-create permission

### Personal Utang

```text
personal.utang.relationship.view
```

Requires that the Personal Account is an authorized participant in that exact relationship.

---

# 11. Account Selection and Organization Switching

## 11.1 Account Profile Selection

A person with more than one account profile may choose which profile to open.

```text
Continue as:

Personal Account
ABC Store Business Account
Platform Account
```

Selecting a profile creates a new scope-bound session.

This is not ordinary scope switching.

## 11.2 Organization Switching

Within an Organization session, a user may switch between organizations where they hold active membership.

```text
ABC Store
XYZ Services
Eduard Trading
```

Requirements:

- only authorized organizations appear
- last active organization may be remembered server-side
- direct URLs still require authorization
- changing organization clears organization and product caches
- active organization is validated server-side

## 11.3 Platform Support

Platform Support is started separately:

```text
Start Support Session
→ Select organization
→ Enter reason
→ Set duration
→ Read-only by default
→ Audit access
```

Platform Administration must not appear beside ordinary organizations in the same organization switcher.

---

# 12. Personal-to-Organization Upgrade and Migration

## 12.1 Start a Business

A Personal Account does not directly become a POS user.

```text
Personal Account
↓
Selects Start a Business
↓
Creates or activates Organization Account Profile
↓
Starts Organization-scoped session
↓
Creates Organization
↓
Becomes initial Organization Owner
↓
Selects plan
↓
Activates POS entitlement
↓
Receives POS Owner role
↓
Uses Business Credit & Loans
```

These are separate operations:

```text
Create Organization
→ Organization Owner

Select commercial plan + billing cycle (Monthly / Annual) + trial or pay-now
→ Organization subscription (AgreedPrice snapshot on the org, not the Personal account)

Activate POS
→ Organization entitlement

Assign POS role
→ POS operating permission
```

## 12.2 Migration Options

The user may optionally migrate selected Personal Utang data.

Supported options:

- contacts only
- outstanding balances only
- full transaction history
- selected due dates and notes
- effective migration date
- destination organization
- destination product
- archive or retain source record

Recommended default:

```text
Personal Contact
+ Outstanding Balance
→ Business Customer
+ Opening Business Credit Balance
```

## 12.3 Migration Rules

Every migration must be:

- explicit
- selective
- previewed
- destination-specific
- idempotent
- auditable
- protected against duplicates
- protected against cross-organization import

Provenance fields:

```text
SourceType
SourceRecordId
ImportedByUserId
ImportedAt
DestinationOrganizationId
DestinationProduct
MigrationBatchId
```

## 12.4 Post-Migration Options

### Keep Separate

Both ledgers remain independently active.

### Archive Personal Record

Personal record becomes read-only.  
Organization record becomes authoritative.

### Mark as Transferred

The source records the destination organization and destination credit account.

Recommended default for migrated balances:

```text
Archive Personal Record
```

## 12.5 No Continuous Synchronization

Do not create automatic two-way synchronization.

```text
Personal ledger
✕ automatic sync
Organization ledger
```

This prevents:

- duplicate payments
- conflicting balances
- privacy leakage
- accounting ambiguity
- accidental staff access to personal data

## 12.6 Linked Participant Consent

For an unlinked personal contact, the owner may create an independent Business Customer record where lawful.

For a linked registered participant, explicit consent is required before transferring:

- identity linkage
- shared history
- acknowledgements
- shared notes
- app relationship

Without consent:

- a separate Business Customer record may be created where lawful
- no Personal Account link is preserved
- no Personal Scope data is exposed

---

# 13. Invitation Types

| Invitation | Result after acceptance |
|---|---|
| Platform Staff Invitation | Platform role or Platform staff relationship |
| Organization Staff Invitation | Organization membership |
| Customer Link Request | Link to one Business Customer record |
| Personal Utang Invitation | Link to one personal debt relationship |

Each invitation type has its own:

- authorization
- acceptance
- expiry
- decline
- revocation
- audit events
- resulting relationship

Accepting one invitation must never create another relationship type.

---

# 14. Product Upgrade Journey

```text
Visitor
↓
Creates Personal Account
↓
Uses free Personal Utang
↓
Invites friends or coworkers
↓
Selects Start a Business
↓
Creates Organization Account Profile
↓
Creates Organization
↓
Becomes Organization Owner
↓
Selects plan
↓
Activates POS
↓
Receives POS Owner role
↓
Optionally migrates selected contacts and balances
↓
Uses Business Credit & Loan Management
↓
Invites Organization Staff
↓
Assigns product-local roles
↓
Adds and links Business Customers
↓
Activates additional products
↓
Grows into a multi-branch, multi-product customer
```

---

# 15. Data Ownership and Isolation

## 15.1 Platform-Owned Data

- global identity
- account profiles
- authentication
- organizations
- product catalog
- plans
- subscriptions
- entitlements
- Platform billing
- Platform audit
- Platform configuration

## 15.2 Organization-Owned Data

- organization profile
- Organization staff relationships
- branches
- Business Customers
- organization settings
- organization audit
- organization-owned product data

## 15.3 Personal-Owned Data

- personal profile data
- personal contacts
- Personal Utang relationships
- reminders
- notification preferences

## 15.4 Product-Owned Data

- operational schema
- operational workflows
- product-local roles
- product-local permissions
- product reports
- product audit details

## 15.5 Hard Isolation Rules

- no cross-organization access
- no cross-account-class session access
- no cross-scope permission inheritance
- no product role from entitlement alone
- no Organization membership from customer linking
- no Organization membership from Personal Utang linking
- no automatic personal-to-business conversion
- no silent account matching
- no shared mutable ledger records
- no hard deletion of required audit, billing, security, or financial history

---

# 16. Lifecycle Rules

## 16.1 Account Profile

```text
Pending Verification
Active
Suspended
Disabled
Closed
```

## 16.2 Organization Membership

```text
Invited
Active
Suspended
Removed
```

## 16.3 Customer Link

```text
Pending
Active
Declined
Revoked
Expired
```

## 16.4 Personal Utang Link

```text
Unlinked
Pending
Linked
Declined
Revoked
Expired
```

## 16.5 Custom Role

```text
Active
Inactive
Retired
```

Rules:

- no hard delete
- inactive roles cannot receive new assignments
- inactive and retired roles grant no permissions
- historical assignments remain
- stale updates return `409 Conflict`

---

# 17. Technical Enforcement Notes

## 17.1 Session Claims

Every authenticated session should include or resolve:

```text
UserIdentityId
AccountProfileId
AccountClass
AllowedScope
SessionId
SecurityStamp
```

Organization sessions additionally require:

```text
ActiveOrganizationId
ValidatedMembershipId
```

Product authorization must not rely solely on client claims.

## 17.2 Trusted Context

- active organization is resolved and validated server-side
- browser-provided organization IDs are never trusted alone
- account class cannot be changed by the client
- Support Session uses a separate audited session context
- cross-class API calls are denied before domain execution

## 17.3 Isolation

Recommended mechanisms:

- tenant filters on every organization-owned query
- explicit `organization_id` predicates
- separate personal ownership columns or tables
- separate product databases where required by architecture
- no shared mutable state between personal and business ledgers

## 17.4 Caching

- cache keys include account profile
- organization cache keys include organization ID
- product cache keys include organization and product
- profile or organization change clears affected caches

## 17.5 API Boundaries

Recommended route families:

```text
/platform/*
/personal/*
/organizations/{organizationId}/*
/products/{productCode}/*
```

Each family validates the required account class and scope.

## 17.6 Audit

Audit at minimum:

- account creation and classification
- session creation and revocation
- Organization creation
- role assignments
- entitlement changes
- Support Session access
- migration preview and execution
- invitation lifecycle
- sensitive financial changes

---

# 18. Implementation Work Packages

## WP01 — Architecture and Domain Reconciliation

- adopt Version 1.5
- reconcile current identity terminology
- define account profile model
- define session boundaries
- define database ownership
- create ADRs
- document migration impact
- no feature implementation

## WP02 — Account Profiles and Session Isolation

- Platform, Personal, and Organization account classes
- scope-bound session issuance
- API family guards
- cross-class denial tests
- profile selection flow
- session revocation

## WP03 — Organization Context and Navigation

- multi-organization membership
- organization switcher
- trusted organization context
- context-safe caching
- organization navigation

## WP04 — Personal Account Foundation

- personal dashboard
- profile
- settings
- notifications foundation
- Personal Utang enrollment
- no organization requirement

## WP05 — Personal Utang Core

- contacts
- I Lent
- I Borrowed
- debt relationships
- transactions
- payments
- adjustments
- balances
- concurrency
- audit

## WP06 — Invitations, Linking, and Notifications

- Personal Utang invitation lifecycle
- explicit acceptance
- participant linking
- reminders
- notification preferences
- rate limits
- anti-harassment controls

## WP07 — Organization Staff and Customer Separation

- Staff
- Staff Invitations
- Business Customers
- Credit Customers
- Customer Link Requests
- Linked Customer App Users
- no customer-to-staff privilege conversion

## WP08 — Start a Business and Utang Migration

- Organization Account Profile activation
- Organization creation
- initial Owner assignment
- POS entitlement activation
- POS Owner assignment
- migration preview
- provenance
- idempotency
- duplicate prevention
- consent
- archive or transfer behavior

## WP09 — Product Access and Navigation

- enabled product discovery
- product navigation
- product-local role assignment
- entitlement versus role separation
- POS boundary preservation

## WP10 — Security, Privacy, UX Hardening, and Closeout

- cross-account-class tests
- cross-user tests
- cross-organization tests
- invitation abuse tests
- migration abuse tests
- Support Session review
- audit review
- privacy review
- full regression
- phase closeout

---

# 19. Acceptance Criteria

The architecture is correctly implemented when:

- Platform sessions can access Platform APIs only
- Personal sessions can access Personal APIs only
- Organization sessions can access Organization and entitled product APIs only
- Platform Administration never appears in the ordinary organization switcher
- Organization switching is limited to active memberships
- Personal Utang works without an organization
- starting a business creates an Organization Account Profile and scoped session
- Organization creation grants Organization Owner only
- POS activation grants entitlement only
- POS role assignment grants POS operating permission
- migration is optional, selective, previewed, and audited
- linked participant data is not transferred without required consent
- Personal and Business ledgers do not share mutable records
- Organization Staff and Business Customers remain separate
- all direct URL and API access is independently authorized
- all sensitive changes are auditable

---

# 20. Changelog

## Version 1.5

- Replaced unrestricted multi-scope switching with isolated account profiles and scope-bound sessions.
- Renamed global Platform User concept to User Identity.
- Added Platform, Organization, and Personal account classes.
- Replaced old user-directory filters with account-class views.
- Removed Platform Administration from the normal scope switcher.
- Split account profile selection from organization switching.
- Strengthened Support Session isolation.
- Clarified Start a Business as Organization Account Profile activation plus Organization creation.
- Clarified that Organization Owner, POS entitlement, and POS role are separate grants.
- Clarified shared ledger reuse as shared components, not shared mutable records.
- Strengthened linked-participant consent rules.
- Reorganized the document for faster human review and implementation planning.

## Version 1.4

- Added technical enforcement notes.
- Clarified multi-organization behavior.
- Strengthened Guest and Invitee definitions.
- Refined Inventory and POS boundaries.

## Version 1.3

- Defined Personal Utang as the free acquisition mini-feature.
- Added optional migration into Business Credit and Loan Management.
- Prohibited automatic synchronization.

## Version 1.2

- Separated identity, persona, relationship, and role.
- Separated Platform, Organization, and product-local RBAC.

## Version 1.1

- Added structured scope descriptions, permissions, and navigation.

## Version 1.0

- Initial version.
