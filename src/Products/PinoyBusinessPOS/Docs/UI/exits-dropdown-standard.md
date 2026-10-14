# ExItS Dropdown Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Dropdown**

**Locked global command:** the Dropdown playground on `/ui-standards` → **Dropdown**. New dropdowns are requested with `ext dropdown`.

This standard evolves with the Dropdown playground. Do **not** pin it permanently to a commit SHA.

Locking this standard does **not** restyle the product `ExitsSelect`. Existing selects stay in place until a control is explicitly migrated.

## Command

Defaults are omitted. `ext dropdown` is Standard intent, Standard appearance, Standard shape, Standard density, Expand transition, single select, enabled.

```
ext dropdown [intent] [appearance] [shape] [density] [transition] [multi] [disabled]
```

Example: `ext dropdown primary outlined rounded comfort scale multi`

Transition tokens are `flip`, `scale`, and `slide`. Expand is the default and is omitted. Rounded shape cannot use Standard appearance.

## Samples

Standard, Soft, Soft outline, Filled, Outlined, and Standout. The field is the search box when the menu is open. Multi select uses checkboxes and an All row.
