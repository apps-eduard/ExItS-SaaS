# Mobile React — Product Surfaces and UX

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-02 (AMEND-03 workspace/product context)
**Depends on:** [current-state-and-replacement-boundaries.md](current-state-and-replacement-boundaries.md), [decisions.md](decisions.md)

This file defines the **proposed future** Mobile Client experience by device class, role, and selling workflow.
It does not change MAUI, Organization Web, Personal Web, or APIs.

Server authorization remains the enforcement point. Hidden navigation is convenience only.

---

## 1. Product identity (future client)

The future React / PWA / Capacitor client is the **ExItS Mobile Client**: one host for Personal Mobile, Organization Owner Mobile, and POS Operations.

| Shared ExItS identity | Identical Platform Admin presentation |
|---|---|
| **YES** — brand tokens, terminology, EN/fil-PH, Light/Dark/System, accessibility | **NO** — do not copy Admin sider/table chrome onto selling or Personal surfaces |

Platform Admin remains a dense control-plane console ([Platform Admin Web](../Platform-Admin-Web/README.md)). This client is mobile-first operational software. Desktop/PWA must not become a second Platform Admin or a dense organization-admin console merely because the viewport is wide.

Organization Web remains the full-administration browser host. This client may remind Owners to open Web for advanced tables, audit, and bulk work.

---

## 2. Device classes

### 2.1 Phone

Primary for:

- Personal / account journeys (register, sign-in, Utang, profile, Start a Business, invitations)
- Quick Organization Owner / Manager tasks (workspace switch **when alternatives exist**, staff invite, branch settings, subscription status, Start Selling)
- POS Operations where practical (own shift, scan, cart sheet, cash/manual GCash/Utang checkout, receipts)

Chrome:

- Bottom navigation for the active shell (Personal vs POS), not more than five primary destinations
- Sheets / drawers for cart, filters, account menu, and confirmations
- Cards and lists instead of wide tables
- Full-screen forms
- Large touch targets (≥ 44–48 CSS px)
- Sticky primary action where the task has one commit (Pay, Save, Send)

Phone portrait is the default. Landscape is usable but must not be required for Personal or cashier completion.

### 2.2 Tablet

Primary for **POS cashier selling**.

Required sell-floor regions (current MAUI landscape reference, ≥ ~900px landscape):

| Region | Role |
|---|---|
| Categories | Filter browse only; never clears the cart |
| Product grid / search | Barcode-first; search is secondary |
| Persistent cart | Lines, qty, totals, remove; always visible in landscape |
| Checkout / payment | Cash, manual GCash, customer-credit per business rules |

Also:

- Persistent workspace / branch / register / shift context in the header
- Split-layout opportunities for catalog vs cart, order vs detail, customer vs ledger
- Touch-optimized tables only when a list is truly tabular (sales history, inventory counts)
- Portrait tablet: sticky cart summary + cart sheet (same as phone), not a shrunk three-column floor

Tablet is **not** the primary Personal Utang device, but Personal must remain usable.

### 2.3 Desktop / large browser (Web/PWA)

Allowed:

- Side navigation for Personal and Owner/ops hubs where it improves scanability
- Tables for history, inventory, reports
- Keyboard support (search focus, barcode wedge, quantity, pay)
- Multi-column layouts for selling and management-lite

Must not:

- Automatically turn POS into a dense admin console
- Duplicate Platform Admin IA (global org catalog, SaaS billing, platform users, entitlements overrides)
- Force Cashiers through Owner governance pages
- Assume mouse-only; touch and keyboard both work

Desktop/PWA selling should still feel like a sell floor: product + cart + pay. Administration-heavy work stays on Organization Web.

---

## 3. UX principles

| Principle | Meaning for this client |
|---|---|
| Mobile-first | Phone Personal and Owner journeys are first-class. Do not ship desktop-only Personal onboarding. |
| Tablet-first for cashier selling | The reference selling layout is tablet landscape split floor. Phone selling is a capable reduction, not the design origin. |
| Modern / premium | Quiet surfaces, tabular money, restrained brand green. No giant decorative heroes, no novelty chrome. |
| Fast perceived response | Cart ± is in-memory. Search is debounced. Skeleton after first paint, not a full-page spinner on every browse. |
| Minimal steps | Default path: scan → cart → pay → receipt. Do not insert extra screens without a business rule. |
| Minimal typing | Barcode, steppers, pickers, saved customers. Typed search is fallback. |
| Barcode-first | Scanner / camera / wedge updates the cart immediately. Category chips never clear the cart. |
| Persistent cart | Session-persistent during the sale (orientation, category, search). Cleared on successful sale, **Sign Out**, or organization/workspace switch. **Lock / auto-lock must not discard the cart** unless a later approved security rule requires it. Not a second financial database. |
| Clear offline / sync status | Header shows Online / Offline / Pending / Syncing / Failed. Financial actions state whether they are local-queued or blocked. |
| Skeleton loading | Lists and sell floor use skeletons. Avoid blocking the whole shell after first content. |
| Immediate success / error | Toasts or inline alerts on pay, save, sync failure. Destructive actions confirm. |
| Responsive | One product; layouts change by device class. No desktop page merely shrunk to phone. |
| Touch-friendly | Primary controls meet touch minima even in Compact density. |
| Accessibility | WCAG 2.2 AA **design target** (not a current-app compliance claim). |
| EN default | English (`en`) is the **default** UI language. Do not infer Filipino from device locale on first launch. |
| fil-PH | Filipino / Tagalog is the required secondary locale (`fil-PH`). Do not add `tl-PH` unless a later decision requires it. Layouts must tolerate longer strings. |
| Light / Dark / System | All three supported. **System is the default.** Immediate switch; persist preference; no app restart; no lost cart/form. |
| Shared UI by default | Repeated chrome and patterns use one shared primitive/composite (top bar, search, empty/error/loading, sync chips, confirm, Internet-required, Copy Diagnostics). Pages compose; they do not fork. |

Motion is functional only (press, sheet, toast). Honor `prefers-reduced-motion` with essentially 0 ms decorative motion.

Density: Compact remains the cashier default (current DesignSystem). Comfortable is for Personal, forms, and optional user preference. Do not import Platform Admin “Balanced” as a POS default.

---

## 4. Current MAUI navigation (evidence)

These are **current** shells. Future React may restyle them; it should not drop the experience split.

| Shell | Current primary chrome | Destinations (summary) |
|---|---|---|
| Auth | No bottom nav | Sign-in, register, welcome, workspace select, Start a Business, device register, onboarding, offline PIN / enrolled-user chooser |
| Personal | Bottom: Home, People, I Lent, I Borrowed, More | `/personal`, Utang, profile, QR, explore POS, linked merchants, orders |
| POS / Owner | Bottom: Home, Products, Sales, Customers, More (Products/Sales/Customers hidden without POS access) | Catalog, sales, customers, More hub (orders, inventory, purchasing, reports, org, branch settings) |

Owner governance: burger **Manage business** on Primary/Main workspace (`/manage-business`). Non-primary workspace uses `/branch-settings`. **Start Selling** is an interface mode; it does not change POS role.

Cashier sell floor today: `/sales/new` (`SaleCheckout.razor`). Cart is in-memory (`SaleCartService`), never SQLite.

---

## 5. Role / experience matrix

Personas below are **experiences**. They are not new authorization roles. Access still requires the locked chain:

```text
Active user
+ active organization membership (when org-scoped)
+ active POS entitlement (when POS-scoped)
+ active POS product-local role (when POS-scoped)
= POS access
```

UI must not grant permission. Organization Owner without a POS role must not see checkout as if entitled. Cashiers must not receive Organization Administration because they can sell.

Platform Administration is **excluded** from this client on every device.

| Actor | Current (MAUI + related hosts) | Proposed future (this client) | Must not present |
|---|---|---|---|
| **Personal User** | PersonalShell; Personal Web additional | Phone-first Personal: home, People, I Lent, I Borrowed, QR, invitations, Start a Business, explore POS, linked-merchant shop | POS checkout, org staff admin, Platform Admin |
| **Organization Owner** (org role, no POS role) | Org summary, Manage business (Primary), branch settings, Web reminder; no checkout | Same: practical Owner essentials on phone/tablet; Web for full admin | POS Operations, Platform Admin |
| **POS Owner** | Owner essentials + full POS ops + Start Selling | Phone: Owner + ops hubs. Tablet/desktop: sell floor when in Start Selling; management-lite lists otherwise | Platform Admin; treating ownership as automatic checkout without POS role (already forbidden) |
| **POS Manager** | Daily ops + Start Selling; not org ownership | Phone/tablet ops; tablet sell floor; limited setup; Web for heavy admin | Org ownership transfer, Platform Admin, Cashier-only restrictions bypass |
| **POS Cashier** | MAUI only for checkout; own shift, assigned register, permitted sales | Tablet-first sell floor; phone selling capable; no Manage business; no org staff/role matrix | Organization Web, Owner governance, void/return of completed sales unless server already allows that role |

Staff membership without a POS role: no POS Operations (current rule preserved).

Future desktop/PWA does **not** move Cashiers onto Organization Web. Organization Web stays Owner/Manager management, not checkout.

---

## 6. POS selling UX (target)

### 6.1 Workflow

```text
Resolve workspace (auto-enter when exactly one authorized choice; chooser only when more than one)
→ resolve launchable Mobile product/experience (skip when exactly one)
→ register + open own shift when required
→ sell floor
→ scan or search product
→ cart updates immediately
→ quantity / change unit / remove
→ customer optional or required by business rule
→ payment
→ confirmation
→ receipt / share
→ local persistence / sync status visible
```

Do **not** force a workspace chooser as the first tap when the user has only one authorized Organization + Branch. See §11 (AMEND-03).

Preconditions the UI may **remind** but must not override:

- Workspace/branch selected (server-authoritative; selection does not itself grant POS)
- Device registered when product policy requires it
- Open shift when shift policy requires it
- Entitlement + POS role for `CreateSale`

Leaving the sell floor with a non-empty cart should confirm. Category change, search, and orientation must **not** clear the cart.

### 6.2 Barcode and search

- Barcode is the fast path (hardware wedge, camera scan where the host allows it).
- Successful scan adds or increments the matching line immediately.
- Unknown barcode: clear error, offer search; do not silently create a product from the sell floor.
- Search is debounced; stale responses discarded.
- Multi-unit selling (current behavior): cashier may sell a configured sell-unit; inventory conversion remains server-authoritative.

### 6.3 Cart

- Session-persistent on the sell floor
- Immediate line totals with tabular numerals
- Quantity stepper ≥ touch minimum
- Remove spatially separated from ±
- Preview amounts only; **server prices the completed sale** when online
- Offline cash snapshots follow existing LocalStore rules; the cart itself is not a ledger

### 6.4 Customer

| Payment | Customer |
|---|---|
| Cash | Optional unless a later approved store rule requires it |
| Manual GCash | Optional unless a later approved store rule requires it; GCash **reference is required** |
| Customer credit / Utang | **Required**; blocked when commercial capability `customer-credit-create` (or successor) forbids new credit |

Do not invent unnamed “customer required for every sale” rules.

### 6.5 Payment boundaries (do not expand)

Planning UX for this package is the current retail set:

| Method | Meaning | UX notes |
|---|---|---|
| **Cash** | Tendered / change | Offline cash checkout only when existing offline policy allows |
| **Manual GCash** | Operator-confirmed transfer; reference required; **not** a gateway verification | Do not collect GCash PIN/OTP/account secrets |
| **Customer credit (Utang)** | Product-based credit when entitled | Online-required in current selling UI; do not invent offline Utang checkout here |

Also preserved:

- Exactly one payment method per simple sale (no split tender in this UX)
- Platform SaaS GCash ≠ POS retail GCash
- No live card/CVV collection in this planning package

Current MAUI also has **simulated** Card and electronic GCash checkout (P19 `FakePaymentGateway`; no live provider). This DOC does **not** make those a required future visual design, does not claim production gateway support, and does not add new wallets, cards, or refund-to-original-channel.

### 6.6 Confirmation, receipt, sync

- Success state with amount, method, and optional customer
- Receipt view + share/handoff using existing document-handoff capability (no new print stack invented)
- Header/sync: if the sale is queued, show Pending Sync; never imply server completion for unsynced local cash
- Idempotent sync; conflicts must not silently change financial records (existing offline design)

---

## 7. Non-selling surfaces (future, by device)

| Surface | Phone | Tablet | Desktop / PWA |
|---|---|---|---|
| Personal Utang | Primary | Usable | Side nav + lists OK; not an admin console |
| Start a Business | Full-screen wizard | Same | Same; do not force Org Web |
| Manage business | Hub + child pages | Hub; tables where helpful | Reminder + Open Organization Web for full control |
| Catalog / inventory / purchasing | Lists, sheets | Split list/detail | Tables + filters; still product ops, not Platform catalog admin |
| Reports | Cards / key metrics | Charts + tables | Tables; export remains Web if already Web-only |
| Settings | Full-screen | Same | Same (theme, language, density, logout) |

---

## 8. Visual quality target

Design target: **WCAG 2.2 AA** (planning quality bar, not a claim that current MAUI is certified).

Must:

- Visible `:focus-visible` on every interactive control
- Semantic labels, names, and error association (not placeholder-only labels)
- Status not by color alone (text, icon, `aria-*`)
- Skip link to main content (current MAUI pattern)
- Safe areas / notches / system bars applied **once** at the host (current MAUI `SafeAreaEdges=Container`); no double top spacer
- Filipino string expansion: wrapping, no fixed-height clipping, no truncated primary CTAs
- Zoom 100–200% without losing Pay / Scan / Save
- Contrast in Light and Dark

Must not:

- Cramped controls under the 44–48 px touch floor
- Desktop page merely scaled down to phone
- Giant decorative mobile hero or marketing splash as daily chrome
- Unnecessary animation; looping motion on the sell floor
- Emoji as production icons

### 8.1 Orientation

| Device | Primary | Also |
|---|---|---|
| Phone | Portrait | Landscape usable; cart sheet OK |
| Tablet | Landscape sell floor | Portrait uses phone-like cart sheet |
| Desktop / browser | Landscape window | Responsive down to tablet/phone breakpoints without requiring a separate “mobile site” |

Sell floor must keep cart contents across orientation change.

### 8.2 Shared chrome and composites

Use **one** AppTopBar / shell family (MOBILE-D-062, MOBILE-D-070). Page-specific titles, actions, and context arrive through configuration and slots from **centralized application state**. The top bar displays current organization, branch, and product/experience context; it does **not** independently query workspace authorization, invent branch context, or implement switching. List pages compose shared SearchBar, FilterBar, PageHeader, EmptyState, ErrorState, LoadingState, StatusChip / SyncStatusChip, and ConnectivityIndicator rather than restyling native controls independently.

Destructive or blocking confirmations use ConfirmDialog. Ordinary vs sensitive Internet-required UX uses the shared toast/dialog pair (AMEND-01). Runtime errors use CopyDiagnosticsButton on the shared ErrorState where applicable.

Customize via props, variants, slots, optional actions, and labels. Do not duplicate a composite to tweak one page. Do not grow a single “Swiss army” component that owns unrelated flows. Share first inside this client; do not import Platform Admin business chrome.

Accessibility, `en` / `fil-PH`, Light/Dark/System, Compact/Comfortable, loading, disabled, keyboard, and touch minima belong on the shared control when they apply.

---

## 9. Localization and theme

Canonical client-wide rules (Web / PWA / Capacitor Android / Capacitor iOS later). **MOBILE-D-064.**

### 9.1 Language

| Role | Locale key | User-facing name |
|---|---|---|
| **Default** | `en` | English |
| **Required secondary** | `fil-PH` | Filipino / Tagalog |

Do **not** introduce a separate `tl-PH` locale unless a later explicit decision requires it.

All reusable/shared UI consumes the **global** localization system. No page implements independent language state. No hard-coded reusable UI strings.

### 9.2 Theme

| Option | Meaning |
|---|---|
| **System** (**default**) | Follow the host/device/browser OS preference. If the OS theme changes while the app is running, the UI updates where the delivery host supports it. |
| Light | Explicit light override |
| Dark | Explicit dark override |

Once the user explicitly selects Light or Dark, that preference **overrides System** until they change it again.

**System is a real stored preference value.** Do not snapshot the OS Light/Dark result and persist it as an explicit Light or Dark choice.

**Current MAUI evidence:** `ThemePreference.System` is already the store default (`MauiThemePreferenceStore` / `ThemeController`). This package locks the same default for the future React host; it does not change MAUI.

### 9.3 First launch

New installation / no saved preference:

- Language: **English** (`en`)
- Theme: **System**

Do not infer Filipino automatically from device locale. Do not infer Light/Dark and permanently store it as an explicit user choice.

### 9.4 Persistence and apply

Language and theme are **non-sensitive client UI preferences**, persisted locally, shared across the entire Mobile React Client.

Changing them must:

- apply immediately
- require **no** restart
- **not** sign the user out
- **not** clear the cart
- **not** clear forms
- **not** clear offline pending work
- **not** change authorization
- **not** modify financial state
- **not** reset current workspace or product without an authorization reason

Theme/language change must not cause app reload, route reset, or an unexpected modal close. Avoid a flash of the wrong theme on startup where practical.

PHP / Philippine locale formatting for money and dates remains required regardless of UI language.

### 9.5 Shared components

Shared controls (MOBILE-D-061–D-063) automatically follow the current locale and theme: AppTopBar, SearchBar, FilterBar, PageHeader, buttons, dialogs, toasts, ErrorState, CopyDiagnosticsButton, StatusChip, SyncStatusChip, ConnectivityIndicator, forms, money/quantity, navigation.

Pages must **not** implement a separate theme or localization system.

fil-PH strings may be longer than English. Shared components must wrap, use flexible widths, avoid fixed-height clipping, keep primary actions visible, and avoid translation-caused horizontal overflow on phone/tablet/desktop.

Theme visual checks: Light, Dark, System-resolved-Light, System-resolved-Dark. WCAG 2.2 AA remains a design bar, not a certification claim.

---

## 10. Authorization and presentation rules

- Navigation visibility ≠ permission
- Same user, org, and operation must get the same API allow/deny as today
- Do not merge Platform Admin responsibilities into Mobile
- Do not present Organization Web checkout (it is not a checkout client)
- Future PWA on a large screen is still this Mobile Client’s layouts, not Admin

---

## 10.1 Account, lock, and connectivity UX (AMEND-01)

Account menu / settings must distinguish **Lock**, **Sign Out**, and **Remove From This Device** (see [offline-sync-auth-and-security.md](offline-sync-auth-and-security.md) §5.3). Shared devices show an enrolled-user chooser with safe labels only.

Internet-required UX is centralized:

- Ordinary blocked actions: short-lived shared toast/banner that names the capability
- Workspace/account switch: persistent dialog; stay in current context
- Optional restrained “Back online” when API reachability returns

Error surfaces that are more than field validation should offer compact **Copy Diagnostics** (see [frontend-architecture-and-reuse.md](frontend-architecture-and-reuse.md)). Visual example:

```text
Something went wrong                         [Copy]

Unable to complete this operation.

ERR-XXXX • Correlation <id>

[ Retry ]
```

Screenshots remain useful for visual defects. Copy Diagnostics is the primary runtime-error handoff into Cursor/support.

---

## 11. Smart workspace and product launch context (AMEND-03)

Planning baseline for the future React Mobile Client. **Does not authorize implementation**, MAUI changes, backend APIs, or product-launch endpoints.

Current MAUI evidence (read-only): `WorkspaceSelectionService.ResolveRoutingPlanAsync`, `ProductAccessResolver` (evaluates `PosProductCodes.PinoyBusinessPos`), `WorkspaceSelect.razor`, `SignIn.razor` post-auth routing, and `WorkspaceSelectionServiceTests`. See [P28-WP14](../reports/P28-WP14-unified-organization-branch-workspace-selection.md). That POS hard-coding is **implementation evidence**, not the future generic React contract (MOBILE-D-069).

### 11.1 Canonical workspace model

**Workspace** = Organization + selected Branch.

Hierarchy:

```text
User
→ Organization
→ Branch
→ operational POS / device / register / shift context
```

Do **not** redefine Workspace as Organization + Branch + Product. Product is a separate launchable ExItS SaaS experience. Branch switching can affect cart, inventory, device, and shift context; changing product is a different navigation concern.

Workspace selection does **not** itself grant POS authorization. Server authorization remains authoritative. Selecting a management branch must not silently:

- grant `CreateSale`
- rebind a POS device
- move inventory
- move an open shift
- change product-local role

### 11.2 Smart routing principle (MOBILE-D-065)

Do **not** show a chooser when there is nothing meaningful to choose.

Resolve available authorized context first.

| Valid choices | Outcome |
|---|---|
| Exactly one | Auto-enter / skip that chooser |
| More than one | Show the corresponding chooser |
| Zero | Explicit setup or access state — do not invent context |

Do not add unnecessary taps. Do not show empty intermediate screens.

### 11.3 Workspace routing matrix

**CASE A — 1 accessible organization + 1 accessible Active branch**

Auto-select workspace → skip workspace chooser → continue to destination.

This includes the common case: the organization has only its Primary/Main branch. Primary/Main is a **real** branch. Do **not** treat “no additional branches” as “zero branches.”

**Current MAUI evidence:** `WorkspaceRoutingOutcome.AutoSelect`; `SignIn.razor` calls `SelectWorkspaceAsync` then continues; `ResolveRoutingPlan_single_org_single_branch_auto_selects`.

**CASE B — 1 accessible organization + 2 or more accessible Active branches**

Show the workspace chooser. The organization may already be expanded. The user taps a branch.

**CASE C — 2 or more accessible organizations**

Show the unified workspace chooser. Organize choices by organization. Each organization exposes **only** branches the user may access.

**CASE D — organization membership exists + zero accessible Active branches**

Do **not** invent a branch. Show an appropriate state such as “No available location,” or, when the exact Owner/setup capability is known, “Set up your first location.” Do not promise a create-branch action unless that capability/API exists.

**Current MAUI evidence:** `WorkspaceRoutingOutcome.NoAccessibleBranch`; `ListWorkspaces_skips_orgs_without_active_branches`.

**CASE E — no eligible organization**

Personal Home / Personal experience according to [client-experience-boundaries.md](../architecture/client-experience-boundaries.md). Do not force organization/workspace selection on a Personal-only user.

**Current MAUI evidence:** `WorkspaceRoutingOutcome.PersonalHome`.

### 11.4 Primary / Main branch (MOBILE-D-066)

An organization with Primary/Main only has **one** branch. Therefore:

```text
1 organization + Primary/Main only = one valid workspace → AUTO-ENTER
```

Do not force the Owner through a workspace chooser every login.

A genuinely branchless / no-accessible-Active-branch organization must not receive an invented branch.

### 11.5 Multiple choices — last used (MOBILE-D-067)

When multiple valid workspaces exist, **show the chooser**.

The previous successful workspace **may** be highlighted as Current, Last used, or equivalent restrained wording. Example:

```text
Choose workspace

ABC Grocery

✓ Main
  Last used

  Mall Branch

  Airport Branch
```

Do **not** silently auto-enter a previous branch merely because it was last used when multiple currently valid choices exist. The user confirms the branch with one tap.

Reason: branch affects inventory, orders, branch configuration, devices, registers, shifts, and transaction origin. Do not accidentally place an Owner/Manager into the wrong store.

**Current MAUI evidence:** `WorkspaceSelect.razor` marks the session workspace with a check and “current” styling (`IsCurrentWorkspace`). It does not auto-bind last-used when the routing plan is `ShowChooser`. Future React may add explicit “Last used” copy; it must not add silent last-used auto-entry.

When the chooser is opened from inside the app:

- current organization should be obvious
- current branch should be obvious
- current organization may be expanded initially
- selected branch should be marked Current
- other authorized branches remain selectable

Do not make the user remember which branch they are currently operating.

### 11.6 Switch workspace

Preserve one canonical workspace-switching flow.

Preferred entry: Account / menu / burger — **Switch workspace**.

The shared AppTopBar displays Organization and Branch but must not independently implement authorization or switching logic.

Avoid multiple competing branch switchers across pages. Do not rebuild a switcher per feature.

**Adaptive actions (MOBILE-D-070):**

- 1 authorized workspace only → omit “Switch workspace” from normal navigation
- 2+ authorized workspaces → show “Switch workspace”
- If availability cannot be known safely: fail conservatively; do not show fake alternatives

### 11.7 Switch with active cart

If workspace changes while the sale cart is non-empty:

```text
Switch workspace
→ detect non-empty cart
→ explicit confirmation
```

Example:

```text
Discard current cart and switch workspace?

[Continue sale]
[Discard and switch]
```

Do not silently discard the cart. Do not move an existing cart into a different organization/branch. User/context switching must not rewrite transaction ownership.

**Current MAUI evidence:** `WorkspaceSelect.razor` `ConfirmDialog` (`WorkspaceSelect_DiscardCartTitle` / Continue sale / Discard and switch).

### 11.8 Online requirement for workspace switch

Workspace switching requires server-authoritative validation.

```text
Switch workspace while offline
→ DO NOT switch
→ stay in current workspace
→ shared Internet-required persistent explanation
```

Example concept:

```text
Internet required

You're currently working offline in:

ABC Grocery
Main Branch

Connect to the internet before switching workspaces.

Your pending offline transactions remain safe.

[Got it]
```

Do not clear: current offline grant, pending sync work, cart automatically, enrolled PIN, or user enrollment.

See [offline-sync-auth-and-security.md](offline-sync-auth-and-security.md) (AMEND-01 sensitive OnlineRequired dialog).

### 11.9 Cashier / device-bound optimization

Do not force every user through the same Owner-style chooser.

If effective authorization/device constraints result in only **one** valid operational workspace for the current user/device → auto-enter it.

Example: Cashier + device bound to Main + only authorized operational context is Main → skip chooser.

Do **not** present branches the cashier/device cannot actually enter.

Owner/Manager management workspace access remains separate from POS device authorization.

Wrong device branch for Enter POS: blocked explanation; **do not** silently rebind the device.

Open shift that blocks an operational switch: blocked explanation according to existing rules. Do not collapse these into generic “Access denied.”

### 11.10 Product vs catalog products (MOBILE-D-069)

This section refers **only** to ExItS SaaS products/experiences this Mobile Client is authorized to launch.

Do **not** confuse this with store catalog products (SKU/inventory).

Current POS implementation is hard-coded around Pinoy Business POS (`ProductAccessResolver` → `PosProductCodes.PinoyBusinessPos`). That is current evidence. Do **not** make that hard-coding the future generic React architecture.

Do not invent Product B/C as shipped products. Use hypothetical labels only in architectural examples. Do not add PLM or any other parked product to the Mobile Client unless separately authorized.

### 11.11 Smart product resolution

For future Mobile-capable entitled ExItS products/experiences:

| Launchable Mobile product experiences | Outcome |
|---|---|
| Zero | Remain in Personal/Organization experience or show an appropriate access state. Do not offer the product. |
| One | Auto-enter / auto-launch. No product chooser. |
| More than one | Show a product chooser. |

Example (hypothetical labels only):

```text
Choose what to open

[ Pinoy Business POS ]
[ Future Product B ]
[ Future Product C ]
```

**Adaptive:** 1 launchable product → omit “Switch product.” 2+ → show “Switch product.”

### 11.12 Workspace then product

Recommended conceptual resolution order:

```text
Authenticated identity
→ resolve Personal vs organization context
→ resolve workspace where applicable
→ resolve launchable Mobile product/experience where applicable
→ destination
```

Use smart skipping at every layer.

Examples:

| Context | Choosers |
|---|---|
| 1 org + 1 branch + 1 launchable Mobile product | none → direct destination |
| 1 org + 3 branches + 1 product | workspace chooser only |
| 1 org + 1 branch + multiple launchable products | product chooser only |
| multiple workspaces + multiple launchable products | workspace chooser, then product chooser only if more than one valid product remains for the selected context |

Product launch eligibility may depend on authoritative organization membership, entitlement, subscription/commercial state, account class, product assignment, product-local role, and client support. Derive choices from authorized capabilities, not a static client list.

Do **not** invent new backend APIs in this amendment. If current APIs are not generic enough for multi-product launch, record the API shape as **implementation-time inspection**. Current evidence: POS evaluates a single product code on Platform access; a generic Mobile product catalog API is **not** claimed here.

### 11.13 Personal experience and Start a Business

Personal-only (0 eligible organizations) → Personal Home. Do not remove Personal identity merely because organization contexts exist.

If a Personal user later has or creates organizations, authorized organization/workspace entry remains available through the approved Mobile experience.

Preserve:

```text
Personal
→ Start a Business
→ Organization created
→ Primary/Main branch/context established by existing approved server flow
→ entitlement/setup
→ Owner essentials / POS setup
```

Do not add an unnecessary chooser when the newly created organization has only one authorized workspace.

### 11.14 Failure states (do not collapse)

| State | Outcome |
|---|---|
| No organization | Personal experience |
| Organization but no accessible Active branch | Branch/location setup or access state |
| Multiple workspaces | Chooser |
| Workspace access revoked | Safe re-resolution; stale last-used workspace is not kept merely because it was stored locally |
| Product not entitled | Do not offer the product |
| Device wrong branch for Enter POS | Blocked explanation; no silent rebind |
| Open shift blocks operational switch | Blocked explanation per existing rules |
| Offline workspace switch | Internet-required persistent explanation |

When online validation later succeeds, **server denial/revocation wins** (branch access removed, membership suspended, product entitlement/role removed, branch archived/inactive). Do not silently fall back to another branch for financial operations. Resolve an authorized safe destination.

### 11.15 Shared AppTopBar context (MOBILE-D-070)

One shared AppTopBar / shell family. It receives current context from shared app state.

Conceptual display (layout varies by device size):

```text
Pinoy Business POS

ABC Grocery
Main Branch

Online • Synced
```

Even when there is only one branch, show useful context where appropriate (`ABC Grocery` / `Main`). Do **not** show a meaningless dropdown arrow or switch affordance when no alternative workspace exists.

The top bar must **not**:

- query workspace authorization independently on every page
- duplicate workspace or product switch logic
- invent branch context
- become a second router

Pages consume shared context.

### 11.16 Reusable component plan

Conceptual names (implementation names may differ). Do not implement now. Do not create a mega-component.

| Concept | Role |
|---|---|
| WorkspaceResolver | One routing policy: auto-enter / chooser / empty / Personal |
| WorkspaceChooser | Unified org-grouped chooser family |
| WorkspaceCard / WorkspaceRow | Organization and branch rows |
| ProductLauncher / ProductChooser | Launchable ExItS experience selection |
| CurrentContextDisplay | Shared org / branch / product presentation |
| SwitchWorkspaceAction | Shown only when 2+ authorized workspaces |
| SwitchProductAction | Shown only when 2+ launchable Mobile products |

Rules: one resolver policy; one chooser family; one shared current-context model; pages do not invent their own branch chooser; top bar consumes shared context.

### 11.17 Language, theme, accessibility

Chooser/context UX follows MOBILE-D-064: default English; secondary `fil-PH`; theme default System; also Light and Dark.

fil-PH text must not clip branch, organization, or product names or actions. Theme/language switching must not reset current workspace or product without an authorization reason.

Chooser accessibility (planning bar, not certification):

- full keyboard accessibility
- touch targets at the approved minimum
- current selection conveyed by text/icon, not color only
- organization grouping semantically understandable
- screen reader names include enough context (organization + branch)
- long organization/branch names wrap
- no forced horizontal scrolling on phone
- loading, empty, and error states

---

## 12. Explicit non-goals for DOC-02

- React implementation, Capacitor plugins, PWA service worker
- MAUI visual rewrite
- New payment methods or live gateways
- New roles
- Pixel-perfect component library choice (later DOC)
- Claiming WCAG certification of the current app
- Implementing WorkspaceResolver / ProductChooser
- Creating product-launch APIs
- Adding parked products (including PLM) to the Mobile Client
