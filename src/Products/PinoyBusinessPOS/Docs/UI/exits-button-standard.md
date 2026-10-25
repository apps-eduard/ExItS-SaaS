# ExItS Button Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Canonical component:** `ExItS.PinoyBusinessPOS.React/src/components/ui/button.tsx`
**Visual authority:** `/ui-standards` → **Buttons**
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/`)

**Interaction semantics (Save / Edit / Delete / Primary hierarchy):** see [EXITS_UI_STANDARD.md](./EXITS_UI_STANDARD.md) and `/ui-standards`.

The shared ExItS `Button` component is the **canonical button implementation** for Pinoy Business POS React.

**Locked global command:** the Button playground on `/ui-standards` → **Buttons**. New buttons are requested with `ext btn`. That command is the ExItS Button standard to use.

Pages **MUST** reuse the shared `Button`.

Pages **MUST NOT** create local visual button systems when the shared `Button` can express the required action.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component path and `/ui-standards` → Buttons instead.

---

## Model (locked)

Semantic meaning and visual treatment are **separate axes**. They compose. Do **not** invent combinatorial named variants such as `primaryElevated` or `dangerOutline`.

```
Button
├─ Intent / Tone          (WHY — semantic meaning) — 8 locked
│  ├─ Primary
│  ├─ Neutral (Secondary)
│  ├─ Success
│  ├─ Info
│  ├─ Warning (Warn)
│  ├─ Help
│  ├─ Danger
│  └─ Contrast
│
├─ Appearance / Treatment (HOW — visual rendering)
│  ├─ Solid
│  ├─ Outline
│  ├─ Ghost
│  ├─ Elevated
│  ├─ Gradient
│  └─ Glossy
│
├─ Shape                  (geometry)
│  ├─ Auto
│  ├─ Standard
│  ├─ Soft
│  ├─ Pill
│  └─ Round
│
└─ State
   ├─ Normal
   ├─ Hover
   ├─ Focus
   ├─ Disabled
   └─ Loading
```

| Axis | Meaning | Values |
|------|---------|--------|
| **Intent** | Why the button looks that way | `primary` · `neutral` · `success` · `info` · `warning` · `help` · `danger` · `contrast` |
| **Appearance** | How it is drawn | `solid` · `outline` · `ghost` · `elevated` · `gradient` · `glossy` |
| **Emphasis** | Fill strength (solid/elevated/gradient) | `soft` (default) · `strong` (Severities / filled) |
| **Shape** | Geometry | `auto` · `standard` · `soft` · `pill` · `round` |
| **State** | Interaction | normal / hover / focus / disabled / loading |

**Elevated, Outline, Ghost, and Gradient are NOT intents.**

The locked intent set is the Diamond **8 severities**. Gallery label **Secondary** = API `neutral`. Gallery **Warn** = API `warning`.

### Recommended defaults

| Role | Intent | Appearance |
|------|--------|------------|
| Ordinary / non-primary | `neutral` | `solid` |
| Main action | `primary` | `solid` |

Do **not** call Solid “Standard”. Use **Solid** consistently.

Component runtime default remains **Primary + Solid** for compatibility with historical `variant="default"` (unspecified props). New non-primary code should set `intent="neutral"` explicitly when needed.

### Canonical API

```tsx
<Button intent="primary" appearance="solid" />
<Button intent="danger" appearance="outline" />
<Button intent="neutral" appearance="ghost" />
<Button intent="primary" appearance="elevated" />
```

Prefer `getActionButtonStyle(action)` from `action-semantics.ts` for common verbs.

---

## Intent / Tone

| Intent | Gallery label | Meaning |
|--------|---------------|---------|
| **Primary** | Primary | Main / brand action |
| **Neutral** | Secondary | Normal non-semantic action |
| **Success** | Success | Positive / activate / complete |
| **Info** | Info | Informational |
| **Warning** | Warn | Caution / reversible risky action |
| **Help** | Help | Help / assistive guidance |
| **Danger** | Danger | Destructive / error / severe |
| **Contrast** | Contrast | High-contrast / inverse chrome |

All **8** are **APPROVED / LOCKED**. Do not drop Help or Contrast from the standard.

PRIMARY does **not** mean hardcoded green — product Primary uses ExItS primary tokens (`--exits-primary`, etc.). SUCCESS / WARNING / DANGER / INFO use `--exits-*` semantic tokens. HELP / CONTRAST use `--exits-severity-help` / `--exits-severity-contrast` (canonical for those intents).

```tsx
<Button intent="help" appearance="solid" emphasis="strong" />
<Button intent="contrast" appearance="outline" />
```

Danger solid has two strengths via legacy alias:

| Strength | API | Use |
|----------|-----|-----|
| Soft | `intent="danger"` `appearance="solid"` or legacy `variant="destructive"` | Ordinary danger chrome |
| Strong | legacy `variant="dangerStrong"` | Irreversible confirmation only |

---

## Appearance / Treatment

| Appearance | Meaning |
|------------|---------|
| **Solid** | Filled / default treatment |
| **Outline** | Bordered lower-emphasis |
| **Ghost** | Minimal / backgroundless |
| **Elevated** | Same tone as solid + depth (shadow / subtle lift). Respects Reduced Motion |
| **Gradient** | Branded gradient treatment |

### Elevated

Elevated modifies **depth only**. Primary + Solid and Primary + Elevated share the same semantic primary tone. Elevated must not invent a new color meaning.

### Gradient

Gradient is appearance-only. Prefer **Primary** (brand). Non-primary gradients fall back to a solid-like surface so semantic meaning is not overridden unpredictably.

---

## Muted (removed as intent)

**Muted is not a semantic intent.** It was a low-emphasis neutral look.

| Legacy | Canonical |
|--------|-----------|
| `variant="secondary"` (historical “Muted”) | `intent="neutral"` + `appearance="solid"` |

Keep legacy `variant="secondary"` working via alias; do not introduce `intent="muted"`.

---

## Gallery mapping (UI Standards)

**Locked visual authority:** `/ui-standards` → **Buttons** → **Gallery** (`UiStandardsButtonGallery`) mirrors the **Diamond PrimeNG Button Full sample** layout (Default, Icons, Severities, Raised, Rounded, Text, Outlined, Rounded Icons, Loading, Button Group).

Reference: https://diamond.primeng.dev/uikit/button

That Full sample **is** the ExItS Button visual standard for appearance coverage. Semantic API remains ExItS `intent` + `appearance` on the shared `Button` — map Diamond / Prime labels to ExItS axes; do **not** add Prime-only variants to the component API.

| Diamond / Prime section | ExItS |
|-------------------------|-------|
| Default (Submit / Disabled / Link) | Primary+Solid · disabled (muted chrome, readable label) · Primary+Ghost |
| Icons | `size="icon"` · icon children leading/trailing |
| Severities | Solid × each of the **8 locked** intents (Secondary = Neutral; Warn = Warning) |
| Raised | `appearance="elevated"` |
| Text | `appearance="ghost"` |
| Rounded | `shape="pill"` |
| Outlined | `appearance="outline"` |
| Rounded Icons | `size="icon"` + `shape="round"` |

### Severity colors

**Primary** follows the user’s Preferences primary palette (`--exits-primary` / brand tokens) — it is **not** a fixed Diamond emerald.

**Secondary · Success · Info · Warn · Help · Danger · Contrast** use locked `--exits-severity-*` Diamond fills (gallery + Help/Contrast product intents).

Product Success / Info / Warning / Danger also keep `--exits-*` semantic tokens on the shared `Button` when not using strong severity showcase fills.

Locking this gallery does **not** require rewriting existing product buttons. Migrate call sites incrementally; reuse shared `Button` + action semantics.

---

## Common action mapping

Use `EXITS_ACTIONS` / `getActionButtonStyle`. Verb alone does **not** force Primary — hierarchy still decides the primary action in a group.

| Action | Intent | Appearance |
|--------|--------|------------|
| Save | Primary | Solid |
| Create / New | Primary (main) | Solid |
| Add | Primary if main; Neutral + Outline if secondary | |
| Edit | Neutral | Outline |
| Cancel | Neutral | Ghost (Outline on some surfaces) |
| Activate | Success | Outline (Solid when main confirmation) |
| Deactivate | Warning | Outline |
| Delete | Danger | Outline (Solid / strong inside destructive confirmation) |
| Preview / Print / Download / Retry | Neutral | Outline |

---

## Architecture

```
React Button
    ↓
resolveButtonVisual (intent + appearance, or legacy variant/treatment)
    ↓
CVA (`buttonVariantsCva`)
    ↓
Tailwind + ExItS tokens
```

| Layer | Role |
|-------|------|
| **Button** | ExItS-owned shared control |
| **Radix `Slot` / `asChild`** | Composition only |
| **CVA** | Organizes `intent` / `appearance` / `size` / `shape` |
| **Tailwind** | Applies styles |
| **ExItS tokens** | Visual identity |

Related helper: `buttonIconMotion` (optional Lucide child motion on `group/button`).

---

## Legacy compatibility

Do **not** mass-rename call sites. Legacy props remain supported:

| Legacy | Maps to |
|--------|---------|
| `variant="default"` | Primary + Solid |
| `variant="secondary"` | Neutral + Solid (muted look) |
| `variant="outline"` | Neutral + Outline |
| `variant="ghost"` | Neutral + Ghost |
| `variant="success"` / `info` / `warning` | matching Intent + Solid |
| `variant="destructive"` | Danger + Solid (soft) |
| `variant="dangerStrong"` | Danger + Solid (strong) |
| `treatment="elevated"` | Appearance Elevated |
| `treatment="gradient"` | Appearance Gradient (when supported) |
| `treatment="flat"` | Solid (no depth) |

New code should use `intent` + `appearance`. Prefer `getActionButtonStyle` over `getActionIntent` (deprecated).

---

## Shape standard

| API | Use |
|-----|-----|
| `shape="auto"` (default) | Follows Control Shape preference (Square, Standard, Soft, Pill) |
| `shape="standard"` | Fixed `--exits-radius-md` |
| `shape="soft"` | More rounded |
| `shape="pill"` | Capsule |
| `shape="round"` | Circular — **primarily ICON ONLY** (`size="icon"`) |

Do **not** use ROUND for normal labeled text buttons.

---

## Size

| API | Use |
|-----|-----|
| `size="sm"` | Compact labeled control (toolbar / inline actions) |
| `size="default"` | Density-aware labeled control |
| `size="icon"` | Square / round icon-only |
| `size="large"` | Exceptional CTA only |

### Disabled contrast (locked)

Disabled buttons **MUST NOT** use whole-control opacity fade on solid fills (white / `primary-foreground` on washed brand fails contrast). Shared `Button` disabled chrome uses muted surface + muted text at full opacity so the label stays readable while clearly non-interactive.

---

## Icons

- Use **Lucide**.
- Default labeled layout: `[ICON] Label`.
- Icons optional when they improve recognition.
- ICON ONLY requires `size="icon"`, `aria-label`, tooltip/title, focus ring.

Cancel / Close / Back icons (locked): Cancel → `CircleX`; Cancel & return → `CornerUpLeft`; Close → `X`; Back → `ArrowLeft`. Cancel is not automatically Danger.

---

## Motion

Subtle, fast (`--exits-motion-fast`), professional; respect `prefers-reduced-motion`.

| Category | Behavior |
|----------|----------|
| Press | `active:scale-[0.985]` |
| Elevated / Gradient lift | hover `-translate-y-px` + shadow; reduced-motion disables lift |
| Contextual icons | optional `buttonIconMotion.*` |
| Loading | spinner + `disabled` + `aria-busy` |

Danger actions **MUST NOT** shake, bounce, flash, or pulse continuously.

---

## Theme + density

Buttons **MUST** inherit Preferences: theme, density, control shape, motion, primary palette.

---

## Cursor command (locked global)

The Button playground is the locked way to ask for an ExItS button. Copy the command from the playground. Standard values are omitted.

```
ext btn [icon] [intent] [appearance] [shape] [density] [icon name] [ripple] [disabled]
```

| Token | When it appears | Values |
|-------|-----------------|--------|
| `ext btn` | Always | Default button |
| `icon` | Icon only is on | Icon-only control |
| intent | Not Standard | `primary` `secondary` `success` `info` `amber` `danger` `warn` `help` `black` |
| appearance | Not Standard | `soft` `soft-outline` `soft-glass` `outline` `raised` `ghost` `glossy` `glossy-glass` `glossy-raise` `gradient` `gradient-glass` `rounded` `flat` |
| shape | Not Standard | `square` `soft` `pill` |
| density | Not Standard | `compact` `comfort` |
| icon name | Not None | `check` `plus` `star` `bookmark` `bell` `info` `heart` `trash` `alert` `moon` |
| `ripple` | Ripple is on | Click ripple |
| `disabled` | Disabled is on | Non-interactive |

Label text keeps the typed capitalization. `SAVE` stays `SAVE`. `save` stays `save`.

| Command | Meaning |
|---------|---------|
| `ext btn` | Standard intent, appearance, shape, and density. No icon. Ripple off. |
| `ext btn primary` | Primary intent |
| `ext btn primary outline` | Primary + outline |
| `ext btn primary soft-outline` | Primary + soft tint with an accent border |
| `ext btn primary ghost pill` | Primary + ghost + pill |
| `ext btn icon` | Icon only, otherwise standard |
| `ext btn icon ghost` | Icon only + ghost |
| `ext btn compact star` | Compact density + star icon |
| `ext btn primary ripple disabled` | Primary, ripple on, disabled |

Do **not** use the retired `EXITS BUTTON + …` phrases for new work.

Map the command onto the shared `Button` (`intent`, `appearance`, `shape`, `size="icon"` for icon only). The playground on `/ui-standards` → Buttons is the visual reference. Do not mass-replace existing buttons.

---

## UI Standards page

`/ui-standards` → **Buttons** shows:

1. **Button playground** — locked global command and live preview
2. **Button samples** — Quasar-style treatment samples

---

## Explicit prompt overrides

1. Explicit task instruction
2. ExItS Button Standard (this document)
3. Existing page presentation

Domain correctness and accessibility always remain mandatory.

---

## Mass migration

Existing pages migrate **incrementally**. Locking this model does **not** require a search-and-replace of all buttons.
