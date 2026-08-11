# P5-WP05 — Authentication, Onboarding and Closeout

[Phase 5](../phases/phase-05-pos-maui-foundation.md) | [Portfolio](../portfolio-progress.md) | [Previous: P5-WP04](P5-WP04-reusable-mvp-components.md)

## 1. Status and Phase 5 closeout decision

**P5-WP05 Complete.** Phase marker: `P5-WP05-authentication-onboarding-closeout`.

**Phase 5 closeout: Complete with documented risks.**

All Phase 5 work packages (P5-WP01–P5-WP05) are delivered for the approved foundation scope. Phase 5 is **not** production-ready while production authentication (R-091), POS operational roles, offline business support, and gateways remain open.

## 2. Authentication architecture

| Piece | Implementation |
|---|---|
| Identity mechanism | Approved **Development/Testing** only: Platform User Id + `X-Dev-Platform-User-Id` |
| Production auth | **Not implemented** — sign-in blocked outside Development/Testing |
| Abstractions | `IAuthenticationService`, `ISessionStore`, `ISecureTokenStore`, `ICurrentUserContext`, `IProductAccessResolver`, `IOnboardingPreferenceStore` |
| Secure storage | `MauiSecureTokenStore` via MAUI `SecureStorage` (session user id, opaque marker, issued/expiry) |
| Preferences | Theme/density/culture + onboarding progress + selected org id (non-secret) |
| API client | `PlatformAccessClient` + `DevPlatformUserHeaderHandler` |
| Audit | `LoggingAuthEventSink` — local structured events only; Platform remains audit SoR |

**Never stored:** passwords, Bearer JWTs (none issued), Preferences tokens, logs of secrets.

**No automatic retry** of sign-in / refresh / logout / access-changing mutations (client retries GET only).

## 3. Onboarding and organization selection

Sequence: Welcome → Language → Theme → Density → Dev confirm (Dev/Testing) → Sign in → Organization select → Access confirm → Home.

- Preferences reused; credentials never persisted
- Resume via onboarding step preference
- Eligible orgs only (active membership + evaluate Allowed for `pinoy-business-pos`)
- Selected org is preference; revalidated on restore/switch; cleared on deny/logout

## 4. POS commercial access and role boundary

Server-side `GET /api/v1/platform/access/evaluate` is authoritative. Fail closed for GracePeriod/PastDue/Suspended/Cancelled/Expired, missing/stale entitlement, missing membership, revoked assignment, unknown/inactive states.

**Does not assign** Cashier, Store Manager, POS Administrator, or any product-local role. Commercial entry eligibility ≠ operational permission.

## 5. MAUI routes

| Route | Purpose |
|---|---|
| `/` | Boot redirect |
| `/welcome`, `/onboarding/*` | First-run preferences |
| `/signin` | Dev identity / production unavailable |
| `/organization-select` | Eligible org picker |
| `/onboarding/access-confirm` | Commercial access confirmation |
| `/access-denied` | Localized denial |
| `/home`, `/settings` | Protected shell |
| Deferred business routes | Remain deferred; auth-gated |

## 6. Tests and Android evidence

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 41 |
| Admin unit | 27 |
| DesignSystem | 28 |
| ApiClient | 17 |
| Maui | 26 |
| Integration | 84 |
| **Total** | **484** |

Baseline 474 not reduced (net +10).

Release Android APK publish succeeded. `adb devices` empty — interactive validation **not claimed**; R-109 remains open.

## 7. Explicit exclusions

Sales, inventory, customers, Utang, repayments, reporting, offline sync, gateways, QR, cards, POS operational roles, production JWT/MFA/SSO/AD.

## 8. Risks updated

R-091 (production auth), R-098 (dev identity), R-106 (secure storage), R-109 (emulator), plus P5-WP05 notes for session restore/org-switch/commercial-vs-role confusion remain documented in `docs/risks-and-issues.md`.

## 9. Portfolio independence

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## 10. Exact next work package

**Phase 6 — Utang MVP** (first authorized work package when Phase 6 is approved). Do **not** begin Phase 6 until explicitly authorized.

## 11. Commits

| Kind | Message | Hash |
|---|---|---|
| Feature | `feat(pos): authentication onboarding and phase 5 closeout` | `81eaa892cb6ac1ffb1b201b69dc7e390e5536586` |
| Docs hash record | `docs(pos): record P5-WP05 commit hashes and phase 5 closeout` | `b93322a125654ff0538b2dd712e4615a73b82cdc` |
