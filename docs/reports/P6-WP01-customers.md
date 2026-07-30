# P6-WP01 — Customers

Phase marker: `P6-WP01-customers`

## Status

**Complete with documented risks.** Organization-isolated PinoyBusinessPOS customer management only. Credit, ledger, repayments, sales, inventory, and offline sync remain excluded (later Phase 6 WPs). OD-07 and OD-08 remained open at delivery of this WP; later resolved in P6-WP05.

## Delivered capability

- POS Domain `POSCustomer` / `POSCustomerId` / `PosOrganizationId` (Platform org GUID reference, no cross-DB FK)
- Separate POS database boundary: `ExItS_PinoyBusinessPOS`, schema `pos`
- Migration `AddPosCustomers` (PK, required org + display name, status check, UTC timestamps, xmin concurrency, filtered unique active mobile per org)
- Application use cases: create, update profile, get, paginated list/search, deactivate, reactivate
- POS API `/api/v1/pos/customers` (+ deactivate/reactivate) with `X-Pos-Organization-Id` scope (404 fail-closed across orgs)
- MAUI `/customers`, `/customers/new`, `/customers/{id}`, `/customers/{id}/edit` using DesignSystem
- English + Tagalog customer strings; deferred credit messaging; Light/Dark and Compact/Comfortable unchanged
- Projects: Domain, Infrastructure, Api, UnitTests, IntegrationTests

## Explicit exclusions

Credit accounts, remarks-based credit, balances, ledger, repayments, due dates, statements, receipts, credit limits, interest/penalties, sales, inventory, offline sync. No global customer identity across stores. No PHI/HealthCare data.

## Duplicate policy (MVP)

Display names need not be unique. Active customers cannot share the same normalized mobile inside one organization. Same mobile allowed across organizations. Stable `pos.customer.mobile.conflict` (409).

## Persistence and migration

- Database: `ExItS_PinoyBusinessPOS` / schema `pos` / table `customers`
- Migration apply / rollback-to-0 / re-apply validated in Testcontainers
- No Platform or HealthCare tables; no cross-database FKs

## API capability

| Method | Route |
|---|---|
| GET | `/api/v1/pos/customers` |
| POST | `/api/v1/pos/customers` |
| GET | `/api/v1/pos/customers/{customerId}` |
| PUT | `/api/v1/pos/customers/{customerId}` |
| POST | `/api/v1/pos/customers/{customerId}/deactivate` |
| POST | `/api/v1/pos/customers/{customerId}/reactivate` |

Organization scope via `X-Pos-Organization-Id`. Development-stage only (no production JWT). Typed DTOs + ProblemDetails `errorCode`.

## MAUI customer experience

List (search, responsive table/cards), create, detail (deactivate/reactivate confirm), edit. Deferred credit banner. No balance/Utang controls.

## Organization isolation

All queries filter by organization id from header/session. Cross-organization get/update returns 404.

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Platform unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 28 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 27 | 0 | 0 |
| POS unit | 8 | 0 | 0 |
| POS integration | 4 | 0 | 0 |
| Platform integration | 84 | 0 | 0 |
| **Total** | **497** | **0** | **0** |

Android Release APK: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk` (also unsigned `.apk`). No interactive emulator attached (R-109 remains).

## Security limitations

- POS customer API trusts `X-Pos-Organization-Id` without production authentication
- Development/Testing Platform identity remains the only auth path for MAUI
- Not production-secure

## HealthCare freeze

`git ls-files -- HealthCare/` empty; `git check-ignore -v HealthCare/` → `.gitignore:/HealthCare/`; HealthCare not in `ExItS.slnx`.

## Risks / open decisions

- R-109: no interactive Android emulator validation
- Later resolved in P6-WP05 (see that report). Remained open at delivery of this WP.
- Production auth still missing (R-091)
- Trial-expiry credit editing rules not applicable (no credit yet)

## Files / docs changed

POS Domain/Application/Infrastructure/Api/ApiClient/Maui customers; phase-06 roadmap; portfolio; README; FILE-MANIFEST; engineering docs (architecture, data ownership, security, authorization, localization, UI, testing); risks; release-plan; this report.

## Git evidence

- Feature commit: `674ad0660b0bd11bca75f2e90e329c4579ff592a`
- Docs hash-record commit: `0b921f076235ec70d5f26388bd2e95bcfd6ba7cd`
- Phase marker: `P6-WP01-customers`
- Remote `main` matches local `0b921f0`

## Exact next work package

**P6-WP02 — Remarks-Based Credit** — do **not** begin until explicitly authorized.
