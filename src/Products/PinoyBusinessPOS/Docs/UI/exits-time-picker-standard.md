# ExItS Time Picker

**Status:** PILOT
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Time picker**

The Time picker playground is a pilot. Request it with `ext time`. The preview input has its own command, `ext time input`.

## Commands

Defaults are omitted. `ext time` is Standard intent, Standard appearance, Clock variety, 12-hour, no Now button, single time `09:30`, enabled.

```
ext time [intent] [appearance] [roll] [24h] [now] [range] [disabled]
ext time input [intent] [input appearance] [transition]
```

Example: `ext time 24h now`

## Samples

Standard, Raised, Gradient, Flat, Roll, 24-hour, Now, Range, and Disabled. The 24-hour switch sits on the time picker. When it is on, the clock shows 00–23 only, AM/PM is removed, and the preview input shows `09:30`. The preview input keeps a clock icon, and shows a clear icon when it has a time. The clock face is an outline. Drag or click the face to choose any minute, including 1 and 2; the labeled marks stay on every fifth minute. The preview input closes after that minute is chosen. In range mode it stays open until the end time is chosen. Now uses the fixed showcase time `09:30`.
