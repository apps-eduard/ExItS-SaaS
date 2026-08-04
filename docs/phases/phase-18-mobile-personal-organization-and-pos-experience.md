# Phase 18 — Mobile Personal, Organization, and POS Experience

[Client experience boundaries](../architecture/client-experience-boundaries.md) | [Portfolio](../portfolio-progress.md) | [Phase 17](phase-17-pos-mvp-operational-onboarding-and-first-sale.md) | [Phase 19](phase-19-mobile-pos-operations-and-cashier-experience.md)

## Status

**Complete (implementation/scope)** — closed 2026-08-04 by owner request. Physical-phone validation was **partial**. **Not Device Verified.**

Phase 18 delivered Mobile Personal / Organization essentials, role routing, and catalog (Products / Categories) experience on top of existing Platform + POS APIs. Remaining operational POS UIs (Inventory, Registers, Shifts, Sales, Customers, Reports, and full Cashier UI completion) move to [Phase 19](phase-19-mobile-pos-operations-and-cashier-experience.md).

The application remains **not production-ready**. Phase 14 remains **In Progress**. Do not start P14-WP03 work under this phase closeout.

| Field | Value |
|---|---|
| Phase | 18 — **Complete (implementation/scope)** |
| Baseline / tip commit | `f86dcd2` |
| Implementation commit | `4b8b7270417d0f9e612855ed746d7fd80819adee` |
| Production-ready | **No** |
| Device Verified | **No** — partial phone validation only |
| User mobile validation | **Partial** — Products and Categories phone-validated; Quick Login / access routing fixed pending final retest |
| P18-WP08 | **Complete** (closeout recorded; partial phone validation) |
| Handoff | [Phase 19](phase-19-mobile-pos-operations-and-cashier-experience.md) — Mobile POS operations and Cashier experience |

## Work packages

| WP | Focus | Documented status |
|---|---|---|
| [P18-WP01](../reports/P18-WP01-mobile-foundation-and-authentication.md) | Mobile foundation and authentication | Code Complete and Build Verified |
| [P18-WP02](../reports/P18-WP02-personal-account-and-start-business.md) | Personal account and Start a Business | Code Complete and Build Verified (+ Personal MVP UI follow-up) |
| [P18-WP03](../reports/P18-WP03-organization-selection-and-owner-essentials.md) | Organization selection and Owner essentials | Code Complete and Build Verified |
| [P18-WP04](../reports/P18-WP04-pos-role-routing-and-navigation.md) | POS role routing and navigation | Code Complete and Build Verified |
| [P18-WP05](../reports/P18-WP05-pos-owner-and-manager-mobile-experience.md) | POS Owner and Manager Mobile experience | Code Complete and Build Verified |
| [P18-WP06](../reports/P18-WP06-cashier-selling-experience.md) | Cashier selling experience | Code Complete and Build Verified (full Cashier UI completion → Phase 19) |
| [P18-WP07](../reports/P18-WP07-mobile-security-resilience-and-localization.md) | Security, resilience, localization | Code Complete and Build Verified |
| [P18-WP08](../reports/P18-WP08-end-to-end-validation-and-closeout.md) | End-to-end validation and closeout | **Complete** (closeout recorded; partial phone validation) |

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
| MAUI frontend | **Implemented** — screens and flows in source (not ViewModels-only) for Phase 18 scope |
| Automated tests | **Tested** — MAUI.Tests / focused Auth + RoleHome / PosSync suites recorded in P18-WP08 |
| MAUI Android build | **Build Verified** — emulator + PhysicalDevice Tailscale Debug APK delivered |
| Physical phone validation | **Partial** — Products phone-validated; Categories phone-validated; Quick Login / access routing fixed pending final retest |
| Device Verified (full journey) | **No** |
| Deferred to Phase 19 | Inventory, Registers, Shifts, Sales, Customers, Reports, full Cashier UI completion |

## Phone-validation record (Phase 18 close)

| Item | Result |
|---|---|
| Products | **Phone-validated** |
| Categories | **Phone-validated** |
| Quick Login / access routing | **Fixed** — pending final retest |
| PhysicalDevice Tailscale APK | **Delivered** |
| Inventory / Registers / Shifts / Sales / Customers / Reports / full Cashier UI | **Moved to Phase 19** |

## End-to-end journey (Phase 18 scope vs Phase 19 handoff)

```text
User registers in Mobile
→ signs in
→ starts a business
→ organization is created
→ user becomes Organization Owner and first POS Owner
→ continues inside Mobile
→ completes POS setup
→ creates a product / category          ← Phase 18 (phone-validated)
→ adds staff / assigns POS roles        ← Phase 18 (implementation)
→ Cashier / shift / sale / receipt      ← Phase 19 completion
→ inventory / registers / customers / reports ← Phase 19
→ Owner or Manager Start Selling UX     ← Phase 19 completion
```

| Evidence class | Scope |
|---|---|
| Implemented in code | Journey paths exist across Platform APIs, POS APIs, and MAUI screens for Phase 18 scope |
| Covered by automated tests | Auth dual-token, role routing, selling-mode return, POS unit/integration, Platform Start Business / product-local coverage |
| Build verified | Application, ApiClient, MAUI Android host compile; PhysicalDevice Tailscale APK delivered |
| User / phone validated | **Partial** — Products and Categories; not full Device Verified |

## Closure rule (satisfied for implementation/scope)

Phase 18 is **Complete (implementation/scope)** as of 2026-08-04 by owner request. This closeout does **not** claim Device Verified, full phone journey pass, or production readiness. Operational POS UIs and full Cashier experience completion continue under Phase 19. Phase 14 remains open; production remains blocked.
