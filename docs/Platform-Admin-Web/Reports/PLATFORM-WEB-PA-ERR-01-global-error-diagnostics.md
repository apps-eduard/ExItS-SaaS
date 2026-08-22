# PLATFORM-WEB-PA-ERR-01 — Global error diagnostics + copy for support

**Status:** Implemented on `feat/platform-admin-error-diagnostics` (not merged)  
**Work package:** PA-ERR-01  
**Starting HEAD:** `191190893be870506180fc9782c5dd4d813b0c05`  
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-local-access`

## Objective

One canonical, safe, copyable diagnostic model for Platform Admin React so any meaningful failure — including network/connection failures — can be reported to support or pasted into Cursor without exposing secrets.

Owner workflow:

1. Something fails → error panel appears  
2. Click **Copy error details**  
3. Paste into Cursor → report includes application, build, environment, page, operation, HTTP method/path, status, error code, trace ID, correlation ID, network classification, timestamp

## Architecture

### Canonical model

`DiagnosticRecord` (`src/lib/diagnostics/diagnostic-types.ts`) is the single shared shape. Key fields:

- `errorReference` — short client reference (`ERR-XXXXXX`)
- `timestampUtc`, `application`, `buildSha`, `environment`, `frontendMode`, `localValidationEnabled`
- `pagePath`, `operation`, `category`, `userMessage`
- `httpMethod`, `apiPath`, `httpStatus`, `httpStatusLabel`
- `errorCode`, `traceId`, `correlationId`
- `networkOnline`, `networkFailureKind`, `browserPlatform`, `retryable`

### Normalization pipeline

| Step | Module | Role |
|---|---|---|
| HTTP / fetch | `platform-http.ts` | `PlatformApiError`, `PlatformNetworkError`, correlation IDs, sanitized paths |
| Classification | `classify-http-error.ts` | HTTP status + errorCode → `DiagnosticCategory` |
| Normalization | `normalize-diagnostic-error.ts` | Unknown error → `DiagnosticRecord` + runtime context |
| Redaction | `diagnostic-redaction.ts` | Query/path sanitization, error reference generation |
| Clipboard | `build-diagnostic-report.ts` | `formatDiagnosticForClipboard()` — stable field order |
| Auth routing | `auth-workflow-diagnostics.ts` | Business token/password errors stay friendly; network/service/rate-limit → `ErrorState` |

### UI components

- **`ErrorState`** — friendly title, reference, copy action, retry/reload
- **`CopyDiagnosticsButton`** — clipboard API + manual textarea fallback
- **`AppErrorBoundary`** — render crashes → `REACT_RENDER_ERROR` diagnostics
- **`DiagnosticsProvider`** — global async/event notices (single alert)

### Backend

`PlatformSecurityPipeline.UsePlatformSecurity()` registers:

- **`UseExceptionHandler`** — unhandled exceptions → safe ProblemDetails HTTP 500  
  - `errorCode`: `platform.unhandled_error`  
  - `traceId`, `correlationId` preserved  
  - No stack trace, exception type, SQL, or paths in response  
  - Full exception logged server-side
- **Correlation middleware** — reads `X-Correlation-Id`, stores in `HttpContext.Items`, echoes on response

Testing-only endpoint (not in Production):

- `GET /api/v1/platform/__test__/unhandled` — gated by `Testing` environment

## Error categories

| Category | Typical source |
|---|---|
| `NETWORK_ERROR` | Fetch `TypeError`, `PlatformNetworkError` |
| `TIMEOUT` | Network timeout (when classified) |
| `SERVICE_UNAVAILABLE` | HTTP 502/503/504 |
| `RATE_LIMITED` | HTTP 429 |
| `AUTHENTICATION_REQUIRED` | HTTP 401 (session) |
| `FORBIDDEN` | HTTP 403 |
| `VALIDATION_ERROR` | HTTP 400 validation |
| `NOT_FOUND` | HTTP 404 |
| `CONFLICT` | HTTP 409 |
| `SECURITY_REQUEST_ERROR` | Antiforgery / CSRF (419 when used) |
| `DOMAIN_ERROR` | Known application error codes |
| `SERVER_ERROR` | HTTP 500+ |
| `REACT_RENDER_ERROR` | Error boundary |
| `UNEXPECTED_CLIENT_ERROR` | Other client failures |

Business auth flows (invalid/expired token, password policy) keep friendly inline alerts; copyable diagnostics apply to network, service, rate-limit, and unexpected server failures.

## Clipboard format

Produced by `formatDiagnosticForClipboard()`:

```
EXITS PLATFORM ERROR REPORT

Error Reference: ERR-8F32A1
Time: 2026-08-22T11:30:00+03:00
Application: Platform Admin React
Build: 19119089
Environment: Local Validation
...
Safe to paste into Cursor: YES
```

Missing values render as `Not available`. HTTP status for network failures renders as `Not received`.

## Redaction rules

Never copied or displayed in user-visible diagnostics:

- Passwords (`password`, `newPassword`, `oldPassword`, `confirmPassword`)
- Bearer tokens, cookies, antiforgery tokens
- Reset/activation/recovery tokens, handoff tickets, device-registration tokens
- OAuth codes/state when sensitive
- Raw request bodies
- Production stack traces and server exception internals

URL query stripping removes `?token=`, `?code=`, `?ticket=`, `?access_token=`, `?refresh_token=`, etc. Page paths copy as `/admin/reset-password` without secrets.

API paths copy as relative paths (e.g. `POST /api/v1/platform/auth/reset-password`). Server 500+ user messages use generic text; ProblemDetails `detail` with internals is not shown.

## Trace and correlation

- Every API request sends `X-Correlation-Id` (client-generated UUID).
- API error responses may echo correlation ID in header and ProblemDetails.
- `traceId` comes from server ProblemDetails only — never invented client-side.
- Network failures before response use the client request correlation ID.

## Local Validation vs Production

| Field | Local Validation | Production |
|---|---|---|
| Build SHA | Yes | Yes |
| Frontend mode / API mode | Yes | Omitted or minimal |
| Local Validation flag | Yes | No |
| Stack traces in copy | No | No |
| Server exception detail in UI | No (generic for 500+) | No |

## Owner troubleshooting workflow

1. Reproduce the failure on the affected page.
2. Open the error panel (or global notice).
3. Note the **Error Reference** (`ERR-…`) for human support.
4. Click **Copy error details**.
5. Paste into Cursor or support chat.
6. Use **Retry** after transient failures (e.g. API restarted).

## Sample safe copied reports

### Network failure

```
EXITS PLATFORM ERROR REPORT

Error Reference: ERR-7F31C2
Time: 2026-08-22T08:30:00.000Z
Application: Platform Admin React
Build: abc12345
Environment: Local Validation

Page:
/admin/reset-password

Operation:
Reset password

Category:
NETWORK_ERROR

Message:
Unable to connect to Platform API.

HTTP Method:
POST

API Path:
/api/v1/platform/auth/reset-password

HTTP Status:
Not received

Error Code:
NETWORK_UNAVAILABLE

Trace ID:
Not available

Correlation ID:
a1b2c3d4-e5f6-7890-abcd-ef1234567890

Browser Online:
Yes

Retryable:
Yes

Safe to paste into Cursor:
YES
```

### 403 permission

```
Category:
FORBIDDEN

Message:
Unable to complete this request.

HTTP Method:
GET

API Path:
/api/v1/platform/organizations/00000000-0000-0000-0000-000000000001

HTTP Status:
403

Error Code:
platform.authorization.forbidden

Trace ID:
00-abc123def456-7890-1234-567890abcdef-01

Correlation ID:
11111111-2222-3333-4444-555555555555
```

### 409 conflict

```
Category:
CONFLICT

HTTP Status:
409

Error Code:
platform.subscription.conflict

Message:
Unable to complete this request.
```

### 500 server failure

```
Category:
SERVER_ERROR

HTTP Status:
500

Error Code:
platform.unhandled_error

Message:
Unable to complete this request.

Trace ID:
00-server-trace-id-here

Correlation ID:
11111111-2222-3333-4444-555555555555
```

## Tests

- **Frontend:** `src/lib/diagnostics/diagnostics.test.ts` — redaction, HTTP classification, clipboard format, network model
- **Frontend:** `ErrorState.test.tsx`, auth page tests, organization workspace tests
- **Backend:** `tests/ExItS.Platform.IntegrationTests/ApiUnhandledExceptionTests.cs`

## Exclusions

- No POS React changes
- No Agent 2 commercial workflow semantic changes (display/classification only)
- No merge to `main`
- No production public error-throw endpoint
