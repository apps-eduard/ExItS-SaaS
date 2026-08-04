# P18-WP02 — Personal Account and Start a Business

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Agent emulator evidence recorded (not Device Verified)** — Pending User Validation (see P18-WP08) |
| Date | 2026-08-03 |

## 1. Objective

Personal Mobile area: home, profile, organization list, Start a Business, and continuation inside Mobile after organization creation.

## 2. Scope

Personal Account on Mobile only (MVP). Full Org Admin remains Web.

## 3. Existing functionality reused

- Platform Personal `POST /api/v1/personal/start-business`
- Start a Business server behavior: Organization Owner grant; POS entitlement activation; first POS Owner when entitlement activates (`AssignPosOwnerRole`)
- Existing membership and eligible-organization reads

## 4. Backend / API work completed

- Maui client methods: `StartBusinessAsync`, memberships, organization get, account-profile select (client surface)
- `ContinueAfterStartBusinessAsync` applies rotated Platform session and binds POS organization when entitled
- No duplicate Start Business API

## 5. MAUI screens and flows completed

- `/personal`, `/personal/profile`, `/personal/settings`, `/personal/invitations/accept`, `/start-business`
- Organization list on personal home with continue into org / POS when entitled
- Pending organization invitations (list + accept-by-id + token accept)
- Sign-in with no organization navigates to Personal home
- Continuation after Start a Business stays in Mobile (`/org` and POS gate)
- AuthShell layout without POS bottom-nav padding (phone-friendly)

## 6. Files / components changed (representative)

- `Maui/Components/Pages/Personal/PersonalHome.razor`, `PersonalProfile.razor`, `StartBusiness.razor`
- `Application/Auth/AuthenticationService.cs` (`ContinueAfterStartBusinessAsync`)
- Platform access models/client Start Business DTOs

## 7. Authorization and organization-isolation behavior

Start Business requires authenticated Platform session. Created organization is selected in session; POS access follows server entitlement + role grants. Cross-org data remains denied by Platform/POS authorization.

## 8. Tests executed and totals

| Suite | Result |
|---|---|
| Platform UnitTests (Auth / StartBusiness / ProductLocal filter) | **60 passed** |
| MAUI.Tests | **73 passed** |

## 9. MAUI build result

**Build Verified** (Android host compile).

## 10. Emulator / device validation result

**Agent emulator evidence (2026-08-03) — not Device Verified.** Personal-only user lands on Personal home with Start a Business and Account context switcher; empty organization list is EmptyState (not an error). Start Business host provisioning + Mobile continuation previously exercised via CDP (`artifacts/p18-wp08`). Pending User Validation (P18-WP08).

## 11. Known limitations

- Personal Utang deep screens not expanded on Mobile (deferred; Platform/Admin + Personal APIs remain)
- Organization staff invitee **decline** not supported (admin revoke only); accept list + token paths are on Mobile
- Staff invitation delivery still depends on Platform email/outbound configuration

## 12. Deferred items

Device E2E of register → Start Business; Personal Utang Mobile; invitee decline.

## 13. Current status

Implemented · Tested · Build Verified · Personal MVP UI completion recorded in [P18-personal-mvp-mobile-ui-completion](P18-personal-mvp-mobile-ui-completion.md) · Pending User Validation (phase-level; see P18-WP08 / P19-WP08 Retest)

## 14. Commit reference

Implementation: `4b8b727`. Personal MVP UI completion: see tip commit after Personal Mobile audit. Documentation reconciliation on `main`.
