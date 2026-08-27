# EXITS-V1-CLOSURE-01 — React Ownership Initiation + External Business QR Acquisition

**Package:** EXITS-V1-CLOSURE-01
**Status:** COMPLETE
**Branch:** `feat/personal`
**Baseline:** `c8955395b61f5b08a28bed9db2706479227ff8bc`
**Implementation SHA:** `4f43b2bdb4fed01140277a1048718b5ef7a6ed7b`

## Purpose

Finish two remaining React/customer-acquisition capabilities in one package:

A. React Organization Owner ownership-transfer **initiation**  
B. External Business QR → public store → sign-in/register → resume → optional PWA install

## A. Ownership initiation

### Backend

Unchanged. Reuses:

- `POST .../ownership-transfer/resolve-target`
- `POST .../ownership-transfer/request`
- `GET .../ownership-transfer/pending`
- `POST .../ownership-transfers/{id}/cancel`

Recipient Accept/Decline remains PERS-OWNERSHIP-01 (`/personal/ownership-transfers`).

`OWNERSHIP_MIGRATION_CREATED=NO`  
`OWNERSHIP_BACKEND_SEMANTICS_CHANGED=NO`

### React

| Item | Value |
| --- | --- |
| Route | `/org/ownership-transfer` |
| Guard | `RequireOrganizationOwnerMembership` (Platform Organization Owner only) |
| Nav | Manage business (`/org`) → Transfer ownership |
| Online-only | `ONLINE_REQUIRED_CODES.OrgOwnershipTransfer` |

Flow: pending card + cancel **or** resolve Personal EX/QR → review → confirm disclosures → request → Pending until recipient accepts.

## B. External Business QR acquisition

### Public HTTPS QR

- Displayed Business QR / share / copy-link use: `{origin}/store/ORG######`
- Identifier: **PublicOrganizationId** (no slug CMS in v1)
- Legacy `exits://qr/v1/organization/ORG######` still resolves server-side
- Internal Organization GUID / email / phone / tokens **not** in QR

### Anonymous public API

`GET /api/v1/public/stores/{publicOrganizationId}`

Minimal DTO: `PublicOrganizationId`, `DisplayName`, `OrderingAvailable`  
Generic unavailable for unknown/inactive/suspended. Rate-limited. No membership/staff/ownership grant.

### Public route

`/store/:publicOrganizationId` — **not** behind RequireSession.

- Anonymous: Sign in / Create account with safe `?continue=/store/ORG…`
- Personal authenticated: Continue → linked shop or `/personal/linked-merchants`
- Organization staff: blocked with Personal sign-in CTA
- Offline: online-required message (no stale business data)

### Continuation

- Client intent: sessionStorage `exits.acquisition.storeIntent` + validated `continue` query
- Open-redirect protection: internal paths only (`isSafeAuthContinuePath`)
- Sign-in / activation resume store intent when safe
- Customer link / staff / ownership **not** auto-created

### PWA

- Existing manifest/SW preserved; business ops remain ONLINE_ONLY
- Optional `InstallExitsOffer` via `beforeinstallprompt`; dismissible; never blocks store
- Post-install resume: best-effort via same URL / stored intent; not guaranteed across browsers

## Explicit non-goals / deferred

- Full RMAP-B05 CMS, social links, SEO, custom domains, slug history
- Workforce `/store/{slug}/login`
- Billing payer migration, POS role/device transfer, Personal data transfer
- Offline ownership / offline checkout / new commerce engine

`RMAP_B05_AUTHORIZED` remains **NO** for the full design; only this bounded acquisition subset is authorized/implemented.

## Tests

- Platform: `PublicStoreLandingLookupTests`, OwnershipTransfer suite
- React: ownership initiation page, store-acquisition safety, public-store client, QR URL privacy
- Playwright: `exits-v1-ownership-initiation.spec.ts`, `exits-v1-business-qr-acquisition.spec.ts`, plus PERS-OWNERSHIP-01 / regression gates

## Files (primary)

- Platform: `OrganizationPublicIdentityUseCases.cs` (`LookupPublicStoreLanding`), `PublicStoreEndpoints.cs`
- React: `OrgOwnershipTransferPage`, `PublicStoreLandingPage`, `OrgBusinessQrPage`, `store-acquisition.ts`, `business-qr-url.ts`, `InstallExitsOffer`, auth continue wiring
- Docs: this file
