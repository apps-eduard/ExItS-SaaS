# Mobile React — Migration, Testing, and Implementation Gates

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-07 (AMEND-03 workspace/product validation cases)
**Depends on:** all prior Mobile-React documents, especially [current-state-and-replacement-boundaries.md](current-state-and-replacement-boundaries.md) and [client-experience-boundaries.md](../architecture/client-experience-boundaries.md)

This file defines **safe coexistence** of the current MAUI Mobile Client with a future React / PWA / Capacitor client, plus testing, visual checkpoint, and authorization gates.

It does **not** authorize a React scaffold, PWA production, Capacitor packaging, cutover, or MAUI retirement.

**Do not assume MAUI can be removed.** Until Gate J, MAUI remains available.

---

## 0. Current-host facts (do not guess)

| Fact | Evidence |
|---|---|
| Current Mobile Client | `ExItS.PinoyBusinessPOS.Maui` — Android-first (`net10.0-android`), BlazorWebView host |
| Host scope | Auth + Personal Mobile + Organization Owner Mobile + POS Operations in one process (MOBILE-D-005) |
| Client boundaries (MVP, still in force) | Platform Admin = Web only; Personal = Mobile; Owner essentials = Mobile; full org admin = Web; POS operations = Mobile |
| Organization Web | Not a checkout client; Cashier is denied that host and uses MAUI |
| Personal Web | Additional browser Personal host; not the Mobile Client |
| Future project path | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/` — **not created** |
| Backends | Platform API + POS API + PostgreSQL remain the system of record (MOBILE-D-004) |
| Offline | LocalStore + encrypted outbox consumed by MAUI (DOC-05). Replacement of LocalStore is a later authorized package, not implied by a UI rewrite |
| Native today | Android MAUI. Current csproj: iOS/Windows/MacCatalyst not required for the existing MAUI foundation |

Approved presentation rule (unchanged):

```text
Platform Administration = Web only
Personal Account = Mobile
Organization Owner Essentials = Mobile
Full Organization Administration = Web
POS Product Operations = Mobile
```

A future React host, if authorized, is a **CLIENT HOST replacement candidate**. It is not a new product domain. It must still host Personal, Owner essentials, and POS Operations unless a later Product Owner decision splits those experiences — this track does **not** split them.

---

## 1. Coexistence plan

No big-bang deletion. Each stage may pause. Later stages must not start without the listed gate.

```text
STAGE 0  Current MAUI (authoritative)
STAGE 1  React scaffold (Gate C)
STAGE 2  Web/PWA foundation (Gate D)
STAGE 3  Selected feature parity
STAGE 4  Offline parity (Gate F)
STAGE 5  Device/hardware parity (Gate G)
STAGE 6  Capacitor Android validation (Gate H)
STAGE 7  Controlled acceptance / cutover (Gate I)
STAGE 8  MAUI retirement (Gate J) — only after explicit authorization

iOS native: Gate K (separate, later)
```

Until Stage 8, **both** clients may exist. Production traffic stays on MAUI until Gate I says otherwise. Organization Web and Personal Web stay on their current Blazor hosts (MOBILE-D-003).

### STAGE 0 — Current MAUI remains the Mobile Client

- MAUI is the authoritative / current mobile client (MOBILE-D-002).
- CURRENT_IMPLEMENTATION_REQUIREMENT (native CSS / Razor) still governs MAUI.
- Documentation of React does **not** change this stage.
- Install/debug paths remain existing MAUI emulator and physical-device docs.

### STAGE 1 — React scaffold only after explicit authorization

- Gate **C** required.
- Create the sibling project path; do **not** overwrite `.Maui` or `.Web`.
- Empty/app-shell only: routing chrome, theme tokens, i18n plumbing, typed API client stubs.
- No production users. No store listing. No MAUI code deletion.

### STAGE 2 — Web/PWA foundation

- Gate **D** required.
- Browser delivery of the React app behind existing HTTPS / reverse-proxy model.
- PWA installability and **static app cache** per DOC-04. Service worker is **not** LocalStore.
- Production PWA rollout remains a later subset of Gates D / I — documenting Stage 2 does not ship PWA to customers.

### STAGE 3 — Selected feature parity

- Feature-by-feature, not route-count.
- Typical first slice: Gate **E** visual checkpoint (login/workspace + sell-floor surfaces in §5).
- Personal and Owner surfaces may follow selling, or a Product Owner–chosen slice, but must appear in the parity matrix before cutover of those experiences.
- MAUI remains the production-path client.

### STAGE 4 — Offline parity

- Gate **F**.
- Match DOC-05: cash-only offline checkout until MAUI/API evidence changes; idempotent sync; no silent financial rewrite; OD-10 outbox retention.
- PWA static cache must not be used as proof of offline selling.

### STAGE 5 — Device / hardware parity

- Gate **G**.
- Adapter contracts from DOC-06. Degrade where the host cannot.
- Do not block Stage 3/4 on Bluetooth printers, NFC, or terminals that **current MAUI also does not have**.
- Parity is against **current MAUI capability**, plus explicitly authorized new hardware — not against an invented hardware catalog.

### STAGE 6 — Capacitor Android validation

- Gate **H**.
- Signed/sideload Android package of the **same** React app.
- Secure storage, camera/QR, share, LocalStore-equivalent persistence per DOC-04/05.
- Independent release channel from web/PWA (MOBILE-D-033).
- Does not retire MAUI.

### STAGE 7 — Controlled acceptance / cutover

- Gate **I**.
- Named cohort / store / branch / device list. Rollback plan (§7) active.
- Frontend versions stay inside the API compatibility window.
- MAUI APK/install remains available as fallback.

### STAGE 8 — MAUI retirement

- Gate **J** only. **Explicit Product Owner authorization.**
- Remove or archive the MAUI host **after** cutover acceptance, not before.
- Do not delete MAUI because documentation is complete, because React builds, or because a PWA exists.

### iOS — separate later gate

- Gate **K**.
- Before K: iPhone/iPad may use browser/PWA (MOBILE-D-030). That is reachability, not native parity.
- Current MAUI is Android-first; iOS native is not “restore what MAUI already ships on iOS.”
- Hardware that needs native (printers, NFC, terminals) waits for K or remains Android-only.

---

## 2. Parity model

Parity is **feature-based**, not “N Razor routes = N React routes.”

A feature is a user-completable capability (example: cash checkout with change, Personal I Lent list, redeem POS device QR). URL shape may differ.

### 2.1 Tracking fields

Every tracked feature uses:

| Field | Meaning |
|---|---|
| **Current MAUI capability** | What the live host does today (evidence) |
| **Future React route/feature** | Planned surface in the replacement host |
| **API capability** | Platform and/or POS endpoints; server remains authoritative |
| **Offline behavior** | Queueable / online-required / projection-only (DOC-05) |
| **Device behavior** | Scanner, camera, share, printer, etc. (DOC-06); often N/A |
| **Authorization** | Entitlement + product role + host rules; UI is not permission |
| **Role** | Personal / Owner / Manager / Cashier / staff — who may see it |
| **EN / fil-PH** | Both locales required before that feature is parity-complete (`en` default; `fil-PH` secondary) |
| **Light / Dark / System** | Theme required before that feature is parity-complete (**System default**) |
| **Accessibility** | DOC-02 bar (focus, names, contrast, touch, reduced motion) |
| **Tests** | Automated and/or device tests that cover the feature |
| **Status** | `NOT_STARTED` · `IN_PROGRESS` · `PARITY` · `DEFERRED` · `WONT_PORT` |

`WONT_PORT` needs a written reason (example: Platform Admin must never appear on Mobile). `DEFERRED` is not silent omission.

### 2.2 Scope of the matrix (when implementation starts)

Track at least these experience groups. Do **not** reduce “mobile” to POS checkout.

- Auth / workspace / device registration (smart skip vs chooser; Adaptive Switch workspace/product)
- Personal Account (Utang, QR, Start a Business, invitations)
- Organization Owner essentials (Manage business subset)
- POS Operations (sell, catalog, customers, shifts, registers, purchasing, reports)
- Offline/sync chrome
- Settings (theme, language, density, Lock / Sign Out / Remove From This Device)

Organization Web full administration and Platform Admin are **out of this matrix** (different hosts).

This DOC does **not** fill a complete feature-by-feature inventory. Filling rows is an implementation-track artifact after Gate C.

### 2.3 Parity rules

- Same user + org + operation must get the same API allow/deny as today.
- Cashiers do not gain Organization Administration. Owner without a POS role does not gain checkout (MOBILE-D-018).
- Simulated Card/GCash is not production parity UX (MOBILE-D-020).
- Hardware MAUI never had is not a Stage 7 blocker unless Gate G explicitly added it.
- Visual sameness with Razor is not required; visual **quality** and workflow completeness are.

---

## 3. Testing layers

Future React work (after Gate C) uses layers below. Current MAUI/.NET tests remain; do not weaken them to pass a client rewrite.

| Layer | Purpose | Notes |
|---|---|---|
| TypeScript strict / typecheck | No `any`; `tsc --noEmit` in CI | Frozen in DOC-03 |
| Lint / format | Consistent TS/CSS; no drive-by restyle of MAUI | Tooling chosen at scaffold time |
| Vitest | Unit tests for adapters, formatters, outbox state machines, Zod schemas | Prefer testing contracts, not pixel CSS |
| Testing Library | Component behavior (roles, labels, checkout buttons) | Accessibility-friendly queries |
| API client tests | Typed HTTP, problem+json, idempotency headers, auth handlers | Must not hit production DBs |
| Sync / offline tests | DOC-05 scenarios plus AMEND-01: Lock/Sign Out/Remove, PIN isolation, ordinary vs sensitive OnlineRequired UX | PostgreSQL/Testcontainers remain the proof of **server** behavior |
| Copy Diagnostics tests | Allowlist/redaction: fixtures with tokens/PIN/payloads must not appear on the clipboard | Vitest on the builder; no secrets in snapshots |
| Playwright | Browser/PWA journeys: sign-in, sell floor, theme/locale, workspace/product chooser when multiple choices exist | Not a substitute for physical Android |
| PWA tests | Manifest, SW does not cache-first financial APIs, update prompt does not destroy cart | DOC-04 |
| Accessibility / axe | Automated smoke on checkpoint pages | Does not claim WCAG certification |
| Responsive screenshots | Phone / tablet portrait / tablet landscape / desktop | **Humans approve**; see §5 |
| Android emulator | Capacitor or web-in-emulator smoke | Local Validation, not production |
| Physical Android device | Camera, HID wedge, share, secure storage, real radios | Required before Gate H/I for device-touching features |
| Hardware validation | Printer/drawer/terminal **only if** Gate G authorized that hardware | Do not fake “printer OK” in CI |
| Network-loss tests | Airplane / flapping during checkout and sync | Align with DOC-05 |
| Performance testing | Qualitative sell-floor feel + budgets chosen at implementation time | **No invented numeric SLOs in this DOC** |
| iOS device testing | Later | Gate **K**; not a Stage 6 requirement |

.NET solution restore / Release build / existing tests stay mandatory for any repo change that touches backends. A React CI job does not replace `ExItS.slnx` validation when APIs change.

### 3.1 Workspace and product context validation (AMEND-03)

Documented for a future implementation package. **Do not implement these tests in this amendment.**

| Case | Expected |
|---|---|
| Personal-only (no eligible organization) | Personal Home; no workspace chooser |
| 1 org / 1 accessible Active branch | Auto-select; skip workspace chooser |
| 1 org / Primary/Main only | Auto-select (Primary is one real branch) |
| 1 org / 2+ accessible Active branches | Workspace chooser |
| 2+ accessible organizations | Unified workspace chooser grouped by organization |
| Org membership + zero accessible Active branches | Explicit empty/setup state; no invented branch |
| Multiple workspaces | Current/last used may be highlighted; **not** silently auto-entered |
| Single valid cashier/device operational workspace | Auto-enter; inaccessible branches absent |
| Wrong device branch for Enter POS | Blocked explanation; **no** silent rebind |
| Non-empty cart + workspace switch | Confirm; Continue sale vs Discard and switch |
| Offline PIN cold start | Restore grant-bound workspace only |
| Offline workspace switch attempt | Blocked; Internet-required persistent explanation; stay in current workspace |
| Revocation learned online | Stale last-used workspace no longer usable; no silent financial fallback branch |
| One launchable Mobile ExItS product | Skip product chooser |
| Multiple launchable Mobile ExItS products | Product chooser |
| Unauthorized / unentitled product | Absent from chooser and switch actions |
| 1 authorized workspace | “Switch workspace” omitted |
| 2+ authorized workspaces | “Switch workspace” shown |
| 1 launchable product | “Switch product” omitted |
| AppTopBar | Displays correct organization/branch (and product when applicable) from shared state; no switch affordance when no alternative |
| Locale | `en` default; `fil-PH` secondary; names/actions do not clip |
| Theme | System default; Light; Dark; does not reset workspace/product |
| Viewports | Phone, tablet, desktop |
| Accessibility | Keyboard; touch minima; current selection not color-only; semantic org grouping; wrap long names |

These cases belong in Vitest (resolver policy), Testing Library (chooser/top bar), and Playwright (journeys). They do not authorize a React project in this package.

---

## 4. Performance principles

Planning principles only. **Do not invent unsupported numeric SLOs** (no claimed TTI, FPS, or bundle-kilobyte gates here). Implementation packages may add measured budgets with evidence.

- **Scan → product feedback feels immediate.** HID/type/camera result should update the cart path without waiting for a full page navigation.
- **Cart updates locally.** Session cart is not a ledger (MOBILE-D-019). Server prices the completed sale when online.
- **No blocking server round-trip for purely local cart interaction** (qty ±, remove, line preview).
- **Virtualize large lists** where a real catalog/history would jank; do not virtualize the three-line cart.
- **Bounded queries** (existing paging/search patterns); no unbounded “load all products.”
- **Lazy-load** non-sell routes (Manage business children, reports, settings).
- **Controlled bundle size** — selling route must not pull Owner-admin or Personal-Utang graphs by default.
- **Minimal native bridge traffic** — adapters, not per-keystroke plugin calls.
- **Background sync where safe** — FIFO outbox when online; never silent rewrite; never SW cache-first money.
- **No animation that delays selling.** Honor `prefers-reduced-motion`. No looping sell-floor motion (DOC-02).

---

## 5. First visual checkpoint (Gate E)

The **first** future implementation visual checkpoint is the sell-capable chrome, not the entire MAUI surface area.

### 5.1 Surfaces

| Surface | Why it is in the first checkpoint |
|---|---|
| Login / workspace | Auth and org/device context before any sale; enrolled-user chooser when trusted; **skip** workspace chooser when exactly one authorized workspace |
| POS selling screen | Primary cashier job (tablet landscape) |
| Product browse / search | Barcode-first lookup |
| Cart | Session-persistent; local updates; not discarded by Lock |
| Checkout / payment selection | Cash, Manual GCash, Utang per current rules — no new methods |
| Offline / sync indicator | LOCAL / PENDING / SYNCED / FAILED; ordinary Internet-required toast vs sensitive dialog |
| Copy Diagnostics | Compact [Copy] on runtime errors (not field validation) |
| Phone navigation | Personal/Owner/ops chrome at phone density |
| Tablet landscape | Primary sell-floor layout |
| Desktop / PWA | Side nav/tables allowed; must not become Admin |

### 5.2 Required screenshot matrix

For each surface in §5.1, capture:

| Locale / theme | Viewport |
|---|---|
| EN Light (explicit) | Phone portrait |
| EN Dark (explicit) | Phone portrait |
| EN System-resolved-Light **or** System-resolved-Dark | Phone portrait (cover both resolved appearances before Gate I) |
| fil-PH (Light or Dark, at least one full pass; both themes before Gate I for these surfaces) | Phone portrait |
| EN Light | Tablet portrait |
| EN Light | Tablet landscape |
| EN Light | Desktop / PWA width |

Theme testing must include **Light**, **Dark**, **System-resolved-Light**, and **System-resolved-Dark**. First-launch default is System (not stored as explicit Light/Dark).

Minimum before Gate E can even be **submitted**: EN Light + EN Dark + fil-PH on **phone**, plus tablet landscape sell floor, plus one desktop/PWA frame. Remaining combinations, including both System-resolved appearances, complete before Gate I for checkpoint surfaces.

### 5.3 Approval

**Cursor / the coding agent cannot self-approve screenshots.**

A human Product Owner (or named visual reviewer) must accept or reject the checkpoint. Automated axe/Playwright may **fail** a gate; they may not **pass** visual quality.

---

## 6. Implementation gates

Every gate below requires **explicit Product Owner approval** where the table says PO. Completing or approving Mobile-React documentation (Gate A materials) **does not** authorize implementation.

| Gate | Name | Unlocks | Requires (planning) | PO |
|---|---|---|---|---|
| **A** | Documentation approved | Permission to treat this doc set as the planning baseline | DOC-01…DOC-08 plus AMEND-01…AMEND-03 reviewed; contradictions recorded | Yes — **planning baseline approved 2026-08-19**. Does **not** unlock C–K or merge. |
| **B** | Backend / client gap plan | Known API/auth/offline gaps scheduled | Written gaps vs current Platform/POS APIs; no silent new endpoints as “frontend work” | Yes |
| **C** | React scaffold authorization | Stage 1 — create `ExItS.PinoyBusinessPOS.Client` | Gate A; must not touch MAUI retirement; CI typecheck/lint/Vitest smoke | Yes |
| **D** | PWA foundation | Stage 2 — browser/PWA shell | Gate C; DOC-04 cache vs LocalStore; **production PWA still separate** unless PO says ship | Yes |
| **E** | First visual checkpoint | Stage 3 slice of sell-floor UX | Gate D or an agreed browser-only slice; screenshot matrix; **human visual sign-off** | Yes |
| **F** | Offline / sync parity | Stage 4 | Gate E (or PO-approved overlap); DOC-05 tests; cash-only until evidence changes | Yes |
| **G** | Device integration | Stage 5 | Adapters + capability matrix; no invented terminals | Yes |
| **H** | Android Capacitor | Stage 6 | Gates F/G as applicable to native; emulator **and** physical Android for device-touching features | Yes |
| **I** | Production acceptance / cutover | Stage 7 — React may become the production-path Mobile Client for a defined cohort | Parity matrix for in-scope features; rollback drill; API compatibility window | Yes |
| **J** | MAUI retirement | Stage 8 — MAUI may be removed | Gate I complete; fallback period elapsed; PO **explicit** retirement | Yes |
| **K** | iOS native rollout | Later Capacitor iOS | Separate from H/I/J; PWA-on-iOS is not K | Yes |

Locked today:

| Item | Status |
|---|---|
| Gate A — documentation as planning baseline | **APPROVED** (does not unlock implementation or merge) |
| Gate C — React implementation | **NOT AUTHORIZED** |
| Gate D production PWA | **NOT AUTHORIZED** |
| Gate H Capacitor production | **NOT AUTHORIZED** |
| Gate J MAUI retirement | **NOT AUTHORIZED** |
| Gate K iOS native | **NOT AUTHORIZED** (later) |
| Merge to `main` | **AWAITING PRODUCT OWNER AUTHORIZATION** |

---

## 7. Rollback

Until Gate J:

- **MAUI remains available as fallback** (sideload / existing install channel).
- Cutover (Gate I) must name how to revert a cohort to MAUI without a data rewrite.
- **Do not require a database migration solely because the client changes.** Product schema changes follow existing POS/Platform migration rules, not a UI host swap.
- **Backend remains .NET** (Platform API + POS API).
- **Product Domain / Application / Infrastructure remain.** Do not port sale/payment/entitlement rules into JavaScript as the source of truth.
- LocalStore / outbox on a device is per-install. Switching hosts does not magically move unsynced rows; devices must sync (or accept LOCAL-UNSYNCED risk) before a host swap on that device.
- Web/PWA and Capacitor channels are independent; rolling back one must not require rolling back the other unless they share a broken API assumption.

Organization Web and Personal Web are **not** rollback targets for POS checkout. Cashiers stay on a Mobile Client host (MAUI or future React), not Org Web.

---

## 8. Explicit non-goals

- Authorizing or creating the React project in this package
- Deleting, freezing, or “soft-retiring” MAUI
- Splitting Personal / Owner / POS into three products
- Inventing performance SLOs or WCAG certification
- Filling the complete parity inventory before Gate C
- iOS native as part of Android Capacitor validation

**Implementation: NOT STARTED.**  
**MAUI retirement: NOT AUTHORIZED.**  
**React implementation: NOT AUTHORIZED.**
