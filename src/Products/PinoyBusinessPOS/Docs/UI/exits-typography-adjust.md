# ExItS Typography Adjust

**Status:** APPROVED / LOCKED
**Scope:** PinoyBusinessPOS React UI
**Visual authority:** `/ui-standards` → **Typography**

Cursor must treat a named target plus `fw`, `fz`, or `fwz` as a complete instruction.

## Command

```
<target> fw+n | fw-n | fz+n | fz-n | fwz+n | fwz-n
```

Case does not matter.

| Say this | Do this |
| --- | --- |
| `table header fw+10` | Raise the table header font-weight by 10% of its current weight |
| `table header fz+10` | Raise the table header font-size by 10% and keep the unit |
| `table header fwz+10` | Raise the table header font-weight and font-size by 10% together |
| `table header fw+10 fw+10` | One step of `fw+20` from the original current weight |

## Font-weight

`fw+n` increases the current weight by n% of itself. `fw-n` decreases it.

- If the absolute change is smaller than 10, use 10.
- Round the result to the nearest integer.
- Keep the result between 100 and 900.

At weight 400, `fw+1` becomes 410. At weight 500, `fw+10` becomes 550.

## Font-size

`fz+n` and `fz-n` are a pure percentage of the current size. Keep decimals. Keep the unit (`px`, `rem`, `em`).

At `14px`, `fz+1` becomes `14.14px`.

## Both

`fwz+n` and `fwz-n` apply that same percent to font-weight and font-size in one step. Weight still uses the minimum step, rounding, and 100–900 clamp. Size stays a pure percentage with its unit.

## Implementation

Use `adjustFontWeight` and `adjustFontSize` in `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/ui-standards/relative-typography.ts`.

Change only the named target. Do not retune a shared token that other targets use.
