# ExItS Dropdown Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Dropdown**

**Locked global command:** `ext dropdown primary outlined`. New dropdowns use Primary intent and Outlined appearance. Shape and density stay Standard unless named.

This standard evolves with the Dropdown playground. Do **not** pin it permanently to a commit SHA.

Locking this standard does **not** restyle the product `ExitsSelect`. Existing selects stay in place until a control is explicitly migrated.

## Command

Defaults are omitted. `ext dropdown` is Standard intent, Standard appearance, Standard shape, Standard density, Scale transition, single select, enabled.

The playground opens on the locked global default, `ext dropdown primary outlined`. Reset returns to that lock.

```
ext dropdown [intent] [appearance] [shape] [density] [transition] [multi] [disabled]
```

Example: `ext dropdown primary outlined rounded comfort flip-up multi`

Transition tokens are `flip-up`, `flip-down`, and `jump-up`. Scale is the default and is omitted. Rounded shape cannot use Standard appearance.

## Samples

Standard, Soft, Soft outline, Filled, Outlined, and Standout. The field is the search box when the menu is open. Multi select uses checkboxes and an All row.
