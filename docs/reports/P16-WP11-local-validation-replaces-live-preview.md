# P16-WP11 — Local Validation replaces Live Preview (stabilization)

| Field | Value |
|---|---|
| Status | **In progress** (stabilization / deployment-architecture cleanup) |
| Phase | Phase 16 — Implementation Complete, Under Validation |
| Does not | Start Phase 17 · Close Phase 16 · Weaken Phase 14 Production WPs |

## Objective

Remove the separate **Live Preview** concept and replace it with **production-equivalent Local Validation**.

Same application code, containers, routing, authorization, and database boundaries validated locally must be deployable to Production with **configuration-only** differences (hosts, ports, secrets, TLS, providers).

## Terminology

| Removed | Replacement |
|---|---|
| Live Preview / Preview Mode / Preview Deployment | Local Validation / Local Deployment / Production-equivalent Local Validation |
| `LivePreview:*` config | `LocalValidation:*` |
| `LIVE_PREVIEW_*` env | `LOCAL_VALIDATION_*` |
| `compose.live-preview.yaml` | `compose.local-validation.yaml` |
| `Start-LivePreviewLocal.ps1` | `Start-LocalValidation.ps1` |
| `/api/v1/platform/live-preview/*` | `/api/v1/platform/local-validation/seed-identities` (seed discovery only) |

## Deployment model

```text
Browser
   |
Local service routing (host ports or optional compose profile apps)
   |
   +-- Platform/Admin Web
   +-- Platform API
   +-- POS API
   |
Local Docker infrastructure
   |
   +-- Platform PostgreSQL
   +-- POS PostgreSQL
```

Production topology template remains `compose.production.yaml` (TLS reverse proxy). Local validation uses the **same app images/code** with local ports and `LocalValidation:Enabled` seed.

## Code / packaging changes

| Area | Change |
|---|---|
| Application / Infrastructure / API / POS | `LivePreview` namespaces → `LocalValidation` |
| Quick-login sessions route | **Removed** — normal `POST /api/v1/platform/auth/login` with approved named Local Validation identities |
| Seed-identities GET | Retained for Platform↔POS bootstrap coordination only |
| Admin UI | No Live Preview selector (credential login only) |
| Hosted services | `LocalValidationHostedService` / `PosLocalValidationHostedService` (non-Production + Enabled) |
| Tools | `tools/Start-LocalValidation.ps1`, `Stop-LocalValidation.ps1` |
| Deploy | `compose.local-validation.yaml`, `.env.local-validation.example`, READMEs |

## Operator migration

1. Copy `.env.local-validation.example` → `.env.local-validation` (or rename old `.env.live-preview` and replace `LIVE_PREVIEW_` → `LOCAL_VALIDATION_`).
2. New Docker project/volume names: `exits-local-validation*` (prior `exits-live-preview*` volumes are not auto-attached — re-seed or migrate volumes manually).
3. Start: `.\tools\Start-LocalValidation.ps1`
4. Sign in via Admin login with the approved Local Validation identities (Olivia Mendoza, Rafael Torres, Maria Santos, Carlo Reyes, Ana Cruz, Daniel Garcia, Luis Navarro, Sofia Ramos) via normal Platform credential login; password from `LocalValidation:SharedPassword` / `LOCAL_VALIDATION_SHARED_PASSWORD` env (never commit the secret).

## Validation checklist (ongoing)

- [x] Local Validation stack starts (DBs + APIs + Admin)
- [x] Ant Design assets load on Admin (Development host; parent `DOTNET_ENVIRONMENT` cleared by launcher)
- [x] Credential login works for approved Local Validation identities via `POST /api/v1/platform/auth/login` (8/8)
- [x] No Live Preview / quick-login routes remain (`/local-validation/sessions` and `/live-preview/*` → 404)
- [x] Production compose omits `LocalValidation__Enabled`; Production startup forbids `LocalValidation:Enabled=true`
- [x] Architecture + Platform/Admin/POS unit + integration suites pass (Release)
- [ ] Broader user-acceptance / navigation walkthrough for each identity (remaining WP11 work)
- [ ] Phase 16 remains under validation (not closed) — **do not mark WP11 complete**

## Out of scope

- Phase 17
- Closing Phase 16
- Rewriting historical P14 Live Preview report bodies (filenames retained; superseded notes added; active docs use Local Validation)
- Unrelated MAUI “preview” (receipt/statement) copy
