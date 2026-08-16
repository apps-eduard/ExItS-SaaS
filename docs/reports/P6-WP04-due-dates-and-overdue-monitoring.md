# P6-WP04 — Due Dates and Overdue Monitoring

Phase marker: `P6-WP04-due-dates-and-overdue-monitoring`

## Status

**Complete with documented risks.** Organization-owned optional due dates on credit entries with append-only change history and derived overdue monitoring via FIFO aging. Outstanding formula remains active credits − active repayments. Interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, offline sync, and payment-allocation persistence remain excluded. Platform SaaS payments remain separate. OD-07 and OD-08 were later resolved in P6-WP05. **P6-WP05 was not started in this WP.**

## Delivered capability

- Optional `DateOnly` calendar due date per credit entry (`CurrentDueDate` denormalized on `credit_entries`)
- Append-only due-date change history (`CreditDueDateChange` / `credit_due_date_changes`) with required reason, actor, and UTC timestamp
- Set / change / clear due date; reversed credits cannot receive due dates
- FIFO aging read model: active repayments applied to active credits by `CreatedAtUtc` ASC then `Id` ASC; no persisted allocations
- Overdue derived: Active + due date before effective business date + remaining FIFO unpaid > 0
- Customer overdue summary; org overdue customer/credit lists; aged-credits listing with filters
- Migration `AddPosCreditDueDates`
- POS API due-date/overdue routes; MAUI overdue + credit due-date workflows; EN + `fil-PH`
- Phase marker `P6-WP04-due-dates-and-overdue-monitoring`

## Due-date ownership and history

Due dates are owned by the credit entry inside the POS organization. The current value is denormalized on the credit for efficient reads. Every set, change, or clear appends a history row (previous → new, reason, ChangedBy Platform user GUID, ChangedAtUtc). History is not editable or deletable. Amount, remarks, and credit create timestamps are unchanged by due-date mutations.

## Date semantics

- Stored due dates are `DateOnly` calendar dates (PostgreSQL `date`), not timestamps.
- Effective business date = server UTC calendar date (`DateOnly` from UTC now).
- Organization local timezone is **not** defined — documented limitation until a later decision.
- Past due dates are allowed; unpaid active credits with a past due date are overdue immediately under the rules below.

## FIFO aging (read model only)

Active repayments are applied to active credits ordered by `CreatedAtUtc` ASC, then `Id` ASC. Remaining unpaid per credit is computed in memory. No allocation rows are persisted. Customer outstanding remains **active credits − active repayments** (unchanged from P6-WP03).

## Overdue rules

A credit is overdue when all of:

1. Status is Active
2. Current due date is set and is strictly before the effective business date
3. Remaining FIFO unpaid amount > 0

Reversed credits are never overdue. Fully FIFO-offset credits are not overdue (status Paid). Credits without a due date are not overdue. DueToday / DueSoon / Upcoming are distinct display statuses and are not overdue.

## Explicit exclusions

Statements, printable receipts, trial-expiry rules, interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, offline sync, payment-allocation persistence. No Platform tables or cross-database foreign keys.

## Persistence and migration

- Database: `ExItS_PinoyBusinessPOS` / schema `pos`
- Column: `credit_entries.current_due_date` (`date`, nullable)
- Table: `credit_due_date_changes` (id, organization_id, credit_entry_id, customer_id, previous_due_date, new_due_date, reason, changed_by, changed_at_utc)
- FKs to `credit_entries` and `customers` (Restrict)
- Indexes: org + current_due_date; org + changed_at; org + credit + changed
- Migration `AddPosCreditDueDates`; rollback target `AddPosRepayments`; apply / rollback / re-apply validated in Testcontainers

## API capability

| Method | Route |
|---|---|
| PUT | `/api/v1/pos/credit/{creditEntryId}/due-date` |
| DELETE | `/api/v1/pos/credit/{creditEntryId}/due-date` |
| GET | `/api/v1/pos/credit/{creditEntryId}/due-date-history` |
| GET | `/api/v1/pos/customers/{customerId}/overdue-summary` |
| GET | `/api/v1/pos/customers/{customerId}/aged-credits` |
| GET | `/api/v1/pos/overdue/customers` |
| GET | `/api/v1/pos/overdue/credits` |

Organization scope via `X-Pos-Organization-Id`. Actor via `X-Dev-Platform-User-Id` for set/clear. Development-stage only.

## MAUI experience

Routes: `/overdue`, `/customers/{id}/overdue`, credit detail due-date set/clear + history. Org and customer overdue lists with status badges and filters. Customer detail surfaces overdue summary counts. EN + `fil-PH` (`DueDate_*`, `Overdue_*`). Statements/receipts remain deferred messaging.

## Organization isolation

All queries/commands filter by organization. Cross-organization access returns 404. ChangedBy is a Platform user GUID reference only (no cross-DB FK).

## Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Platform unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 28 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 27 | 0 | 0 |
| POS unit | 26 | 0 | 0 |
| POS integration | 10 | 0 | 0 |
| Platform integration | 84 | 0 | 0 |
| **Total** | **521** | **0** | **0** |

Baseline preserved: 512 → 521. Release build: 0 warnings, 0 errors.

Android Release APK path unchanged under MAUI `bin/Release/net10.0-android/`. No interactive emulator/device — **R-109 remains open**.

## Security limitations

- POS APIs trust organization/actor headers without production authentication
- Development/Testing Platform identity remains the only MAUI auth path
- Not production-secure
- Effective business date uses server UTC calendar day only (no org timezone)

## Portfolio independence

No unauthorized nested product tree is tracked; ignored via `.gitignore`; not in `ExItS.slnx`.

## Risks and open decisions

- R-109: no interactive Android emulator validation
- R-124: POS org header mistaken for production authz
- R-127: derived outstanding / aging mistaken for stored balance or allocations
- R-128: repayment/due-date actor header mistaken for production audit identity
- Org timezone undefined: effective business date is server UTC calendar day only
- Later resolved in P6-WP05 (see that report). Remained open at delivery of this WP.

## Files / docs changed

POS Domain/Application/Infrastructure/Api/ApiClient/Maui due-date + overdue slice; phase-06; portfolio; README; FILE-MANIFEST; engineering docs; risks; release-plan; this report.

## Git evidence

- Feature commit: `9947d95cba27c8311091f95ea51c79be1de0acb9`
- Docs commit: `345bfdd1f98d953b2f6c54eb454a4a28d0f17ef5`
- Phase marker: `P6-WP04-due-dates-and-overdue-monitoring`
- Hashes recorded after authorized commit/push; portfolio independence verification re-verified at that time

## Exact next work package

**P6-WP05 — Statements, Receipts and Trial Rules** (delivered separately — see that report)
