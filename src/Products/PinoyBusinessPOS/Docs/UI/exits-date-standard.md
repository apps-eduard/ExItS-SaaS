# ExItS Date Picker Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Date picker**

**Locked global command:** the Date picker playground on `/ui-standards` → **Date picker**. New date pickers are requested with `ext date`. The preview input has its own command, `ext date input`.

This standard evolves with the Date picker playground. Do **not** pin it permanently to a commit SHA.

## Commands

Defaults are omitted.

```
ext date [intent] [appearance] [roll] [today] [range] [disabled]
ext date input [intent] [input appearance] [transition]
ext date time [intent] [input appearance] [transition] [24h]
```

Examples: `ext date primary raised roll today range` and `ext date input outlined flip`.

Calendar is the default variety. Expand is the default popup transition and is omitted. Standard input appearance is omitted.

## Samples

Standard, Raised, Gradient, Flat, Roll, Today, Range, and Disabled. Each row filters intents, and the default selection is Standard, Primary, and Secondary. The preview input opens the calendar and closes after a finished date selection. In range mode it stays open until the end date is chosen. PREVIEW DATE AND TIME is an input for both values. It shows the time with AM/PM, and the 24-hour switch shows `09:30` instead. It opens the calendar and the clock together, and closes after the user has chosen a date and a time. Its command is `ext date time`.
