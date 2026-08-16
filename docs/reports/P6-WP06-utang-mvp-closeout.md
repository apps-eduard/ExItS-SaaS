# P6-WP06 — Utang MVP Closeout

Phase marker: `P6-WP06-utang-mvp-closeout`

## Status

**Complete with documented risks. Phase 6 closed.** Reconciled P6-WP01 through P6-WP05 as one coherent Utang MVP. Hardened Production commercial-header fail-closed behavior, fixed Tagalog statement/receipt localization and share handoff strings, added full lifecycle and migration-chain closeout tests, and reconciled OD documentation drift. No new business scope. **Not production-ready** while Development/Testing authentication, commercial/actor headers, missing POS operational roles, and R-109 remain open. **Phase 7 was not started.**

Feature commit: `9f33420f5f77bade398db6d59728ad9def895683`

## Closeout decision

Phase 6 is **complete** for the approved Utang MVP scope:

- All P6-WP01–P6-WP05 requirements reconciled
- No critical Phase 6 defect remaining after closeout fixes
- Financial integrity and organization isolation proven by automated tests
- Capability matrix consistent (Platform entry + POS feature grants)
- Migration chain apply / stepwise rollback / re-apply validated
- Full solution tests pass; Android Release APK builds
- Documentation matches implementation
- Portfolio independence preserved (Platform + authorized products only)
- Working tree clean and `main` matches `origin/main` after push

## Final delivered Utang MVP scope

| Area | Delivered |
|---|---|
| Customers | Org-owned profiles; optional normalized mobile; soft deactivate/reactivate; no global identity |
| Credit | Positive decimal + required remarks; active customer; append-only; audited reversal |
| Repayments / ledger | Positive repayments; inactive may repay; overpayment blocked; unified deterministic ledger; outstanding = active credits − active repayments |
| Due dates / overdue | Optional `DateOnly`; append-only history; FIFO read-model aging; derived overdue |
| Statements / receipts | Projection statements with opening/closing reconciliation; deterministic `RCPT-{guid:N}` receipts; reversed marked |
| Trial / continuity | Centralized `UtangCapabilityPolicy`; Platform POS continuity entry; Suspended denies |

## Explicit deferred / out of scope

Sales, inventory, interest, penalties, credit limits, installments, write-offs, gateways, QR, cards, tax invoices, payment-allocation persistence, production offline sync (Phase 7), POS operational roles (Cashier / Store Manager / POS Admin), production JWT/MFA authentication.

## Closeout hardening delivered

1. **Production commercial headers ignored** — outside Development/Testing, `X-Pos-Subscription-Status` / `X-Pos-Feature-Grants` are ignored and access fails closed (`pos.commercial.access_unknown`).
2. **fil-PH statement/receipt localization** — remaining English keys and share-text labels localized; MAUI shows localized receipt disclaimer body.
3. **Full lifecycle API test** — customer → credit → due date → partial/exact repay → overpayment reject → reverse repayment → overdue → clear due date → reverse credit → statement → idempotent reversed receipt → cross-org 404.
4. **Phase 6 migration chain test** — apply to latest → rollback through each WP migration → re-apply; asserts `pos` schema tables, no Platform/statement/receipt/ledger_entries tables, filtered unique mobile index `ux_customers_org_active_mobile`.
5. **Docs OD drift** — WP01–WP04 and phase exclusions no longer claim OD-07/08 remain open (resolved in P6-WP05).

## Financial integrity and domain invariants

- `Outstanding = active credits − active repayments`
- Outstanding cannot go negative from an active repayment
- Credit reversal blocked when it would make outstanding negative
- Financial rows are never edited or deleted; reversals remain visible
- Ledger running balance deterministic (RecordedAtUtc ASC, Id ASC)
- Statement opening/closing reconcile with ledger signed effects
- Receipt retrieval idempotent; reversed receipts labeled
- FIFO overdue is read-model only; reversed/fully offset credits are not overdue
- No editable or independently persisted balances/overdue totals

## Security, organization isolation, and capability matrix

- Server org scope via `X-Pos-Organization-Id`; cross-org → 404
- Every Phase 6 operation gated by `UtangCapabilityPolicy` + feature grants
- Product entry and feature authorization are separate; both must pass
- Suspended / missing / stale / unknown → deny
- Continuity (PastDue/Cancelled/Expired): view, repay, reverse credit, statement, receipt only
- Commercial and actor headers are Development/Testing-stage only — **not production-secure**
- Platform access does not assign product-local operational roles
- Auth event sink filters password/token/authorization-like properties

### Final capability matrix

| Capability | Trialing | Active | Grace | PastDue | Cancelled | Expired | Suspended | Grant |
|---|---|---|---|---|---|---|---|---|
| Enter POS | Y | Y | Y | Y* | Y* | Y* | N | view or repay for continuity |
| View / statement / receipt / reverse credit | Y | Y | Y | Y | Y | Y | N | `customer-credit-view` |
| Record repayment | Y | Y | Y | Y | Y | Y | N | `customer-credit-repay` |
| Reverse repayment | Y | Y | Y | N | N | N | N | `customer-credit-repay` |
| Create/edit customer, create credit, due date | Y | Y | Y | N | N | N | N | `customer-credit-create` |

\*Continuity entry requires view or repay grant.

## Persistence and migrations

| Migration | Purpose |
|---|---|
| `20260730073757_AddPosCustomers` | Customers + filtered unique active mobile |
| `20260730081049_AddPosCreditEntries` | Credit entries |
| `20260730084848_AddPosRepayments` | Repayments |
| `20260730091301_AddPosCreditDueDates` | Due dates + history |

- Database: `ExItS_PinoyBusinessPOS`, schema `pos`
- No Platform tables or cross-database foreign keys
- No statement/receipt/ledger_entries persistence
- No new migration in P6-WP06
- `Migrate()` is not called on POS API startup

## API inventory (Phase 6)

Prefix `/api/v1/pos` unless noted. Org header required. Commercial capability enforced.

| Area | Methods |
|---|---|
| Customers | GET/POST `/customers`; GET/PUT `/customers/{id}`; POST deactivate/reactivate |
| Credit | GET summary/list/get; POST create; POST reverse |
| Due dates | PUT/DELETE `/credit/{id}/due-date`; GET history; overdue summary/lists |
| Repayments | GET summary/ledger/list/get; POST create; POST reverse |
| Statements | GET `/customers/{id}/statement` |
| Receipts | GET `/repayments/{id}/receipt` |
| Health | GET `/health` |

## MAUI end-to-end result

Routes cover list/search/create/edit/detail, credit, repayments, ledger, overdue, statement, receipt, commercial restriction UX. DesignSystem, EN/`fil-PH`, themes, density, responsive markers reused. Share handoff reports initiation only. Interactive device validation **not** performed — R-109 remains open.

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.PinoyBusinessPOS.UnitTests | 39 | 0 | 0 |
| ExItS.Platform.UnitTests | 265 | 0 | 0 |
| ExItS.PinoyBusinessPOS.Maui.Tests | 27 | 0 | 0 |
| ExItS.DesignSystem.Tests | 28 | 0 | 0 |
| ExItS.PinoyBusinessPOS.ApiClient.Tests | 17 | 0 | 0 |
| ExItS.Platform.Admin.UnitTests | 27 | 0 | 0 |
| ExItS.ArchitectureTests | 41 | 0 | 0 |
| ExItS.PinoyBusinessPOS.IntegrationTests | 16 | 0 | 0 |
| ExItS.Platform.IntegrationTests | 84 | 0 | 0 |
| **Full solution** | **544** | **0** | **0** |

Baseline 541 preserved and exceeded (+3 closeout tests). Release Android APK: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`.

## Production limitations (do not claim production ready)

- Development/Testing Platform-user authentication only
- Commercial entitlement and actor headers are not production authz/audit
- No POS operational roles
- R-109: no interactive Android emulator/device validation
- R-022: entitlement stale/refresh durations still open
- R-091 and related header/auth risks remain open

## Risks and decisions

| ID | Status |
|---|---|
| OD-07 / OD-08 / OD-09 | **Resolved (P6-WP05)** — documented; closeout reconciled historical drift |
| OD-11 | Open (GCash duplicate hard-block) |
| R-109 | Open — no interactive Android validation |
| R-091 / R-124 / R-128 | Open — not production-secure headers/auth |
| R-022 | Open — stale/refresh durations |

## Portfolio independence

No unauthorized nested product tree is tracked; ignored; not in `ExItS.slnx`.

## Git evidence

| Field | Value |
|---|---|
| Feature commit | `9f33420f5f77bade398db6d59728ad9def895683` |
| Docs commit | `bb70a1bd3cbe5875b1f824cfad533c2c54a2de06` |
| Phase marker | `P6-WP06-utang-mvp-closeout` |
| Final working tree | Clean; matches `origin/main` after push |

## Exact next authorized work package

**Phase 7 — Offline Sync** — first work package as authorized in the Phase 7 roadmap (do not begin until explicitly authorized).
