# Mobile React — Current State and Replacement Boundaries

Evidence baseline: `origin/main` `5a9be9417b7a2217227ae93e9280102992861615`.
This file describes **current** clients and a **proposed** replacement direction.
It does not authorize implementation.

---

## 1. Canonical terminology

| Term | Meaning in this track |
|---|---|
| **CLIENT HOST** | The application process/UI shell that presents experiences. A host is not a product database. |
| **PRODUCT DOMAIN** | Authoritative business data and rules. Platform owns identity/org/commercial; PinoyBusinessPOS owns retail operations. |
| **Mobile Client** | The current Android MAUI Blazor Hybrid host (`ExItS.PinoyBusinessPOS.Maui`). It is **not** POS-only. |
| **Personal Mobile** | Personal Account experience inside the Mobile Client (`PersonalShell`, routes under `/personal`, Start a Business). |
| **Organization Owner Mobile** | Practical organization governance inside the Mobile Client (`/manage-business`, `/org/*`, `/branch-settings`, workspace selection). |
| **POS Operations** | PinoyBusinessPOS operational experience inside the Mobile Client (`PosShell`: sales, catalog, inventory, shifts, registers, customers, purchasing, reports). |
| **POS Mobile** | Informal historical phrase. Prefer **POS Operations** when meaning checkout/ops, or **Mobile Client** when meaning the host. |
| **Organization Web** | Browser Organization Administration host (`ExItS.PinoyBusinessPOS.Web`, Local Validation `:8093`). Management/reporting. **Not** a POS checkout client. |
| **Personal Web** | Browser Personal product host (`ExItS.Personal.Web`, Local Validation `:8094`). Separate from Mobile Client. |
| **Platform Admin** | Platform operator console (`ExItS.Platform.Admin`). Web only. Must not appear on Mobile Client. |
| **Web/PWA** | Proposed future browser/installable web delivery of the replacement Mobile Client. Not current production. |
| **Capacitor Android** | Proposed future native Android wrap of the React client. Not current production. |
| **Capacitor iOS** | Proposed later native iOS wrap. Not current production; after Android. |
| **CURRENT_IMPLEMENTATION_REQUIREMENT** | A rule that governs the **current** MAUI/Razor client. Still in force for that client. |
| **PROPOSED_REPLACEMENT_CLIENT_ARCHITECTURE** | Planning direction for a future client host. Not authorized. Does not rewrite historical requirements. |

**Forbidden shorthand:** using “mobile” to mean only POS when the current host also contains Personal Mobile and Organization Owner Mobile.

---

## 2. Current client map (evidence)

| Experience | Current primary host | Additional current host | Product domain owner |
|---|---|---|---|
| Platform Administration | Platform Admin (Web) | None | Platform |
| Personal Account | Mobile Client (`PersonalShell`) | Personal Web (`:8094`) | Platform (Personal scope) |
| Organization Owner essentials | Mobile Client (`/manage-business`, `/org/*`) | Organization Web for full control | Platform (org/membership/commercial) + POS for product-local roles |
| Full Organization Administration | Organization Web | Mobile provides the practical subset | Platform + POS management APIs |
| POS Operations (checkout, shifts, selling) | Mobile Client (`PosShell`) | None for checkout | PinoyBusinessPOS |

Approved MVP presentation rule (unchanged; [client-experience-boundaries.md](../architecture/client-experience-boundaries.md)):

```text
Platform Administration = Web only
Personal Account = Mobile
Organization Owner Essentials = Mobile
Full Organization Administration = Web
POS Product Operations = Mobile
```

P25/ADR-022 later added dedicated Organization Web and Personal Web hosts. Those are additional **CLIENT HOSTS**. They do not move POS operational data into Platform, and Organization Web remains **not a POS checkout client**.

---

## 3. Current Mobile Client (MAUI Blazor Hybrid)

**Project:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui`

**Evidence of host type:**

- SDK: `Microsoft.NET.Sdk.Razor` + `UseMaui` + `Microsoft.AspNetCore.Components.WebView.Maui`
- Target: `net10.0-android` (Android-first; csproj comment: iOS/Windows/MacCatalyst not required for P5-WP01)
- Application title: `ExItS POS`; application id: `com.exits.pinoybusinesspos`
- Native host: `MainPage.xaml` `BlazorWebView` with `SafeAreaEdges="Container"`
- Default Blazor layout: `PosShell`; Personal and auth pages override with `@layout`

**Do not infer scope from the project name.** The assembly is named PinoyBusinessPOS.Maui, but the host contains three experience shells:

| Shell | Role | Evidence |
|---|---|---|
| `AuthShell` | Unauthenticated / setup chrome | `@layout Layout.AuthShell` on `/signin`, `/register`, `/welcome`, `/workspace-select`, `/start-business`, device registration, onboarding, offline PIN |
| `PersonalShell` | Personal Mobile | Bottom tabs Home / People / I Lent / I Borrowed / More; routes `/personal`, `/personal/utang/*`, linked merchants, orders, QR, profile |
| `PosShell` | Organization Owner Mobile + POS Operations | Default layout; bottom nav Home / Products / Sales / Customers / More when POS access exists; `/manage-business` governance hub |

Install notes (Local Validation / Debug only, not production): [Maui-Emulator-Install.md](../../Maui-Emulator-Install.md), [Maui-PhysicalDevice-Install.md](../../Maui-PhysicalDevice-Install.md).

### 3.1 Personal experience (present)

Routes include `/personal`, `/personal/utang/people`, `/personal/utang/lent`, `/personal/utang/borrowed`, `/personal/more`, `/personal/profile`, `/personal/settings`, `/personal/my-qr`, `/personal/explore-pos`, `/start-business`, linked-merchant shop/statement, personal orders, invitations.

Personal data is **not** owned by the POS product database. Platform owns Personal identity and Personal Utang. The Mobile Client presents it. Local personal projections exist in LocalStore (`LocalPersonalUtangStore`).

### 3.2 Organization Owner Mobile (present)

Routes include `/manage-business`, `/org`, `/org/profile`, `/org/staff`, `/org/subscription`, `/org/devices`, `/organization/branches`, `/branch-settings`, `/workspace-select`, `/organization-select`, sales-document education, tax/privacy pages.

Governance on Mobile is the practical subset. Full administration remains Organization Web. Workspace Primary/Main is the Mobile governance gateway (P28-WP15B).

### 3.3 POS Operations (present)

Routes include `/sales/new` (checkout), `/sales`, `/catalog`, `/inventory`, `/customers`, `/shifts`, `/registers`, `/purchasing`, `/suppliers`, `/orders`, `/reports`, `/dashboard`, `/devices/register`.

POS operational data lives in `ExItS_PinoyBusinessPOS` schema `pos`. Organization membership alone does not grant POS access; entitlement + product-local role are required.

---

## 4. Current web clients (not the Mobile Client)

### 4.1 Organization Web (`ExItS.PinoyBusinessPOS.Web`)

- Blazor Web App with **Ant Design Blazor**
- Local Validation port `:8093`
- Management/reporting: overview, staff, branches, catalog, inventory reports, sales history, suppliers, settings
- Explicit page copy: not a POS checkout client (`Boundary_NotPos`)
- Owner/Manager use Organization Web; Cashier is denied this host and uses MAUI only

This is **not** the deferred historical “POS Web client” from client-experience-boundaries §15 (checkout on web). Do not collapse those two phrases.

The heading “Organization Web (PWA)” in [global-search-filter-pattern.md](../engineering/global-search-filter-pattern.md) is a **search-pattern note** for the current Blazor host. It is not a Capacitor/PWA product and is not this planning track.

### 4.2 Personal Web (`ExItS.Personal.Web`)

- Blazor Web App with Ant Design Blazor
- Local Validation port `:8094`
- Browser Personal product UI over existing Personal APIs
- No checkout

MVP client-experience-boundaries still list Personal Account as Mobile-primary. Personal Web is an additional host from P25. This track must not treat Personal Web as the current Mobile Client.

### 4.3 Platform Admin (`ExItS.Platform.Admin`)

- Ant Design Blazor operator console
- Must not appear on Mobile Client
- Separate React planning track: [Platform Admin Web](../Platform-Admin-Web/README.md)

---

## 5. Current .NET backend (retained)

| Layer | Project | Role |
|---|---|---|
| POS Domain | `ExItS.PinoyBusinessPOS.Domain` | Persistence-independent POS model |
| POS Application | `ExItS.PinoyBusinessPOS.Application` | Use cases, auth orchestration, offline abstractions |
| POS Infrastructure | `ExItS.PinoyBusinessPOS.Infrastructure` | EF Core + Npgsql, schema `pos` |
| POS API | `ExItS.PinoyBusinessPOS.Api` | HTTP API (`:8092` Local Validation) |
| POS ApiClient | `ExItS.PinoyBusinessPOS.ApiClient` | Typed HTTP client used by MAUI and Organization Web |
| Platform | `ExItS.Platform.*` | Identity, orgs, memberships, catalog, plans, subscriptions, entitlements (`:8091`) |

Rules that remain in force:

- No cross-product database access or cross-database foreign keys
- Domain remains persistence-independent; Application must not reference Infrastructure
- UI projects must not reference Infrastructure, EF Core, or Npgsql
- PostgreSQL remains the server database
- A future React client would call existing APIs; it would not become a second system of record

---

## 6. Current LocalStore / offline

**Project:** `ExItS.PinoyBusinessPOS.LocalStore`

Evidence (`AddPinoyBusinessPosLocalStore`):

- Per-context SQLite via `Microsoft.Data.Sqlite`
- Generic encrypted `offline_operations` outbox (AES-GCM; key in SecureStorage, not SQLCipher)
- Local selling catalog + cash sale store
- Encrypted customer/credit store
- Local Personal Utang store
- Selective connected-supplier local cache (not a full supplier catalog)
- Queue processor with access revalidation (`BlockedByAccess`)

MAUI registers LocalStore, `MauiSecureTokenStore`, connectivity, and offline dispatchers (sales, catalog, customers/credit, personal Utang, reconnect auto-sync).

Offline is **product-owned**. Platform does not own the POS offline database. Replacement of this mechanism is not authorized in this package.

---

## 7. Current authentication model

Shared identity: one Platform User. Web and Mobile must not create a separate identity for the same person ([client-experience-boundaries.md](../architecture/client-experience-boundaries.md) §8).

Locked access chain ([authentication-architecture.md](../engineering/authentication-architecture.md)):

```text
Platform User
  → Organization Membership
  → Product Access / Entitlement
  → Product-Local Role and Grants
```

MAUI evidence:

- Password sign-in through `AuthenticationService` against Platform
- Bearer access token stored in `MauiSecureTokenStore` (MAUI `SecureStorage`; never passwords)
- Token introspect / bind / revoke on Platform
- Dev/Testing GUID/header fallback is Dev/Testing-only; Production fail-closed
- Offline PIN enrollment/unlock pages exist on AuthShell
- Organization context and selected branch/workspace are session facts, not client-invented authority
- Post-sign-in routing uses `WorkspaceSelectionService.ResolveRoutingPlanAsync`: PersonalHome / AutoSelect (1 org + 1 Active branch) / ShowChooser / NoAccessibleBranch — see P28-WP14. Future React planning for this matrix is AMEND-03; this track does not change MAUI.
- `ProductAccessResolver` currently evaluates `PosProductCodes.PinoyBusinessPos` (single product). That hard-coding is current evidence, not a generic multi-product Mobile contract.
- APIs remain the authorization enforcement point; hidden nav is convenience only

Browser hosts use Platform session cookies (ADR-022 unified sign-in). A future React Mobile Client must consume the same Platform identity contracts; cookie vs Bearer for Capacitor is a later DOC decision (MOBILE-D-013 / MOBILE-D-010).

---

## 8. Current design system and UI stacks

| Host | UI stack | Rule |
|---|---|---|
| Mobile Client (MAUI) | Native CSS + Razor + `ExItS.DesignSystem` (`--exits-*` tokens) | No Ant Design. No Tailwind. |
| Organization Web | Ant Design Blazor + shared DesignSystem tokens where loaded | ADR-022 |
| Personal Web | Ant Design Blazor | ADR-022 |
| Platform Admin | Ant Design Blazor | ADR-015 |

[ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md) keeps Platform Admin and POS MAUI on separate UI implementations. Shared consistency is semantic tokens, terminology, and contracts — not one component library.

Current MAUI design spec [production-mobile-design-system.md](../specs/mobile/production-mobile-design-system.md) forbids adding React packages, Bootstrap, Tailwind, or a second design system **on the current MAUI host**. That remains a CURRENT_IMPLEMENTATION_REQUIREMENT for MAUI. It is not a ban on documenting a future replacement host.

---

## 9. CURRENT_IMPLEMENTATION_REQUIREMENT vs PROPOSED_REPLACEMENT_CLIENT_ARCHITECTURE

### CURRENT_IMPLEMENTATION_REQUIREMENT (do not rewrite)

From [pinoy-business-pos-requirements.md](../product/pinoy-business-pos-requirements.md) Experience requirements:

> Native CSS / Razor components (no Ant Design, no Tailwind)

This describes the **current MAUI/Razor client architecture**. Related current statements:

- Final portfolio boundaries: “POS UI: Own; native CSS / DesignSystem”
- UI design system: PinoyBusinessPOS native foundation; no Ant; no Tailwind
- ADR-010: PinoyBusinessPOS MAUI retains native Razor + native CSS / DesignSystem
- Production mobile design system: do not add React packages or Tailwind to the current MAUI host

This package **does not** delete or edit that requirement in the product requirements file.

### PROPOSED_REPLACEMENT_CLIENT_ARCHITECTURE (planning only)

Future planning direction for a **new** client host:

- React + TypeScript
- Web/PWA delivery where appropriate
- Capacitor
- Android first
- iOS later
- .NET backend retained
- PostgreSQL retained
- Current MAUI remains active until explicit cutover

This is **not** a silent reinterpretation of the Razor/CSS requirement. Until cutover is authorized, MAUI continues to follow CURRENT_IMPLEMENTATION_REQUIREMENT.

---

## 10. Replacement boundaries

```text
CURRENT Mobile Client (MAUI Blazor Hybrid)
  keep operational
  keep as the production-path client until explicit cutover
  do not retire in this documentation series

PROPOSED Mobile Client (React + TypeScript, Web/PWA, Capacitor)
  documentation only
  implementation NOT AUTHORIZED
  PWA production NOT AUTHORIZED
  Capacitor production NOT AUTHORIZED
  coexist with MAUI until a later authorized cutover
```

Must not change in this track:

- Platform / POS database ownership
- Entitlement vs product-role split
- Organization Web remaining non-checkout
- Platform Admin remaining Web-only
- LocalStore behavior (unless a later authorized package)

Capability-boundary note: [platform-product-capability-boundary.md](../engineering/platform-product-capability-boundary.md) §10 says MAUI does not **own** Personal registration, organization creation, or SaaS payment. That is **PRODUCT DOMAIN** ownership (Platform). The Mobile Client **hosts** those screens. Both statements can be true; do not collapse them.

---

## 11. Authorization status (repeat)

| Item | Status |
|---|---|
| React mobile implementation | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| PWA production rollout | **NOT AUTHORIZED** |
| Capacitor production rollout | **NOT AUTHORIZED** |
| Documentation of the planning direction | This package |
