# P25-WP04 — Web Host Legacy Cleanup and Local Validation Identity Determinism

## 1. Assignment

| Field | Value |
|---|---|
| Phase | 25 |
| Work package | P25-WP04 Web Host Legacy Cleanup and Local Validation Identity Determinism |
| Status | Code Complete / Ready for Owner Validation |
| Branch | `main` |
| Date | 2026-08-13 |
| Starting SHA | `e979cc12182a73d1db249051e62b560541e415d1` |
| Device Verified | **No** |
| Production Ready | **No** |
| Database migration | **No** |

## 2. Observed problem

After `Reset-LocalValidation.ps1 -ConfirmReset`, the owner created/subscribed one user (**Mica Uy**). Canonical login at `http://127.0.0.1:8090/admin/login` still listed many identities (Organization, Personal, Platform Administration), including Maria Santos, Carlo Reyes, Ana Cruz, Daniel Garcia, Luis Navarro, and Sofia Ramos.

Separately, Platform.Admin still hosted live Organization and Personal product pages after P25-WP02/WP03 split Org Web (:8093) and Personal Web (:8094).

## 3. Identity audit — why extra picker entries appeared

**Picker source:** `GET /api/v1/platform/local-validation/quick-login-identities` → `ListLocalValidationQuickLoginIdentities`.

Classification of each picker entry:

| Class | Meaning |
|---|---|
| **B** | Dynamically discovered **active** Platform users with password credentials and active account profiles |
| **A** | Canonical baseline fixtures Olivia/Rafael when those rows exist (labeled `Baseline ·`) |
| **C** | Full catalog fixture *definitions* exist in `LocalValidationIdentityCatalog` but are **not** listed unless corresponding DB rows are active |
| **D** | Generated org-staff fixtures only when Full seed materialized them |
| **E** | Historical Full-catalog rows that survived `PlatformAdministratorsOnly` seed (the defect) |
| **F** | Not leaking from unit-test fixtures |
| **G** | Production path returns **404**; Development/Local Validation only |

The picker is **not** a static catalog dump. It lists current database accounts. Account class comes from `AccountProfile.AccountClass` + active memberships + Platform role assignments — not display names.

Root causes of the surprise list:

1. **Reset** wipes volumes and reseeds Olivia + Rafael only.
2. Ordinary **Start** after reset used `PlatformAdministratorsOnly` but **did not decommission** Full-catalog users if they were already in the database (additive seed).
3. A later **Full** seed (or leftover Full DB) recreated Maria/Carlo/Ana/Daniel/Luis/Sofia.
4. Owner-created **Mica** correctly appeared as a real DB user.

Chosen model: **MODEL A**

- Picker = current Local Validation database accounts.
- Canonical baseline fixtures Olivia + Rafael are always intended after reset/start (labeled Baseline).
- Owner-created accounts (Mica) appear in the scopes they actually own.
- Full catalog fixtures are **decommissioned** when SeedScope is `PlatformAdministratorsOnly`.
- `-SeedScope Full` is an explicit opt-in that recreates the eight-identity demo catalog.

## 4. Reset ownership matrix

`.\tools\Reset-LocalValidation.ps1 -ConfirmReset` is **Local Validation only** (rejects Production). It stops apps, **deletes named Docker volumes** `exits_local_validation_platform_db_data` and `exits_local_validation_pos_db_data`, clears Local Validation DataProtection keys, then starts with `-SeedScope PlatformAdministratorsOnly -PurgeTransactional`.

| Data | Reset volume wipe | Ordinary Start (admins-only) | Start `-SeedScope Full` |
|---|---|---|---|
| Platform users | Wiped, then Olivia + Rafael reseeded | Keep owner-created; **decommission Full catalog fixtures** | Recreate 8 catalog identities |
| Credentials | Wiped / reseeded for baseline | Baseline refreshed; owner-created kept | Catalog credentials reseeded |
| Personal profiles | Wiped | Owner-created kept; Full-catalog Personal (Luis/Sofia) decommissioned | Luis/Sofia + utang seed |
| Organizations | Wiped | Owner-created kept; ABC/XYZ demo orgs closed when not Full | ABC + XYZ recreated |
| Memberships / invitations | Wiped | Owner-created kept; fixture memberships revoked | Catalog memberships recreated |
| Subscriptions / entitlements | Wiped; SaaS catalog reseeded | Owner-created kept; demo orgs closed | Catalog orgs subscribed |
| POS staff / products / sales / shifts | POS volume wiped | Untouched except when purge flag set | Full POS seed with catalog orgs |
| Quick-login **catalog definitions** | Never deleted (code) | Unchanged | Unchanged |
| Quick-login **picker rows** | Only baseline (+ later owner users) | Baseline + owner users; Full fixtures gone | Baseline + Full catalog |

**“Reset users and transactions”** in owner language maps to this **full Local Validation reset**, not a production wipe and not “hide the fixture catalog file.”

Transactional-only purge (`PurgeTransactionalOnSeed` without volume wipe) clears operational rows while keeping identities. Full volume reset is the clean baseline.

## 5. Web audit — route ownership

| Route | Project | Scope | Routable? | Nav visible? | Redirect only? | Target | Action |
|---|---|---|---|---|---|---|---|
| `/admin/login` | Admin | Auth | Yes | N/A | No | Admin | KEEP |
| `/admin` | Admin | Platform | Yes | Yes | Org/Personal → handoff | Admin | KEEP |
| `/admin/workspaces` | Admin | Auth | Yes | Yes | No | Admin | KEEP |
| `/admin/handoff/{app}` | Admin | Auth | Endpoint | Yes | Handoff | Admin | KEEP |
| `/admin/organizations` (+ `/{Id}`) | Admin | Platform operator | Yes | Platform nav | Org shell → 8093 `/overview` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/members` | Admin | Platform operator | Yes | Platform memberships | Org shell → 8093 `/staff` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/invitations` | Admin | Platform operator | Yes | Platform | Org shell → `/staff/invite` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/roles` | Admin | Platform operator | Yes | Platform | Org shell → `/staff/roles` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/branding` | Admin | Platform operator | Yes | Platform settings | Org shell → `/organization/profile` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/catalog` | Admin | Platform operator | Yes | Platform | Org shell → `/products` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/enabled-products` | Admin | Platform operator | Yes | No org-shell nav | Org shell → `/account/subscription` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/commercial` | Admin | Platform operator | Yes | No org-shell nav | Org shell → `/account/subscription` | Admin operator | KEEP + org-shell redirect |
| `/admin/organizations/{id}/product-access` | Admin | Platform support | Yes | No | No | Admin | KEEP |
| `/admin/personal/*` | Admin | Compatibility | Yes | No (handoff nav) | Yes → 8094 | Personal Web | KEEP redirect |
| `/admin/personal/start-business` | Admin | Compatibility | Yes | No | Yes → 8094 `/start-business` | Personal Web | KEEP redirect |
| `/overview`, `/staff`, `/products`, … | Org Web | Organization | Yes | Org sidenav | No | Org Web | KEEP |
| `/home`, `/utang/*`, `/start-business`, … | Personal Web | Personal | Yes | Personal sidenav | No | Personal Web | KEEP |
| Org/Personal `/login` | Product hosts | Auth CTA | Yes | No picker | Canonical 8090 | Admin login | KEEP |

Start a Business **form** lives on Personal Web (`StartBusiness.razor`) using Personal onboarding APIs. Admin keeps a compatibility redirect only. No duplicate form.

## 6. Cleanup delivered

- Local Validation `PlatformAdministratorsOnly` decommissions Full-catalog fixture users (stable email/username keys, not display names) and closes ABC/XYZ demo orgs when not Full.
- Quick-login labels canonical baseline entries; picker helper text states MODEL A.
- Admin org-shell / personal-shell sidenav: Open Web + Workspaces only (no hidden product menus).
- Organization-shell hits on Admin org product URLs hand off to 8093 with `SafeReturnPath`.
- Personal Admin pages remain compatibility redirects to 8094.
- Start a Business presentation on Personal Web; Admin redirect retained.
- Dead AdminNav org/personal submenu fragments removed.

Backend Personal/Organization application and MAUI were not deleted.

## 7. Auth preserved

Canonical sign-in remains 8090. Separate cookies `.ExItS.Admin.*` / `.ExItS.OrgWeb.*` / `.ExItS.PersonalWeb.*`. One-time handoff. One quick-login picker. Production quick-login **404**. Invalid picker key rejected.

## 8. Tests

See Git/test evidence in this report’s closeout section and the final agent report. Architecture tests guard host ownership, one canonical login, no product pages in `ExItS.Web.UI`, and org-shell handoff. Local Validation contract tests cover picker source, seed keys, Full-fixture decommission, Production unavailability, and AccountClass routing.

## 9. Owner acceptance checklist

**A. RESET**

1. Stop Local Validation.
2. Run `.\tools\Reset-LocalValidation.ps1 -ConfirmReset`.
3. Start Local Validation (default SeedScope = PlatformAdministratorsOnly).
4. Open 8090 `/admin/login`.
5. Confirm only Olivia + Rafael (Baseline) appear.
6. Repeat reset/start.
7. Confirm no duplicate identities.

**B. OWNER CREATED USER**

8. Create Mica Uy using the normal flow.
9. Subscribe / create organization as intended.
10. Reload quick login.
11. Confirm Mica appears only in scopes she truly owns (Personal and/or Organization Administration — not Platform Administration unless she has a Platform role).
12. Confirm Maria/Carlo/Ana/Daniel/Luis/Sofia do **not** appear unless `-SeedScope Full` was used.

**C. PLATFORM**

13. Login Platform Administration (Olivia/Rafael).
14. Confirm 8090.
15. Platform-only sidenav.
16. Try old Org routes as an Organization identity → redirect to 8093, not live Admin org product UX.
17. Try old Personal routes → redirect to 8094, not live Admin Personal UX.

**D. ORGANIZATION**

18. Login Mica Organization identity.
19. Confirm 8093.
20. Org sidenav.
21. Correct organization context.
22. No duplicate product page on 8090.

**E. PERSONAL**

23. Login Mica Personal identity if she has Personal scope.
24. Confirm 8094.
25. Personal sidenav.
26. Start a Business form on 8094.
27. No duplicate Personal page on 8090.

**F. SSO**

28. Personal → Organization switch (no second password).
29. Organization → Personal switch.
30. Unauthorized scope absent/denied.

**Browser Verified: No** until the owner confirms. **Device Verified: No.** **Production Ready: No.**

## 10. Exclusions

- No production reset capability.
- No shared unrestricted cookie.
- No checkout on Org/Personal Web.
- No deletion of Personal domain/application or Organization backend.
- No MAUI Personal deletion.
- Unrelated Personal linked-merchant stash left untouched.

## 11. Git

Starting SHA: `e979cc12182a73d1db249051e62b560541e415d1`

| Commit | Message |
|---|---|
| `11269762` | fix(local-validation): make quick login identities deterministic |
| `11f22c6d` | refactor(platform-admin): remove migrated organization and personal product UI |
| `4093c092` | test(web): guard host route ownership and compatibility redirects |
