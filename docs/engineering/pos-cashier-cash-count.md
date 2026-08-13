# POS cashier cash count and denomination-assisted reconciliation

Organization-level cash counting for PinoyBusinessPOS cashier shifts. Extends the existing shift aggregate; it is not a separate cash-control subsystem.

PinoyBusinessPOS is currently **PHP-authoritative** (`PosOperationalSetup.CurrencyCode` defaults to `PHP`). Denomination defaults are Philippine values. Owners can add future values (for example `5000`) from settings without a code deployment.

## Active CashCountMode policies

Configurable policies:

| Mode | Default for new organizations | Opening | Closing |
|---|---|---|---|
| **Required** | **Yes** | Cashier must enter the total physical cash in the drawer before the shift activates. | Cashier must enter the total physical cash before close. Server-enforced. |
| **Optional** | No | Cashier may enter a total or skip. | Cashier may enter a total or skip. |

Skipped remains `null` / Not counted. Never convert skipped to PHP 0. A genuine count of zero remains distinct from skip.

`Off` is **not selectable**. It remains on the enum only so historical `CashierShift.EffectiveCashCountMode` snapshots can still be read. Migration `AddPosCashDenominationsAndRequiredDefault` converts leftover `operational_setups.cash_count_mode = Off` to `Optional`. New opens treat any leftover org Off as Optional (`CashCountModes.ForNewShift`). API `ParseConfigurable` rejects new attempts to set Off.

## Authorization

Only an authorized organization owner/admin with `ManageOperationalSetup` may change Cash Count Policy. Server-enforced on `PUT /api/v1/pos/operational-setup` and `PUT /api/v1/pos/operational-setup/cash-denominations`.

Cashiers with `ManageShifts` can perform the count but cannot change the policy unless they also have `ManageOperationalSetup`.

Personal users cannot change it (`OrganizationRequired`). Cross-organization updates are rejected.

The same organization-level setting is presented in:

- Organization Web → Settings → Cash handling → Cash Count Policy
- MAUI → Operational setup → Cash Count Policy

Changes apply to the **next** shift. An already-open shift keeps `EffectiveCashCountMode` captured at open.

## Authoritative cash count

The authoritative physical cash values are `OpeningCashAmount` / `OpeningCashCounted` and `ClosingCashAmount`.

Denomination assistance is optional. Required means the **total** is required, not the breakdown.

When a breakdown is supplied, the server recalculates `sum(DenominationValue * Quantity)` and rejects a mismatch with the submitted total. The calculator total is copied into the authoritative cash-count amount. Breakdown never replaces it, never changes sales, expected cash, cash in/out, refunds, or variance.

Expected cash remains:

```text
Opening cash
+ Net cash sales (physical cash only)
+ Cash in
− Cash out
− Cash refunds
= Expected closing cash
```

Variance = Counted cash − Expected cash.

GCash / ManualGCash and Utang are **not** physical drawer cash and are excluded from denomination counting and net cash sales.

## Optional denomination helper

MAUI shift opening and closing show a money icon beside the cash amount. Tapping it opens **Denomination breakdown** (not labeled “Cash Count”). Cashiers may type the total or use the helper. Disabled denominations are not offered. Cashiers cannot invent denominations during a count.

Entry method is implied: breakdown present = denomination-assisted; absent = manual total.

## Denomination configuration

`OrganizationCashDenomination`: `Id`, `OrganizationId`, `Value`, `DisplayLabel`, `IsEnabled`, `SortOrder`.

PHP defaults seeded **only when an organization has no denomination rows** (idempotent; repeated setup does not duplicate). Fresh default set:

PHP 1000, 500, 200, 100, 50, 20, 10, 5, 1, **0.25**, **0.05**, **0.01**.

PHP **0.50** and PHP **0.10** are **not** part of this current default. Owners may still add `0.50`, `0.10`, `5000`, or another custom value from settings without a code change. Existing organization configurations are **not** rewritten: custom rows and previously seeded lists stay as stored. Historical shift breakdowns are untouched.

Values are `decimal` money (`numeric(18,2)` / `SaleMoney`, 2 dp AwayFromZero). Centavos calculate exactly (`0.25 × 3 = 0.75`, `0.05 × 3 = 0.15`, `0.01 × 5 = 0.05`).

Full administration is on Organization Web; MAUI owner/admin can enable/disable and add values. Cashiers count with configured denominations; they cannot change organization setup.

Using the helper copies the calculated total into authoritative Opening / Closing Cash on Hand. Denomination UI/UX is preserved (no layout redesign in this refinement).

Historical `cashier_shift_cash_count_lines` snapshot `DenominationValue` and `Quantity` for Opening or Closing. Later config changes do not rewrite history.

## Closing reconciliation

MAUI close entry does not show expected cash before the cashier submits a count (historical Off snapshots still skip the count prompt). After count review and after close, show Opening, Cash Sales, Cash In, Cash Out, Cash Refunds, Expected, Counted, Variance, Balanced / Over / Short. Optional “View denomination breakdown”.

## Offline

MAUI shift open/close remain **online-only**. Denomination quantities are local UI state until the online open/close call. No new offline sync subsystem.

## Migration

`AddPosCashDenominationsAndRequiredDefault` (`20260813153741`):

- `operational_setups.cash_count_mode` default **Required**; leftover **Off → Optional**; check constraint `IN ('Optional', 'Required')`
- `organization_cash_denominations` (unique org+value)
- `cashier_shift_cash_count_lines` (unique shift+kind+value); historical Off on `cashier_shifts.effective_cash_count_mode` unchanged

Existing Required remains Required. Existing Optional remains Optional.

No additional migration for centavo defaults: `value` is already `numeric(18,2)`. Application seed list change only; existing `organization_cash_denominations` rows are not rewritten.

## Tests

Covered in POS unit, integration, MAUI UI guards, and Org Web settings guards: new-org Required default; owner set Required/Optional from Org Web and MAUI; cashier/Personal/cross-org rejected; shift snapshot; Off retired for new configuration; denomination seed/add/5000/duplicates/zero/negative; centavo line totals (`0.25`, `0.05`, `0.01`); default set excludes `0.50`; custom `0.50`/`0.10`/`5000` still allowed; opening/closing denomination total becomes Cash on Hand; disabled not offered; manual vs assisted; server recalculate; mismatch rejected; repeated close does not duplicate lines; cash/GCash/Utang semantics; Required/Optional skip vs zero.

## Owner acceptance

Device Verified is **No** until the owner validates on a physical device. Browser Verified is **No** until the owner confirms Org Web.

### A. Policy default

1. Create a new organization.
2. Confirm Cash Count = Required.

### B. Policy web

3. Open Org Web.
4. Settings → Cash handling.
5. Change Required → Optional.
6. Save/reload.
7. Confirm persisted.

### C. Policy mobile

8. Login MAUI as authorized owner/admin.
9. Change Optional → Required.
10. Confirm the same server setting updates.
11. Confirm a cashier-only account cannot change it.

### D. Required opening manual

12. Open a new shift.
13. Enter PHP 1,000 manually.
14. Open shift.
15. Confirm success.

### E. Required opening denomination

16. Open the next shift.
17. Tap the money icon.
18. Enter quantities.
19. Confirm calculated total.
20. Tap Use Total.
21. Confirm the cash count field receives the total.
22. Open shift.

### F. Custom denomination

23. Add denomination 5000 as owner/admin.
24. Open the denomination helper.
25. Confirm 5000 appears without a code change.

### G. Closing denomination

26. Make cash sales.
27. Close shift.
28. Tap denomination helper.
29. Count bills/coins.
30. Submit.
31. Confirm expected vs counted.
32. Confirm Balanced / Over / Short.
33. View denomination breakdown.

### H. Optional

34. Set Cash Count Optional.
35. Open a new shift and skip count.
36. Close and skip count.
37. Confirm Not counted, not PHP 0.

### I. Snapshot

38. Open a shift under Required.
39. Change organization policy to Optional.
40. Confirm the current shift still requires a count.
41. Open the next shift.
42. Confirm Optional applies.
