# ExItS QuantityStepper

When adding or changing quantity / ± qty UI in PinoyBusinessPOS React:

1. Use the global `QuantityStepper` from `@/components/exits/MoneyQuantity`.
2. **Standard / default:** `variant="outline"` (surface capsule; primary border; follows Preferences → Control Shape).
3. **Solid filled:** `variant="auto"` (primary capsule).
4. **White center:** `variant="field"` (primary capsule ends; surface/white qty like an input).
5. Prefer controlled `onChange` with `min` / `step` / `precision` (or `unitOfMeasure` + `sellingMode`).
6. Do not add a raw `<input type="number">` plus a separate ± control.
7. Use `editOnClick` for compact cart/line middle-tap-to-edit.
8. Pass `variant="default"` only when the legacy form field group chrome is intentionally required.

Authoritative: `.cursor/rules/exits-ui-architecture.mdc`, `/ui-standards` → Global Preference Preview.
