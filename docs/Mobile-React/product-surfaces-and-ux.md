# Mobile React — Product Surfaces and UX

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-02  
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
- Quick Organization Owner / Manager tasks (workspace switch, staff invite, branch settings, subscription status, Start Selling)
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
| EN default | English is default UI language. |
| fil-PH | Filipino is the required secondary locale. Layouts must tolerate longer strings. |
| Light / Dark / System | Required. Immediate switch; persist preference; no app restart; no lost cart/form. |
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
Select workspace / branch
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

Preconditions the UI may **remind** but must not override:

- Workspace/branch selected
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

Use **one** AppTopBar / shell family. Page-specific titles, actions, and context arrive through configuration and slots. List pages compose shared SearchBar, FilterBar, PageHeader, EmptyState, ErrorState, LoadingState, StatusChip / SyncStatusChip, and ConnectivityIndicator rather than restyling native controls independently.

Destructive or blocking confirmations use ConfirmDialog. Ordinary vs sensitive Internet-required UX uses the shared toast/dialog pair (AMEND-01). Runtime errors use CopyDiagnosticsButton on the shared ErrorState where applicable.

Customize via props, variants, slots, optional actions, and labels. Do not duplicate a composite to tweak one page. Do not grow a single “Swiss army” component that owns unrelated flows. Share first inside this client; do not import Platform Admin business chrome.

Accessibility, `en` / `fil-PH`, Light/Dark/System, Compact/Comfortable, loading, disabled, keyboard, and touch minima belong on the shared control when they apply.

---

## 9. Localization and theme

- Default locale: **en**
- Required secondary: **fil-PH** (Filipino; Tagalog wording may appear in copy)
- No hard-coded user-visible strings in reusable components
- PHP / Philippine locale formatting for money and dates
- Theme: **Light / Dark / System**, persisted, no flash of wrong theme where host allows
- Theme/language/density change must not drop the cart

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

## 11. Explicit non-goals for DOC-02

- React implementation, Capacitor plugins, PWA service worker
- MAUI visual rewrite
- New payment methods or live gateways
- New roles
- Pixel-perfect component library choice (later DOC)
- Claiming WCAG certification of the current app
