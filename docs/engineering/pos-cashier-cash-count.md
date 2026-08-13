# POS configurable cashier cash count

Organization-level cash counting for PinoyBusinessPOS cashier shifts. Extends the existing shift aggregate; it is not a separate cash-control subsystem.

## Audit result

Existing `CashierShift` already had:

- Opening float (`OpeningCashAmount`, always stored, `0` allowed)
- Closing declaration (`ClosingCashAmount`, nullable in storage but previously always required by `Close`)
- Canonical expected cash: Opening + NetCashSales + CashIn − CashOut − CashRefundsOnShift (`CashierShiftExpectedCash`)
- Variance = counted − expected when a close amount was supplied
- Cash in/out movements
- Organization POS settings in `PosOperationalSetup` (one row per org)
- Online-only MAUI shift open/close; close of an already-closed shift is idempotent

Gaps: no Off/Optional/Required policy, opening was always a required float, close always required counted cash, skipped count could not be persisted as null, Shift Detail coalesced null counted cash to PHP 0, and org setting changes would have applied immediately to an open shift.

Denomination counting does not exist and is not added here. Cashiers enter one total counted amount.

## CashCountMode

Authoritative server/domain value on `PosOperationalSetup.CashCountMode`.

| Mode | Default | Opening | Closing |
|---|---|---|---|
| Off | — | No physical count prompt. Shift opens without counted opening cash. | Shift closes without counted cash. |
| Optional | **New organizations** | Cashier may enter opening cash or skip. | Cashier may enter counted cash or skip. |
| Required | Existing completed stores (migration backfill) | Opening count required before the shift becomes active. | Counted cash required before close. Server-enforced. |

Default for new / incomplete organizations: **Optional**.

Off does **not** disable sales, payments, expected-cash calculation, reporting, or audit records.

## Expected cash

Unchanged canonical formula:

```text
Opening cash
+ Net cash sales
+ Cash in
− Cash out
− Cash refunds on shift
= Expected closing cash
```

Expected cash is always computed and snapshotted on close, including Off and skipped Optional closes.

## Counted cash and variance

When a physical count is supplied:

```text
Variance = CountedCash − ExpectedCash
```

| Variance | Classification |
|---|---|
| 0 | Balanced |
| > 0 | Over |
| < 0 | Short |

Skipped count persists as `ClosingCashAmount = null` and `CashVarianceAmount = null`. It is **not** stored as `0` and is **not** fabricated as `Counted = Expected`. Counted zero remains distinct from not counted.

Variance is an auditable fact. Transactions are not mutated to force balance. No auto-created adjustments.

## Active-shift snapshot

`CashierShift.EffectiveCashCountMode` is captured at open from the current organization setting.

If an admin later changes `PosOperationalSetup.CashCountMode`, the open shift keeps its snapshotted rule. The next shift uses the updated mode.

`OpeningCashCounted` records whether opening cash was physically counted. Uncounted opening float is stored as `0` for expected-cash arithmetic only.

## Offline

MAUI shift open/close remains **online-only** (existing P10-WP04 gate). The snapshotted mode lives on the shift row, so an already-open shift does not need a live settings read to know whether close requires a count.

Close of an already-closed shift remains a success no-op (idempotent retry; does not duplicate close or overwrite counted amounts). Cash movements keep existing `Idempotency-Key` / client `MovementId` behavior.

`CashierShiftRepository.UpdateAsync` now calls `SaveChangesAsync` so close/cancel persist before the next request (open of the next shift, reports).

## Authorization

- Change `CashCountMode`: `ManageOperationalSetup` (organization owner/admin).
- Perform cash count / open / close: existing `ManageShifts`.
- Organization id is taken from the authorized request scope, never trusted from the client body.
- Personal users have no organization scope for these APIs (`OrganizationRequired`).
- Cross-organization updates cannot change another org's setting.

## Reporting

Shift summary and cash-variance reports distinguish:

- `NotRequired` (Off, no count)
- `NotPerformed` (Optional skip)
- `Counted` (amount present)

`TotalCashVariance` still sums only rows where `CashVarianceAmount` is not null. Skipped counts are not treated as PHP 0.

## Migration

`AddPosCashierCashCountMode`:

- `operational_setups.cash_count_mode` default `Optional`; completed existing rows backfilled to `Required`
- `cashier_shifts.effective_cash_count_mode` default `Required` (legacy shifts always counted)
- `cashier_shifts.opening_cash_counted` default `true`
- Close consistency check allows closed shifts with null counted cash when variance is also null

No duplicate opening/closing amount columns.

## Owner acceptance

See the checklist in this document's companion report index row. Device Verified is **No** until the owner validates on a physical device.

### OPTIONAL MODE

1. Organization sets Cash Count = Optional.
2. Cashier opens shift without entering opening cash.
3. Confirm shift opens.
4. Make cash sales.
5. Close shift.
6. Confirm expected cash appears.
7. Skip counted cash.
8. Confirm shift closes.
9. Confirm history shows "Not counted" rather than PHP 0.

### OPTIONAL WITH COUNT

1. Open shift with PHP 1,000 opening cash.
2. Make PHP 2,000 cash sales.
3. Expected closing cash should reflect PHP 3,000, subject to existing cash movement rules.
4. Enter counted cash PHP 2,950.
5. Close.
6. Confirm Short = PHP 50.
7. Confirm transactions remain unchanged.

### REQUIRED MODE

1. Set Cash Count = Required.
2. Attempt open without required opening count.
3. Confirm rejected if opening count is required by existing workflow.
4. Open properly.
5. Make sales.
6. Attempt close without counted cash.
7. Confirm rejected.
8. Enter counted cash.
9. Confirm close succeeds.
10. Confirm variance displayed correctly.

### OFF MODE

1. Set Cash Count = Off.
2. Open shift.
3. Confirm no cash-count prompt blocks opening.
4. Make sales.
5. Close shift.
6. Confirm no physical count required.
7. Confirm expected cash/reporting still exists where applicable.

### SETTING SNAPSHOT

1. Open shift while Optional.
2. Change org setting to Required.
3. Confirm current shift still behaves Optional.
4. Open next shift.
5. Confirm next shift behaves Required.
