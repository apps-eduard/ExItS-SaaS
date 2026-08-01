# P16-WP01 — Entity, Role, API, and Migration Impact Matrix

[Architecture v1.5](saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md) | [ADR-016](../decisions/ADR-016-account-profile-isolation.md) … [ADR-020](../decisions/ADR-020-personal-utang-migration-and-provenance.md)

| Field | Value |
|---|---|
| Work package | P16-WP01 — Architecture and Domain Reconciliation |
| Status | Documentation complete (no schema/code in WP01) |
| Architecture | Accepted for Phase 16 implementation (2026-08-02) |
| Starting commit (post Admin fix) | `6f0ff2311c0141be92cfd52de279d1878d0b86c0` |
| Authorization for WP02 | Explicitly received (complete Phase 16 authorized) |

## Explicit constraints

| Constraint | Statement |
|---|---|
| Phase 14 | **Unchanged.** Production Deployment and Operations remains separate and unfinished. Phase 16 must not close, replace, or weaken Phase 14 requirements. |
| Production readiness | Application remains **not production-ready**. |
| Live Preview | Seeds and Live Preview paths exist; **Production remains blocked for LivePreview**. |
| WP01 scope | Documentation and reconciliation only — no schema migration, no account conversion, no breaking API changes. |

---

## 1. Current entities vs target model

| Current (code / persistence) | Architecture v1.5 concept | WP01 resolution |
|---|---|---|
| `PlatformUser` | **User Identity** (verified person) | Keep `PlatformUser` as persistence/domain name initially. Document and evolve APIs toward “User Identity” language. Split Account Profiles in WP02 without requiring a same-commit table rename. |
| *(none)* | **Account Profile** + **Account Class** (Platform / Personal / Organization) | New model in WP02. Today’s flat user effectively acts as an undifferentiated principal. |
| `PlatformUserCredential` + security stamp | Identity credential / stamp for User Identity | Remains identity-owned; sessions bind stamp at issue (already present as `SecurityStampAtIssue`). |
| `PlatformAuthSession` + optional `SelectedOrganizationId` | Scope-bound session with claims in ADR-017 | Extend additively: AccountProfileId, AccountClass, AllowedScope; org fields only for Organization sessions. |
| `OrganizationMembership` + `OrganizationRole` | Organization staff relationship inside Organization Scope | Retained; meaningful only under Organization Account sessions. |
| Platform system roles (`PlatformAdministrator`, `BillingAdministrator`, `PlatformSupport`) + `PlatformPermission` | Platform Account RBAC | Retained; valid only for Platform Account / Platform Scope. |
| POS product roles (Owner, Manager, Cashier, Viewer, …) + POS Utang/credit | Product-local authorization + Business Credit | Unchanged ownership; Organization session + entitlement + product role required. |
| POS Business Utang (product DB) | Organization Business Credit path | Distinct from Personal Utang (ADR-019). |
| Personal Utang | Personal Scope ledger | Design-only until WP04–WP05; no tables in WP01. |
| Support Session | Audited Platform→tenant support context | Not implemented as full Support Session yet; Admin org selection ≠ Support Session (ADR-018). |
| `UserDirectoryFilter` (All / Unassigned / Organization / PlatformStaff) | Account-class directory views | Replace conceptually with All / Platform / Organization / Personal / Requires Assignment (architecture §3.4). Persistence filter enum may lag UI wording. |

### Conceptual mapping (transition)

```text
PlatformUser.Id          ≈ UserIdentityId
(future AccountProfile)  ≈ AccountProfileId + AccountClass
PlatformAuthSession.Id   ≈ SessionId
SecurityStampAtIssue     ≈ SecurityStamp (at issue)
SelectedOrganizationId   ≈ ActiveOrganizationId (Organization sessions only; server-validated)
```

---

## 2. Role and permission mapping

| Layer | Current | Target under Phase 16 | Notes |
|---|---|---|---|
| Identity | `PlatformUser` | User Identity | No permissions by itself. |
| Platform RBAC | `PlatformSystemRole` / role definitions + `PlatformPermission.*` | Platform Account permissions | Includes ViewPortfolio, ManageOrganizations, ManageCatalog, ManagePlatformUsers, ManageMemberships, ManageProductAccess, ManageSubscriptions, ManageManualPayments, ManageEntitlementOverrides, ViewAuditRecords. Future: Support Session start / elevate permissions. |
| Organization membership | `OrganizationOwner`, `OrganizationAdministrator`, `OrganizationMember` | Organization Account + membership role | Does not grant Platform or Personal APIs; does not alone grant POS. |
| Product (POS) | POS Owner / Manager / Cashier / Viewer / … | Product-local roles | Require org entitlement + active membership + product role (ADR-011). |
| Personal Utang | *(none)* | Lender / Borrower relationship roles | Not permanent RBAC; Personal Scope only. |
| Business Customer | POS customer / credit customer | Organization relationship, not staff | Must not be treated as Organization Member. |
| Support | Informal Admin org context | Support Session (ADR-018) | Not membership; read-only default. |

### Authorization formula (target)

```text
Organization entitlement
+ active Organization membership
+ active product-local role
= product access
```

Platform permissions never inherit into Organization or Personal. Personal relationship roles never inherit into Organization or Platform.

---

## 3. API route family impacts

| Route family (target) | Current state | Impact |
|---|---|---|
| `/api/v1/platform/*` | Primary Platform API surface (users, orgs, memberships, catalog, authz, audit, …) | Remain Platform Scope. Guard: Platform Account session only. Admin continues here. Rename “users” → identity/profiles is later and additive where needed. |
| `/api/v1/personal/*` | Largely absent | Introduce with Personal foundation / Utang (WP04–WP06). Reject Platform and Organization sessions. |
| `/api/v1/organizations/{organizationId}/*` | Org-scoped resources often under `/platform/organizations/...` with actor/dev patterns | Evolve toward Organization Account family; server-validate membership and ActiveOrganizationId. Avoid trusting client org headers alone. |
| `/api/v1/products/{productCode}/*` or POS product APIs | PinoyBusinessPOS APIs with org + commercial/role middleware | Keep product-owned; require Organization session class + entitlement + product-local role. No Personal or Platform ordinary access. |
| Support Session endpoints | Not as dedicated audited Support Session | Future Platform APIs under `/platform/...` for start/end/elevate; distinct from org switcher. |

Cross-class calls are denied **before** domain execution (architecture §17.2).

---

## 4. Database ownership

| Data | Owner DB | Phase 16 note |
|---|---|---|
| User Identity, credentials, sessions, account profiles | **Platform** | Additive profile/session columns or tables in WP02+. |
| Organizations, memberships, Platform roles/permissions, catalog, subscriptions, entitlements, Platform audit | **Platform** | Unchanged ownership. |
| Personal profile, Personal Utang, personal reminders/preferences | **Personal ownership boundary** (Platform-hosted Personal tables or dedicated Personal store — decide in WP04/WP05; must not live in POS tenant tables) | No WP01 schema. |
| POS sales, inventory, **Business Utang/credit**, product-local roles | **PinoyBusinessPOS** | Unchanged; no Personal Utang rows. |
| Cross-DB FKs | **Forbidden** | Stable IDs only (ADR-003). |

---

## 5. Migration / rollout strategy

| Phase | Strategy |
|---|---|
| WP01 | Docs/ADRs/matrix only. |
| WP02+ sessions | **Additive** session fields; issue new claims on login/profile selection. Prefer re-login over silent reinterpretation of old sessions. |
| Backward compatibility | During transition, accept legacy sessions that lack AccountClass only where explicitly documented and narrowly scoped (e.g. Live Preview / Dev), then force profile-bound re-issue. Do not treat legacy session + SelectedOrganizationId as Platform Support Session. |
| Persistence rename | Optional later rename `PlatformUser` → UserIdentity; **not required** to start WP02 if mapping is documented and APIs remain coherent. |
| Personal Utang → Business Credit | Optional, selective, previewed, idempotent, audited (ADR-020); no continuous sync. |
| Production | No production migration apply-at-startup; Phase 14 unfinished; LivePreview blocked in Production. |

---

## 6. Terminology conflicts and resolutions

| Conflict | Resolution |
|---|---|
| “Platform User” vs User Identity | **User Identity** = verified person. Persist as `PlatformUser` initially. Admin copy moves toward Identity / Accounts. |
| “Platform User” vs Platform Account | **Platform Account** = account class for SaaS vendor staff. Not every `PlatformUser` row is a Platform Account. |
| User directory “Unassigned” / “Personal Product Users” | Prefer **Requires Assignment** and **Personal Accounts**. Valid Personal Accounts are not “unassigned.” |
| Selected organization vs Organization Account | Org selection is **context inside** an Organization session, not a substitute for Account Class. |
| Admin org browsing vs Support Session | Browsing Platform org records ≠ tenant operational Support Session (ADR-018). |
| POS “Utang” vs Personal Utang | POS Utang = **Business Credit** path. Personal Utang = free Personal Scope feature (ADR-019). Shared libraries only. |
| Membership vs product role vs entitlement | Three separate grants: Org Owner ≠ POS entitlement ≠ POS Owner role. |
| Customer link / Utang link vs staff | Linking never creates Organization membership. |

---

## 7. Inventory snapshot (as of starting commit)

| Area | Present | Gap vs v1.5 |
|---|---|---|
| Flat identity + credential + session | Yes | No AccountProfile / AccountClass |
| Optional SelectedOrganizationId | Yes | Not full ActiveOrganizationId + ValidatedMembershipId enforcement model |
| Platform + org role catalogs | Yes | Not scope-bound to account class on session |
| POS Business Utang | Yes | N/A — keep; separate from Personal |
| Personal Account / Personal Utang | No | WP04–WP06 |
| Support Session entity/API | No | Later WPs / hardening |
| API family guards by AccountClass | No | WP02 |

---

## 8. Exit checklist (WP01)

- [x] Architecture v1.5 status accepted for Phase 16 implementation
- [x] ADR-016 … ADR-020 accepted
- [x] Entity and API impact matrix complete
- [x] Migration / rollout strategy documented
- [x] Critical terminology conflicts resolved in this matrix
- [x] Authorization for P16-WP02 received (complete Phase 16 authorized by user)

Completion report, portfolio-progress, and phase status tables are owned by the parent closeout — not updated in WP01 doc-only file creation.
