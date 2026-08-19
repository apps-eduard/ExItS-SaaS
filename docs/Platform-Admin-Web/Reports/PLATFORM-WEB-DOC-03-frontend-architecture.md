# PLATFORM-WEB-DOC-03 — Frontend Architecture Report

**Status:** Complete  \n
**Branch:** `docs/platform-admin-web-v2`

## Delivered capability

This package defines the approved target React frontend architecture (planning-only) for the future `src/Platform/ExItS.Platform.Admin.Web/` application and documents security + dependency boundaries without any implementation work.

Specifically, it delivers:

1. **Evidence audit (pre-decision)** for how the repo currently handles:
   - Platform interactive session cookies (interactive Admin host)
   - Platform API session authentication tokens (cookie / session header / Authorization token parsing)
   - CORS configuration (origins + credentials)
   - Problem-details error conventions (RFC7807-like: `application/problem+json` + `errorCode`)
   - Correlation ID behavior (`X-Correlation-Id`)
   - OpenAPI/Swagger presence/absence (recorded gap when not found)
   - Shared UI design principles (semantic tokens/accessibility guidance)
   - Production hosting assumptions (reverse-proxy HTTPS termination)

2. **Freeze target frontend stack** for DOC-03:
   - React + TypeScript + Vite
   - Tailwind CSS + shadcn/ui + Lucide icons
   - React Router
   - TanStack Query + TanStack Table
   - React Hook Form + Zod
   - Motion
   - Backend: existing .NET ExItS Platform API

3. **Frontend quality/security rules**:
   - Server state vs form vs transient UI state responsibilities
   - Strict TypeScript guidance
   - Normalized error handling and session-expiration handling approach
   - Evidence-based authentication posture (cookie/session first)
   - Recorded integration gaps where evidence is incomplete (CSRF and typed-client generation)

4. **Dependency policy** for future implementation work packages:
   - Review cadence expectations
   - No auto-merge dependency updates
   - Lock file requirement at implementation start

## Evidence anchors (examples)

Key evidence sources referenced in `docs/Platform-Admin-Web/frontend-architecture.md`:

- Cookie session establishment for interactive Admin host (`.ExItS.Admin.Auth`, HttpOnly, SameSite=Lax, sliding expiration; `app.UseAntiforgery()` present)
- Platform API session token extraction (`.ExItS.Platform.Auth` cookie, `X-ExItS-Session-Token` header, Authorization scheme fallback)
- Platform API login/logout/session endpoints:
  - `/api/v1/platform/auth/login`
  - `/api/v1/platform/auth/logout`
  - `/api/v1/platform/auth/me`
- Platform API CORS configuration via `Cors:AllowedOrigins` and `.AllowCredentials()`
- Platform API error conventions via `application/problem+json` and `errorCode` mapping
- No Swagger/OpenAPI registration evidence found (typed-client generation recorded as a future gap)
- Shared UI design system guidance in `docs/engineering/ui-design-system.md`
- Production topology / reverse-proxy HTTPS assumptions in `docs/engineering/production-deployment-architecture.md`

## Exclusions

- No frontend app created
- No React code added
- No backend changes
- No dependency installation, version pinning, or lock file changes
- No .cursor/rules modifications

