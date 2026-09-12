# ExItS Form Field Focus Standard

**Status:** STANDARD / CANONICAL FOUNDATION  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Applies to:** text / number / currency / date / select / textarea form controls (`.exits-input`, `.exits-select`, shared `Input`, native date/time, table stealth editors)  
**Does not apply to:** Buttons, chips, tabs, nav, Search Field layout (Search may share tokens)

Visual baseline matches the restrained Search Field focus language — **not** thick `ring-2` / `outline: 2px` + offset.

---

## Tokens

| Token | Role |
|-------|------|
| `--exits-field-border` | Normal border |
| `--exits-field-border-hover` | Hover border |
| `--exits-field-border-focus` | Focus border → `--exits-primary` |
| `--exits-field-focus-ring` | Soft outer ring (Primary @ 28%) |
| `--exits-field-focus-ring-width` | `1px` |
| `--exits-field-border-error` | Invalid → `--exits-danger` |
| `--exits-field-focus-ring-error` | Invalid focus soft ring |
| `--exits-field-radius` | Field radius (independent of Control Shape) |

---

## Canonical recipes

### Normal focus

```
border-color: var(--exits-field-border-focus);
box-shadow: 0 0 0 var(--exits-field-focus-ring-width) var(--exits-field-focus-ring);
outline: none;
```

### Error focus (precedence over Primary)

```
border-color: var(--exits-field-border-error);
box-shadow: 0 0 0 var(--exits-field-focus-ring-width) var(--exits-field-focus-ring-error);
outline: none;
```

Invalid markers: `[aria-invalid="true"]` and `.border-destructive`.

---

## Rules

1. **1px Primary border + ~1px Primary-soft outer ring** — no `ring-2`, no `outline: 2px` + offset, no double focus.
2. Focus must **not** change control width/height or shift layout (border width stays 1px; ring is box-shadow only).
3. Form fields keep `--exits-field-radius` under Pill Control Shape.
4. Focus follows **current Primary** palette (no hardcoded Blue).
5. Mouse/touch focus into fields remains visible (`:focus` as well as `:focus-visible`).
6. Disabled fields: no interactive Primary ring.
7. Search Field may reuse the same tokens; layout/Control Shape stay Search-specific.
8. Button / chip / tab focus is **out of scope**.

---

## Cursor shorthand

| Apply | Meaning |
|-------|---------|
| **FIELD FOCUS** | Use Form Field Focus tokens / `.exits-input` / `.exits-select` |
| **FIELD FOCUS + ERROR** | Danger border + Danger-soft ring via `aria-invalid` |

Example:

```
Use the ExItS Form Field Focus Standard.
Apply: FIELD FOCUS.
```
