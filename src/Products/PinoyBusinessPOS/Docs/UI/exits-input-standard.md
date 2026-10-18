# ExItS Input Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Input**
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/`)

**Locked global command:** the Input playground on `/ui-standards` → **Input**. New inputs are requested with `ext input`. That command is the ExItS Input standard to use.

This standard evolves with the Input playground. Do **not** pin it permanently to a commit SHA. Reference `/ui-standards` → Input and this document.

Locking this standard does **not** restyle existing product fields. Shared `Input`, `.exits-input`, and [exits-form-field-focus-standard.md](./exits-form-field-focus-standard.md) stay in place until a field is explicitly migrated. Search stays on [exits-search-field-standard.md](./exits-search-field-standard.md). Buttons stay on [exits-button-standard.md](./exits-button-standard.md).

---

## Model (locked)

Intent, appearance, and shape are **separate axes**. They compose. Do **not** invent combinatorial names such as `primaryFilled`.

```
Input
├─ Intent                 (color)
│  ├─ Standard
│  ├─ Primary
│  ├─ Secondary
│  ├─ Success
│  ├─ Info
│  ├─ Amber
│  ├─ Danger
│  ├─ Warn
│  ├─ Help
│  └─ Black
│
├─ Appearance
│  ├─ Standard            (underline)
│  ├─ Filled
│  ├─ Outlined
│  └─ Standout
│
├─ Shape
│  ├─ Square
│  ├─ Standard
│  ├─ Soft
│  └─ Pill                (outlined)
│
└─ Flags
   ├─ Clearable
   ├─ Required
   ├─ Error
   └─ Disabled
```

| Axis | Meaning | Values |
|------|---------|--------|
| **Intent** | Accent color | `standard` · `primary` · `secondary` · `success` · `info` · `amber` · `danger` · `warn` · `help` · `black` |
| **Appearance** | How the field is drawn | `standard` · `filled` · `outlined` · `standout` |
| **Shape** | Geometry | `square` · `standard` · `soft` · `pill` |
| **Density** | Playground type size and height | `compact` · Standard (`balance`, omitted) · `comfort` |
| **Icon** | Optional leading Lucide icon | `check` · `plus` · `star` · `bookmark` · `bell` · `info` · `heart` · `trash` · `alert` · `moon` |

White Standard fill uses a black accent. In dark mode, Standard and Black labels are white, except Standout and Error. Standout keeps its own fill and ink (white fill with black ink, or black fill with white ink). Error stays red.

### Recommended default

`ext input` — Standard intent, Standard underline, Standard shape, current density. Label rests in the field and floats when the field is focused or has text.

The Input playground **default global** lock is `ext input primary outlined`. A new input uses that lock: Primary intent, Outlined appearance, Standard shape.

---

## Appearance

| Appearance | Look |
|------------|------|
| **Standard** | Underline. The line grows from the center on focus or when the field has text. |
| **Filled** | Accent wash. The floated label sits just inside the top of the fill. Typed text is centered between that label and the bottom edge. The clear control lines up with the text. |
| **Outlined** | Border. On focus or with text, the label sits on the top edge so the border cuts through the middle of the word. On focus, a 3px bottom edge grows from the center, the same bar as the dropdown. |
| **Standout** | Solid fill and contrasting ink. Same inner layout as Filled. No underline. |
| **Soft** / **Soft outline** | Same inner layout as Filled. Soft outline adds an accent border. |
| **Pill** | Outlined pill. Shape token `pill`. Same radius as the button Pill shape. |

Label and underline motion is **0.9s**. It stays at that duration so a reduced-motion snap does not collapse it.

The required mark is a red asterisk inside the floating label. It is not part of the accessible name.

---

## Density

Density scales type on the Input playground preview. Gallery samples stay at the standard size.

| Token | Type size |
|-------|-----------|
| `compact` | 14px |
| Standard (omitted) | 14px |
| `comfort` | 16px |

---

## Input types

Types live on the **Input types** card. They are **not** playground tokens and do **not** appear in `ext input`.

| Type | Behavior |
|------|----------|
| Text | Standard underline field |
| Password | Show / hide eye |
| Email | Hint `Sample: name@email.com`. A non-empty invalid value shows “Enter a valid email address.” |
| Mobile international | `+63 9XX XXX XXXX` |
| Mobile local | `09XX XXX XXXX` (no country code) |
| Number, Search, URL | Standard underline field |
| Date, Time | Label stays above the line. Native calendar and clock icons sit on the underline. No extra boxed control. |
| Textarea | Own row. Text wraps inside the multiline box. Resize grip stays on that box. |

---

## Cursor command (locked global)

Copy the command from the Input playground. Standard values are omitted.

```
ext input [intent] [appearance] [shape] [density] [icon name] [clearable] [required] [error] [disabled]
```

| Token | When it appears | Values |
|-------|-----------------|--------|
| `ext input` | Always | Default field |
| intent | Not Standard | `primary` `secondary` `success` `info` `amber` `danger` `warn` `help` `black` |
| appearance | Not Standard | `soft` `soft-outline` `filled` `outlined` `standout` |
| shape | Not Standard | `square` `soft` `pill` |
| density | Not Standard | `compact` `comfort` |
| icon name | Not None | `check` `plus` `star` `bookmark` `bell` `info` `heart` `trash` `alert` `moon` |
| `clearable` | Clearable is on | Clear control |
| `required` | Required is on | Asterisk in the label |
| `error` | Error is on | Invalid state |
| `disabled` | Disabled is on | Non-interactive |

Label text keeps the typed capitalization.

| Command | Meaning |
|---------|---------|
| `ext input` | Standard intent, appearance, shape, and density |
| `ext input primary` | Primary intent |
| `ext input filled` | Filled appearance |
| `ext input primary soft-outline` | Primary + soft tint with an accent border |
| `ext input primary outlined pill` | Primary + outlined + pill |
| `ext input filled required` | Filled and required |
| `ext input standout clearable` | Standout with a clear control |

Do **not** invent a new shorthand. Do **not** put a type token in the command.

---

## UI Standards page

`/ui-standards` → **Input** shows:

1. **Input playground** — locked global command and live preview
2. **Input samples** — Standard, Filled, Outlined, Standout, and Pill color rows
3. **Input types** — text, password, email, mobile, number, search, URL, date, time, and textarea

---

## Explicit prompt overrides

1. Explicit task instruction
2. ExItS Input Standard (this document)
3. Existing page presentation

Domain correctness and accessibility always remain mandatory.

---

## Mass migration

Existing pages migrate **incrementally**. Locking this model does **not** require a search-and-replace of all inputs.
