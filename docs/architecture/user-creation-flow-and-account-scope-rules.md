# User Creation Flow and Account Scope Rules

> **Status:** Authoritative architecture and security reference  
> **Applies to:** ExITS SaaS Platform, Organization, and Personal account flows  
> **Current validation phase:** Phase 16 — P16-WP11  
> **Purpose:** Prevent incorrect account classification, cross-scope access, and inconsistent user onboarding

---

## 1. Executive Summary

ExITS uses **one human identity** with explicit account profiles.

The supported account classes are:

- **Platform Account**
- **Organization Account**
- **Personal Account**

These account classes are not interchangeable. A user must never receive another account class automatically merely because they can log in, belong to an organization, have application settings, hold a POS role, or were created by an administrator.

The creation path determines the initial account class.

| Creation path | Result |
|---|---|
| Platform Administrator creates or invites staff | Platform Account only |
| User completes Personal signup | Personal Account only |
| User selects **Start a Business** | Organization Account + Owner membership in the new organization |
| Organization Owner invites staff | Organization Account + membership in the inviting organization |
| Product role is assigned | Product-local role only; account class does not change |

---

## 2. Core Domain Model

### 2.1 User Identity

A **User Identity** represents the human login identity.

It normally contains:

- unique identity ID
- display name
- email address
- authentication status
- verification status
- account status
- security metadata

A User Identity alone does not define the user’s business scope.

### 2.2 Account Profiles

```text
User Identity
  ├─ Platform Account Profile
  ├─ Organization Account Profile
  └─ Personal Account Profile
```

For the normal creation flows in this document, the user starts with one intended profile only.

### 2.3 Organization Membership

Organization membership is not an account class.

```text
Organization Account
  → Organization Membership
      → Organization Role
```

Initial Organization roles:

- Owner
- Staff

### 2.4 Product-Local Roles

A product-local role is not an account class and is not an Organization-wide role.

For Pinoy Business POS (internal key `pinoy-business-pos`):

- POS Owner
- Store Manager
- Cashier
- Reporting User

Correct example:

```text
Carlo Reyes
  Account class: Organization
  Organization role: Staff
  POS role: Cashier
```

Incorrect example:

```text
Carlo Reyes
  Account class: Cashier
```

---

## 3. Authoritative Creation Rules

### Rule A — Platform Staff Creation

A Platform Administrator may create or invite Platform staff.

```text
Platform Account only
+ required Platform role
```

The new user must not receive:

- Personal Account profile
- Organization Account profile
- Organization membership
- POS role
- tenant operational access

### Rule B — Personal Signup

A user who completes Personal signup receives:

```text
Personal Account only
```

The user must not receive:

- Platform Account profile
- Organization Account profile
- Organization membership
- POS role

### Rule C — Start a Business

A verified user may choose **Start a Business**.

```text
Organization Account
+ Organization Owner membership
+ ownership of the newly created organization only
```

The user must not gain access to any other organization.

### Rule D — Organization Staff Invitation

An Organization Owner may invite staff into the current organization.

```text
Organization Account only
+ membership in the inviting organization only
+ required Organization role
```

An optional product-local role may be assigned separately.

The invited user must not receive:

- Platform Account profile
- Personal Account profile
- membership in another organization
- unrelated product access

---

## 4. Platform Staff Creation Flow

### 4.1 Who Can Create Platform Staff

Only an authorized Platform Administrator may create or invite Platform staff.

Platform Support must not create or assign privileged Platform roles unless a specific permission allows it.

### 4.2 MVP Platform Staff Fields

Required:

- First Name
- Last Name
- Display Name
- Email
- Platform Role
- Require Email Verification

Generated (server-controlled):

- Staff Number (`STF-000001` style; unique; immutable; never derived from email)
- Account Status
- Created At
- Created By

Optional:

- Phone
- Employee Code

Do not collect department, manager, hire date, birthday, gender, address, or profile photo in this MVP scope.

### 4.3 Required Platform Role

A Platform role is mandatory before activation.

Initial roles:

- Platform Administrator
- Platform Support

The system must not:

- activate a Platform Account with no role
- silently default to Platform Administrator
- assign a role the creator is not authorized to grant

### 4.4 Expected Result

```text
Create Platform Staff
→ create User Identity
→ create Platform Account profile
→ assign selected Platform role
→ apply activation rules
→ record audit event
```

### 4.5 Platform Account Isolation

A newly created Platform Account must remain Platform-only.

It must appear under:

```text
Accounts
  → Platform Accounts
```

It must not appear under:

- Organization Accounts
- Personal Accounts
- Needs Review, unless provisioning genuinely fails

---

## 5. Optional Email Verification for Platform Staff

SMTP may not be available during Local Validation.

The creation form should include:

```text
Require email verification
```

### 5.1 Verification Enabled

```text
Create identity
→ create Platform profile
→ assign Platform role
→ status: Pending Verification
→ generate activation token
→ send activation email
→ user verifies email
→ user sets password
→ account becomes Active
```

If delivery fails:

- do not silently activate the account
- keep the account Pending Verification or Delivery Failed
- show a clear administrative result
- allow an authorized resend
- record the failure

### 5.2 Verification Disabled

This may be allowed during Local Validation or controlled administrative onboarding.

```text
Create identity
→ create Platform profile
→ assign Platform role
→ create secure initial activation path
→ require password change on first login where supported
→ account becomes Active
```

Never expose permanent passwords in:

- browser JavaScript
- HTML
- logs
- normal API responses
- source-controlled documentation
- audit detail fields

### 5.3 Production Rule

Production should default to verification required.

Disabling verification in Production must require:

- explicit configuration
- explicit permission
- audit event
- secure temporary activation flow

Configuration must fail closed when incomplete or inconsistent.

---

## 6. Personal Signup Flow

### 6.1 Standard Flow

```text
Open signup
→ enter identity information
→ accept terms and privacy notice
→ verify email or mobile
→ create Personal Account profile
→ enter Personal scope
```

### 6.2 Result

The user receives:

- User Identity
- Personal Account profile
- Personal preferences/settings
- Personal-scope navigation

The user does not receive:

- Platform role
- Organization membership
- POS entitlement
- POS role

### 6.3 Platform Administrator Limit

Platform staff should not normally create active Personal users directly.

Allowed support actions may include:

- resend verification
- suspend or reactivate
- assist with recovery
- review flagged records
- repair onboarding state
- send an activation invitation

Platform staff should not know or set the user’s permanent password.

---

## 7. Start a Business Flow

### 7.1 Standard Flow

```text
Verified user
→ selects Start a Business
→ enters organization details
→ confirms ownership
→ organization is created
→ Organization Account profile is created
→ Owner membership is created
→ product entitlement is provisioned where approved
```

### 7.2 Ownership Rule

The creator becomes Owner of the newly created organization only.

The creator must not automatically receive access to:

- another organization
- global Platform administration
- unrelated tenant data

### 7.3 Product Roles

Product-local roles are assigned separately.

```text
Organization role: Owner
POS role: POS Owner
```

The POS role does not replace the Organization role.

---

## 8. Organization Staff Creation and Invitation Flow

### 8.1 Who Can Add Staff

Only an authorized Organization Owner or staff administrator may invite staff.

The organization must come from the current server-validated Organization session.

The browser must not be allowed to assign another organization through:

- query string
- hidden field
- local storage
- modified request payload
- stale organization state

### 8.2 MVP Organization Staff Fields

Required:

- First Name
- Last Name
- Display Name
- Email
- Organization Role (`Owner` or `Staff` only)
- Require Email Verification

Optional:

- Phone
- Employee Code
- Branch (collected for future store assignment; not a full HR branch module)
- POS / Product Role (separate from Organization role)

Do not display Member, Disabled, or Removed as user-facing labels.

### 8.3 Required Organization Role

An Organization role is mandatory.

Initial roles:

- Owner
- Staff

For ordinary staff, the normal role is Staff.

The system must not silently assign Owner.

### 8.4 Optional Product Role

A product-local role may be assigned only when:

- the organization has the product entitlement
- the creator has permission
- the role exists in the product
- the assignment is explicit

Example:

```text
Organization role: Staff
POS role: Cashier
```

### 8.5 Expected Result

```text
Organization Owner invites staff
→ create or link User Identity
→ create Organization Account profile
→ create membership in current organization only
→ assign Organization role
→ optionally assign product-local role
→ record audit event
```

### 8.6 Organization Isolation

ABC staff must not be created in XYZ.

XYZ staff must not be created in ABC.

All organization assignment must be validated server-side.

---

## 9. Existing Identity Handling

An email may already belong to a User Identity.

The system must not create a duplicate identity.

### 9.1 Existing Personal Identity Invited to an Organization

```text
Existing identity
→ explicit Organization invitation
→ user accepts
→ Organization Account profile is added if needed
→ membership is added to the inviting organization only
```

### 9.2 Existing Identity Invited to Platform Staff

This must be an explicit, highly controlled process.

```text
Existing identity
→ authorized Platform invitation
→ explicit acceptance or approved assignment
→ Platform Account profile added
→ Platform role assigned
```

The system must never silently convert an Organization or Personal identity into Platform staff.

### 9.3 Multiple Profiles

Multiple profiles may be supported by the architecture, but they must always be:

- explicit
- audited
- authorized
- scope-bound
- independently revocable

They must never be created by inference.

---

## 10. Account and Membership Status Lifecycle

Account status and Organization membership status are separate concepts. A status change must affect only the intended scope.

### 10.1 Global Account Statuses

| Status | Meaning | Login behavior | Reversible |
|---|---|---|---|
| Pending Verification | Required identity verification is incomplete | Blocked | Yes |
| Pending Activation | Provisioning or activation is incomplete | Blocked | Yes |
| Active | Account may authenticate and use authorized scopes | Allowed | Yes |
| Suspended | Temporary security or administrative restriction | Blocked | Yes |
| Deactivated | Retained but inactive account; similar to a reversible soft delete | Blocked | Yes |
| Invitation Expired | Invitation can no longer be accepted | Blocked | New invitation required |
| Delivery Failed | Activation or invitation message was not delivered | Blocked where verification is required | Yes |
| Needs Review | Manual correction or provisioning review is required | Normally blocked | Yes |

A user must not be marked Active while a required verification or activation step is incomplete.

`Deactivated` is not permanent deletion. The identity, profiles, roles, history, and audit records remain retained and may be restored by an authorized administrator.

### 10.2 Platform Account Transition Matrix

| Current status | Target status | Allowed | Required confirmation |
|---|---|---:|---|
| Active | Suspended | Yes | Confirmation; reason according to policy |
| Active | Deactivated | Yes | Confirmation + required reason |
| Suspended | Active | Yes | Confirmation only |
| Suspended | Deactivated | Yes | Confirmation + required reason |
| Deactivated | Active | Yes | Step-up authentication + required reason |
| Deactivated | Suspended | Yes | Confirmation + required reason |

A permanently deleted or legally purged record, if supported in the future, must use a separate irreversible action and status.

### 10.3 Suspend a Platform Account

Use suspension for a temporary restriction.

```text
Active
→ authorized Platform Administrator selects Suspend
→ confirmation dialog is displayed
→ server verifies permission
→ last-active-Platform-Administrator protection is checked
→ status becomes Suspended
→ active sessions and refresh tokens are revoked
→ new login is blocked
→ Platform profile and Platform role are retained
→ audit event is recorded
```

Suspension must not delete the identity, remove its Platform profile, remove its role, or create another account profile.

### 10.4 Reactivate a Suspended Platform Account

```text
Suspended
→ authorized Platform Administrator selects Reactivate
→ normal confirmation dialog is displayed
→ server verifies permission
→ status becomes Active
→ revoked sessions remain invalid
→ user signs in normally
→ existing Platform profile and role are preserved
→ audit event is recorded
```

Suspended-to-Active requires confirmation only. It does not normally require password re-entry unless a platform-wide security policy requires step-up authentication for every sensitive action.

### 10.5 Deactivate a Platform Account

Use deactivation when the Platform staff account is no longer expected to be used regularly, such as when an employee leaves the company.

```text
Active or Suspended
→ authorized Platform Administrator selects Deactivate
→ confirmation dialog is displayed
→ reason is required
→ server verifies permission
→ last-active-Platform-Administrator protection is checked
→ status becomes Deactivated
→ active sessions and refresh tokens are revoked
→ login is blocked
→ Platform profile, assigned role, history, and audit records are retained
→ audit event is recorded
```

Deactivation is a reversible retained state, not permanent deletion.

### 10.6 Reactivate a Deactivated Platform Account

This is a high-risk action and requires step-up authentication.

```text
Deactivated
→ authorized Platform Administrator selects Reactivate
→ high-risk confirmation dialog is displayed
→ acting administrator enters their own current password
→ MFA challenge is completed when MFA is enabled
→ reactivation reason is required
→ server verifies password, MFA, and permission
→ status becomes Active
→ old sessions remain invalid
→ user signs in normally
→ existing Platform profile and role are preserved
→ audit event is recorded
```

Rules:

- Ask for the acting administrator’s password, never the deactivated user’s password.
- Do not log, audit, persist, or expose the password or MFA secret.
- Reactivation must not bypass email-verification requirements.
- Reactivation must not create Personal or Organization profiles.

### 10.7 Move a Deactivated Platform Account to Suspended

```text
Deactivated
→ authorized Platform Administrator selects Move to Suspended
→ confirmation dialog is displayed
→ reason is required
→ server verifies permission
→ status becomes Suspended
→ login remains blocked
→ existing profile and role are preserved
→ audit event is recorded
```

This transition changes the administrative classification but does not restore login access.

### 10.8 Organization Membership Status

Organization membership status applies only to one Organization. It must not automatically change the global identity status.

| Membership status | Meaning |
|---|---|
| Active | User may enter the Organization according to assigned permissions |
| Suspended | Temporary restriction from that Organization only |
| Deactivated | Retained but inactive membership, when supported |

### 10.9 Suspend Organization Staff Membership

The Organization Owner manages routine Staff access in their own Organization.

```text
Active Organization Staff membership
→ Organization Owner selects Suspend Membership
→ confirmation dialog is displayed
→ server validates the active Organization session
→ server validates Owner permission
→ server confirms the target belongs to the same Organization
→ last-active-Owner protection is checked
→ membership becomes Suspended
→ access to that Organization is blocked
→ global identity remains unchanged
→ other memberships and account profiles remain unchanged
→ audit event is recorded
```

Use the explicit label **Suspend Membership**, not the ambiguous label **Suspend User**.

### 10.10 Reactivate Organization Staff Membership

```text
Suspended Organization membership
→ Organization Owner selects Reactivate Membership
→ confirmation dialog is displayed
→ server validates Owner permission and same-Organization scope
→ membership becomes Active
→ Organization access is restored
→ global identity remains unchanged
→ product-local roles remain unchanged unless separately revoked
→ audit event is recorded
```

### 10.11 Deactivate Organization Staff Membership

Where supported:

```text
Active or Suspended membership
→ Organization Owner selects Deactivate Membership
→ confirmation dialog is displayed
→ reason is required
→ same-Organization permission is verified
→ membership becomes Deactivated
→ Organization access remains blocked
→ global identity and other profiles remain unchanged
→ membership history is retained
→ audit event is recorded
```

A deactivated membership may be restored by an authorized Organization Owner, subject to the same-Organization and last-active-Owner safeguards.

### 10.12 Global Suspension of Organization or Personal Identities

Only an authorized Platform Administrator may globally suspend an Organization or Personal identity for a Platform-level reason such as:

- suspected account compromise
- fraud or abuse
- legal or compliance requirement
- serious security incident
- emergency containment

```text
Platform Administrator selects Global Account Suspension
→ high-impact confirmation is displayed
→ reason is required
→ Platform permission is verified
→ global identity becomes Suspended
→ all sessions are revoked
→ login is blocked across all scopes
→ profiles, memberships, roles, and history are retained
→ audit event is recorded
```

Global suspension must not be used for routine Organization staff management. Routine restriction belongs to the Organization Owner through membership suspension.

### 10.13 Suspension and Reactivation Authority

| Action | Organization Owner | Platform Support | Platform Administrator |
|---|---:|---:|---:|
| Suspend Staff membership in own Organization | Yes | No | Emergency override only |
| Reactivate Staff membership in own Organization | Yes | No | Emergency override only |
| Deactivate Staff membership in own Organization | Yes, when supported | No | Emergency override only |
| Assign Organization role Owner or Staff | Yes, with safeguards | No | Normally no |
| Assign an authorized product-local role | Yes | No | Normally no |
| Global account suspension | No | No by default | Yes |
| Global account reactivation | No | No by default | Yes |
| Deactivate Platform Account | No | No | Yes |
| Reactivate deactivated Platform Account | No | No | Yes, with step-up authentication |

### 10.14 Owner and Administrator Safety Rules

The system must prevent suspension, deactivation, or demotion of the only active Organization Owner.

```text
This Organization must have at least one active Owner.
Assign another Owner before changing this account.
```

The system must also prevent suspension, deactivation, role removal, or unsafe self-action against the final active Platform Administrator.

These protections must be enforced server-side, not only in the UI.

### 10.15 Required Status Action Labels

Use unambiguous labels:

**Platform Account**

- Suspend
- Reactivate
- Deactivate
- Move to Suspended

**Organization Membership**

- Suspend Membership
- Reactivate Membership
- Deactivate Membership

**Global Identity**

- Global Account Suspension
- Global Account Reactivation

Do not use one generic status action for different scopes.

### 10.16 Confirmation Dialog Requirements

Every status-changing dialog must show:

- user display name
- email address
- current status
- target status
- effect on login and scope access
- reason field when required
- Cancel and explicit confirmation actions

For Deactivated-to-Active, additionally require:

- acting administrator’s current password
- MFA challenge when enabled
- required reactivation reason

### 10.17 Organization Owner Indication

In Platform and Organization directories, the Organization role must be visible and separate from product-local roles.

```text
Maria Santos
Account type: Organization
Organization: ABC Sari-Sari Store
Organization role: Owner
Product role: POS Owner
```

```text
Carlo Reyes
Account type: Organization
Organization: ABC Sari-Sari Store
Organization role: Staff
Product role: POS Cashier
```

Use accessible text tags:

- **Owner** — visually emphasized tag
- **Staff** — neutral tag

Color must not be the only indicator. Organization role and POS role must never be combined into one badge.

---

## 11. Scope-Bound Session Rules

Every authenticated session must have one active scope.

```text
Platform session
Organization session
Personal session
```

A session must not silently switch account class.

When a new session starts:

- issue a new session or token
- load the correct scope
- clear previous navigation state
- clear previous permission caches
- clear organization context where inappropriate
- clear account-directory data from the previous scope
- prevent stale data from being displayed

Examples:

- Platform login must not reuse Organization navigation
- Organization login must not fetch Platform account data
- Personal login must not retain Organization context

---

## 12. Navigation Rules

### 12.1 Platform Accounts Menu

```text
Accounts
  All Accounts
  Platform Accounts
  Organization Accounts
  Personal Accounts
  Needs Review
```

Definitions:

- **All Accounts** — all account identities visible to authorized Platform staff
- **Platform Accounts** — Platform staff only
- **Organization Accounts** — identities with Organization Account profiles
- **Personal Accounts** — identities with Personal Account profiles
- **Needs Review** — incomplete, conflicting, failed, or manually reviewable records

### 12.2 Organization People Menu

```text
People
  Organization Staff
  Invitations
  Customers
  Customer Linking
```

Definitions:

- **Organization Staff** — staff memberships in the active organization (`/admin/organizations/{id}/members`)
- **Invitations** — staff invitation lifecycle (`/admin/organizations/{id}/invitations`)
- **Customers** — Business Customers
- **Customer Linking** — links a Business Customer to an app identity

Organization Staff and Invitations are separate routes and separate page components. Navigating to Invitations must replace Staff content (not query-tab reuse of the Staff page).

Organization Staff and Business Customers are different concepts.

### 12.2.1 Shared MVP staff person fields

Platform Staff and Organization Staff (including invite forms) share the same person fields:

| Field | Required | Notes |
|---|---|---|
| First Name | Yes | |
| Last Name | Yes | |
| Display Name | Yes | May default from First + Last |
| Email | Yes | Normalized; uniqueness via identity rules |
| Phone | No | Validated when provided |
| Employee Code | No | |
| Require Email Verification | Yes (choice) | |
| Account Status | System | Pending Verification / Active / Suspended / Deactivated |

**Platform-only:** Platform Role (required); Staff Number (`STF-000001`, unique, immutable, server-generated, not from email).

**Organization-only:** Organization Role (Owner / Staff); Branch (optional); Product Role (optional: POS Owner, Store Manager, Cashier, Reporting User).

Organization Role and Product Role must stay separate. Internal enum `OrganizationMember` may remain in persistence; user-facing label is always **Staff**.

### 12.3 Personal Menu

```text
Contacts
```

Contacts are people involved in Personal Utang relationships.

### 12.4 Menu-State Rule

| State | Behavior |
|---|---|
| Implemented and authorized | Visible and clickable |
| Planned but not implemented | Visible, disabled, marked Coming soon |
| Unauthorized | Hidden completely |

Hidden navigation does not replace server-side authorization.

---

## 13. Validation Identity Reference Set

The Local Validation dataset should use clean, single-scope identities.

### Platform

| Name | Account class | Role |
|---|---|---|
| Olivia Mendoza | Platform | Platform Administrator |
| Rafael Torres | Platform | Platform Support |

### Organization — ABC Sari-Sari Store

| Name | Account class | Organization role | POS role |
|---|---|---|---|
| Maria Santos | Organization | Owner | Owner |
| Carlo Reyes | Organization | Staff | Cashier |

### Organization — XYZ Mini Grocery

| Name | Account class | Organization role | POS role |
|---|---|---|---|
| Ana Cruz | Organization | Owner | Owner |
| Daniel Garcia | Organization | Staff | Cashier |

### Personal

| Name | Account class |
|---|---|
| Luis Navarro | Personal |
| Sofia Ramos | Personal |

Expected classification:

```text
Olivia Mendoza  → Platform only
Rafael Torres   → Platform only

Maria Santos    → Organization only
Carlo Reyes     → Organization only
Ana Cruz        → Organization only
Daniel Garcia   → Organization only

Luis Navarro    → Personal only
Sofia Ramos     → Personal only
```

No approved validation identity should receive multiple account profiles.

---

## 14. Invalid Flows

### Invalid Platform Creation

```text
Platform Admin creates user
→ identity created
→ no Platform profile
→ no Platform role
```

Reason: creates an unusable or unclassified account.

### Invalid Automatic Personal Profile

```text
Any identity is created
→ Personal profile added automatically
```

Reason: causes incorrect multi-scope classification.

### Invalid Organization Assignment

```text
ABC Owner creates staff
→ request payload selects XYZ
→ user becomes XYZ staff
```

Reason: violates organization isolation.

### Invalid Product Role Classification

```text
POS Cashier
→ treated as account class
```

Reason: product roles are not account classes.

### Invalid Direct Personal Creation by Platform

```text
Platform Admin directly creates active Personal user with password
```

Reason: bypasses self-registration, verification, consent, and ownership.

### Invalid Active Account Without Role

```text
Platform Account active
→ no Platform role
```

Reason: active Platform access must always be authorized by an assigned role.

---

## 15. Authorization Requirements

### Platform Staff Creation

Before creation:

1. authenticate Platform session
2. verify Platform Administrator permission
3. validate selected Platform role
4. validate verification/activation configuration
5. create identity and profile
6. assign role
7. commit transaction
8. audit result

### Organization Staff Creation

Before creation:

1. authenticate Organization session
2. validate active organization membership
3. validate staff-management permission
4. validate requested Organization role
5. validate optional product role
6. constrain membership to the active organization
7. create or link identity
8. commit transaction
9. audit result

Authorization must happen before sensitive data is queried or committed.

---

## 16. Audit Requirements

Audit at least:

- Platform staff invitation created
- Platform role assigned
- email verification required or bypassed
- verification delivery attempted
- delivery failed
- Organization staff invitation created
- Organization membership assigned
- Organization role assigned
- product-local role assigned
- existing identity linked
- account activated
- account suspended
- account reactivated
- account moved to Needs Review

Audit records must not contain:

- passwords
- activation secrets
- raw reset tokens
- unnecessary personal data

---

## 17. Local Validation Rules

Local Validation may allow:

- email verification disabled
- shared local activation secret
- local/null notification provider
- deterministic validation users
- reset and reseed workflow

These capabilities must be:

- explicitly configured
- unavailable in Production
- server-controlled
- audited where relevant
- fail-closed

Local Validation must still use:

- normal authentication
- normal authorization
- real database persistence
- real account profiles
- real memberships
- real role checks
- scope-bound sessions

---

## 18. Production Rules

Production must use the same application code and account model.

Only configuration differs.

Production should require:

- verified email or approved identity provider
- secure activation
- SMTP or approved messaging provider
- strong password policy
- role assignment before activation
- auditable invitations
- secure secrets
- TLS
- fail-closed validation configuration

Production must not enable:

- Local Validation seed users
- Local Validation reset
- shared validation passwords
- quick-login
- preview authentication shortcuts
- automatic profile inference

---

## 19. Acceptance Criteria

The user creation model is acceptable only when all of the following are true:

- Platform creation requires a Platform role
- Platform-created users become Platform-only
- Personal signup creates Personal-only accounts
- Start a Business creates ownership only for the new organization
- Organization invitation creates Organization-only staff
- Organization membership is not treated as account class
- POS role is not treated as account class
- no automatic Personal profile is created
- account status matches verification state
- SMTP failure does not silently activate an account
- duplicate email does not create a duplicate identity
- existing identity linking is explicit and audited
- cross-organization assignment is denied
- scope-bound sessions load only permitted navigation and data
- Local Validation behavior is disabled in Production
- the eight validation identities classify as 2 Platform, 4 Organization, and 2 Personal
- Organization roles display only as Owner or Staff
- Owner is visibly identified with an accessible text tag
- global account status and Organization membership status remain independent
- Suspended-to-Active requires normal confirmation
- Deactivated-to-Active requires acting-administrator password, MFA when enabled, and a reason
- Deactivated-to-Suspended remains allowed without restoring login
- routine Organization Staff suspension is controlled by the Organization Owner
- global suspension is reserved for authorized Platform-level security or compliance actions
- the final active Organization Owner and final active Platform Administrator are protected

---

## 20. Quick Reference

```text
Platform Admin creates user
→ Platform Account only
→ Platform role required

User signs up
→ Personal Account only

User starts a business
→ Organization Account
→ Owner of the new organization only

Organization Owner invites staff
→ Organization Account
→ membership in that organization only
→ Organization role required

Product role assignment
→ product-local authorization only
→ account class does not change
```

---

## 21. Permanent Rule

> **Never infer an account class from unrelated records.**  
> Account profiles must be created explicitly by the approved creation or invitation flow.
