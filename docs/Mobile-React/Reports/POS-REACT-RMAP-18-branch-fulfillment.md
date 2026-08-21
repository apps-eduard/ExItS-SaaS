# RMAP-18 — Branch fulfillment admin + readiness

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag | Value |
|------|-------|
| `RMAP_18_AUTHORIZED` | YES (authorized after RMAP-17 PASS) |
| `RMAP_18_PASS` | PASS |
| `RMAP_18_CLIENT` | PASS |
| `RMAP_18_CAPABILITIES` | PASS |
| `RMAP_18_UI` | PASS |
| `RMAP_18_MAP_FALLBACK` | PASS |
| `RMAP_18_I18N` | PASS |
| `RMAP_18_VITEST` | PASS |
| `RMAP_18_E2E` | PASS |
| `RMAP_18_TYPECHECK` | PASS |
| `RMAP_18_NATIVE_SPEAKER` | PENDING |
| `RMAP_B05_AUTHORIZED` | **NO** — public landing/slug CMS **not started** |
| `RMAP18_SCHEMA_CONTRACT_GAP` | NO |
| `HARD_STOP` | NO (await RMAP-19 authorization separately) |

## Contract

| Area | Finding |
|------|---------|
| API | Existing Platform `/api/v1/platform/organizations/{org}/branches` (+ update, operating-hours, fulfillment-readiness, fulfillment-settings, online-orders-pause, delivery-policy) — **no invented contracts** |
| Coordinates | WGS84 lat/lng authoritative; `ClearCoordinates` when both empty |
| Readiness | Server `MissingRequirements` / `ReasonCodes` displayed; client does **not** invent readiness rules |
| Map provider | If `VITE_MAP_TILES_URL` / `VITE_MAP_EMBED_URL` missing → safe coordinate/address fallback (no Capacitor; GPS assist once via browser `getCurrentPosition` only) |
| Capabilities | Owner / OrganizationAdministrator (`hasOrganizationManagementAuthority` / `canManageBranchFulfillment`); POS Manager/Cashier alone DENY |
| Offline | Online Platform mutations only |

## Implementation

- `branch-fulfillment-client.ts` — list/update branch, hours, readiness, fulfillment settings, pause, delivery policy
- Features under `src/features/branches/` — list + edit form, WGS84 helpers, map links, readiness label mapping, hours drafts
- Routes `/org/branches`, `/org/branches/:branchId` under admin experience guard
- Nav from Organization essentials + Owner role home
- i18n `branches.*` + `org.branchesLink` in en, fil-PH, ceb-PH, ilo-PH, hil-PH
- Vitest: coords bounds, map fallback, GPS once (no watch), readiness labels, hours, capabilities, client paths
- Playwright `e2e/rmap-18-branch-fulfillment.spec.ts`
- Report + roadmap status update

## Exclusions

- **RMAP-B05** public landing / slug CMS (`RMAP_B05_AUTHORIZED=NO` — not started)
- Continuous background GPS / Capacitor geolocation
- Invented readiness rules when server codes are present
- Migrations / backend changes
- Native-speaker i18n sign-off
- Customer storefront / ordering ops (RMAP-19)

## Validation

### React gates

| Gate | Result |
|------|--------|
| prettier (touched) | PASS |
| typecheck | PASS |
| Vitest (branch fulfillment focused) | PASS |
| Playwright `rmap-18-branch-fulfillment` | PASS |

Responsive matrix (branch list):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Address + valid WGS84 coords save; invalid latitude rejected client-side
- Pickup / Delivery Enabled vs Disabled (and Not ready from server readiness)
- Operating hours edit + delivery policy upsert
- Server missing-requirements messaging
- Cashier denied `/org/branches`
- Unknown branch isolation (not found)
- Map tiles missing → fallback panel; external maps links when coords valid; GPS assist control present
- Locale smoke (Filipino via Preferences)
- Responsive 4 viewports

## Exact next

Do **not** start RMAP-19 until authorized. Do **not** start RMAP-B05 (`RMAP_B05_AUTHORIZED=NO`). Native-speaker i18n review remains PENDING.
