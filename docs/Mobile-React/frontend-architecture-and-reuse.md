# Mobile React — Frontend Architecture and Code Reuse

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-03  
**Depends on:** [current-state-and-replacement-boundaries.md](current-state-and-replacement-boundaries.md), [product-surfaces-and-ux.md](product-surfaces-and-ux.md)

This file freezes the **PROPOSED_REPLACEMENT_CLIENT_ARCHITECTURE** for a future React host.
It does not change MAUI, Organization Web, Personal Web, DesignSystem Razor components, or .NET APIs.

CURRENT_IMPLEMENTATION_REQUIREMENT for MAUI remains: native CSS / Razor, no Ant Design, no Tailwind.

---

## 0. Current-code audit (evidence)

| Layer | Location | What the future React host may reuse as *concept* | What it must not import |
|---|---|---|---|
| MAUI host | `ExItS.PinoyBusinessPOS.Maui` | Shell split (Auth / Personal / POS), device adapters, Bearer + SecureStorage, LocalStore wiring | The MAUI project, BlazorWebView, Razor pages |
| Organization Web | `ExItS.PinoyBusinessPOS.Web` | Same POS HTTP contracts for management-lite; AntDesign is **not** the visual target | Ant Design, `ExItS.Web.UI`, Org Web pages |
| POS ApiClient | `ExItS.PinoyBusinessPOS.ApiClient` | Typed HTTP surface (`IPosApiClient`, catalog/sales/customers/…, `PlatformAccessClient`, Bearer/session handlers, idempotency, `ApiResult`, problem+json) | C# assemblies from the browser |
| POS Application | `ExItS.PinoyBusinessPOS.Application` | Auth orchestration shapes, offline operation types, capability names | Domain/Application as a JS port of server rules |
| POS LocalStore | `ExItS.PinoyBusinessPOS.LocalStore` | Per-context SQLite, encrypted outbox, FIFO sync, access revalidation | Direct SQLite schema ownership invented in the browser; a second SoR |
| DesignSystem | `src/Shared/ExItS.DesignSystem` | `--exits-*` tokens, density, theme, EN/fil-PH resource *conventions* | Razor primitives (`Button.razor`, …) inside React |
| Backends | Platform API `:8091`, POS API `:8092`, PostgreSQL | Authoritative identity, org, commercial, and POS operational rules | Frontend database access |

MAUI already depends on Application-level adapter interfaces (`IConnectivityService`, `IDocumentHandoffService`, `ISecureTokenStore`, `ILocalStoreRootPathProvider`) plus MAUI-local scanner/image picker. Organization Web does **not** reference LocalStore. That split is the model: selling/offline is device-capable; Org Web is online management.

POS API registers `AddProblemDetails()` and writes `application/problem+json`. Typed OpenAPI generation is not assumed (same gap as Platform Admin planning).

---

## 1. Target runtime topology

```text
React + TypeScript (one application)
        |
        +-- Browser Web
        +-- PWA (installable browser delivery where useful)
        +-- Capacitor
             +-- Android first
             +-- iOS later

ASP.NET Core / .NET (unchanged)
        |
        +-- Platform API  (identity, orgs, memberships, entitlements, Personal)
        +-- POS API       (catalog, sales, inventory, shifts, customers, …)
        |
PostgreSQL (Platform DB + ExItS_PinoyBusinessPOS)
```

Rules:

- No frontend access to PostgreSQL, EF Core, Npgsql, or product `DbContext`
- No cross-product database access
- UI must not become a second pricing/authorization engine
- One React codebase for Web / PWA / Capacitor; host differences live in **adapters**

---

## 2. Approved frontend planning stack

Frozen for this replacement client only. **Do not pin versions** in planning docs.

| Area | Choice |
|---|---|
| Core | React, TypeScript (strict, no `any`), Vite |
| Presentation | Tailwind CSS, shadcn/ui (or the same underlying primitives), Lucide |
| Routing | React Router |
| Server state | TanStack Query |
| Tables | TanStack Table where lists are truly tabular |
| Forms | React Hook Form + Zod |
| Motion | Motion, restrained; honor `prefers-reduced-motion` |
| Native packaging | Capacitor |
| Browser install | PWA where useful (service worker **not** authorized as production rollout here) |
| Backend | Existing Platform API + POS API |

**Not in the default stack:** Redux or another global store. Cart, sheets, and chrome are local React state (current MAUI `SaleCartService` is in-memory session state). Add a global store only if a later implementation package proves Query + local state insufficient.

**Not in this client:** Ant Design (that is Admin / Org Web / Personal Web today). Canonical brand remains DesignSystem **green** (`#166534` light), not Admin Ant blue.

Tailwind/shadcn here do **not** rewrite the MAUI requirement. They apply only after an authorized replacement host exists.

---

## 3. Recommended future source location

**Recommended path (not created):**

```text
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/
```

**Do not create this project in this documentation package.**

### 3.1 Why this path (audit, not invention)

[repository-boundaries.md](../engineering/repository-boundaries.md) allows:

```text
src/Platform/*
src/Products/PinoyBusinessPOS/*
src/Shared/*
```

| Candidate | Verdict |
|---|---|
| Overwrite `ExItS.PinoyBusinessPOS.Maui` | **Forbidden** (MOBILE-D-002) |
| Overwrite `ExItS.PinoyBusinessPOS.Web` | **Forbidden** — that is Organization Web Admin, not the Mobile Client |
| `src/Platform/...` | **Forbidden** — POS operational UI must not live in Platform |
| `src/Shared/...` | **Forbidden** — this is an app host, not a shared library |
| New top-level `src/Clients/...` | **Not used** — would invent a repository boundary not in the current model |
| `src/Platform/ExItS.Personal.Web` | **Wrong host** — browser Personal product; not the MAUI replacement |

The current Mobile Client already lives under PinoyBusinessPOS (`*.Maui`) while hosting Personal and Owner screens that call Platform APIs. The replacement is a **new sibling host**, same ownership pattern as MAUI: CLIENT HOST under the POS product folder, PRODUCT DOMAIN still split (Platform vs POS APIs).

`*.Client` is distinct from `*.Web` (Organization Web) and `*.Maui` (current host). The folder name does **not** mean “POS-only experience” (MOBILE-D-005 / MOBILE-D-006).

Expected internal layout (documentation only):

```text
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/
  src/
    app/           # routes, providers
    features/      # personal, owner, selling, catalog, customers, inventory, …
    components/
      ui/          # shadcn primitives
      exits/       # ExItS-mapped composites (money, stepper, sync chip)
    api/           # typed HTTP clients (Platform + POS)
    adapters/      # device contracts + web/capacitor implementations
    hooks/
    layouts/       # Auth / Personal / POS shells
    lib/           # formatters, i18n helpers, problem+json
    styles/        # Tailwind + --exits-* mapping
    i18n/          # en + fil-PH
```

Capacitor config and PWA assets would live beside `src/` when implementation is authorized. One app, three delivery modes.

---

## 4. Reuse strategy

### A. Shareable concepts with Platform Admin React

May share or adapt **conventions**, not Admin screens:

- ExItS semantic design tokens (`--exits-*`)
- Typography and tabular money
- Color semantics (primary / success / warning / danger / info)
- Localization conventions (en default, fil-PH, resource keys, no hard-coded UI strings)
- Theme concepts (Light / Dark / System)
- Formatting utilities (PHP, dates)
- Validation conventions (Zod schemas mirroring API, not replacing server validation)
- HTTP / error conventions (credentials or Bearer per host, `X-Correlation-Id`, problem+json `errorCode`)
- Accessibility patterns (focus, labels, reduced motion, WCAG 2.2 AA design target)

These may later live as a small shared package **if** a later authorized package creates one. Until then, copy tokens/conventions deliberately; do not import Admin feature modules.

### B. Do not share Platform business UI

Must not couple this client to:

- Platform organizations admin pages
- Platform billing / SaaS payment UI
- Platform entitlements administration UI
- Platform governance / audit explorer UI
- Platform operator authorization components (`view_portfolio`, support-session operator chrome, etc.)

Platform Admin React (`src/Platform/ExItS.Platform.Admin.Web/`, planned) is a different application. Shared ExItS identity: **yes**. Shared operator console: **no**.

### C. High reuse inside Web / PWA / Capacitor

The same feature modules should run on all three deliveries:

- Product browsing, cart, checkout
- Customer / Utang workflows allowed on this host
- Inventory / purchasing / shifts (role-gated)
- Typed POS + Platform API clients
- Translations, forms, Zod schemas
- Offline **coordination** (queue, sync status, capability gates) — semantics aligned with current LocalStore/outbox, not a new business database

Layout chrome may differ by device class (bottom nav vs side nav vs tablet split). Business flows must not fork into three products.

### D. Shared UI primitives and composites (within this client)

The React Mobile Client must use **reusable shared UI primitives and ExItS composites** for controls and patterns that repeat across pages.

Pages **compose** shared components. They do not independently recreate repeated UI or behavior. A visual or behavior change to a shared control should propagate from **one** place.

Conceptual examples (names are planning labels, not a created package):

- AppTopBar / shell chrome
- SearchBar
- FilterBar
- PageHeader
- RefreshButton, CancelButton, BackButton, RetryButton, CopyDiagnosticsButton
- EmptyState, ErrorState, LoadingState
- StatusChip, SyncStatusChip, ConnectivityIndicator
- ConfirmDialog
- InternetRequiredToast / InternetRequiredDialog (AMEND-01)
- Common form fields
- Common money and quantity displays

**Rules:**

1. Repeated UI/behavior must not be recreated independently on each page.
2. Shared component visual/behavior changes should propagate from one place.
3. Pages compose shared components rather than fork them.
4. Shared components support **controlled** customization through props, variants, slots/children, optional actions, and labels/content.
5. Page-specific customization must not require duplicating the underlying component.
6. Do **not** create an oversized generic component with dozens of unrelated behaviors. Keep primitives and composites focused.
7. Do **not** prematurely put every component in a **cross-application** shared package. Share first **inside** the Mobile React Client. Extract to a cross-app package only when real reuse with Platform Admin React is proven.
8. Platform Admin **business UI** must not be imported into Mobile. Shared concepts and design tokens are allowed (Reuse A). Business chrome remains app-specific (Reuse B).
9. **Shell / top-bar:** one shared top-bar/shell family. Page-specific context is supplied via configuration and slots. Pages must not independently rebuild common chrome.
10. Accessibility, localization, theme, density, loading, disabled, keyboard, and touch behavior live in the shared component where applicable — not re-implemented per page.

Current MAUI analogue: DesignSystem primitives (`SearchBar`, `Button`, `EmptyState`, …) plus POS shell chrome. Future React must not import Razor components; it follows the same **reuse discipline**.

This DOC still does not create TypeScript files.

---

## 5. Device adapter architecture (conceptual — no code)

Features depend on **application-level adapter contracts**, not on `window`, Capacitor plugins, or MAUI APIs directly.

| Contract | Current MAUI evidence | Web/PWA adapter (concept) | Capacitor adapter (concept) |
|---|---|---|---|
| Scanner | `IQrCodeScanService` | Keyboard wedge / USB scanner; camera if permitted | Camera + hardware scanner plugins |
| Camera | `IProductImagePicker` | File input / getUserMedia | Capacitor camera |
| Storage | `ISecureTokenStore`, local files | Web crypto + origin storage; **no tokens in localStorage** if cookie session is used | Secure storage plugin |
| Connectivity | `IConnectivityService` | `navigator.onLine` + API reachability | Network plugin + API reachability |
| Share | `IDocumentHandoffService` (share initiated ≠ print succeeded) | Web Share API / download | Native share sheet |
| File / receipt export | Document handoff / download | Blob download | Share or filesystem |
| Printer | None found | Optional browser print; no assumed ESC/POS | Optional later plugin |
| NFC | None found | Typically unavailable | Optional later; no-op until authorized |
| PaymentTerminal | None found (no live card collection in DOC-02) | No-op | No-op until a real terminal work package |

Unimplemented adapters return a clear “not available on this host” result. Features must degrade (manual barcode entry, share instead of print). Do **not** invent live card terminals or NFC payments in this package.

This DOC does not create TypeScript interfaces.

---

## 6. Code ownership (what lives where)

```text
pages / features     → presentation + orchestration only
api/                 → typed HTTP contracts (Platform + POS)
server state         → TanStack Query
forms                → React Hook Form + Zod
local / offline      → dedicated client persistence/sync layer
device access        → adapter layer
business authority   → .NET Platform API + POS API + PostgreSQL
```

| Concern | Owner | Must not |
|---|---|---|
| Pricing, tax, entitlements, role grants | Server | Recalculate authority in JS |
| Checkout completion | POS API (online) or existing offline cash envelope | Silent success without queue/API |
| Cart preview totals | Client preview only (current `SaleCartService` rule) | Treat preview as ledger |
| AuthN | Platform (cookie session and/or Bearer introspect) | Separate identity per host |
| AuthZ | Server on every protected call | Nav visibility as security |
| Offline outbox | Client persistence aligned with current operation types + server idempotency | Second PostgreSQL or a different conflict model |

Do not port authoritative .NET Domain/Application rules into JavaScript “to run offline.” Offline may **queue** already-defined operation types and **project** server-approved snapshots, as LocalStore does today.

Existing C# `ExItS.PinoyBusinessPOS.ApiClient` and `LocalStore` remain for MAUI (and Org Web for ApiClient). The React host gets TypeScript clients that speak the **same HTTP contracts**. Dual clients during coexistence are expected; contract drift is forbidden.

---

## 7. State management

| Kind | Tool |
|---|---|
| Server/cache | TanStack Query |
| Forms | React Hook Form + Zod |
| Cart, selling mode, sheets | React state / narrow context |
| Theme, density, locale | Persisted preference + context |
| Auth session facts | Query + memory; tokens only in secure/cookie stores |

No default global store. Query cache is not a substitute for server authorization.

---

## 8. API and auth posture (evidence-based)

Flow:

```text
React feature
  → hook / feature service
    → typed TS API client
      → Platform API and/or POS API
```

- Preserve correlation IDs when APIs expose `X-Correlation-Id`
- Normalize problem+json (`title`, `status`, `detail`, `errorCode`, `traceId`)
- Retry only idempotent-safe operations (current C# client retries GET once)
- Abort in-flight search on navigation

**Capacitor / current MAUI-like:** password + Bearer introspect; token in secure storage (MOBILE-D-013).

**Browser Web / PWA:** prefer HttpOnly cookie session used by existing web hosts when the origin model allows it. Do not store access tokens in `localStorage`. CSRF for cookie-authenticated mutations is an **open integration gap** (same class of gap as Platform Admin React planning). Do not invent a CSRF bypass here.

Final cookie-vs-Bearer matrix per delivery mode is an implementation gate, not a new identity system.

---

## 9. Copy Diagnostics (AMEND-01)

When an error is shown, the user/developer should click **one** Copy Diagnostics control and paste the result directly into Cursor or support.

Purpose:

```text
error occurs
→ click Copy Diagnostics
→ paste into Cursor
→ enough safe context to identify the failing area
```

A screenshot is **not** the primary debugging artifact for runtime errors. Screenshots may still help visual defects.

**Current MAUI evidence:** Settings support diagnostics can copy a formatted snapshot (`SupportDiagnosticsView` + `FormatReport`) with a forbidden-marker check. That is a **settings page**, not a global one-click control on every error. The future host must provide a **common format/service** usable from inline errors, page errors, toasts, and fatal boundaries.

Not every validation error needs a global error screen. Presentation stays severity-appropriate (inline / page / toast / fatal boundary). The builder is shared.

### 9.1 Visible UI

Keep the on-screen state compact. Example:

```text
Something went wrong                         [Copy]

Unable to complete this operation.

ERR-XXXX • Correlation <id>

[ Retry ]
```

### 9.2 Copied message (conceptual)

```text
EXITS ERROR DIAGNOSTICS

Application:
ExItS Mobile Client

App Version:
<safe version/build/commit>

Delivery:
Web / PWA / Capacitor Android / Capacitor iOS

Platform:
<safe OS/browser/device platform>

Route/Screen:
<route or screen>

Operation:
<operation>

Error Reference:
<client-generated safe reference>

Error Type:
<normalized safe error type>

HTTP Status:
<when applicable>

Error Code:
<server/application safe error code>

Correlation ID:
<when supplied by API>

Connectivity:
Online / Offline / Server Unreachable

Sync State:
<safe aggregate state>

Local Operation ID:
<when useful and non-secret>

Timestamp:
<ISO timestamp>

Message:
<safe user/technical normalized message>

SECURITY:
Sensitive credentials and protected payloads excluded.
```

Prefer identifiers and correlation references over raw payloads. Preserve API correlation IDs when exposed (`X-Correlation-Id` / problem+json `traceId`).

### 9.3 Redaction (allowlist)

The diagnostic builder **must** use an allowlist / redaction model. Do not rely on developers remembering to redact per screen.

Copy Diagnostics MUST NEVER include:

- password
- PIN
- PIN verifier / hash / salt
- access token
- refresh / session token
- recovery credential
- Authorization / Cookie headers
- encryption keys
- raw card data, CVV
- GCash PIN / OTP
- protected payment secrets
- decrypted financial / customer payloads
- arbitrary request/response bodies containing PII

### 9.4 Intended sources

- React render / error boundary
- Normalized API errors
- Auth / session errors
- Offline / connectivity errors
- Sync / outbox failures
- Local storage failures
- Capacitor / native adapter failures
- Scanner / printer / payment-terminal adapter failures later

---

## 10. Quality and dependency policy

- TypeScript strict; feature isolation; route lazy-loading where useful
- Loading / empty / error / forbidden on data surfaces
- Error boundaries on shell and routes
- No mock data in production runtime; no secrets in frontend bundles
- Lockfile required when implementation begins
- No auto-merge of dependency PRs
- Review security patches promptly; minors periodically; majors on a dedicated branch

---

## 11. Explicit non-goals

- Creating `ExItS.PinoyBusinessPOS.Client`
- Adding Capacitor/PWA to the solution
- Replacing LocalStore or MAUI
- Sharing Admin billing/governance UI
- A premature cross-app component package or importing Admin business chrome into Mobile
- Pinning npm versions
- Introducing Redux
- NFC / printer / payment-terminal products
