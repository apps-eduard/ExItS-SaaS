# ExItS Button Standard

**Status:** APPROVED / LOCKED  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Canonical component:** `ExItS.PinoyBusinessPOS.React/src/components/ui/button.tsx`  
**Visual authority:** `/ui-standards` → **Buttons**  
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/`)

The shared ExItS `Button` component is the **canonical button implementation** for Pinoy Business POS React.

Pages **MUST** reuse it.

Pages **MUST NOT** create local visual button systems when the shared `Button` can express the required action.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component path and `/ui-standards` → Buttons instead.

---

## Architecture

```
React Button component
    ↓
CVA (`buttonVariants`)
    ↓
Tailwind utility classes
    ↓
ExItS semantic CSS tokens (`--exits-*`, theme primary aliases)
```

| Layer | Role |
|-------|------|
| **Button** | ExItS-owned shared control |
| **Radix `Slot` / `asChild`** | Composition only (polymorphic trigger) — does **not** own appearance |
| **CVA** | Organizes `variant` / `size` / `shape` / `treatment` |
| **Tailwind** | Applies styles |
| **ExItS tokens** | Visual identity (theme, density, semantic colors, motion) |

No third-party visual button library controls Button appearance.

Related helper (same module):

| Export | Role |
|--------|------|
| `buttonIconMotion` | Optional contextual Lucide child motion classes (`group/button`) |

---

## Intent vocabulary (Cursor shorthand → Button API)

Do **not** rename public API props to match shorthand. Map instead.

| Cursor shorthand | Button API | Meaning |
|------------------|------------|---------|
| **PRIMARY** | `variant="default"` (default) | Main / save / create / submit / continue |
| **SUCCESS** | `variant="success"` | Approve / accept / complete / mark paid |
| **MUTED** | `variant="secondary"` | Quiet secondary / non-primary alternative |
| **OUTLINE** | `variant="outline"` | Bordered secondary utility |
| **GHOST** | `variant="ghost"` | Quiet navigation / tertiary |
| **INFO** | `variant="info"` | Informational (view / preview) |
| **WARNING** | `variant="warning"` | Cautionary / reversible operational |
| **DANGER** | `variant="destructive"` | Destructive / reject / void / remove |
| **DANGER STRONG** | `variant="dangerStrong"` | High-risk irreversible confirmation only |

### Action semantics

| Intent | Typical actions |
|--------|-----------------|
| PRIMARY | Save, Create, Add, Submit, Continue |
| SUCCESS | Approve, Accept, Complete, Mark paid |
| MUTED | Secondary non-primary action; quiet alternative |
| OUTLINE | Filter, Download, Change branch, utility |
| GHOST | Back, Close, More, quiet tertiary |
| INFO | View details, Preview |
| WARNING | Deactivate, Pause, Reset, Suspend |
| DANGER | Decline, Delete, Remove, Void; destructive business cancel |
| DANGER STRONG | Delete permanently; irreversible confirmation |

---

## Cancel / Close / Back icons (locked)

| Meaning | Lucide icon |
|---------|-------------|
| **Cancel** (UI / form / dialog dismiss) | `CircleX` |
| **Cancel and return** / abandon edit and go back | `CornerUpLeft` |
| **Close-only** (dialogs / icon-only close) | `X` |
| **Back** / navigation | `ArrowLeft` |
| **Void** | `Ban` |
| **Undo** | `Undo2` |
| **Reset** | `RotateCcw` |

Rules:

- **Cancel is not automatically Danger.**
- UI / form / dialog Cancel → prefer **MUTED** (`secondary`) or **GHOST**.
- Business-operation cancellation with destructive consequences → may use **DANGER**.

---

## Shape standard

| Cursor shorthand | Button API | Use |
|------------------|------------|-----|
| **STANDARD** | `shape="standard"` (default) | Normal business control |
| **SOFT** | `shape="soft"` | More rounded modern control |
| **PILL** | `shape="pill"` | Capsule / special text-button cases |
| **ROUND** | `shape="round"` | Circular control — **primarily ICON ONLY** (`size="icon"`) |

Do **not** use ROUND for normal labeled text buttons.

Icon-only shape matrix: STANDARD / SOFT / ROUND (with `size="icon"`).

---

## Treatment standard

| Cursor shorthand | Button API | Use |
|------------------|------------|-----|
| **FLAT** | `treatment="flat"` (default) | Restrained surface |
| **ELEVATED** | `treatment="elevated"` | Subtle depth + approved hover lift |
| **GRADIENT** | `treatment="gradient"` | Subtle same-color-family CTA depth |

Rules:

- Gradients stay restrained and same-family.
- No unrelated multi-color gradients, neon, excessive shadow, or glassmorphism.

---

## Size

| API | Use |
|-----|-----|
| `size="default"` | Density-aware labeled control (`--exits-control-height`) |
| `size="icon"` | Square / round icon-only matching density height |
| `size="large"` | Exceptional CTA only — not routine CRUD / toolbar |

---

## Icons

- Use **Lucide**.
- Default labeled layout: `[ICON] Label`.
- Directional / open actions may place icon on the right: `Continue [ArrowRight]`, `Open [ExternalLink]`.
- Icons are optional — use when they improve recognition.
- Typical size in samples: `className="size-4"` on Lucide children.

### Icon-only requirements

ICON ONLY requires:

- `size="icon"`
- `aria-label`
- `title` / tooltip
- keyboard accessibility
- visible focus ring (component default)

ROUND icon-only is approved (`size="icon"` + `shape="round"`).

Examples: Edit → `Pencil`; Refresh → `RefreshCw`; Print → `Printer`; Delete → `Trash2`; More → `MoreHorizontal`.

---

## Motion standard (locked)

Principles: subtle, fast (`--exits-motion-fast`), professional, no layout shift; feel more than notice.

| Category | Behavior |
|----------|----------|
| **STANDARD MOTION** | Tokenized transitions on background, border, color, shadow, transform |
| **PRESS FEEDBACK** | `active:scale-[0.985]` (disabled resets scale) |
| **ELEVATED LIFT** | `treatment="elevated"` / `gradient` hover `-translate-y-px` + shadow |
| **CONTEXTUAL ICON MOTION** | Optional `buttonIconMotion.*` on Lucide children |
| **DIRECTIONAL ICON MOTION** | Arrow / ExternalLink helpers |
| **LOADING SPINNER** | Lucide spinner + `disabled` + `aria-busy` (page-owned pattern) |

Do **not** invent generic Button API variants for bounce, shake, pulse, or flash.

### Contextual icon motion (`buttonIconMotion`)

| Kind | Motion |
|------|--------|
| `continue` | ArrowRight → slight translateX |
| `back` | ArrowLeft → slight -translateX |
| `open` | ExternalLink → slight up/right |
| `add` | Plus → subtle scale |
| `view` | Eye → subtle scale |
| `refresh` | RefreshCw → restrained rotate |
| `delete` | Trash2 → restrained lift/scale only |
| `warning` | subtle scale |
| `more` | subtle scale / opacity |

Danger actions **MUST NOT** shake, bounce, flash, or pulse continuously.

### Loading

- Spinner / loading indicator inside shared `Button`
- Preserve reasonable width
- Disable / prevent duplicate activation
- Expose `aria-busy` (and disabled) as appropriate
- Do not invent page-specific loading-button components

### Reduced motion (required)

Respect `prefers-reduced-motion` (Tailwind `motion-reduce:*` + ExItS motion tokens zeroed in globals).

Reduced-motion users must not receive unnecessary transform / rotation / lift / scale. Color, border, and focus feedback may remain.

---

## Primary color + semantic independence

**PRIMARY does not mean hardcoded green.**

PRIMARY means ExItS semantic primary tokens (`bg-primary`, `--exits-primary`, `--exits-primary-hover`, etc.).

Do **not** treat `bg-green-*` / `text-green-*` as canonical.

This preserves future Preferences → Primary Color.

If Primary Color = Purple:

| Intent | Result |
|--------|--------|
| PRIMARY | Purple (brand primary) |
| SUCCESS | Remains semantic success green |
| WARNING | Remains amber |
| DANGER / DANGER STRONG | Remains danger red |
| INFO | Remains information semantic color |

---

## Theme + density

Buttons **MUST** inherit Preferences:

- Theme: light / dark / system
- Density: compact / balance / comfort (`--exits-control-height`, padding tokens)

No page-specific duplicate preference state for buttons.

---

## UI Standards page

`/ui-standards` → **Buttons** is the human visual reference for intents, shapes, treatments, icon-only, round icon-only, motion, states, and Cursor shorthand.

---

## Cursor shorthand examples

```
Save: PRIMARY + SOFT + WITH ICON
Cancel: MUTED or GHOST + WITH ICON · CircleX
Cancel & return: MUTED or GHOST + WITH ICON · CornerUpLeft
Approve: SUCCESS + WITH ICON
Deactivate: WARNING + WITH ICON
Delete: DANGER + WITH ICON
Delete permanently: DANGER STRONG + WITH ICON
Edit: ICON ONLY ROUND GHOST
Refresh: ICON ONLY ROUND MUTED
Delete icon: ICON ONLY ROUND DANGER
Main CTA: PRIMARY + SOFT + ELEVATED + WITH ICON
Continue: PRIMARY + WITH ICON · directional icon on right
```

Other tokens: `STANDARD` / `SOFT` / `PILL` / `ROUND`, `FLAT` / `ELEVATED` / `GRADIENT`, `WITH ICON` / `NO ICON` / `ICON ONLY` / `ICON ONLY ROUND`.

---

## Explicit prompt overrides

Priority:

1. Explicit task instruction  
2. ExItS Button Standard (this document)  
3. Existing page presentation  

If a task says `NO ICON`, `STANDARD`, `GHOST`, etc., that explicit instruction overrides default recommendations.

Domain correctness and accessibility always remain mandatory.

---

## Mass migration

Existing pages migrate **incrementally** in future tasks. Locking this standard does **not** require a search-and-replace of all buttons.
