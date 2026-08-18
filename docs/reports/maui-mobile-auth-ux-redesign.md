# MAUI mobile authentication UX redesign

| Field | Value |
|---|---|
| Status | **Code Complete / Test Guarded** |
| Starting SHA | `e1191fe1c01513dcfd53c3488307fffe868c6bbe` |
| Implementation commit | `d2d59b08` |
| Device validation | **No** — not Device Verified |
| Production Ready | **No** |
| Date | 2026-08-18 |

Related: [maui-auth-experience.md](../engineering/maui-auth-experience.md) · [P18-WP01](P18-WP01-mobile-foundation-and-authentication.md) · [P19 offline PIN](P19-offline-operability-foundation.md)

## 1. Objective

Keep all existing MAUI authentication/security behavior and redesign Sign In, Personal Sign Up, Forgot Password, and Activate into one branded mobile auth shell.

## 2. Delivered UX

- Green POS-brand hero (`EXPERT IT SOLUTIONS` / `Pinoy Business POS`) with CSS-only decorative circles
- Overlapping white rounded auth card (~24px) and auth-scoped rounded inputs/buttons (~16px / ~52px)
- Sign In / Sign Up tabs inside the card
- `/signin` → Sign In active; `/register` → Sign Up active
- Compact Remember / Forgot row; **Use PIN** is a small link only while connectivity is offline **and** PIN is eligible
- No large offline information panel; no full-width Use PIN button on the login card
- Facebook / Google remain circular placeholders with accessible names
- Development test-user `<select>` sits **below** the card; username-only fill preserved; Production still cannot expose it
- Forgot Password and Activate use the same visual shell without tabs

## 3. Explicit exclusions / unchanged semantics

- No phone/password registration (backend still display name + email + activation)
- No real Google/Facebook OAuth
- No change to PIN authorization, grants, remember-me, return route, navigation gate, or Local Validation guards
- Authenticated `AuthShell` first-time-setup pages still use `StoreHeader`
- Global POS `TextInput` / `Button` styles were not changed

## 4. Tests / build

| Suite | Result |
|---|---|
| `ExItS.PinoyBusinessPOS.Maui.Tests` Release | **494 passed**, **4 failed** (pre-existing, unrelated: Sales checkout stepper/stock copy, sync-status string, AuthService “Cashier” guard) |
| New `AuthExperienceUxGuardTests` | Passed |
| MAUI `net10.0-android` Release | **Build succeeded** (4 pre-existing warnings, 0 errors) with `AndroidSdkDirectory` set |

## 5. Honesty gates

Device Verified: **No**. Production Ready: **No**.
