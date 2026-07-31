# P14-WP02A — Live Preview Test Users and Quick Login

Phase marker: `P14-WP02A-live-preview-test-users-and-quick-login`

Package: **P14-WP02A Gap Fix — Live Preview Test Users and Quick Login**
Prior tip: `cb380fa969932eaeadd1c90ec8ec9d00038a9d75`
Feature tip: `c0d29daf533e7afaf79781771c79f103c6f28dc4`

## Status

**Complete.** Live-preview stack (`exits-live-preview`) exercises real Platform authentication: anonymous Admin redirects to `/admin/login`, deterministic preview identities are seeded (Platform + POS-owned DBs), and the login page offers **Live Preview Test User** quick-login that creates a real Platform session. **Not Production.** **P14-WP03 not started.**

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Auth gate | Admin `FallbackPolicy` when `LivePreview:Enabled` (Staging compose) |
| Platform seed | `InitializeLivePreviewDataset` + `LivePreviewHostedService` (migrate + seed when Enabled) |
| POS seed | `InitializePosLivePreviewRoles` + `PosLivePreviewHostedService` (own DB; discovers IDs via Platform API) |
| Quick login | `GET/POST /api/v1/platform/live-preview/*` + Admin login dropdown |
| Guards | Production rejects `LivePreview:Enabled=true` |
| Compose | Staging + `LivePreview__Enabled=true` + shared password env |

## 2. Preview identities

| Key | Display | Outcome |
|---|---|---|
| `platform-admin` | Preview Platform Administrator | `PlatformAdministrator`; no POS-local role |
| `org-admin` | Preview Organization Administrator | Org admin + POS access + POS `Owner` |
| `pos-cashier` | Preview POS Cashier | Member + POS access + POS `Cashier` |
| `no-pos` | Preview User — No POS Access | Membership; no product access |
| `no-org` | Preview User — No Organization | User only |

## 3. Operator

```powershell
cd deploy\docker
# ensure LIVE_PREVIEW_SHARED_PASSWORD is set (min 12 chars) in .env.live-preview
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d --build
```

Open **http://localhost:8090/** → redirected to `/admin/login` → select test user → **Sign in as test user**.

## 4. Explicit exclusions

- P14-WP03 TLS/proxy
- Production enablement of LivePreview
- Changing packaging stack ports/DBs
- Invented role names (uses existing Platform/POS roles)

## 5. Validation

| Check | Result |
|---|---|
| Admin anonymous → `/admin/login` | 302 |
| Identities API | 5 identities returned |
| Quick-login session | Real `sessionToken` for `platform-admin` |
| POS role seed | Owner + Cashier assigned |
| Full Release tests | **1267 passed / 0 failed / 0 skipped** |

## Exact next

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when authorized.

