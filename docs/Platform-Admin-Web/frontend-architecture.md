# Platform Admin Web — Frontend Architecture (React, Security, Dependencies)

**Status:** Documentation Only — implementation not authorized  
**Purpose (DOC-03):** Define the target React frontend architecture for the future `src/Platform/ExItS.Platform.Admin.Web/` application, based on existing Platform API authentication/session evidence.

---

## 0. Evidence audit (what exists today)

### 0.1 Browser auth/session evidence (interactive Admin host)

`src/Platform/ExItS.Platform.Admin/Program.cs` configures cookie authentication for the current interactive Admin host:

- Cookie name: `.ExItS.Admin.Auth`
- `HttpOnly = true`
- `SameSite = Lax`
- Secure policy varies by environment
- Login/logout paths under `/admin/login` and `/admin/logout`
- Cookie expiration: 30 minutes; sliding expiration enabled

Admin host also configures anti-forgery middleware (`app.UseAntiforgery()`).

### 0.2 Platform API authentication evidence (browser/API access)

`src/Platform/ExItS.Platform.Api/Authentication/PlatformSessionAuthenticationHandler.cs` shows the Platform API accepts a session token from:

1. An HttpOnly cookie
   - cookie name is configured via `PlatformSessionOptions.CookieName` (default `.ExItS.Platform.Auth`)
2. A request header
   - header name is configured via `PlatformSessionOptions.SessionTokenHeaderName` (default `X-ExItS-Session-Token`)
3. An `Authorization` header that starts with the Platform session scheme (server parses it as a token)

`src/Platform/ExItS.Platform.Api/Identity/AuthEndpoints.cs` confirms session establishment:

- `POST /api/v1/platform/auth/login` sets the session cookie via `AppendSessionCookie(...)` and returns a session DTO.
- `POST /api/v1/platform/auth/account-profiles/select` can also refresh/set the session cookie.
- `GET /api/v1/platform/auth/me` validates the session token and returns the current/renewed authentication DTO.
- `/api/v1/platform/auth/logout` deletes the session cookie.

`AppendSessionCookie(...)` uses:

- `HttpOnly = true`
- `SameSite = Lax`
- `Secure = true` outside development/testing
- `Path = "/"` (cookie is available to the whole app origin)

### 0.3 CORS evidence

`src/Platform/ExItS.Platform.Api/Common/PlatformSecurityPipeline.cs` configures CORS:

- Allowed origins come from `Cors:AllowedOrigins`
- Policy uses:
  - `.WithOrigins(origins)`
  - `.AllowAnyHeader()`
  - `.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")`
  - `.AllowCredentials()`

### 0.4 CSRF / antiforgery evidence

Search evidence in `src/Platform/ExItS.Platform.Api` indicates:

- No general `UseAntiforgery` / `AddAntiforgery` wiring was found in Platform API startup code.
- Endpoint-level antiforgery disabling is present for at least one endpoint family (example: `GlobalCatalogEndpoints...DisableAntiforgery()`).

**Security posture gap to record:** For a cookie-authenticated React client, the Platform API CSRF protection strategy must be confirmed at implementation time (i.e., whether antiforgery is enforced for mutations and how). This doc does not invent a CSRF workaround because no complete evidence was found here.

### 0.5 Error/problem-details conventions evidence

`src/Platform/ExItS.Platform.Api/Program.cs` registers `AddProblemDetails()`.

The exception handler in `src/Platform/ExItS.Platform.Api/Common/PlatformSecurityPipeline.cs` writes:

- `ContentType = application/problem+json`
- JSON payload containing `title`, `status`, `detail`, `errorCode`, `traceId`

`src/Platform/ExItS.Platform.Api/Common/PlatformApiResults.cs` maps failures to `Results.Problem(...)` with an `errorCode` extension:

- `errorCode` is returned as an extension field in the problem+json response.

### 0.6 Correlation ID evidence

`src/Platform/ExItS.Platform.Api/Common/PlatformSecurityPipeline.cs` reads `X-Correlation-Id` from requests (or uses `TraceIdentifier`), and returns it on responses via `OnStarting`.

### 0.7 OpenAPI/Swagger evidence

No Swagger/OpenAPI registration evidence was found in Platform API source code (`AddSwaggerGen`, `UseSwagger`, etc.).  
**Future tooling gap:** typed API client generation should not be assumed; this will require either manual typed clients or an explicit later tooling/contract work package.

### 0.8 Shared UI assets evidence

`docs/engineering/ui-design-system.md` defines semantic CSS tokens and accessibility/responsive/table conventions across web/mobile surfaces.

Platform Admin uses Ant Design Blazor today; this DOC-03 intentionally targets a React stack for the future application, while preserving the repo’s “semantic naming / accessible semantics / data-dense tables” principles.

### 0.9 Deployment/hosting evidence (production topology)

`docs/engineering/production-deployment-architecture.md` states production uses:

- Reverse proxy with HTTPS termination
- Admin routed at `/admin/*`, Platform API at `/platform/*`
- Cookies marked `Secure` in production (consistent with cookie evidence above)

**Implication for React:** the future React app should assume it runs behind the reverse proxy on HTTPS and that cookies are usable on the origin that hosts the frontend.

---

## 1. Approved Target Frontend Stack (freeze for DOC-03)

Core:
- React
- TypeScript
- Vite

Presentation:
- Tailwind CSS
- shadcn/ui
- Lucide icons

Routing:
- React Router

Server state:
- TanStack Query

Data tables:
- TanStack Table

Forms:
- React Hook Form
- Zod

Motion:
- Motion

Backend:
- existing .NET ExItS Platform API

Non-goals / constraints for docs:
- Do not pin versions in planning docs. Versions are pinned only during authorized implementation.
- Do not add Redux (or another global state library) unless a proven need is established in an implementation work package.

---

## 2. Target application boundary (expected location; not created here)

Target future location:
- `src/Platform/ExItS.Platform.Admin.Web/`

This doc defines expected frontend source organization without creating the app:

```text
src/
  app/
  features/
  components/
    ui/
    exits/
  api/
  hooks/
  layouts/
  lib/
  styles/
```

Feature folders should map to Platform business areas (as defined by the Platform Admin Web IA in DOC-02), and must not include POS/PLM product operational workflows.

---

## 3. State management rules (what goes where)

Rule of thumb:
- Server state = server source of truth → TanStack Query
- Form state = user input + validation → React Hook Form + Zod
- Transient UI state (dialogs open/close, active tab selection, hover state) → React state
- Shared UI context only when justified (e.g., narrow navigation context or selection context)
- Avoid duplicating backend authoritative state in multiple places
- No default “global state” framework; choose local state + TanStack Query cache boundaries

---

## 4. API client boundary (React must not own auth/business rules)

React UI flow:
1. React page / component
2. Feature service / hook
3. Typed API client
4. `ExItS.Platform.Api` HTTP endpoints

Rules:
- No direct DB access
- No Infrastructure references
- No Application/Domain assembly coupling from the browser
- No “authorization semantics” enforced only by frontend gating
- Server permission checks remain authoritative
- Error handling must be normalized and consistent with the API problem+json convention
- Preserve correlation/request IDs when available (`X-Correlation-Id` evidence)
- Abort/cancellation support where appropriate (especially during fast navigation / search)
- Retry policies must be safe for idempotency (retry only when the HTTP operation is known safe)

Typed client generation:
- Because no OpenAPI/Swagger evidence was found, typed-client generation is not assumed.
- Until a contract/tooling package exists, typed clients should be implemented manually (or via explicitly authorized tooling in later work packages).

---

## 5. Authentication security posture (cookie/session first; evidence-based)

### 5.1 Preferred browser posture

Evidence shows Platform API session token support for:
- HttpOnly cookie (`.ExItS.Platform.Auth` by default)
- `X-ExItS-Session-Token` header
- `Authorization` token scheme fallback

For the future React admin UI, the preferred browser posture is:
- Use the cookie-based session established by Platform auth endpoints.
- Make API calls with credentials enabled (so the HttpOnly cookie is sent).
- Avoid localStorage/sessionStorage for auth tokens.

### 5.2 CSRF posture

Because the Platform API antiforgery strategy was not fully evidenced here (no global antiforgery wiring found in Platform API startup, only endpoint-level disable evidence was found), the React client must not assume it is safe to send cookie-authenticated mutations without CSRF protection.

**Recorded integration gap:** confirm and finalize CSRF behavior for cookie-authenticated mutation endpoints before implementing React mutations in an authorized implementation work package.

### 5.3 Session expiration handling

React should use:
- `GET /api/v1/platform/auth/me` to validate/renew and reflect auth state
- On `401`/session-invalid style problem responses, clear cached server state and redirect users to the login flow

---

## 6. Frontend quality rules (security + UX)

TypeScript:
- Strict mode
- Avoid `any`

Architecture:
- Avoid “framework spread”: component reuse before duplication
- Feature isolation (minimize cross-feature coupling)
- Route lazy-loading where useful

Reliability:
- Loading/empty/error/forbidden states mandatory for data-driven surfaces
- Error boundaries required for routing and top-level app shell
- No fake successful backend behavior
- No mock data in production runtime
- Environment config must never embed secrets in frontend bundles

Accessibility:
- Accessible semantics for forms, tables, dialogs, navigation
- Ensure keyboard focus is correct for modal/drawer interactions
- Respect reduced motion where motion is used

---

## 7. Dependency policy (DOC-03 freeze rules)

Security update:
- review promptly.

Patch/minor:
- review periodically, normally monthly.

Broader dependency health:
- review approximately quarterly.

Major versions:
- dedicated migration branch + explicit validation.

Tooling constraints:
- When implementation begins, a lock file is required.
- Automated tooling may propose updates, but must not auto-merge dependency PRs.
- Do not add dependencies solely because they seem convenient.

---

## 8. Summary of DOC-03 decisions

This DOC-03 freezes:
- The approved target React stack
- State management, server-error handling, and API client boundaries
- Evidence-based authentication posture (cookie/session) and the recorded CSRF + OpenAPI/Swagger gaps
- A dependency governance policy for future implementation

