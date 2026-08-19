# Platform Admin Web — Implementation Status

Program: `Platform Admin Web (Modernization)`  
Implementation start: `2026-08-19`  
Authorization: Product Owner authorized implementation through the first visual foundation checkpoint only  
Branch: `feat/platform-admin-web-v2`  
Application path: `src/Platform/ExItS.Platform.Admin.Web/`

## Gates

| Gate | Status | Notes |
|---|---|---|
| A — Documentation approved | PASSED | DOC-01..10 + AMEND-01 merged to main |
| B — Backend/browser readiness | PASSED for foundation | CSRF recorded as `BLOCKS_FUTURE_MUTATION`; OpenAPI recorded as `DEFERRED_TOOLING`; social-login session token in URL recorded as `BLOCKS_CUTOVER` for that flow |
| C — React scaffold | AUTHORIZED in PWEB-IMPL-01 | Feature screens not authorized |
| D–G | NOT AUTHORIZED | No cutover; no old Admin retirement |

## Scope boundaries

- Existing Blazor Admin (`src/Platform/ExItS.Platform.Admin`) remains the active operator console
- No cutover authorization
- No old Admin retirement authorization
- No POS/PLM operational scope
- Implementation authorized only through the first visual foundation checkpoint (Login + design foundation + shell + dashboard), after this scaffold package
- PWEB-IMPL-01 stops after scaffold; Login/Dashboard/shell UI are later packages

## Gate B findings (2026-08-19)

| Area | Classification | Evidence |
|---|---|---|
| `POST /api/v1/platform/auth/login` | READY_FOR_FOUNDATION | Cookie session established |
| `POST /api/v1/platform/auth/logout` | READY_FOR_FOUNDATION | Session invalidated; cookie deleted |
| `GET /api/v1/platform/auth/me` | READY_FOR_FOUNDATION | Session validate/renew |
| Account-profile selection | READY_FOR_FOUNDATION | `POST /api/v1/platform/auth/account-profiles/select` |
| Session cookie | READY_FOR_FOUNDATION | HttpOnly, SameSite=Lax, Secure outside Development/Testing |
| CORS + credentials | READY_FOR_FOUNDATION | `AllowCredentials()`; origins from `Cors:AllowedOrigins` (Vite origin must be added before cross-origin browser calls) |
| CSRF / antiforgery | BLOCKS_FUTURE_MUTATION | No `AddAntiforgery`/`UseAntiforgery` in Platform API; cookie-authenticated mutations are not yet confirmed CSRF-safe |
| problem+json | READY_FOR_FOUNDATION | `AddProblemDetails()` + pipeline exception handler |
| `X-Correlation-Id` | READY_FOR_FOUNDATION | Request/response correlation in `PlatformSecurityPipeline` |
| External authentication | READY_FOR_FOUNDATION for later screens | Google/Facebook challenge/complete exist when configured |
| Session token in external-login return URL | BLOCKS_CUTOVER | `ExternalAuthEndpoints` appends `sessionToken=` to `returnUrl`; reusable session credentials must not appear in URLs |
| Development/Testing test-user picker | READY_FOR_FOUNDATION | Existing Admin Local Validation identity picker; Production must never render it |
| Mailpit | PRESERVED | Local Validation SMTP catcher; not changed in this package |
| Development/Testing Test Payments | PRESERVED | Local Validation only; no real payment integration |
| OpenAPI/Swagger | DEFERRED_TOOLING | No Swagger registration; typed clients remain manual |

Mailpit and Development/Testing fake payments remain in place. This package does not add production email or real payment integration.
