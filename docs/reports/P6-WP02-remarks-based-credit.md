# P6-WP02 — Remarks-Based Credit

Phase marker: `P6-WP02-remarks-based-credit`

## Status

**Complete with documented risks.** Organization-owned remarks-based customer credit entries with derived outstanding and explicit reversal. Repayments, payment allocation, full payment ledger, due dates, statements, receipts, interest, penalties, credit limits, sales, inventory, gateways, QR/cards, and offline sync remain excluded (later Phase 6 WPs). Platform SaaS payments remain separate. OD-07 and OD-08 remained open at delivery of this WP; later resolved in P6-WP05. **P6-WP03 was not started.**

## Delivered capability

- Domain `CreditEntry` / `CreditEntryId` / `CreditEntryStatus` (Active | Reversed)
- Invariants: positive decimal amount (max 2 decimal places), required trimmed remarks, UTC timestamps, active customer required to create
- Append-only history: amount/remarks immutable after create; no edit/delete of recorded credit
- Explicit reversal with required reason (Active → Reversed only)
- Derived outstanding = sum of **active** credit entry amounts only (not a stored customer balance)
- Customer credit summary (outstanding, active count, total count) and paginated history
- Migration `AddPosCreditEntries` on `ExItS_PinoyBusinessPOS` / schema `pos` / table `credit_entries`
- POS API credit routes under `/api/v1/pos/customers/{customerId}/…` with `X-Pos-Organization-Id` (404 fail-closed across orgs)
- MAUI customer detail: outstanding, history, add credit, reverse; `/customers/{id}/credit/new`
- English + Tagalog credit strings; repayments deferred messaging
- Phase marker `P6-WP02-remarks-based-credit`

## Explicit exclusions

Repayments, payment allocation, full payment ledger, due dates, statements, receipts, interest, penalties, credit limits, sales, inventory, gateways, QR/cards, offline sync. No Platform/HealthCare tables or cross-database FKs. HealthCare remains frozen.

## Persistence and migration

- Database: `ExItS_PinoyBusinessPOS` / schema `pos` / table `credit_entries`
- Columns: id, organization_id, customer_id, amount (numeric 18,2), remarks, status, created_at_utc, reversed_at_utc, reversal_reason, xmin
- Checks: status ∈ {Active, Reversed}, amount > 0
- FK `fk_credit_entries_customers` → `pos.customers` (Restrict)
- Indexes: org+customer+created, org+customer+status
- Migration apply / rollback-to-`AddPosCustomers` / re-apply validated in Testcontainers

## API capability

| Method | Route |
|---|---|
| GET | `/api/v1/pos/customers/{customerId}/credit-summary` |
| GET | `/api/v1/pos/customers/{customerId}/credit-entries` |
| POST | `/api/v1/pos/customers/{customerId}/credit-entries` |
| GET | `/api/v1/pos/customers/{customerId}/credit-entries/{entryId}` |
| POST | `/api/v1/pos/customers/{customerId}/credit-entries/{entryId}/reverse` |

Organization scope via `X-Pos-Organization-Id`. Development-stage only (no production JWT). Typed DTOs + ProblemDetails `errorCode`.

## MAUI experience

Customer detail shows outstanding (`MoneyDisplay`), active/total counts, add-credit (active customers), append-only history with reverse-reason dialog. Create page at `/customers/{id}/credit/new`. Repayments/ledger deferred banners. Light/Dark and Compact/Comfortable unchanged.

## Organization isolation

All credit queries/commands filter by organization id from header/session and customer id. Cross-organization summary/create returns 404. Same POS DB FK to customers; no Platform cross-DB FK.

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Platform unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 28 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 27 | 0 | 0 |
| POS unit | 14 | 0 | 0 |
| POS integration | 6 | 0 | 0 |
| Platform integration | 84 | 0 | 0 |
| **Total** | **505** | **0** | **0** |

Baseline preserved: 497 → 505 (+8 focused credit tests). Release build: 0 warnings, 0 errors.

Android Release APK: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk` (also unsigned `.apk` and `publish/` copies). No interactive emulator/device attached (`adb` unavailable) — **R-109 remains open**.

## Security limitations

- POS credit API trusts `X-Pos-Organization-Id` without production authentication
- Development/Testing Platform identity remains the only auth path for MAUI
- Not production-secure

## HealthCare freeze

`git ls-files -- HealthCare/` empty; `git check-ignore -v HealthCare/` → `.gitignore:/HealthCare/`; HealthCare not in `ExItS.slnx`.

## Risks and open decisions

- R-109: no interactive Android emulator validation
- R-124: POS org header mistaken for production authz
- R-125: customer notes must still not be treated as credit (credit uses dedicated entries)
- R-127: outstanding is derived from active entries only — must not be mistaken for a mutable stored balance or repayment ledger
- Later resolved in P6-WP05 (see that report). Remained open at delivery of this WP.

## Files / docs changed

POS Domain/Application/Infrastructure/Api/ApiClient/Maui credit slice; phase-06 roadmap; portfolio; README; FILE-MANIFEST; engineering docs (data ownership, security, authorization, localization, testing); risks; release-plan; this report.

## Git evidence

- Feature commit: `ead6942187ca9a9c507dcf706bbece2e507a8645`
- Docs commit: `d76eb3574fadae0e8358109994ba8d750acbb4ad`
- Phase marker: `P6-WP02-remarks-based-credit`
- Pushed to `origin/main`; local and remote `main` match; working tree clean

## Exact next work package

**P6-WP03 — Payments and Ledger** (not started — do not begin until explicitly authorized)
