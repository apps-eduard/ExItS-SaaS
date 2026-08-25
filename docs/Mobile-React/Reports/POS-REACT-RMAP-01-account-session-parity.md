# RMAP-01 STATUS

PASS

# BASELINE

starting SHA: `18c6c9706041123f7dbc811d7fb04cdb625bddf0`  
branch: `feat/pos-react-client`  
remote: `origin/feat/pos-react-client` @ same SHA  
clean: YES at start

# CONTRACT REVIEW

## backend/domain files
- `PlatformAuthSessionInfoDto` + `ValidateAndRenewPlatformSession` (`SessionUseCases.cs`)
- `AccountScopeGuardMiddleware`, auth login/me/logout, account-profiles ensure/select
- POST-B00 staff lock: `HomeOrganizationId`, `OrganizationContextLocked`, `AccountClass` from session profile

## MAUI reference
- Sign-in + session restore; ensure/select Organization profile before org context (owner path deferred to RMAP-02 for full ensure+select UX)

## authoritative docs
- Identity lifecycle, migration roadmap RMAP-01, B00 report

## contradictions found
- `/me` previously omitted `homeOrganizationId` / `organizationContextLocked` that login returned → **fixed** (additive DTO fields)

## owner decision needed
NO

# IMPLEMENTATION

## exact scope
- AccountClass helpers + `RequireAccountClass` / Personal / Organization route guards
- Sign-in UX distinguishes Personal email vs `local@ORG######` (hint only; no class inference)
- Account-profiles API client paths (list/select/ensure) for later owner flows
- Workspace routing: PersonalHome only for Personal (or unset); Organization/Platform with zero orgs → NoAccessibleBranch
- Personal AccountClass never auto-binds org workspace
- `/me` parity for staff lock fields (Platform Application + POS mirror DTO)

## files changed (representative)
- Client: `account-class.ts`, `SessionGuards.tsx`, `SignInPage.tsx`, `router.tsx`, `WorkspaceProvider.tsx`, `workspace-resolver.ts`, `platform-auth-client.ts`, `browser-session.ts`, i18n, e2e mocks/specs, unit tests
- Platform: `IPlatformAuthSessionRepository.cs`, `SessionUseCases.cs`
- POS Application: `PlatformAccessModels.cs`
- Tests: `ApiSessionAuthTests.cs`

## shared components reused
- PageHeader, ErrorState, LoadingState, existing auth Card/Input/Button

## backend changes
YES — additive `/me` fields only (same values as login)

# SECURITY / ISOLATION

- AccountClass from server session only (never email inference)
- Personal cannot open Organization-only routes; Organization cannot open Personal-only
- Platform denied on Organization sell/workspace surfaces
- LinkedPersonalUserId not used for authz
- CSRF logout preserved; sessionToken still stripped from browser snapshot

# UI VALIDATION

Sign-in + denied AccountClass surfaces covered by Playwright (desktop chromium). Full responsive matrix not required beyond foundation reuse for this package’s non-catalog visual scope; denied state uses shared ErrorState/PageHeader.

| Viewport | Notes |
|----------|--------|
| 375×812 | Covered by existing shell/sign-in harness patterns; RMAP-01 focused on session/class |
| 768×1024 | Same |
| 1024×768 | Same |
| 1440×900 | Same |

# TESTS

```text
npm test  → 95 passed
npm run typecheck → pass
npx playwright test e2e/rmap-01-account-session.spec.ts e2e/auth-session.spec.ts → 8 passed
```

Known baseline reds: `PREEXISTING_BASELINE_RED_START_BUSINESS` — not exercised by this WP (unchanged).

# DOCS

- This report
- `react-current-state.md`, `react-migration-roadmap.md`, `capability-parity-matrix.md`, `validation-matrix.md`, `backend-contract-map.md` (as applicable)

# GIT

implementation SHA: `52072a3062ce20ad74f4dcd386aa2f1e9199e3db`  
docs/report SHA: `c93695e153aa3840df07b384d87b3a1dbff37618`  
remote SHA: `c93695e153aa3840df07b384d87b3a1dbff37618`  
ahead/behind: 0 / 0  
clean: YES

# FLAGS

RMAP_01_PASS=YES  
LOCAL_EQUALS_REMOTE=YES  
WORKING_TREE_CLEAN=YES
