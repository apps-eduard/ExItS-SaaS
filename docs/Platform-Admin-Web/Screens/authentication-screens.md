# Platform Admin Web — Authentication Screen Specifications

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-FINAL-AMEND-01  
**Branch:** `docs/platform-admin-web-v2`  
**API audit SHA:** `618a7b61711a2baee5a1589bd49bbd3312eb4eec`

---

## 0. Scope

Authentication screens are the entry point to the Platform SaaS Control Center. They establish visual identity, brand presence, and the authentication flow before the application shell is loaded.

Mobile-responsive authentication is a **first-class UX requirement**. Unlike the administrative shell (desktop-first), authentication screens must be deliberately polished for phone-sized screens because administrators may need to sign in from any device.

---

## 1. Capability requirement IDs (authentication)

| ID | Description | Status | Verified route | Auth | Evidence |
|---|---|---|---|---|---|
| `PWEB-CAP-AUTH-LOGIN` | Authenticate with credentials (email + password) | **EXISTS** | `POST /api/v1/platform/auth/login` | AllowAnonymous, rate-limited | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-LOGOUT` | Invalidate session and clear cookie | **EXISTS** | `POST /api/v1/platform/auth/logout` | AllowAnonymous | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-ME` | Validate/renew session, return auth state | **EXISTS** | `GET /api/v1/platform/auth/me` | AllowAnonymous | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-FORGOT-PASSWORD` | Request password reset email | **EXISTS** | `POST /api/v1/platform/auth/forgot-password` | AllowAnonymous, rate-limited | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-RESET-PASSWORD` | Reset password with token | **EXISTS** | `POST /api/v1/platform/auth/reset-password` | AllowAnonymous, rate-limited | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-REGISTER` | Register personal account (email + display name) | **EXISTS** | `POST /api/v1/platform/auth/register` | AllowAnonymous, rate-limited | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-ACTIVATE` | Activate personal account with token + password | **EXISTS** | `POST /api/v1/platform/auth/activate-account` | AllowAnonymous, rate-limited | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-EXTERNAL-CHALLENGE` | Initiate external login (Google/Facebook) | **EXISTS** | `GET /api/v1/platform/auth/external/{provider}/challenge` | AllowAnonymous, rate-limited | `ExternalAuthEndpoints.cs` |
| `PWEB-CAP-AUTH-EXTERNAL-COMPLETE` | Complete external login callback | **EXISTS** | `GET /api/v1/platform/auth/external/{provider}/complete` | AllowAnonymous | `ExternalAuthEndpoints.cs` |
| `PWEB-CAP-AUTH-CHANGE-PASSWORD` | Change password (authenticated) | **EXISTS** | `POST /api/v1/platform/auth/change-password` | Authenticated | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-ACCOUNT-PROFILES` | List account profiles for user | **EXISTS** | `GET /api/v1/platform/auth/account-profiles` | Authenticated | `AuthEndpoints.cs` |
| `PWEB-CAP-AUTH-PROFILE-SELECT` | Select account profile, set session | **EXISTS** | `POST /api/v1/platform/auth/account-profiles/select` | Authenticated | `AuthEndpoints.cs` |

---

## 2. A) Sign In

### Purpose

Primary entry point for Platform administrators to authenticate and establish a session.

### Route concept

`/admin/login` — displayed before the application shell loads. Uses a minimal auth layout (no sidebar, no navigation).

### Information hierarchy (top to bottom)

1. **Brand identity** — ExItS brand mark and product name
2. **Product identity** — "Platform" or "SaaS Control Center" subtitle
3. **Welcome heading** — "Sign in" or equivalent
4. **Credential form** — email field, password field
5. **Primary action** — "Sign In" button
6. **Password recovery** — "Forgot password?" link
7. **Account creation** — "Create account" / "Register" link (if `PWEB-CAP-AUTH-REGISTER` is available)
8. **Social authentication** — "Continue with Google", "Continue with Facebook" (when providers are configured)
9. **Development tools** — Test user selector (Development/Testing only; see §2.8)

### Credential fields

| Field | Type | Autocomplete | Requirements |
|---|---|---|---|
| Email | `email` or `text` | `username` | Required. Proper `<label>`. Clear focus ring. Validation error state. |
| Password | `password` | `current-password` | Required. Proper `<label>`. Clear focus ring. Password visibility toggle. Validation error state. |

Password-manager-friendly semantics: proper `<form>`, `<label>`, `autocomplete` attributes, and no JS-only form submission that breaks autofill.

### Social authentication

Supported providers (when configured):

- **Continue with Google** — accessible button with provider icon and text label
- **Continue with Facebook** — accessible button with provider icon and text label

Do not invent providers that do not exist. Do not use ambiguous icon-only controls.

Each social button navigates to `/api/v1/platform/auth/external/{provider}/challenge` with appropriate return URL.

### States

| State | Behavior |
|---|---|
| Initial | Empty form, primary action enabled |
| Loading | Disable form + primary action. Show spinner/indicator on the button. |
| Invalid credentials | Inline error: "Invalid email or password." Field values preserved (except password cleared). |
| Validation failure | Field-level error messages below the relevant field. |
| Network failure | Inline error: "Unable to connect. Please try again." Retry enabled. |
| Account disabled/locked | Server error displayed: "Account is locked" or equivalent from problem+json response. No internal detail leakage. |
| Session expired return | Show notice: "Your session has expired. Please sign in again." (triggered by `?notice=session-expired` query param) |
| External login error | Error from query param displayed as inline alert. |
| Successful login | Redirect to dashboard or intended return path. |

### Mobile requirements

- Reduce oversized decorative hero/header treatment on narrow screens
- Credential form visible without scrolling on standard phone viewports
- Touch-friendly control heights (≥ 48px targets)
- Keyboard opening must not make the Sign In button unreachable
- Form remains usable with browser zoom and text scaling
- No clipped content at narrow widths

### "Remember me"

Include only if session cookie semantics support extended vs. session-only duration. Current evidence shows 30-minute sliding expiration. Do not display a fake "Remember me" option unless the backend actually supports differentiated session lifetimes.

### Security constraints

- No credentials in URLs
- No auth tokens in localStorage/sessionStorage
- HttpOnly session cookie (server-managed)
- No credential logging to console
- CSRF posture: per DOC-03 recorded gap — confirm at implementation time
- Protected logout semantics (server invalidates session)

### Required backend capabilities

- `PWEB-CAP-AUTH-LOGIN`
- `PWEB-CAP-AUTH-ME`
- `PWEB-CAP-AUTH-EXTERNAL-CHALLENGE` (when social providers configured)

### Accessibility

- All form controls have associated labels
- Error messages linked via `aria-describedby`
- Focus management: on load, focus the email field
- Keyboard: Tab order follows visual layout. Enter submits the form.
- Reduced motion honored for any loading animations
- Sufficient contrast in both Light and Dark themes

### Responsive behavior

- Desktop: centered card with generous whitespace
- Tablet: centered card, reduced horizontal padding
- Phone: full-width card, minimal padding, form fills viewport width

---

## 2.8 Development Test User

In Development/Testing environments only:

- A test user selector allows developers to quickly sign in as predefined test users
- Visually separated from the real sign-in form (below a clear divider)
- Visually subdued — secondary/muted styling, not competing with the primary sign-in
- Preferably collapsible or contained in a "Development Tools" region
- **Never rendered in Production**
- Backend evidence: `LocalValidationIdentityPicker` component fills credentials via JS; `testing/complete` external auth endpoint returns 403 in production

---

## 3. B) Create Account / Register

### Purpose

Allow new personal account registration. Two-step flow: register (sends activation email), then activate (set password).

### Route concept

`/admin/register` — linked from the Sign In page.

### Flow

1. **Registration form**: Display Name, Email
2. **Submit**: calls `PWEB-CAP-AUTH-REGISTER`
3. **Success**: confirmation message — "Check your email to activate your account."
4. **Activation**: separate page/route processes the activation token + password

### States

| State | Behavior |
|---|---|
| Initial | Empty form |
| Loading | Disable form, show loading indicator |
| Validation failure | Field-level errors |
| Success | Confirmation message with email instruction |
| Duplicate email | Server error displayed without revealing whether the email exists (per security best practice; actual server behavior governs) |
| Network failure | Inline error with retry |

### Required backend capabilities

- `PWEB-CAP-AUTH-REGISTER`
- `PWEB-CAP-AUTH-ACTIVATE`

---

## 4. C) Account Activation

### Purpose

Complete personal account registration by setting a password with the activation token.

### Route concept

`/admin/activate` with token from email link.

### Flow

1. Token extracted from URL
2. Password field + confirm password field
3. Submit calls `PWEB-CAP-AUTH-ACTIVATE`
4. Success: redirect to Sign In with success notice

### States

| State | Behavior |
|---|---|
| Invalid/expired token | Error: "This activation link has expired or is invalid." Link to re-register. |
| Success | Redirect to Sign In |

### Required backend capabilities

- `PWEB-CAP-AUTH-ACTIVATE`

---

## 5. D) Forgot Password

### Purpose

Allow users to request a password reset email.

### Route concept

`/admin/forgot-password` — linked from Sign In page.

### Flow

1. Email field
2. Submit calls `PWEB-CAP-AUTH-FORGOT-PASSWORD`
3. Always show success message regardless of whether the email exists (prevent email enumeration)

### States

| State | Behavior |
|---|---|
| Initial | Email field |
| Loading | Disable form |
| Success | "If an account exists with that email, a reset link has been sent." |
| Network failure | Inline error with retry |

### Required backend capabilities

- `PWEB-CAP-AUTH-FORGOT-PASSWORD`

---

## 6. E) Reset Password

### Purpose

Complete password reset using the token from the reset email.

### Route concept

`/admin/reset-password` with token from email link.

### Flow

1. Token from URL
2. New Password + Confirm Password fields
3. Submit calls `PWEB-CAP-AUTH-RESET-PASSWORD`
4. Success: redirect to Sign In with confirmation notice

### States

| State | Behavior |
|---|---|
| Invalid/expired token | Error with link to Forgot Password |
| Validation failure | Field-level errors (password requirements) |
| Success | Redirect to Sign In |

### Required backend capabilities

- `PWEB-CAP-AUTH-RESET-PASSWORD`

---

## 7. F) Session Expired

### Purpose

When an active session expires, redirect to Sign In with a clear notice.

### Behavior

- API calls return session-invalid problem response
- Shell shows session-expired notice (toast or banner)
- User is redirected to `/admin/login?notice=session-expired`
- Cached server state is cleared (TanStack Query cache invalidated)
- Login page displays: "Your session has expired. Please sign in again."
- After re-authentication, return to the intended route when safe

### Required backend capabilities

- `PWEB-CAP-AUTH-ME` (session validation)

---

## 8. Browser security posture (preserved from DOC-03)

- Reusable session credentials must not be placed in URLs
- CSRF/antiforgery posture: recorded gap per DOC-03 §5.2
- Protected logout: server invalidates session, client deletes cookie
- Server-authoritative authentication
- HttpOnly/Secure/SameSite=Lax session cookie
- No localStorage/sessionStorage auth credentials
- No credential/token console logging
- Correlation ID (`X-Correlation-Id`) preserved in auth requests

---

## 9. Explicit non-goals

- No MFA enrollment or challenge screens (MFA enforcement is deferred)
- No impersonation/support-login screens
- No organization staff invitation acceptance screens (handled by separate Organization Web flows)
- No API key management screens
- No OAuth/OIDC provider configuration screens
