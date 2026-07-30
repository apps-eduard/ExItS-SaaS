# P6-WP03 — Payments and Ledger

Phase marker: `P6-WP03-payments-and-ledger`

## Status

**Complete with documented risks.** Organization-owned customer repayments with overpayment protection and a unified read-only Utang ledger. Outstanding is derived as active credits − active repayments. Due dates, statements, printable receipts, trial-expiry behavior, interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, and offline sync remain excluded. Platform SaaS payments remain separate. OD-07 and OD-08 remain open. **P6-WP04 was not started.**

## Delivered capability

- Domain `Repayment` / `RepaymentId` / `RepaymentStatus` (Active | Reversed)
- Invariants: positive decimal amount (≤2 decimals), optional remarks/reference, RecordedBy actor, UTC timestamps
- Append-only repayments; explicit reversal with required reason + ReversedBy
- Inactive-customer policy: **allow repayment against existing debt** (new credit still requires Active)
- Overpayment blocked inside a serializable transaction (`pos.repayment.exceeds_outstanding`, `pos.repayment.outstanding_zero`)
- Credit reversal blocked when it would make outstanding negative
- Unified chronological ledger read model (credits ∪ repayments; no persisted ledger table)
- Derived outstanding = active credits − active repayments
- Migration `AddPosRepayments` → `pos.repayments`
- POS API repayment/ledger/utang-summary routes; MAUI payment + ledger pages
- Phase marker `P6-WP03-payments-and-ledger`

## Inactive-customer policy

Customer must exist in the same organization. **Repayments are allowed while Inactive** so existing debt can be reduced. Creating new credit still requires Active (P6-WP02 rule unchanged).

## Overpayment policy

An active repayment must not make outstanding negative. Validation uses the current derived balance inside a serializable transaction. Exact-balance repayment is allowed. Zero outstanding rejects new repayments. No wallet, change, or refund handling.

## Unified ledger and outstanding

Read-only chronological union of credit entries and repayments ordered by `RecordedAtUtc`, then entry ID. Running balance is computed from signed active effects. Reversed rows remain queryable with no effect on current outstanding.

## Explicit exclusions

Due dates, statements, printable receipts, trial-expiry behavior, interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, offline sync. No Platform/HealthCare tables or cross-database FKs.

## Persistence and migration

- Database: `ExItS_PinoyBusinessPOS` / schema `pos` / table `repayments`
- Columns: id, organization_id, customer_id, amount, remarks, status, recorded_at_utc, recorded_by, reversed_at_utc, reversal_reason, reversed_by, xmin
- Checks: status, amount > 0, reversal consistency
- FK to `pos.customers` (Restrict)
- Migration apply / rollback-to-`AddPosCreditEntries` / re-apply validated in Testcontainers

## API capability

| Method | Route |
|---|---|
| GET | `/api/v1/pos/customers/{customerId}/utang-summary` |
| GET | `/api/v1/pos/customers/{customerId}/ledger` |
| GET | `/api/v1/pos/customers/{customerId}/repayments` |
| POST | `/api/v1/pos/customers/{customerId}/repayments` |
| GET | `/api/v1/pos/repayments/{repaymentId}` |
| POST | `/api/v1/pos/repayments/{repaymentId}/reverse` |

Organization scope via `X-Pos-Organization-Id`. Actor via `X-Dev-Platform-User-Id` for record/reverse. Development-stage only.

## MAUI experience

Customer detail uses utang summary, Record payment, Open ledger. Routes: `/customers/{id}/ledger`, `/customers/{id}/repayments/new`, `/customers/{id}/repayments/{repaymentId}`. EN + `fil-PH`. Deferred due-date/statement/receipt messaging.

## Organization isolation

All queries/commands filter by organization. Cross-organization access returns 404. RecordedBy/ReversedBy are Platform user GUID references only (no cross-DB FK).

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Platform unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 28 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 27 | 0 | 0 |
| POS unit | 19 | 0 | 0 |
| POS integration | 8 | 0 | 0 |
| Platform integration | 84 | 0 | 0 |
| **Total** | **512** | **0** | **0** |

Baseline preserved: 505 → 512. Release build: 0 warnings, 0 errors.

Android Release APK: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`. No interactive emulator/device — **R-109 remains open**.

## Security limitations

- POS APIs trust organization/actor headers without production authentication
- Development/Testing Platform identity remains the only MAUI auth path
- Not production-secure

## HealthCare freeze

`git ls-files -- HealthCare/` empty; ignored via `.gitignore`; not in `ExItS.slnx`.

## Risks and open decisions

- R-109: no interactive Android emulator validation
- R-124: POS org header mistaken for production authz
- R-127: derived outstanding mistaken for stored balance
- R-128: repayment actor header (`X-Dev-Platform-User-Id`) mistaken for production audit identity
- OD-07 / OD-08 remain open

## Files / docs changed

POS Domain/Application/Infrastructure/Api/ApiClient/Maui repayment + ledger slice; phase-06; portfolio; README; FILE-MANIFEST; engineering docs; risks; release-plan; this report.

## Git evidence

- Feature commit: `de39091f6110acbc721ac78da51a92acefd6775a`
- Docs commit: _(recorded after docs commit)_
- Phase marker: `P6-WP03-payments-and-ledger`
- Pushed to `origin/main`; local and remote `main` match; working tree clean

## Exact next work package

**P6-WP04 — Due Dates and Overdue Monitoring** (not started — do not begin until explicitly authorized)
