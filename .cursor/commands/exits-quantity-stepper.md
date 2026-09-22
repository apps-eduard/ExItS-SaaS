# ExItS QuantityStepper

When adding or changing quantity / ± qty UI in PinoyBusinessPOS React:

1. Use the global `QuantityStepper` from `@/components/exits/MoneyQuantity`.
2. **Default:** `variant="auto"` (primary capsule; follows Preferences → Control Shape).
3. Prefer controlled `onChange` with `min` / `step` / `precision` (or `unitOfMeasure` + `sellingMode`).
4. Do not add a raw `<input type="number">` plus a separate ± control.
5. Use `editOnClick` for compact cart/line middle-tap-to-edit.
6. Pass `variant="default"` only when the form field group chrome is intentionally required.

Authoritative: `.cursor/rules/exits-ui-architecture.mdc`, `/ui-standards` → Form controls.
