# P18-WP02 — Personal Account and Start a Business

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Agent emulator evidence recorded (not Device Verified)** — Pending User Validation (see P18-WP08) |
| Date | 2026-08-03 |

## 1. Objective

Personal Mobile area: personal-first home (Utang / profile / settings), Explore Pinoy Business POS (catalog plans), explicit Start a Business confirmation, and continuation inside Mobile after organization creation.

Organization list and Account context switcher are **not** on Personal home; existing members switch via Settings / Organization Select.

## 2. Scope

Personal Account on Mobile only (MVP). Full Org Admin remains Web.

## 3. Existing functionality reused

- Platform Personal `POST /api/v1/personal/start-business`
- Platform commercial `GET /api/v1/commercial/plans`
- Start a Business server behavior: Organization Owner grant; POS entitlement activation; first POS Owner when entitlement activates (`AssignPosOwnerRole`)
- Existing membership and eligible-organization reads

## 4. Backend / API work completed

- Maui client methods: `GetCommercialPlansAsync`, `StartBusinessAsync`, memberships, organization get, account-profile select (client surface)
- `ContinueAfterStartBusinessAsync` applies rotated Platform session and binds POS organization when entitled
- No duplicate Start Business API

## 5. MAUI screens and flows completed

- `/personal` (personal-first; Explore POS CTA), `/personal/explore-pos`, `/personal/profile`, `/personal/settings`, `/personal/invitations/accept`, `/start-business?planKey=…`
- Plan selection does not create an organization; confirmation on Start Business does
- Account context switcher on Personal Settings; Organization Select empty path → Explore POS
- Sign-in with no organization navigates to Personal home
- Continuation after Start a Business stays in Mobile (`/org` and POS gate)
- AuthShell layout without POS bottom-nav padding (phone-friendly)
- Personal registration (`/register`) shares the rounded Sign In / Sign Up auth card; fields remain display name + email (no invented phone/password registration). See [maui-auth-experience.md](../engineering/maui-auth-experience.md).

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

- Payments / Reminders / History remain Coming Soon on Mobile nav (entry history is available inside relationship detail)
- Organization staff invitee **decline** not supported (admin revoke only)
- Staff invitation delivery still depends on Platform email/outbound configuration

## 12. Deferred items

Device E2E of full Personal Utang journey; Payments/Reminders dedicated screens; invite-from-relationship UI polish.

## 13. Current status

Implemented · Tested · Build Verified · Personal Utang Mobile parity recorded in [P18-personal-mvp-mobile-ui-completion](P18-personal-mvp-mobile-ui-completion.md) · Pending User Validation (P19-WP08 Retest)

## 14. Commit reference

Implementation: `4b8b727`. Personal MVP + Utang Mobile: see tip commits on `main`.
