# P18-WP08 — End-to-End Validation and Closeout

| Field | Value |
|---|---|
| Status | **In Progress** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) — **Open** |
| Implementation commit | `4b8b7270417d0f9e612855ed746d7fd80819adee` |
| Validation-fix commit (commercial grants / preferred home) | `3e4314cc35a4428cdaf258df54ed005cbd7080c0` |
| Validation-fix commit (Quick Login / Access Denied follow-up) | *(see §14 — filled after push)* |
| PhysicalDevice Tailscale profile | `9022d95` |
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

Implementation: `4b8b727`. Validation-pass fixes: auth bind commercial grants (reject Active+empty features; Development grant fallback), Local Validation Quick Login (dedupe per user; profile select; AccessToken required; denser fonts), Owner/Manager/Cashier preferred-home before bind, `NotifySessionAccessChanged` after org persist, dashboard preferred-home trust, PhysicalDevice Tailscale Debug profile. This WP remains open for user checklist results.

## 7. Authorization and organization-isolation behavior

API-authoritative: Platform session for Personal/Org essentials; POS bearer + org context for operations; selling mode is UI mode only. Organization Owner may enter Owner / Manager / Cashier UI without changing the POS grant (`SellingModeService.PreferredHomeRoute`).

## 8. Tests executed and totals (automated)

| Suite | Result |
|---|---|
| MAUI.Tests | **86 passed**, 0 failed |
| Focused Auth + RoleHomeResolver | **30 passed**, 0 failed (subset) |
| PosSyncStatusAndAccessPolicyTests | **14 passed**, 0 failed |

Regression added/updated in this validation pass:

- `SelectOrganization_with_token_bind_loads_commercial_grants`
- `SelectOrganization_with_token_bind_fills_dev_grants_when_features_missing`
- `ResolvePosHome_uses_preferred_home_when_effective_role_unavailable`
- `ResolvePosHome_uses_preferred_home_when_effective_status_none`
- `ResolvePosHome_uses_preferred_home_when_effective_role_unparseable`
- `ResolvePosHome_uses_preferred_home_for_unknown_pos_role_codes`
- `ResolvePosHome_without_preferred_still_denies_when_effective_missing`
- `NotifySessionAccessChanged_rearms_shell_after_clear_without_initialize`

## 9. MAUI build result

**Build Verified** (emulator + PhysicalDevice Debug APKs). Not a claim of Device Verified for the Phase 18 journey.

| Build | Path / notes |
|---|---|
| Emulator Debug | `…\bin\Debug\net10.0-android\com.exits.pinoybusinesspos-Signed.apk` (`127.0.0.1` + `adb reverse`) |
| PhysicalDevice Debug (arm64) | `…\bin\Debug\net10.0-android\android-arm64\com.exits.pinoybusinesspos-Signed.apk` — also copied as `C:\Users\speed\Desktop\ExItS-POS-APK\ExItS-POS-PhysicalDevice-Debug.apk` (`PosLocalValidationTarget=PhysicalDevice`, Tailscale `100.120.79.81`) |

## 10. Emulator / device / user validation result

**Pending User Validation.** Phase 18 remains **Open**. **Not Device Verified.**

Agent-assisted Local Validation evidence (AVD `HealthCare_Pixel_API34`, Platform `:8091` / POS `:8092`, Mailpit `:8025`):

| # | Scenario | Result |
|---:|---|---|
| 1 | Personal-only login → Personal home | **Pass** (emulator, earlier) |
| 2 | Personal Start a Business CTA + no POS chrome | **Pass** (emulator, earlier) |
| 3 | Account context switcher visible (Personal) | **Pass** (emulator, earlier) |
| 4 | One-org login → org/POS bind | **Retest** (earlier Partial / flaky BindToken) |
| 5 | Context switcher on More hub | **Pass** (emulator, when POS shell reached) |
| 6 | Switch to Personal without logout | **Retest** (API shipped; automation flaky) |
| 7 | Multi-org selector lists ABC + XYZ with roles | **Pass** (emulator) |
| 8 | Multi-org enter / role home | **Retest** |
| 9 | Sign out | **Retest** |
| 10 | Sign-in again / restore | **Retest** |
| 11 | Clear session → login → add category/product | **Retest** (fix shipped; user confirmation pending) |
| 12 | Quick Login → Owner / Manager / Cashier (no Access Denied) | **Retest** (fix shipped; user confirmation pending) |
| 13 | Physical phone Tailscale install + health reachability | **Retest** (APK prepared; user phone results not confirmed) |
| 14 | Physical phone Quick Login → Owner/Manager/Cashier | **Retest** |
| 15 | Physical phone add category/product after org entry | **Retest** |

**Not Device Verified** for the full Phase 18 journey. Do not mark passed or Phase 18 Complete until the user confirms the checklist.

### 10a. Confirmed issues fixed in this validation pass

#### Issue A — Catalog Access Restricted after clear + login + org entry

| Field | Detail |
|---|---|
| Tested scenario | `adb shell pm clear` → sign in → select organization as Owner → add category/product |
| Observed | UI showed Access Restricted for Manage Catalog |
| Root cause | Bind omitted commercial feature codes; Active+empty status was treated as sufficient |
| Fix | Require non-empty feature codes; Development fallback to `DefaultDevelopmentGrants` when bind succeeded |
| Validation result | Automated: `SelectOrganization_with_token_bind_fills_dev_grants_when_features_missing` **Pass**. User/phone **Retest** |
| Commit | See §14 |

#### Issue B — Quick Login → Owner / Manager / Cashier Access Denied

| Field | Detail |
|---|---|
| Tested scenario | Quick Login → org Owner buttons → Owner/Manager/Cashier homes |
| Observed | Access Denied after Quick Login (normal password login worked) |
| Root cause | Process shell validation not re-armed after bind; preferred home applied after bind; effective-role lag |
| Fix | `NotifySessionAccessChanged` after org persist; `EnterWorkingAs` before bind; RoleHome/dashboard preferred-home trust; Quick Login dedupe + AccessToken gate + denser fonts |
| Validation result | Automated preferred-home + NotifySessionAccessChanged tests **Pass**. User/phone **Retest** |
| Commit | See §14 |

Fixes also recorded earlier / adjacent in this pass:

- Soft-input AdjustResize + keyboard inset for address/setup fields
- Catalog/expense category add: loading stuck after save (try/finally + quiet reload)
- Owner org entry shows Owner / Manager / Cashier working-as buttons
- Local Validation Quick Login dropdown on MAUI Sign-in (excludes Platform identities; uses SharedPassword)
- PhysicalDevice Local Validation profile for Tailscale (`9022d95`)

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
| Validation-fix commit (grants / preferred home baseline) | `3e4314c` |
| PhysicalDevice Tailscale profile | `9022d95` |
| Validation-fix commit (Quick Login / Access Denied follow-up) | *(filled after push)* |
| Phase 18 Complete closeout commit | **Not created** — blocked until user confirmation |

### Remaining user retest items (after this fix commit)

1. Clear app → sign in → org Owner → add category and product (no Access Restricted) — **Retest**
2. Quick Login → Owner home — **Retest**
3. Quick Login → Manager home — **Retest**
4. Quick Login → Cashier home — **Retest**
5. Physical phone (Tailscale APK) — install + same flows — **Retest**
6. Continue full checklist in § User mobile validation checklist

Mark Pass / Fail / Retest only from your results. Do not treat agent automation as Device Verified.

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
| 1 | Registration | Retest | |
| 2 | Activation | Retest | |
| 3 | Sign-in | Retest | Includes Quick Login |
| 4 | Session restore | Retest | |
| 5 | Start a Business | Retest | |
| 6 | Organization creation | Retest | |
| 7 | Organization selection | Retest | Owner/Manager/Cashier entry |
| 8 | Organization Owner essentials | Retest | |
| 9 | Staff creation / invitation | Retest | |
| 10 | POS role assignment | Retest | |
| 11 | POS setup | Retest | |
| 12 | Product creation | Retest | After Access Restricted fix |
| 13 | Register and shift | Retest | |
| 14 | Owner Start Selling | Retest | |
| 15 | Manager Start Selling | Retest | |
| 16 | Cashier selling | Retest | |
| 17 | Cash checkout | Retest | |
| 18 | Receipt | Retest | |
| 19 | Stock reduction | Retest | |
| 20 | Shift close | Retest | |
| 21 | Reports | Retest | |
| 22 | Entitlement denial | Retest | |
| 23 | Membership suspension | Retest | |
| 24 | Role revocation | Retest | |
| 25 | Logout | Retest | |

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
