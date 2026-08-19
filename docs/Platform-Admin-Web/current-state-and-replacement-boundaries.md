# Platform Admin Web — Current State and Replacement Boundaries

This file defines the boundaries between the current Blazor Platform Admin and the planned documentation-only replacement.

## A. Existing application (`src/Platform/ExItS.Platform.Admin`)

Current application is a Blazor Web app using Ant Design Blazor components (verified via `ExItS.Platform.Admin` project references).

Key characteristics (working reference; not judged):

- Responsibilities: Platform control-plane UI for platform administration functions.
- Routing surface: `/admin/*` pages such as:
  - `/admin/users` (Platform staff user profiles + invite/management flows)
  - `/admin/products` and `/admin/product-entry` (catalog product administration)
  - `/admin/plans` (plan list/details via product/plan hierarchy)
  - `/admin/subscriptions` (subscription management UI)
  - `/admin/platform-roles` (platform role definitions + permissions)
  - `/admin/privacy-compliance` and related evidence pages
  - `/admin/workspaces` (workspace selection / navigation model)
- Authentication / session plumbing:
  - Cookie-based authentication in the Admin host
  - Login/logout endpoints exist under `/admin/*` (and the app establishes a browser session by calling Platform auth endpoints)
  - The Admin host forwards the authenticated Platform session token when calling Platform APIs
- Authorization approach:
  - Global page gating uses ASP.NET Authorization (`AuthorizeRouteView` + `[Authorize]` on pages)
  - UI permission shaping is circuit-scoped: `PlatformPermissionState` loads authorization facts (convenience only) and the UI hides nav items / mutation controls when permissions are missing.
- How Admin reaches backend capabilities:
  - UI injects `IPlatformApiClient` (backed by `PlatformApiClient`)
  - `PlatformApiClient` calls Platform backend HTTP endpoints under `/api/v1/*`
  - Calls are authenticated via the session token forwarding mechanism.
- Shared UI/design dependencies:
  - Ant Design Blazor (`AntDesign`) plus ExItS shared UI/design projects used by the Admin host:
    - `src/Shared/ExItS.DesignSystem`
    - `src/Shared/ExItS.Web.UI`

Role during transition:

- The current Admin must remain operational and preserved as a fallback reference.
- Replacement work targets feature parity by explicitly comparing functionality, not by deleting the old console early.

## B. Future application (`src/Platform/ExItS.Platform.Admin.Web`)

Planned future replacement frontend is documented as:

- separate frontend application under `src/Platform/ExItS.Platform.Admin.Web` (planned path)
- not part of this documentation package (future frontend work is deferred)
- will consume server-authoritative Platform backend APIs/contracts
- will not directly access Platform persistence (no direct Platform DB access)
- will coexist with the current Admin until explicit cutover validation is completed

Replacement constraints (boundary checks):

- Documentation completion in this series does not authorize implementation.
- Detailed frontend libraries and patterns are decided later (DOC-03 and later).

## C. Product boundaries

Platform Admin boundaries:

- Platform Admin is a Platform control-plane console only.
- It must not become a POS or PLM operational console.

POS:

- POS operational domain remains within the POS product boundaries.

Pinoy Loan Manager (PLM):

- Pinoy Loan Manager operational domain remains within the PLM product boundaries.

Explicit rule:

- Do not leak POS/PLM operational workflows into Platform Admin merely because this repo hosts multiple products.

## D. Replacement strategy

Documented replacement strategy:

- OLD ADMIN (`src/Platform/ExItS.Platform.Admin`)
  - keep operational
  - keep as reference/fallback
  - cutover requires explicit validation
  - removal requires separate authorization
- NEW ADMIN (`src/Platform/ExItS.Platform.Admin.Web`)
  - planned independently later (not in this package)
  - feature parity measured explicitly using DOC-06/DOC-07 planning artifacts
  - cutover only after validation
  - coexist with the old Admin until cutover

