# Phase 16 — Isolated Account Profiles, Personal Utang, and Business Upgrade

[Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Portfolio](../portfolio-progress.md)

## Status

**In progress** (authorized 2026-08-02).

Phase 16 introduces isolated Platform, Personal, and Organization account profiles; scope-bound sessions; Personal Utang as the free acquisition feature; Organization creation; controlled migration into Business Credit; and product-aware navigation.

Phase 14 Production Deployment and Operations remains separate and unfinished. Phase 16 must not silently close, replace, or weaken any Phase 14 production requirement.

The application remains **not production-ready**.

| Work Package | Status | Feature commit | Report |
|---|---|---|---|
| P16-WP01 | **Complete** | `d1e0096caac1b5aa0e47721938635a1e9766c66b` | [report](../reports/P16-WP01-architecture-and-domain-reconciliation.md) |
| P16-WP02 | **Complete** | `f0bb6c9ec87e75e7505087404cad463f931f5a67` | [report](../reports/P16-WP02-account-profiles-and-session-isolation.md) |
| P16-WP03 | **Complete** | `3454a7e6caa0d307d03a03d91abe7250ccad96a1` | [report](../reports/P16-WP03-organization-context-and-navigation.md) |
| P16-WP04 | **Complete** | *(recorded after commit)* | [report](../reports/P16-WP04-personal-account-foundation.md) |
| P16-WP05 | Not started | — | — |
| P16-WP06 | Not started | — | — |
| P16-WP07 | Not started | — | — |
| P16-WP08 | Not started | — | — |
| P16-WP09 | Not started | — | — |
| P16-WP10 | Not started | — | — |

---

## 1. Goal

Implement a secure account and product journey in which:

```text
One verified person
├── Personal Account Profile
├── Organization Account Profile
└── Platform Account Profile
```

Each authenticated session is bound to exactly one account class:

```text
Personal session
→ Personal APIs only

Organization session
→ Organization and entitled product APIs only

Platform session
→ Platform APIs only
```

The product journey becomes:

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

## 2. Authoritative Decisions

### 2.1 Identity and Account Profiles

- A `UserIdentity` represents the verified person.
- Account profiles are classified as:
  - Platform Account
  - Personal Account
  - Organization Account
- One person may own more than one account profile.
- A session is bound to one account profile and one allowed scope.
- Cross-account-class access is prohibited.

### 2.2 Platform Isolation

- Platform Accounts operate only in Platform Scope.
- Platform Administration is never part of the normal Organization switcher.
- Platform users do not become tenant staff through ordinary navigation.
- Tenant access occurs only through an explicit, time-limited, audited Support Session.
- Support Sessions are read-only by default.

### 2.3 Organization Isolation

- Organization Accounts operate only in Organization Scope.
- An Organization Account may belong to multiple organizations.
- Organization switching is allowed only between active memberships.
- Active organization context must be validated server-side.
- Organization membership does not automatically grant product access.

### 2.4 Personal Isolation

- Personal Accounts operate only in Personal Scope.
- Personal Utang works without an organization.
- Lender and Borrower are relationship roles, not permanent RBAC roles.
- Personal records remain private to authorized participants.

### 2.5 Entitlement and Product Authorization

```text
Organization entitlement
+ active Organization membership
+ active product-local role
= product access
```

- Activating POS grants the organization an entitlement.
- It does not automatically grant all Organization staff POS access.
- Product roles such as POS Owner, Manager, Cashier, and Viewer remain product-owned.

### 2.6 Personal Utang and Business Credit

- Personal Utang is the free acquisition feature.
- Business Credit and Loan Management are advanced organization features.
- They may share libraries, validation, UI components, and ledger abstractions.
- They must not share mutable records, ownership, tenant tables, authorization context, or active balances.
- Personal-to-organization migration is optional, selective, previewed, idempotent, and audited.
- Continuous automatic synchronization is prohibited.

---

## 3. Scope

Phase 16 includes:

- User Identity and account-profile model
- Platform, Personal, and Organization account classes
- scope-bound sessions
- account-profile selection
- Organization switching
- trusted Organization context
- Personal Account foundation
- Personal Utang core
- invitations and participant linking
- reminders and notifications
- Organization Staff and Business Customer separation
- Start a Business flow
- Organization creation and initial Owner assignment
- POS entitlement activation
- product-local POS role assignment
- controlled Personal Utang migration
- scope-aware and product-aware navigation
- authorization, privacy, audit, and abuse hardening

---

## 4. Non-Goals

Phase 16 does not:

- complete Phase 14 production deployment
- claim production readiness
- introduce automatic Platform-to-tenant switching
- merge Personal and Organization sessions
- merge Personal Utang and Business Credit records
- automatically sync personal and business ledgers
- treat Business Customers as Organization Staff
- grant product roles from subscriptions alone
- implement every advanced lending feature in the first Personal Utang work package
- restore the historical HealthCare product without separate approval
- replace product-owned operational databases with the Platform database

---

## 5. Work Packages

| Work Package | Title | Status |
|---|---|---|
| P16-WP01 | Architecture and Domain Reconciliation | **Complete** |
| P16-WP02 | Account Profiles and Session Isolation | **Complete** |
| P16-WP03 | Organization Context and Navigation | **Complete** |
| P16-WP04 | Personal Account Foundation | **Complete** |
| P16-WP05 | Personal Utang Core | Not started |
| P16-WP06 | Invitations, Linking, Reminders, and Notifications | Not started |
| P16-WP07 | Organization Staff and Customer Separation | Not started |
| P16-WP08 | Start a Business and Utang Migration | Not started |
| P16-WP09 | Product Access and Navigation Integration | Not started |
| P16-WP10 | Security, Privacy, UX Hardening, and Closeout | Not started |

---

# P16-WP01 — Architecture and Domain Reconciliation

## Objective

Make Version 1.5 of the architecture authoritative and reconcile it with the existing Platform, organization, identity, authorization, and POS implementation.

## Deliverables

- save the architecture document at:
  - `Docs/architecture/saas-scopes-users-boundaries-navigation.md`
- add required ADRs for:
  - account-profile isolation
  - scope-bound sessions
  - Support Session isolation
  - Personal Utang versus Business Credit ownership
  - migration and provenance
- inventory current identity and membership entities
- identify conflicts with existing `Platform User` terminology
- map current roles and permissions to the new model
- document database ownership and migration effects
- document API and route-family impacts
- document backward-compatibility and rollout strategy
- update portfolio and phase references

## Restrictions

- documentation and reconciliation only
- no production feature implementation
- no schema migration
- no account conversion
- no breaking API changes

## Exit Criteria

- architecture approved
- ADRs approved
- entity and API impact matrix complete
- migration strategy documented
- no unresolved critical terminology conflict
- explicit authorization received for P16-WP02

---

# P16-WP02 — Account Profiles and Session Isolation

## Objective

Introduce account classes and ensure each session is restricted to one allowed scope.

## Deliverables

- `UserIdentity` concept
- account-profile entity or equivalent model
- account classes:
  - Platform
  - Personal
  - Organization
- scope-bound session issuance
- session claims or server-resolved context:
  - UserIdentityId
  - AccountProfileId
  - AccountClass
  - AllowedScope
  - SessionId
  - SecurityStamp
- API-family guards:
  - `/platform/*`
  - `/personal/*`
  - `/organizations/{organizationId}/*`
  - `/products/{productCode}/*`
- account-profile selection flow
- session revocation
- cross-account-class denial tests
- audit events for account-profile and session lifecycle

## Exit Criteria

- Platform session cannot call Personal or Organization APIs
- Personal session cannot call Platform or Organization APIs
- Organization session cannot call Platform or Personal APIs
- client cannot change account class
- direct URL access is denied consistently
- regression suite passes
- explicit authorization received for P16-WP03

---

# P16-WP03 — Organization Context and Navigation

## Objective

Support secure multi-organization membership and switching inside Organization Scope.

## Deliverables

- Organization switcher
- active Organization context
- trusted server-side membership resolution
- last-active Organization preference
- route guards
- organization-aware cache keys
- cache clearing on Organization switch
- Organization Administration navigation
- authorization checks for deep links
- cross-organization denial tests

## Exit Criteria

- only active memberships appear
- browser-provided Organization IDs are never trusted alone
- switching clears Organization and product state
- no cross-tenant data leakage
- regression suite passes
- explicit authorization received for P16-WP04

---

# P16-WP04 — Personal Account Foundation

## Objective

Provide a Personal Scope that works without an Organization.

## Deliverables

- Personal dashboard
- Personal profile
- settings
- notification preferences foundation
- Personal Utang availability
- Personal navigation
- account state and authorization guards
- no Organization requirement
- no access to business products from Personal session

## Exit Criteria

- a Personal Account can sign in without an Organization
- Personal APIs reject Organization and Platform sessions
- Personal navigation contains no business administration
- regression suite passes
- explicit authorization received for P16-WP05

---

# P16-WP05 — Personal Utang Core

## Objective

Implement the free Personal Utang ledger.

## Deliverables

- personal contacts
- unlinked contacts
- debt relationships
- `I Lent`
- `I Borrowed`
- loans or advances
- payments
- adjustments
- due dates
- current balance
- append-oriented history
- optimistic concurrency
- audit events
- cross-user authorization tests

## Business Rules

- at least one side belongs to the authenticated Personal Account
- Lender and Borrower are relationship roles
- one user may be Lender in one relationship and Borrower in another
- unrelated users cannot discover or read the relationship
- stale updates return `409 Conflict`
- corrections preserve financial history

## Exit Criteria

- unlinked and linked-ready records are supported
- balances reconcile
- unauthorized users receive no record visibility
- history is retained
- regression suite passes
- explicit authorization received for P16-WP06

---

# P16-WP06 — Invitations, Linking, Reminders, and Notifications

## Objective

Allow safe participant linking and communication.

## Deliverables

- Personal Utang invitation lifecycle:
  - Pending
  - Accepted
  - Declined
  - Revoked
  - Expired
- explicit participant acceptance
- anti-enumeration controls
- shared relationship view
- one-time reminders
- scheduled reminders
- in-app notification
- push notification foundation
- notification preferences
- rate limiting
- anti-harassment controls
- delivery audit

## Exit Criteria

- no silent matching by name, email, or phone
- invitation acceptance creates no Organization membership
- invitation acceptance grants no product role
- sensitive values are minimized in notification previews
- repeated reminders are rate-limited
- regression suite passes
- explicit authorization received for P16-WP07

---

# P16-WP07 — Organization Staff and Customer Separation

## Objective

Create clear boundaries between workers and customers.

## Deliverables

- Organization Staff
- Staff Invitations
- Organization roles
- Business Customers
- Credit Customers
- Customer Link Requests
- Linked Customer App Users
- product-owned customer operations
- no customer-to-staff privilege conversion
- authorization and privacy tests

## Exit Criteria

- Business Customer is never treated as Organization Staff
- Customer Link acceptance creates no staff membership
- staff roles cannot expose unrelated personal records
- product-local roles remain isolated
- regression suite passes
- explicit authorization received for P16-WP08

---

# P16-WP08 — Start a Business and Utang Migration

## Objective

Allow a Personal user to create a business and optionally move selected Utang information into Business Credit.

## Start a Business Flow

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
Becomes Organization Owner
↓
Selects plan
↓
Activates POS entitlement
↓
Receives POS Owner role
```

## Migration Options

- contact only
- opening balance only
- selected transaction history
- selected due dates and notes
- destination Organization
- destination product
- effective migration date
- archive, retain, or mark source as transferred

## Required Controls

- preview before confirmation
- explicit selection
- destination-specific validation
- idempotency
- duplicate prevention
- migration batch ID
- durable provenance
- linked-participant consent where required
- no continuous synchronization
- cross-organization denial
- audit events

## Recommended Default

```text
Personal Contact
+ Outstanding Balance
→ Business Customer
+ Opening Business Credit Balance
```

Then:

```text
Personal record
→ Archived or marked Transferred
```

## Exit Criteria

- Organization Owner, POS entitlement, and POS role are separate grants
- repeated migration does not duplicate records
- linked participant data is not transferred without required consent
- destination Organization owns imported records independently
- source provenance remains available
- regression suite passes
- explicit authorization received for P16-WP09

---

# P16-WP09 — Product Access and Navigation Integration

## Objective

Integrate account, Organization, entitlement, and product-local authorization into navigation and APIs.

## Deliverables

- enabled-product discovery
- product launch rules
- product navigation
- product-local role assignment
- POS Owner, Manager, Cashier, and Viewer boundaries
- entitlement-versus-role enforcement
- direct URL denial
- product-aware caches
- audit events
- no duplicate operational navigation in Organization Administration

## Exit Criteria

- subscribed product appears for the Organization
- unauthorized staff cannot operate it
- removing entitlement disables Organization access
- removing product role disables individual access
- Platform role grants no POS operation
- regression suite passes
- explicit authorization received for P16-WP10

---

# P16-WP10 — Security, Privacy, UX Hardening, and Closeout

## Objective

Complete Phase 16 verification and closeout.

## Required Reviews

- cross-account-class authorization
- cross-user Personal Utang access
- cross-Organization isolation
- invitation abuse
- customer-link abuse
- migration duplication and replay
- linked-participant consent
- notification abuse
- Support Session isolation
- audit completeness
- cache contamination
- direct URL and API authorization
- accessibility and human-readable navigation
- full regression

## Closeout Deliverables

- Phase 16 closeout report
- final test totals
- residual risk register
- open decisions
- production blockers
- updated portfolio progress
- verified commit hashes
- confirmation that Phase 14 remains unchanged unless separately completed

## Exit Criteria

- all Phase 16 acceptance criteria pass
- no critical or high-severity isolation defect remains
- full Release test suite passes
- documentation matches implementation
- working tree clean
- focused commits recorded
- Phase 16 formally closed

---

## 6. Phase-Level Acceptance Criteria

Phase 16 is complete only when:

- Platform sessions access Platform APIs only
- Personal sessions access Personal APIs only
- Organization sessions access Organization and entitled product APIs only
- Platform Administration never appears in the Organization switcher
- Organization switching is limited to active memberships
- Personal Utang works without an Organization
- starting a business creates or activates an Organization Account Profile
- Organization creation grants Organization Owner
- POS activation grants Organization entitlement
- product-local role assignment grants POS operating permission
- migration is optional, selective, previewed, idempotent, and audited
- linked-participant data is not transferred without required consent
- Personal and Business ledgers share no mutable records
- Organization Staff and Business Customers remain separate
- direct URL and API calls are independently authorized
- all sensitive operations are auditable
- Phase 14 production requirements have not been weakened or silently closed

---

## 7. Documentation Placement

Save the architecture document here:

```text
Docs/architecture/saas-scopes-users-boundaries-navigation.md
```

Save this phase document here:

```text
Docs/phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md
```

Future work-package completion reports should use:

```text
Docs/reports/P16-WP01-architecture-and-domain-reconciliation.md
Docs/reports/P16-WP02-account-profiles-and-session-isolation.md
Docs/reports/P16-WP03-organization-context-and-navigation.md
Docs/reports/P16-WP04-personal-account-foundation.md
Docs/reports/P16-WP05-personal-utang-core.md
Docs/reports/P16-WP06-invitations-linking-reminders-notifications.md
Docs/reports/P16-WP07-organization-staff-customer-separation.md
Docs/reports/P16-WP08-start-a-business-and-utang-migration.md
Docs/reports/P16-WP09-product-access-and-navigation-integration.md
Docs/reports/P16-WP10-phase-16-closeout.md
```

Also update:

```text
Docs/portfolio-progress.md
```

Add Phase 16 as **Proposed / Not started** until implementation is explicitly authorized.
