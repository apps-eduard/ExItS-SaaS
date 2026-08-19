# Pinoy Loan Manager — PLM-01A React / PWA / Capacitor Architecture Decision

**Package:** PLM-01A  
**Date:** 2026-08-19  
**Branch:** `docs/plm-react-client-architecture`  
**Starting SHA:** `4ec9e96e9149cd8d014adde3d694872a6d5ef576`  
**Starting `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Documentation / architecture only. Does **not** create the React client, implement UI, implement lending features, implement Capacitor, or implement PWA code.

Does **not** rewrite historical PLM-00 or PLM-01 reports merely to erase Blazor/MAUI wording. Authoritative preferred path is this package and [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md).

---

## Status

| Item | Status |
|---|---|
| PLM-01A | **COMPLETE** after validation |
| PLM-D-00-09 | **Closed / Product Owner Approved** |
| PLM-CLIENT-GATE A | **COMPLETE** (this package) |
| React Client project | **NOT CREATED** |
| PLM-02 | **NOT STARTED** |
| Capacitor / PWA code | **ABSENT** |
| LocalStore | **NOT AUTHORIZED** |
| PinoyBusinessPOS | **UNCHANGED** |

---

## Decision

One shared React + TypeScript client targeting Browser Web, installable PWA, and Capacitor Android. `ExItS.PinoyLoanManager.Web` is retained as the future ASP.NET Core browser host / BFF / reverse-proxy / static-hosting boundary. The previous Blazor Organization Web + MAUI Blazor Hybrid preferred path is superseded. MAUI is not part of the approved preferred implementation path. Server remains authoritative. ExItS Personal remains the separate borrower presentation surface.

Future Client path (does not exist yet):

`src/Products/PinoyLoanManager/ExItS.PinoyLoanManager.Client/`

---

## PLM-D-00-09 closure

| Field | Value |
|---|---|
| Previous state | Open / Product Owner Decision Required |
| New state | Closed / Product Owner Approved |
| Evidence | PLM-01A architecture ADR and Product Owner instruction |
| Resolution | One shared React + TypeScript client for Browser Web, PWA, and Capacitor Android; Web retained as host/BFF; MAUI preferred path superseded |

Left **open:** PLM-D-00-01, PLM-D-00-02, PLM-D-00-04, PLM-D-00-05, PLM-D-00-06, PLM-D-00-07, PLM-D-00-08, PLM-D-00-11, PLM-D-00-12, PLM-D-00-13, D-P12-03, R-091, D-P12-05.

---

## Affected docs

- [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md)
- [../Architecture/react-pwa-capacitor-client.md](../Architecture/react-pwa-capacitor-client.md)
- Canonical indexes and architecture/surface/layout/offline/API docs listed in [../FILE-MANIFEST.md](../FILE-MANIFEST.md)
- [../risks-and-decisions.md](../risks-and-decisions.md) register row for PLM-D-00-09

---

## Boundaries recorded

| Boundary | Recorded decision |
|---|---|
| Web / BFF | Existing Web retained; future same-origin host/BFF; not a second lending UI; not refactored here |
| PWA | Online-first; static shell cache allowed; no authoritative financial/auth/session cache; no financial Background Sync |
| Capacitor | Thin native host later; Android first; not a loan/auth/ledger/API engine |
| Offline | LocalStore not authorized; no financial command queue until PLM-13 designs the required controls |
| Auth | R-091 open; browser prefers same-origin session via Web; no tokens in web storage / SW cache / URLs; Capacitor secure-storage seam later |
| Personal | Separate Platform-owned borrower presentation; not merged into this Client |
| POS isolation | No POS files changed; no POS React reuse or copy |
| Visual standard | Stripe-led desktop organization app; Wise/Revolut-style phone polish; ExItS tokens; shadcn/Radix structure |
| Client gates | A–J documented; Gate A is this package |
| Core roadmap | PLM-02 remains Identity / Organization / Product Access; gates are a cross-cutting frontend track |

---

## Explicit non-goals (this WP)

- Creating `ExItS.PinoyLoanManager.Client`
- `.cs`, `.csproj`, `.ts`, `.tsx`, Vite, npm, PWA manifest, Capacitor, Android, MAUI, database, migration, API, Docker, or business code
- Deleting or refactoring Web
- Starting PLM-02, Gate B, or Capacitor
- Touching PinoyBusinessPOS
- Inventing unresolved lending/financial/legal policy
- Authorizing POS migration
- iOS commitment

---

## Exact next package

**STOPPED AFTER PLM-01A.**

Recommended later order when separately authorized: PLM-CLIENT-GATE B (React scaffold) → PLM-CLIENT-GATE C (PWA/browser foundation) → PLM-02 identity/org/product access integration → continue the business roadmap.

Do not start any of these from this package.
