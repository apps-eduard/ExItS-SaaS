# P18-WP08 — End-to-End Validation and Closeout

| Field | Value |
|---|---|
| Status | **In Progress** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) — **Open** |
| Implementation commit | `4b8b7270417d0f9e612855ed746d7fd80819adee` |
| Validation-fix commit | Recorded in §14 after push (this closeout remains open) |
| Production-ready | **No** |
| User mobile validation | **Pending User Validation** — Phase 18 must not be marked Complete until the user explicitly confirms |
| Date | 2026-08-04 |

## 1. Objective

Hold Phase 18 open until the user personally validates the MAUI mobile application. Record automated test and build evidence now; record the final validation outcome only after explicit user confirmation.

## 2. Scope

In progress closeout work package. Does **not** close Phase 18. Does **not** claim device validation passed.

## 3. Existing functionality reused

Phases 13–17 Platform auth/personal/start-business/product-local roles and POS operational APIs/screens; DesignSystem; localization. Phase 18 implementation commit `4b8b727`.

## 4. Backend / API completion status

**Implemented** (code) via reuse and Maui client expansion. Awaiting user mobile validation of the end-to-end journey.

## 5. MAUI frontend completion status

**Implemented** (code) — screens and flows exist. **User Mobile Validation Pending.**

## 6. Files / components changed

Implementation: `4b8b727`. Validation-pass fixes: auth bind commercial grants, Local Validation Quick Login, Owner/Manager home routing, org-entry HasPosAccess gate, Organization Owner bind bootstrap, catalog/expense category reload hardening, soft-input/keyboard inset. This WP remains open for user checklist results.

## 7. Authorization and organization-isolation behavior

API-authoritative: Platform session for Personal/Org essentials; POS bearer + org context for operations; selling mode is UI mode only. Organization Owner may enter Owner / Manager / Cashier UI without changing the POS grant (`SellingModeService.PreferredHomeRoute`).

## 8. Tests executed and totals (automated)

| Suite | Result |
|---|---|
| MAUI.Tests | **83 passed**, 0 failed |
| Focused Auth + RoleHomeResolver | **27 passed**, 0 failed (subset) |

Regression added/updated in this validation pass:

- `SelectOrganization_with_token_bind_loads_commercial_grants` — bind path persists `SubscriptionStatus` / feature grants
- `ResolvePosHome_uses_preferred_home_when_effective_role_unavailable`
- `ResolvePosHome_uses_preferred_home_when_effective_status_none`
- `ResolvePosHome_without_preferred_still_denies_when_effective_missing`

## 9. MAUI build result

**Build Verified** — Android Debug APK built and installed on emulator `HealthCare_Pixel_API34` during the validation pass. Not a claim of Device Verified for the Phase 18 journey.

## 10. Emulator / device / user validation result

**Pending User Validation.** Phase 18 remains **Open**.

Agent-assisted Local Validation evidence (AVD `HealthCare_Pixel_API34`, Platform `:8091` / POS `:8092`, Mailpit `:8025`):

| # | Scenario | Result |
|---:|---|---|
| 1 | Personal-only login → Personal home | **Pass** (earlier pass) |
| 2 | Personal Start a Business CTA + no POS chrome | **Pass** (earlier pass) |
| 3 | Account context switcher visible (Personal) | **Pass** (earlier pass) |
| 4 | One-org login → org/POS bind | **Partial** (succeeded in earlier run; later BindToken timing flaky) |
| 5 | Context switcher on More hub | **Pass** (when POS shell reached) |
| 6 | Switch to Personal without logout | **Partial** (API/service shipped; automation flaky) |
| 7 | Multi-org selector lists ABC + XYZ with roles | **Pass** |
| 8 | Multi-org enter / role home | **Partial** — see §10a |
| 9 | Sign out | **Partial** |
| 10 | Sign-in again / restore | **Partial** |
| 11 | Clear session → login → add category/product | **Issue found** — Access Restricted; fix shipped; **retest pending user** |
| 12 | Quick Login Org identity → Owner / Manager | **Issue found** — Access Denied; fix shipped; **retest pending user** |

**Not Device Verified** for the full Phase 18 journey. Do not mark passed or Phase 18 Complete until the user confirms the checklist.

### 10a. Confirmed issues fixed in this validation pass

#### Issue A — Catalog Access Restricted after clear + login + org entry

| Field | Detail |
|---|---|
| Tested scenario | `adb shell pm clear` → sign in (e.g. kissy) → select organization as Owner → add category/product |
| Observed | UI showed Access Restricted for Manage Catalog |
| Root cause | `SelectOrganizationWithBindAsync` bound the POS token but did not populate session `SubscriptionStatus` / `EnabledFeatureCodes`. Client `UtangCapabilityEvaluator` gates `ManageCatalog` on those commercial fields |
| Fix | After successful bind with product access, resolve commercial grants via introspect (fallback evaluate) and persist on the session; Platform also bootstraps POS Owner product-local role for Organization Owner on bind when entitlement exists but role is missing |
| Validation result | Automated regression: `SelectOrganization_with_token_bind_loads_commercial_grants` **Pass**. User retest of add category/product **Pending** |
| Commit | See §14 |

#### Issue B — Quick Login Organization → Owner / Manager Access Denied

| Field | Detail |
|---|---|
| Tested scenario | Local Validation Quick Login → Kissy Organization identity → choose Owner or Manager |
| Observed | Access Denied on Owner/Manager home |
| Root cause | `/permissions/effective` returned Status `None` when Dev role resolution set only an in-memory Owner (no DB row yet). `RoleHomeResolver` treated that as Access Denied even after Owner working-as was chosen |
| Fix | POS effective endpoint applies request-role fallback from `PosRoleRequestContext`; `RoleHomeResolver` keeps PreferredHomeRoute when effective role is unavailable/None; Organization entry navigates to Org essentials when `HasPosAccess` is false; Quick Login sets Platform org context for Organization identities |
| Validation result | Automated: preferred-home fallback tests **Pass**. User retest of Org Quick Login → Owner/Manager **Pending** |
| Commit | See §14 |

Fixes also recorded earlier / adjacent in this pass:

- Soft-input AdjustResize + keyboard inset for address/setup fields
- Catalog/expense category add: loading stuck after save (try/finally + quiet reload)
- Owner org entry shows Owner / Manager / Cashier working-as buttons
- Local Validation Quick Login dropdown on MAUI Sign-in (excludes Platform identities; uses SharedPassword)

## 11. Known limitations

- User mobile validation not yet confirmed
- Quick Login uses Local Validation SharedPassword — custom passwords (e.g. kissy `123`) require SharedPassword alignment or manual credential form
- Offline-capable selling not claimed
- Production TLS / MAUI-HTTPS / Phase 14 blockers unchanged
- Formal accessibility certification not claimed

## 12. Deferred items / post-MVP

Multi-branch; gateway payments; split tender; advanced analytics; custom roles; multi Organization Owner; full Org Admin on Mobile.

## 13. Current status

**In Progress.** Phase 18 remains **Open**: Code Complete and Build Verified; User Mobile Validation Pending. Not production-ready. **Not Device Verified. Not Complete.**

## 14. Commit / push / git status

| Item | Value |
|---|---|
| Implementation commit | `4b8b727` |
| Validation-fix commit | *Filled after push* |
| Phase 18 Complete closeout commit | **Not created** — blocked until user confirmation |

### Remaining user retest items (after this fix commit)

1. Clear app → sign in → org Owner → add category and product (no Access Restricted)
2. Quick Login → Kissy **Organization** → Owner home loads
3. Quick Login → Kissy **Organization** → Manager home loads
4. Quick Login → Kissy **Personal** → Personal / org chooser still correct
5. Continue full checklist in § User mobile validation checklist

---

## WP01–WP08 status table

| WP | Status |
|---|---|
| P18-WP01 Mobile foundation and authentication | Code Complete and Build Verified |
| P18-WP02 Personal account and Start a Business | Code Complete and Build Verified |
| P18-WP03 Organization selection and Owner essentials | Code Complete and Build Verified |
| P18-WP04 POS role routing and navigation | Code Complete and Build Verified |
| P18-WP05 POS Owner and Manager Mobile experience | Code Complete and Build Verified |
| P18-WP06 Cashier selling experience | Code Complete and Build Verified |
| P18-WP07 Mobile security, resilience, localization | Code Complete and Build Verified |
| P18-WP08 End-to-end validation and closeout | **In Progress** |

---

## User mobile validation checklist

Phase 18 closes only after the user personally validates the MAUI app and explicitly confirms the outcome.

Instructions: mark each item Pass / Fail / Blocked / Skipped with notes. Do not have an agent invent results.

| # | Validation item | Result (user) | Notes |
|---:|---|---|---|
| 1 | Registration | Pending | |
| 2 | Activation | Pending | |
| 3 | Sign-in | Pending | Includes Quick Login retest |
| 4 | Session restore | Pending | |
| 5 | Start a Business | Pending | |
| 6 | Organization creation | Pending | |
| 7 | Organization selection | Pending | Owner/Manager/Cashier entry |
| 8 | Organization Owner essentials | Pending | |
| 9 | Staff creation / invitation | Pending | |
| 10 | POS role assignment | Pending | |
| 11 | POS setup | Pending | |
| 12 | Product creation | Pending | Retest after Access Restricted fix |
| 13 | Register and shift | Pending | |
| 14 | Owner Start Selling | Pending | |
| 15 | Manager Start Selling | Pending | |
| 16 | Cashier selling | Pending | |
| 17 | Cash checkout | Pending | |
| 18 | Receipt | Pending | |
| 19 | Stock reduction | Pending | |
| 20 | Shift close | Pending | |
| 21 | Reports | Pending | |
| 22 | Entitlement denial | Pending | |
| 23 | Membership suspension | Pending | |
| 24 | Role revocation | Pending | |
| 25 | Logout | Pending | |

### User confirmation block (fill only after validation)

| Field | Value |
|---|---|
| Validator | |
| Date / time | |
| Device or emulator used | |
| Overall outcome | Pending User Validation |
| Explicit confirmation to close Phase 18 | **No** — required before Complete |

---

## Complete end-to-end journey (evidence classes)

```text
User registers in Mobile
→ signs in
→ starts a business
→ organization is created
→ user becomes Organization Owner and first POS Owner
→ continues inside Mobile
→ completes POS setup
→ creates a product
→ adds staff
→ assigns POS Cashier
→ Cashier signs in
→ starts shift
→ completes cash sale
→ receipt is displayed
→ inventory is reduced
→ shift is closed
→ Owner or Manager views reports
→ Owner or Manager taps Start Selling without changing role
```

| Step group | Implemented in code | Covered by automated tests | Build verified | User validated |
|---|---|---|---|---|
| Register / sign-in / Start Business / Owner+POS Owner | Yes | Partial | Yes | Pending |
| Org essentials / staff / POS role assign | Yes | Partial | Yes | Pending |
| POS setup / product / shift / cash sale / receipt / inventory | Yes | Yes | Yes | Pending |
| Reports / Start Selling mode | Yes | Partial | Yes | Pending |
| Denial / suspension / revocation / logout | Yes | Partial | Yes | Pending |

## Production-readiness statement

Phase 18 is **Open** and does **not** make the portfolio production-ready. Do not claim Phase 18 Complete, WP08 closed, device validation passed, or production readiness without the user’s explicit confirmation after mobile validation.
