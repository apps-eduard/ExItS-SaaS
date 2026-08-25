# POS React — Workspace Access Repair 01

## Status

**PASS** (focused repair)

## Starting SHA

`4af3aec63ac5457bdce75afd7217b7de23d06b94` (`feat/pos-react-client`)

## Reproduction

1. Owner login (`kizy@gmail.com` / Local Validation shared password) against Platform `:8091`.
2. Choose workspace → **Kizy Store** visible with Active branches (`Main Branch`, `Kizy Store 02`).
3. Select a branch → React bind issues Platform session grant (`productAccessAllowed: true`, `mappedPosRoleCode: Owner`).
4. React then calls `PUT /pos-api/api/v1/pos/operational-branch`.
5. UI showed **Product access denied** with engineering detail **Bearer access token is inactive or invalid.** (or a false denial after grant).

### Exact failing endpoint (authoritative)

| Field | Value |
|-------|--------|
| URL | `PUT /api/v1/pos/operational-branch` (via Vite `/pos-api`) |
| Auth | `Authorization: Bearer <session-grant accessToken>` + org/branch headers + browser Platform session cookie |
| Observed failure class | After grant succeeded, POS treated the Active branch as missing / inactive |

### ProductAccess facts (Kizy Store)

- Organization: `Kizy Store` (`ca023f5b-925e-4aa5-a843-d48c4c06fa14`)
- Owner: `kizy@gmail.com` — `OrganizationOwner`
- `product_access_assignments` for `pinoy-business-pos`: **Active**
- Session grant: `productAccessAllowed=true`, `mappedPosRoleCode=Owner`

So this was **not** a missing ProductAccess case.

## Root cause

**Failure class: A + C (credential mix-up on POS → Platform branch lookup), misclassified as product denial in React.**

1. Workspace bind correctly obtained a Platform session grant with ProductAccess allowed.
2. `PosOrganizationBranchDirectory` validated the selected branch by calling Platform `GET /api/v1/platform/organizations/{id}/branches`.
3. It forwarded the **product Bearer** access token as `Authorization` to Platform.
4. Platform organization branch APIs authenticate via **cookie / PlatformSession / `X-ExItS-Session-Token`**, not product Bearer. Bearer calls returned **403** (`development-operator:unauthenticated` / authorization denied).
5. POS treated the failed lookup as “branch not Active” → operational-branch failed closed.
6. React mapped that (and genuine 401 bearer-inactive strings) to the workspace **Product access denied** banner, including raw engineering detail.

Secondary defects addressed in the same repair:

- Login did not clear in-memory POS bearer/grant → stale token risk across principals.
- Platform introspection treated **429/5xx** as “inactive token”, producing false `Bearer access token is inactive or invalid.`
- Stale Local Validation POS image initially lacked `operational-branch` (404); rebuilt from this branch for validation.

## Why Owner was affected

Owners use the React browser session (cookie) + session-grant Bearer for POS. Branch Active checks require Platform **session** credentials. Forwarding Bearer alone made every Owner bind fail after a successful ProductAccess grant — a false denial.

## Fix

### Server (POS API)

- `PlatformCallerCredentialForwarder`: forward Cookie (from `Request.Cookies`), `X-ExItS-Session-Token`, `PlatformSession` Authorization, and Dev actor header; **do not** forward product Bearer to Platform org APIs.
- `PosOrganizationBranchDirectory` uses the forwarder for all Platform branch GETs.
- Introspection: 429/5xx throw → `IntrospectionUnavailable` → **503** (`pos.platform_auth.unavailable`), not inactive-token 401.
- Rebuild Local Validation `pos-api` image so `PUT /api/v1/pos/operational-branch` is present.

### React

- Clear POS bearer + session grant on login (`loginWithPassword` + `SessionProvider.signIn`).
- Classify bind failures (`workspace-bind-error.ts`): product denial vs session expired vs branch unavailable vs service unavailable.
- Workspace chooser shows user-facing copy only; technical detail goes to `console.warn`.
- Binding stays on loading state until the check completes.

## Security invariants preserved

- ProductAccess still required (`productAccessAllowed` gate unchanged).
- Owner is not auto-entitled without ProductAccess.
- Invalid Bearer still returns **401** with inactive-token detail (server); UI maps that to session-expired language.
- No hardcoded tokens/orgs; CSRF/auth not weakened.
- ONE HUMAN != ONE LOGIN PRINCIPAL unchanged.

## Tests

| Gate | Result |
|------|--------|
| Vitest (full client) | **122 passed** |
| Vitest workspace-bind-error | **6 passed** |
| Playwright RMAP-03 | **8 passed** (incl. 375/768/1024/1440) |
| typecheck | **PASS** |
| lint | **PASS** (preexisting react-refresh warnings only) |
| prettier --check (touched) | **PASS** |
| build | **PASS** |

### Manual API validation (post-fix POS image)

- Owner grant + Cookie + Bearer → `PUT operational-branch` **200** (`Main Branch`)
- Owner grant + `X-ExItS-Session-Token` + Bearer → **200**
- Invalid Bearer → **401** inactive token (negative)

## Known unrelated failures

- `PREEXISTING_BASELINE_RED_START_BUSINESS` (unchanged; not addressed).
- Local Validation POS image must include this repair (rebuild `pos-api` from `feat/pos-react-client`).

## Final SHA

- Implementation: `4701a4a9b4fd50374e31779bcfd6809e319bee1a`
- Docs: `0cf803e45f0f2471dbf1042a01fa2b75ae8c048c` (this report; SHA note may trail by one docs commit)

## Flags

- `OWNER_WORKSPACE_ACCESS_REPAIR_PASS=YES`
- `ROOT_CAUSE_IDENTIFIED=YES`
- `AUTH_BYPASS_ADDED=NO`
- `RMAP_08_STARTED=NO`
- `RMAP_B04_STARTED=NO`
- `RMAP_TAX_STARTED=NO`
- `REACT_DISCOUNT_UX_STARTED=NO`
- `PRODUCTION_READY=NO`
- `CUTOVER_AUTHORIZED=NO`
