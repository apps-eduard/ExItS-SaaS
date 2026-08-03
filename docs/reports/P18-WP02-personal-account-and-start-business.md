# P18-WP02 — Personal Account and Start a Business

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified; Device Validation Pending** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Device Validation Blocked** |
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

- `/personal`, `/personal/profile`, `/start-business`
- Organization list on personal home with continue into org / POS when entitled
- Sign-in with no organization navigates to Personal home
- Continuation after Start a Business stays in Mobile (`/org` and POS gate)

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

**Device Validation Blocked**.

## 11. Known limitations

- Personal Utang deep screens not expanded in Phase 18 (out of Phase 18 POS journey focus)
- Staff invitation delivery still depends on Platform email/outbound configuration

## 12. Deferred items

Device E2E of register → Start Business; richer personal settings beyond profile display.

## 13. Current status

Implemented · Tested · Build Verified · Device Validation Blocked

## 14. Commit reference

Implementation: `4b8b727`. Documentation reconciliation: Phase 18 docs tip on `main`.
