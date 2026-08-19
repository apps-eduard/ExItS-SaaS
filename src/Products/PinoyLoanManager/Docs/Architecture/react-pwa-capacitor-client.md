# Pinoy Loan Manager — React / PWA / Capacitor Client Architecture

**Status:** Accepted architecture (PLM-D-00-09 / PLM-01A); Gate B scaffold present
**Implementation present:** React Client foundation only — no lending, auth, PWA, or Capacitor
**Last updated:** 2026-08-19

`ExItS.PinoyLoanManager.Client` exists as a Gate B scaffold. Do not add PWA or Capacitor from this document.

ADR: [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md). Surfaces: [application-surface-model.md](application-surface-model.md). Layout: [source-and-project-layout.md](source-and-project-layout.md). Offline: [mobile-offline-boundary.md](mobile-offline-boundary.md).

PinoyBusinessPOS is strictly isolated. Do not reuse or copy the existing POS React client, POS components, POS routes, POS grants, POS money logic, or POS documentation.

---

## Target architecture

```text
ExItS Platform
       |
       | identity / org / commercial contracts
       v
Pinoy Loan Manager
       |
       +-- Domain
       +-- Application
       +-- Infrastructure
       +-- Api
       +-- ApiClient
       |
       +-- Web
       |     ASP.NET Core browser host / BFF / reverse proxy
       |     NOT a second lending UI
       |
       +-- Client
             React + TypeScript (Gate B scaffold)
                    |
                    +-- Browser Web
                    +-- PWA          later (Gate C)
                    +-- Capacitor Android
                            |
                            +-- APK
```

iOS is later only, by separate Product Owner authorization.

---

## One codebase — multiple surfaces

Use one product: `ExItS.PinoyLoanManager.Client`.

Do **not** create `LoanWebReact`, `LoanPwaReact`, or `LoanAndroidReact` as separate applications.

```text
React application
       |
       +-- web adapter
       +-- PWA adapter
       +-- Capacitor adapter
```

Business-facing screens/components are shared where practical. Native-specific code belongs behind explicit adapter interfaces.

---

## `ExItS.PinoyLoanManager.Web`

Retained from PLM-01. Future role:

- same-origin browser hosting
- runtime configuration
- authentication/session boundary
- reverse proxy / BFF boundary
- SPA fallback
- security headers
- production hosting

Must **not** contain authoritative loan calculations, duplicated authorization rules, duplicated financial rules, or a second independent Blazor lending UI.

The current identity shell is scaffold evidence only until an authorized implementation package changes it. Do not delete or refactor Web in PLM-01A.

---

## React Client responsibility

Future `ExItS.PinoyLoanManager.Client` owns presentation and interaction.

It may contain: `app/`, `features/`, `components/`, `components/ui/`, `components/exits/`, `api/`, `adapters/`, `hooks/`, `layouts/`, `lib/`, `styles/`, `i18n/`.

It must **not** own authoritative:

- loan calculations
- interest calculations
- payment allocation
- penalty calculations
- loan lifecycle authorization
- financial posting
- ledger rules
- commercial access decisions
- grant authorization

Those remain server authoritative in Pinoy Loan Manager.

---

## Approved frontend stack

React · TypeScript strict mode · Vite · Tailwind CSS · Radix / shadcn-style primitives · Lucide · React Router · TanStack Query · TanStack Table where appropriate · React Hook Form · Zod · Vitest · React Testing Library · Playwright · Vite-compatible PWA tooling · Capacitor Android later.

Do **not** add Redux by default.

Do **not** add Angular, Vue, Flutter, React Native, Ionic UI framework, Material UI, Bootstrap, or another CSS framework.

Capacitor is the native container/bridge, not the UI framework.

---

## Organization Web scope

Browser/PWA is the **full** organization operational surface. Eventually it may include documented PLM areas:

Dashboard · Borrowers · Traditional Loans · Quick Loans · Applications · Approvals · Active Loans · Payments · Collections · Collectors · Cash Management · Disbursements · Reconciliation · Reports · Configuration · Staff / grants · Audit

This list records **surface ownership only**. Do not implement them here. Do not invent data models, calculations, or API contracts.

---

## Mobile / Android scope

Capacitor Android uses the **same** React application.

Mobile may present a role/capability-optimized subset such as assigned work, borrower lookup, collection workflow, approved field disbursement, cash accountability, and remittance — **only** when those business packages are authorized.

Do not create a separate Android business implementation. Authorization remains server enforced. Hiding a route on Android is not authorization.

---

## Responsive product strategy

One design system, adaptive compositions.

| Viewport | Composition |
|---|---|
| Desktop | Full operational organization application |
| Tablet | Adaptive operational layout |
| Phone | Task-focused mobile composition |

Do **not** shrink desktop pages onto phone. Do **not** make desktop use mobile bottom-navigation patterns. Do **not** make phone use desktop admin sidebars.

Shared product: **yes**. Identical layout at every viewport: **no**.

---

## Visual system

| Surface | Structural basis | Primary visual reference | Secondary |
|---|---|---|---|
| Organization Web / desktop | shadcn/Radix application patterns | Stripe Dashboard (hierarchy, density, tables, filters, status, forms, financial/admin presentation) | Linear / Vercel principles (navigation polish, spacing, dark mode, compact controls) |
| Mobile / PWA phone | same ExItS/shadcn system | Wise / Revolut-style financial mobile UX (hierarchy, settings, forms, compact actions, dark mode) | Operational information hierarchy still follows ExItS / Stripe discipline |

Do **not** copy logos, branding, illustrations, text, proprietary assets, or exact layouts. ExItS identity remains authoritative. Do not copy Stripe blue or another company’s branding.

Locale: English default; `fil-PH` secondary. Theme: System / Light / Dark. Accessibility: WCAG 2.2 AA **design target** (not a certification claim). Exact PLM accent/brand token may be finalized in the React scaffold package if not already defined. Do not invent a new product logo in this docs package.

---

## PWA

First-class deployment target. Initial architecture is **online-first**.

Service worker **may** cache: static application shell; fonts/assets/icons required by the application.

Service worker **must not** cache as authoritative state: Loan API responses; borrower financial state; loan balances; payment, collection, or cash records; authorization responses; session tokens; sensitive financial API payloads.

No Background Sync for financial commands initially. No offline financial posting. Server remains authoritative. PLM-13 remains the future phase for offline/mobile field capability.

---

## Offline

Preserve the safe baseline: **online / server-authoritative first**.

Do not create LocalStore now. Do not define IndexedDB/SQLite financial schemas now. Do not queue payments, collections, disbursements, or financial postings until a dedicated PLM-13 package explicitly designs encryption, device trust, idempotency, stale data, conflicts, revoked permissions, duplicate submission, cash reconciliation, and offline receipt state.

Leave a clean adapter seam for that later work.

---

## Capacitor

Introduce Capacitor only after the React/PWA application reaches its designated implementation gate.

Thin native host only. Future allowed responsibilities may include secure storage, camera, biometrics, notifications, connectivity, device identity, file/document capture, share, and other approved native capabilities.

Capacitor must **not** become a loan calculation engine, authorization engine, financial ledger, or second API implementation. Android first.

---

## Authentication

R-091 remains **OPEN**. Do not claim production-secure authentication.

| Surface | Target separation |
|---|---|
| Browser / PWA | Prefer same-origin browser session architecture through `ExItS.PinoyLoanManager.Web` host/BFF when compatible with Platform auth. No auth/session tokens in `localStorage`, `sessionStorage`, service-worker cache, or URLs. |
| Capacitor | Native secure-storage based credential/session transport may be designed later. Never store reusable native credentials in ordinary web storage. |

---

## API boundary

React Client communicates through approved HTTP contracts only.

No React direct access to PostgreSQL, EF Core, Infrastructure, Platform tables, or PinoyBusinessPOS APIs/database.

`ExItS.PinoyLoanManager.Api` remains the Loan operational authority. Platform data enters only through approved Platform contracts/API boundaries.

---

## ExItS Personal remains separate

Do **not** create a standalone borrower app.

```text
ExItS Personal
    |
    +-- Loan presentation/customer experience

PinoyLoanManager React Client
    |
    +-- organization staff / operational experience
```

Personal consumes approved Loan API/contracts. It never becomes the authoritative Loan ledger.

---

## Performance proving ground

PinoyLoanManager React/PWA/Capacitor is the reference implementation for evaluating whether this frontend architecture is suitable for future ExItS products. This does **not** authorize changing PinoyBusinessPOS.

Future acceptance evidence categories: startup performance; navigation responsiveness; bundle size; memory behavior; low/mid-range Android device behavior; PWA install/update reliability; Android WebView/Capacitor behavior; accessibility; offline/online transition behavior; auth/session reliability; crash/error diagnostics; maintainability; duplicated-code level; automated test reliability.

A future POS migration requires a separate Product Owner GO/NO-GO.

---

## Client gates

Cross-cutting frontend delivery track. Does **not** replace the core PLM business roadmap. Do not start later gates from this package.

| Gate | Meaning |
|---|---|
| **PLM-CLIENT-GATE A** | Architecture decision — this package |
| **PLM-CLIENT-GATE B** | React client scaffold — no lending business screens |
| **PLM-CLIENT-GATE C** | Browser/PWA foundation |
| **PLM-CLIENT-GATE D** | Auth + organization/product access integration |
| **PLM-CLIENT-GATE E** | First real lending vertical slice + mandatory visual review |
| **PLM-CLIENT-GATE F** | Responsive/field workflows |
| **PLM-CLIENT-GATE G** | Capacitor Android shell |
| **PLM-CLIENT-GATE H** | Physical Android device validation |
| **PLM-CLIENT-GATE I** | Performance/reliability architecture assessment |
| **PLM-CLIENT-GATE J** | Production readiness/cutover |

Offline financial operation remains under **PLM-13** and requires its own explicit authorization.

Recommended order after PLM-01A approval (not started here): Gate B → Gate C → PLM-02 identity/org/product access integration → continue business roadmap package-by-package.

---

## Explicit non-goals

- Creating the Client project, PWA code, Capacitor, or Android workspace
- Implementing lending UI or business packages
- Deleting or refactoring Web
- Copying PinoyBusinessPOS React
- Claiming production-secure authentication
- Offline financial posting
