# PLM-D-00-09 — React + PWA + Capacitor Client Strategy

**Status:** Accepted / Product Owner Approved  
**ID:** PLM-D-00-09  
**Date:** 2026-08-19  
**Package:** PLM-01A  
**Evidence:** Product Owner instruction for PLM-01A; this ADR; [../Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md](../Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md)

Does **not** close PLM-D-00-01, PLM-D-00-02, PLM-D-00-04, PLM-D-00-05, PLM-D-00-06, PLM-D-00-07, PLM-D-00-08, PLM-D-00-11, PLM-D-00-12, PLM-D-00-13, D-P12-03, R-091, or D-P12-05.

Companion HOW: [../Architecture/react-pwa-capacitor-client.md](../Architecture/react-pwa-capacitor-client.md).

---

## Context

PLM-00 recorded a **proposed** organization frontend of Blazor Organization Web plus a later MAUI Blazor Hybrid field client. PLM-D-00-09 stayed **Open** as the Web/MAUI component-sharing question. PLM-01 scaffolded `ExItS.PinoyLoanManager.Web` as a Blazor identity shell and deferred MAUI/LocalStore.

Pinoy Loan Manager still needs one organization/field frontend strategy before PLM-02 or any lending UI. The previous proposal duplicated two UI stacks (Blazor Web and MAUI Hybrid) and left sharing unresolved.

PinoyBusinessPOS remains a strictly isolated sibling. This decision must not reuse POS React source, POS business components, POS routes, POS grants, POS money logic, or POS documentation.

R-091 remains open. This ADR must not claim production-secure authentication.

---

## Decision

Pinoy Loan Manager will use **one shared React + TypeScript client** as the primary organization/field frontend.

The same React application will target:

1. Browser Web
2. Installable PWA
3. Capacitor Android / APK

iOS may be considered later and is **not** part of the current commitment.

Future project path (not created in PLM-01A):

`src/Products/PinoyLoanManager/ExItS.PinoyLoanManager.Client/`

`ExItS.PinoyLoanManager.Web` is retained. Its future architectural role is an ASP.NET Core browser host / BFF / reverse-proxy / static-hosting boundary for that React application. It must not become a second independent Blazor lending UI and must not own authoritative loan calculations, duplicated authorization, or duplicated financial rules. The current PLM-01 identity shell remains scaffold evidence until an authorized implementation package changes it. **Do not delete Web.**

The previous preferred architecture (Blazor Organization Web + MAUI Blazor Hybrid field client) is **superseded**. MAUI is deferred and is not part of the approved preferred implementation path. Existing scaffold projects are not deleted in this documentation package.

`ExItS.PinoyLoanManager.LocalStore` is **not authorized**. Offline financial posting is prohibited until a dedicated PLM-13 package designs encryption, device trust, idempotency, stale data, conflicts, revoked permissions, duplicate submission, cash reconciliation, and offline receipt state.

The server remains authoritative. ExItS Personal remains the separate borrower presentation surface. PinoyBusinessPOS remains strictly isolated.

---

## Rationale

- One codebase avoids a second lending UI and an unresolved Web/MAUI sharing strategy.
- Browser, PWA, and Android can share business-facing screens behind environment/capability adapters.
- Organization Web remains the full operational surface; phone/Android may show a role-optimized subset. Hiding a route is not authorization.
- An ASP.NET Core Web host/BFF preserves same-origin browser session architecture when compatible with Platform auth, without putting session tokens in `localStorage`, `sessionStorage`, service-worker cache, or URLs.
- Capacitor stays a thin native host (secure storage, camera, biometrics, notifications, connectivity, device identity, capture, share). It must not become a loan engine, authorization engine, ledger, or second API.
- PWA is online-first. The service worker may cache the static shell and required assets. It must not cache Loan API responses, balances, payments, collections, cash records, authorization responses, session tokens, or sensitive financial payloads as authoritative state. No Background Sync for financial commands initially.
- PinoyLoanManager React/PWA/Capacitor is the proving ground for whether this frontend architecture is suitable for future ExItS products. That does **not** authorize changing PinoyBusinessPOS.

---

## Consequences

- Future client work uses `ExItS.PinoyLoanManager.Client` (to be created only in an authorized Gate B package).
- Web is kept and later adapted as host/BFF; it is not deleted and is not refactored in PLM-01A.
- MAUI is not the preferred field client. PLM-13 remains the phase for offline/mobile field capability, now against the React/PWA/Capacitor track.
- Visual language is recorded now so later packages do not invent a different UI: Stripe Dashboard + Linear/Vercel principles for organization desktop; Wise/Revolut-style polish for phone; ExItS tokens remain authoritative; no third-party brand/assets copied.
- Frontend delivery uses client gates A–J. The core PLM-02…PLM-14 business roadmap is unchanged.
- Cursor must not copy PinoyBusinessPOS React source into PinoyLoanManager.

---

## Risks

- R-091 remains open; browser session vs Capacitor secure-storage details cannot be claimed production-secure.
- Existing Web Blazor identity shell will need a later authorized conversion to host/BFF; until then it is scaffold only.
- PWA/Capacitor misuse could cache financial or auth payloads if later packages ignore this ADR.
- Performance proving-ground work could be mistaken as POS migration authorization. It is not.
- iOS remains unauthorized; Android-first scope must not silently expand.

---

## Alternatives considered

| Option | Description | Outcome |
|---|---|---|
| A. Blazor Web + MAUI | PLM-00 proposed Organization Web (Blazor) plus MAUI Blazor Hybrid field client | Rejected as preferred path. Two UI stacks; PLM-D-00-09 sharing remained open. |
| B. React Web + separate MAUI | React organization web with a separate MAUI field app | Rejected. Still two implementations and sharing risk. |
| C. React + PWA only | Browser/PWA without a native container | Rejected as the full strategy. PWA remains a first-class target, but Capacitor Android is required for the approved APK path. |
| D. One React + PWA + Capacitor client | Single `ExItS.PinoyLoanManager.Client` with web, PWA, and Capacitor adapters | **Selected.** |

---

## Migration implications

- No code migration in PLM-01A.
- Do not delete `ExItS.PinoyLoanManager.Web` or other PLM-01 scaffold projects.
- Do not create `ExItS.PinoyLoanManager.Client`, PWA code, or Capacitor/Android projects in this package.
- Do not create `ExItS.PinoyLoanManager.Maui`.
- Historical PLM-00/PLM-01 reports may keep accurate historical Blazor/MAUI wording. Authoritative preferred path is this ADR.
- A future POS migration would require a separate Product Owner GO/NO-GO. PLM-01A does not authorize it.

---

## Explicit non-goals

- Creating the React client, Vite/PWA/Capacitor projects, npm manifests, or Android workspace
- Implementing UI, lending features, LocalStore, or offline financial queues
- Refactoring or deleting `ExItS.PinoyLoanManager.Web`
- Closing R-091 or inventing production authentication
- Merging ExItS Personal borrower UX into this Client
- Copying PinoyBusinessPOS React source, POS concepts, grants, or money logic
- Authorizing PinoyBusinessPOS migration
- iOS commitment
- Changing unresolved lending, financial, legal, or grant policy
