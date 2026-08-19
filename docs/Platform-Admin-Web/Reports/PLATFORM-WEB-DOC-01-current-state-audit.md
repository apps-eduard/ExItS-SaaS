# PLATFORM-WEB-DOC-01 — Current State Audit

This report documents the current Platform Admin Web implementation as a working reference for later replacement planning.

## Baseline (audit-only)

- Source repository: `apps-eduard/ExItS-SaaS`
- Branch/worktree: `docs/platform-admin-web-v2`
- Baseline origin/main snapshot: `7f576f70665d78b319f31fc1cfa12a7e9c14482f`

## 1. Current Admin: high-level purpose

`src/Platform/ExItS.Platform.Admin` is a Blazor Web application that provides Platform Admin control-plane functionality under `/admin/*`.

It uses Ant Design Blazor as the primary UI framework and calls Platform backend services via an injected HTTP API client.

## 2. Current routing and major surfaces

Routing is defined via a router component that wraps pages in `AuthorizeRouteView` to enforce authentication at navigation time.

Major `/admin/*` page families (non-exhaustive examples verified by `@page` declarations):

- `/admin/workspaces`
- `/admin/users` (plus subroutes such as `/admin/users/{Id}`)
- `/admin/products` (plus `/admin/products/{Id}`)
- `/admin/subscriptions` (plus `/admin/subscriptions/{Id}`)
- `/admin/platform-roles` (plus `/admin/platform-roles/{Id}`)
- `/admin/privacy-compliance` (plus evidence/systems subpages)

Representative page patterns:

- Pages are decorated with `[Authorize]`
- Pages gate UI with:
  - shell mode (`AdminShellContext`) such as Platform vs Organization vs Personal
  - permission codes via `PlatformPermissionState` (UI convenience; not a replacement for server-side auth)

## 3. Authentication approach (session + cookie)

The Admin host configures ASP.NET Core cookie authentication.

Key behaviors observed in `Program.cs` and related services:

- Login flow:
  - public credential entry exists under `/admin/login/*`
  - the host establishes a browser session by calling Platform auth endpoints (and then mints the Admin cookie)
- Logout flow:
  - `/admin/logout` and `/admin/logout` handlers clear the Admin cookie and call Platform logout where possible
- Browser-to-Platform API calls:
  - an HTTP message handler attaches the authenticated session token to outgoing Platform API requests

## 4. Authorization approach (server + UI convenience)

Two layers exist:

1. Server-side authorization:
   - `[Authorize]` attributes plus `AuthorizeRouteView` ensure pages are accessible only to authenticated sessions.

2. UI shaping convenience:
   - `PlatformPermissionState` loads permission codes from Platform authorization facts (loaded once per Blazor circuit)
   - UI uses `HasPermission(...)`/`HasAnyPermission(...)` to hide nav items and disable mutation controls

This distinction matters for replacement: the new frontend must preserve the server-side auth enforcement behavior; it must treat UI permission shaping as convenience only.

## 5. How the current Admin reaches backend capabilities

The UI injects `IPlatformApiClient`, which is backed by `PlatformApiClient`.

`PlatformApiClient` uses an `HttpClient` configured with a base URL and a session-token forwarding handler.

Examples of functional areas contacted by the Admin client include (non-exhaustive):

- Platform admin portfolio summary (admin dashboard data)
- Platform catalog administration:
  - products, product overview
  - plans and plan lifecycle operations
  - personal feature definitions
- Organization administration:
  - organization discovery and directory views
- Platform staff and user management:
  - users, platform staff lists and details
- Platform roles and permission definitions:
  - platform role definition listing and modification
- Privacy/compliance workspaces:
  - privacy evidence overview pages

## 6. Shared UI / design dependencies

Confirmed shared dependencies for the Admin host:

- `AntDesign` components
- `src/Shared/ExItS.DesignSystem` (semantic tokens + shared UI primitives for ExItS)
- `src/Shared/ExItS.Web.UI` (shared web UI and AntDesign integrations)

The replacement documentation must preserve UI stack boundaries. Do not decide new UI libraries in this track.

## 7. Platform/Product boundaries (control-plane vs operational domain)

Current Platform Admin is scoped to Platform control-plane responsibilities:

- Platform users/staff profiles
- Platform org membership and organization selection
- Platform catalog, plans, subscriptions, and entitlements
- Platform roles/permissions and privacy compliance workspace navigation

Modernization must avoid leaking product operational responsibilities into the Platform Admin replacement.

## 8. Replacement-relevant technical debt and drivers (documentation framing)

The replacement track should treat the existing Admin as a reference implementation:

- The Admin chrome and UI gating are tightly integrated with current session/token forwarding behavior.
- Replacement must preserve:
  - server-side auth enforcement semantics
  - session token forwarding to Platform APIs
  - permission loading and UI gating behavior (as convenience only)
- Replacement should be structured so that backend contract usage stays explicit and not coupled to the old UI code paths.

This report is intentionally “audit-only”: it describes current behavior and boundaries to inform DOC-02 onward mapping.

