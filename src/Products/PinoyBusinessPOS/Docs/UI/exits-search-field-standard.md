# ExItS Search Field Standard

**Status:** STANDARD / CANONICAL FOUNDATION  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Canonical component:** `ExItS.PinoyBusinessPOS.React/src/components/exits/SearchField.tsx`  
**Alias:** `ExitsSearchField`  
**Visual authority:** shared `SearchField` + `/ui-standards` (Simple Search section when available)

Search is a **dataset / list filter control**, not a form data-entry field.

---

## Architecture

```
SearchField (shape=auto|standard|pill)
    ↓
.exits-search-field CSS
    ↓
--exits-control-radius  (Control Shape)
--exits-control-height  (Density)
--exits-primary         (thin focus)
```

Form inputs use **`--exits-field-radius`** and **must not** follow Pill Control Shape.

| Control | Radius token | Pill preference |
|---------|--------------|-----------------|
| Search Field | `--exits-control-radius` | Yes |
| Button (auto) | `--exits-control-radius` | Yes |
| Text / Select / Textarea (`.exits-input`) | `--exits-field-radius` | **No** |

---

## Cursor shorthand

| Apply | Meaning |
|-------|---------|
| **SEARCH** | Canonical SearchField, shape auto |
| **SEARCH + STANDARD** | `shape="standard"` |
| **SEARCH + PILL** | `shape="pill"` |
| **SEARCH + CLEAR** | Value present → clear control |
| **SEARCH + LOADING** | `loading` |
| **SEARCH + DISABLED** | `disabled` |

Example:

```
Use the ExItS Search Field Standard.
Apply: SEARCH + PILL + CLEAR.
```

---

## Shape

| Preference / prop | Result |
|-------------------|--------|
| `shape="auto"` + Control Shape Standard | Soft rectangle (`--exits-radius-md`) |
| `shape="auto"` + Control Shape Pill | Capsule (`9999px`) |
| `shape="standard"` | Always rectangle |
| `shape="pill"` | Always capsule |

Precedence: **explicit shape > global Control Shape > standard default radius**.

---

## Focus

- Rest: subtle neutral border  
- Hover: slightly stronger border  
- Focus-within: **1px Primary border** + **1px Primary-soft outer ring**  
- Tokens: `--exits-field-border-focus` / `--exits-field-focus-ring` (shared with Form Field Focus Standard)  
- Not: thick `ring-2` / neon glow  

See also: `Docs/UI/exits-form-field-focus-standard.md`

---

## Behavior ownership

SearchField owns presentation and clear UX.  
Pages own: debounce, query, Enter submit, API calls, routing.

Escape clears the field when focused and non-empty (does not steal empty Escape from modals).

---

## Do not use SearchField for

Product name, SKU, barcode, address, cost, quantity, notes, supplier form fields, table inline editors, or searchable form comboboxes that select a business value.
