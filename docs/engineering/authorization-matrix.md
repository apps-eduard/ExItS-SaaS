# Authorization Matrix

[Architecture summary](approved-architecture-summary.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md)

**Version:** 2.0  
**Status:** Authoritative  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Authorization formula

```text
Authorization
= active User Identity
+ active Account Profile
+ session bound to correct Account Class and scope
+ active Organization membership where required
+ valid Organization role
+ valid Subscription where required
+ enabled Product Entitlement
+ ready Product Instance
+ active Product-local role
+ operation permission
+ resource ownership
+ tenant isolation
```

Every layer fails closed.

Navigation visibility is not authorization.

---

## 2. Account-class boundaries

| Session | Allowed API families |
|---|---|
| Platform | Platform APIs only |
| Personal | Personal APIs only |
| Organization | Organization and authorized Product APIs only |

Cross-class calls are denied before domain execution.

A session must never silently change Account Class.

**Public identity resolve:** Authenticated sessions (Personal or Organization) may call `GET /api/v1/me/public-identity` and `POST /api/v1/users/resolve-public-id`. Resolve returns a minimal DTO only. Success does **not** grant membership, POS role, or Utang relationship — callers must use confirmation + existing invitation/create flows. Spec: [public-user-id-and-qr](../specs/identity/public-user-id-and-qr.md).

---

## 3. Platform roles

Initial Platform roles:

```text
Platform Administrator
Platform Support
```

Optional later roles may include Billing Administrator, Platform Auditor, and Platform Operations.

### Platform permission matrix

| Permission | Platform Administrator | Platform Support |
|---|---:|---:|
| `platform.accounts.view` | Yes | Yes |
| `platform.platform-staff.manage` | Yes | No |
| `platform.accounts.security-manage` | Yes | Limited by explicit permission |
| `platform.organizations.view` | Yes | Yes |
| `platform.catalog.manage` | Yes | No |
| `platform.plans.manage` | Yes | No |
| `platform.subscriptions.view` | Yes | Yes |
| `platform.subscriptions.manage` | Yes | No |
| `platform.entitlements.view` | Yes | Yes |
| `platform.entitlements.manage` | Yes | No |
| `platform.test-payments.view` | Local Validation only | Local Validation only |
| `platform.test-payments.simulate` | Local Validation only | No by default |
| `platform.audit.view` | Yes | Yes |
| `platform.permission.view_privacy_compliance` | Yes (Admin + Auditor) | No |
| `platform.permission.manage_privacy_compliance` | Yes (Admin only) | No |
| `platform.support-session.start` | According to explicit permission | According to explicit permission |

Canonical codes (Domain source of truth): `platform.permission.view_privacy_compliance`, `platform.permission.manage_privacy_compliance`. Privacy Compliance Admin is **Platform shell only** — never Personal, Organization, or POS. See [Phase 21](../phases/phase-21-privacy-compliance-and-regulatory-readiness.md).

### P25 / P26 privacy-sensitive access (summary)

Engineering access boundaries for post–Phase-21 identity and compliance readiness. Not a legal authorization opinion. Detail: [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md).

| Action / data | Personal | Org Owner | Org Staff / Cashier | Platform Admin | Public QR |
|---|---|---|---|---|---|
| View Privacy & Compliance workspace | No | No | No | View: Admin + Auditor; Manage: Admin | No |
| Resolve Personal QR (typed purpose) | Self / authorized link flows | Per feature | Per feature | Support under Platform rules | Identity-minimal only |
| Resolve / display Business QR | No | Yes (org essentials) | Policy-limited | Org management | DisplayName + `PublicOrganizationId` only |
| Ownership transfer initiate | No | Exact current Owner | No | Explicit support only | No |
| Ownership transfer accept | Personal QR target | — | No | No | No |
| Sales-document education acknowledge | No | Exact current Owner | No | No impersonation | No |
| View org compliance eligibility / profile status | No | Limited org status | No review details for cashiers | View; transitions via `ManageOrganizations` | **Nothing** (no TIN / eligibility / evidence) |
| Enable tax-document issuance capability | No | No self-enable | No | `ManageOrganizations` when Approved + current Owner ack (runtime still unavailable) | No |
| Future compliance evidence (not implemented) | N/A | Submit when built | No default access | Authorized reviewers only | No public URLs |

`platform.test-payments.*` must not exist as an effective Production permission.

Platform Admin may create or invite Platform Staff only. It must not normally create active Personal or Organization users.

---

## 4. Personal permissions

| Permission | Personal Account |
|---|---:|
| `personal.profile.view` | Yes |
| `personal.profile.manage` | Yes |
| `personal.contacts.manage` | Yes |
| `personal.utang.manage-own` | Yes |
| `personal.business.start` | Yes, verified identity required |
| `personal.organization.manage` | No |
| `personal.platform.manage` | No |
| `personal.pos.launch` | No from Personal session |

`personal.business.start` initiates an orchestration that creates an explicit Organization profile and new Organization. It does not grant Organization operations to the existing Personal session.

---

## 5. Organization roles

User-facing roles:

```text
Owner
Staff
```

| Permission | Owner | Staff |
|---|---:|---:|
| `organization.profile.view` | Yes | Yes |
| `organization.profile.manage` | Yes | No |
| `organization.staff.view` | Yes | According to policy |
| `organization.staff.invite` | Yes | No by default |
| `organization.staff.manage-membership` | Yes | No |
| `organization.subscription.view` | Yes | According to policy |
| `organization.subscription.change-plan` | Yes | No |
| `organization.billing.view` | Yes | No by default |
| `organization.products.view` | Yes | Yes |
| `organization.product-roles.manage` | Yes, within Product policy | No by default |
| `organization.audit.view` | Yes | According to policy |

The final active Owner is protected server-side from suspension, deactivation, removal, or demotion.

---

## 6. Start a Business authorization

Required:

1. authenticated Personal session
2. verified active User Identity
3. active Personal Account Profile
4. accepted required terms
5. authorized Product and active Plan
6. one-trial-per-Organization/Product policy
7. idempotency key
8. no client-controlled assignment to an existing unrelated Organization

Result:

```text
explicit Organization Account Profile
+ new Organization
+ Owner membership in new Organization only
+ Organization Subscription
+ Product Entitlement
+ Product provisioning
+ explicit POS Owner bootstrap
```

The resulting Organization session is newly issued or explicitly selected.

---

## 7. Commercial authorization

### Trial

| Action | Organization Owner | Platform Administrator |
|---|---:|---:|
| Start approved trial during onboarding | Yes | Support/repair only |
| View trial dates | Yes | Yes |
| Extend trial | No by default | Yes only with explicit permission, reason, and audit |
| Start repeated trial | No | No unless explicit exception policy |

### Plan change

| Action | Organization Owner | Platform Administrator | Staff |
|---|---:|---:|---:|
| View Plans | Yes | Yes | According to policy |
| Preview upgrade/downgrade | Yes | Yes | No |
| Upgrade | Yes | Yes, audited | No |
| Schedule downgrade | Yes | Yes, audited | No |
| Bypass payment result | No | Local Validation simulation only | No |
| Change another Organization | No | Yes only through Platform permission | No |

### Organization Web host (`:8093`)

| Role | Organization Web |
|---|---|
| Organization Owner / Administrator | Allowed (management center) |
| StoreManager (POS) | Allowed (day-to-day; Owner-only surfaces denied) |
| Cashier (POS) | **Denied** — use PinoyBusinessPOS MAUI |
| InventoryStaff / ReportingUser | Allowed with limited navigation |

Identity comes from authenticated Platform session + selected Organization + product Bearer introspection. Development-only organization/actor/commercial headers are not required outside Development/Testing. Post-login workspace list includes Organization entries for **Owner / Administrator** only (Cashier `OrganizationMember` excluded). Development Test User fills username only. See [organization-web-role-and-workflow-matrix.md](organization-web-role-and-workflow-matrix.md), [organization-web-ui-responsive-standard.md](organization-web-ui-responsive-standard.md), and [owner checklist](../validation/organization-web-responsive-owner-checklist.md).

### Test payment

A Local Validation payment simulation requires:

- Local Validation environment
- feature/configuration enabled
- authorized Platform or Organization test action
- current Organization/Subscription validation
- idempotency
- audit

The API must reject test-payment simulation in Production even when a client displays or submits the action.

### POS simulated retail payment (Card / GCash)

Local Validation / Development only:

| Action | Cashier (`CreateSale`) | Owner / Admin / Store Manager |
|---|---:|---:|
| Create / poll / cancel payment attempt | Yes | Yes |
| Dev simulate outcome (`/payment-attempts/{id}/simulate`) | Yes (non-Production host) | Yes (non-Production host) |
| Verify manual GCash transfer attempt | No | Yes |
| Set Paid directly from client | **No** | **No** |

Webhook ingress (`/payment-webhooks/{provider}`) requires valid HMAC signature; no user session. Production must reject simulate routes even if MAUI shows Dev buttons. See [P19-card-gcash-payment-ui-and-simulation](../reports/P19-card-gcash-payment-ui-and-simulation.md).

---

## 8. Product access

Required for POS launch:

```text
Active identity
+ Organization session
+ active membership in active Organization
+ valid Subscription state
+ enabled Entitlement
+ ready Product Instance
+ active POS Product role
```

POS roles:

```text
POS Owner
Store Manager
Cashier
Reporting User
```

Product roles are separate from Organization roles.

Correct:

```text
Organization role: Owner
POS role: POS Owner
```

Incorrect:

```text
Organization role: Cashier
Account class: POS Owner
```

---

## 9. Organization staff invitation

Required:

1. authenticated Organization session
2. active membership
3. staff-management permission
4. active Organization resolved server-side
5. valid requested Organization role: Owner or Staff
6. optional Product role validated separately
7. email/identity linking handled explicitly
8. membership constrained to current Organization

The normal Organization UI uses human fields and email. Raw User ID linking is support-only, permission-gated, reason-required, and audited.

---

## 10. Subscription and entitlement enforcement

- Trialing and Active permit operations only according to feature grants and Product role.
- PastDue follows explicit grace policy.
- Suspended denies protected operations.
- Cancelled and Expired deny new paid operations and may permit explicit continuity grants.
- Missing, invalid, stale beyond policy, or unsupported Entitlement fails closed.
- Plan limits are enforced server-side.
- Downgrade over-limit state retains existing data but blocks additional over-limit creation.

---

## 11. Cross-tenant protections

- Active Organization is resolved server-side.
- Browser-provided Organization ID is never trusted alone.
- Every Organization-owned query includes an Organization predicate.
- Cross-Organization access is denied without revealing sensitive existence.
- Organization switching clears Organization/Product caches and in-memory session state.
- Product role assignment is constrained to the same Organization and Product.
- Payment, Subscription, Entitlement, and provisioning identifiers are validated against the active Organization.

---

## 12. Production restrictions

Production must reject:

- development authorization headers
- Local Validation payment simulation
- Local Validation reset/reseed
- shared validation passwords
- quick login
- automatic account-profile inference
- fixed `10.0.2.2` endpoints as production URLs
