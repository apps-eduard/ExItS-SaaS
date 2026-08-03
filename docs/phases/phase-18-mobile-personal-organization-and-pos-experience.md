# Phase 18 — Mobile Personal, Organization, and POS Experience

[Client experience boundaries](../architecture/client-experience-boundaries.md) | [Portfolio](../portfolio-progress.md) | [Phase 17](phase-17-pos-mvp-operational-onboarding-and-first-sale.md)

## Status

**Code Complete and Build Verified; Device Validation Pending**

| Field | Value |
|---|---|
| Phase | 18 |
| Implementation commit | `4b8b7270417d0f9e612855ed746d7fd80819adee` |
| Documentation closeout | This phase file and P18-WP01–WP08 (tip of `main` after docs reconciliation push) |
| Production-ready | **No** |
| Device / emulator validation | **Device Validation Blocked** (R-109) — no emulator or physical-device run recorded |

## Work packages

| WP | Focus | Documented status |
|---|---|---|
| [P18-WP01](../reports/P18-WP01-mobile-foundation-and-authentication.md) | Mobile foundation and authentication | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP02](../reports/P18-WP02-personal-account-and-start-business.md) | Personal account and Start a Business | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP03](../reports/P18-WP03-organization-selection-and-owner-essentials.md) | Organization selection and Owner essentials | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP04](../reports/P18-WP04-pos-role-routing-and-navigation.md) | POS role routing and navigation | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP05](../reports/P18-WP05-pos-owner-and-manager-mobile-experience.md) | POS Owner and Manager Mobile experience | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP06](../reports/P18-WP06-cashier-selling-experience.md) | Cashier selling experience | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP07](../reports/P18-WP07-mobile-security-resilience-and-localization.md) | Security, resilience, localization | Code Complete and Build Verified; Device Validation Pending |
| [P18-WP08](../reports/P18-WP08-end-to-end-validation-and-closeout.md) | End-to-end validation and closeout | Code Complete and Build Verified; Device Validation Pending |

## Approved client boundaries (authoritative)

| Experience | Client |
|---|---|
| Platform Administration | Web only |
| Personal Account | Mobile |
| Organization Owner essentials | Mobile |
| Full Organization Administration | Web |
| POS operations | Mobile |

After Start a Business, the user continues inside Mobile without being forced to Web.

Organization Owner essentials include organization summary, basic profile, subscription status, entitlement status, staff list, invite/create staff where supported, assign/revoke POS roles, launch POS setup, and the reminder:

> For full organization administration, use the Web application.

## Role model (MVP)

- One Organization Owner per organization.
- Business creator becomes Organization Owner.
- Business creator becomes first POS Owner when POS entitlement activates.
- POS Owner includes Manager and Cashier capabilities.
- POS Manager includes Cashier capabilities.
- POS Cashier has selling capabilities only.
- Owner and Manager use **Start Selling** to enter the cashier-style selling interface without changing role.

## Evidence summary (do not invent)

| Layer | Status |
|---|---|
| Backend / API | **Implemented** — reuse of existing Platform + POS contracts; Maui client expansion |
| MAUI frontend | **Implemented** — screens and flows in source (not ViewModels-only) |
| Automated tests | **Tested** — MAUI.Tests 73; POS Unit 339; POS Integration 135; Platform Unit (Auth/StartBusiness/ProductLocal filter) 60 |
| MAUI Android build | **Build Verified** — succeeded with Android SDK directory and user NuGet cache |
| Emulator / device | **Device Validation Blocked** |

## End-to-end journey (evidence classes)

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

| Evidence class | Scope |
|---|---|
| Implemented in code | Full journey paths exist across Platform APIs, POS APIs, and MAUI screens linked above |
| Covered by automated tests | Auth dual-token, role routing, selling-mode return, POS unit/integration suites, Platform Start Business / product-local coverage |
| Build verified | Application, ApiClient, MAUI Android host compile |
| Not device validated | Interactive Android emulator/physical device E2E |

## Superseded filenames

Earlier provisional docs under `phase-18-mobile-client-experience-completion.md` and P18-WP01–WP06 short-name reports are superseded by this file and P18-WP01–WP08 below.
