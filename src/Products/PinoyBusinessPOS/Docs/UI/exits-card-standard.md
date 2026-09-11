# ExItS Card Standard

**Status:** APPROVED / LOCKED  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Canonical foundation:** `ExItS.PinoyBusinessPOS.React/src/components/ui/card.tsx`  
**Shared variants (CVA):** `ExItS.PinoyBusinessPOS.React/src/components/exits/card-variants.ts`

**Visual authority:** `/ui-standards` → **Cards**  
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsCardsPanel.tsx`)

The shared ExItS Card foundation is the **canonical card / container presentation system** for Pinoy Business POS React.

Pages **MUST** reuse it when Card grouping is appropriate.

Pages **MUST NOT** create page-local visual Card systems when the canonical Card foundation can express the requirement.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component paths and `/ui-standards` → Cards instead.

Card **type / purpose** is independent from **treatment** and **motion**. Do not invent combination-specific components (`productExpandCard`, `entityHoverCard`, …).

---

## Architecture

```
React Card component (`card.tsx`)
    ↓
shared Card variants / CVA (`card-variants.ts`)
    ↓
Tailwind utility classes
    ↓
ExItS semantic CSS tokens (`--exits-*`, density, theme, motion)
```

| Layer | Role |
|-------|------|
| **`Card`** | Presentation shell + interactive / selectable props |
| **CVA / `card-variants`** | Treatment, motion, expand scale, radius, padding, layout, accent |
| **Tailwind** | Applies styles |
| **ExItS tokens** | Visual identity (Primary, surfaces, density, motion, reduced-motion) |

ExItS owns the visual design. No third-party visual Card library controls appearance.

Anatomy helpers (same module): `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter`, `CardMedia`, `CardReveal`.

---

## Canonical API (current)

| Piece | API |
|-------|-----|
| Foundation | `Card` |
| Treatment | `treatment`: `surface` \| `bordered` \| `elevated` \| `interactive` \| `selected` \| `accent` \| `featured` |
| Motion | `motion`: `none` \| `lift` \| `expand` \| `accent` \| `featured` |
| Expand intensity | `expandScale`: `subtle` \| `standard` \| `strong` (default **`standard`**) |
| Layout | `layout`: `vertical` \| `horizontal` |
| Padding | `padding`: `default` \| `compact` |
| Radius | `radius`: `standard` \| `soft` |
| Accent | `accentTone` + `accentPosition` with `treatment="accent"` |
| Interactive | `interactive` (default motion → `lift` when motion unset) |
| Selected | `selected` and/or `treatment="selected"` |
| Hover reveal | `reveal` + `CardReveal` |
| Media zoom | `CardMedia` `zoom` |

**Defaults (current):** `treatment="bordered"`, `motion="none"` (or `lift` when `interactive`), `expandScale="standard"`, `layout="vertical"`, `padding="default"`, `radius="standard"`, accent position **`start`**.

Independent dimensions: **type/purpose** + **treatment** + **motion** + **layout**. Do **not** explode into purpose-specific motion components.

---

## Canonical Card types (usage / purpose)

These are **usage patterns**, not separate React components (unless a page composes them).

| Type | Canonical use |
|------|----------------|
| **BASIC CARD** | Grouped content, forms, informational sections, simple detail blocks |
| **SUMMARY CARD** | Order / payment summary, totals, concise grouped business values |
| **KPI CARD** | Dashboard metric, key business number, compact performance summary |
| **ACTION CARD** | One clear action / workflow entry (Request stock, New sale, …) |
| **ENTITY CARD** | Customer, Supplier, Organization, Branch, Warehouse |
| **PRODUCT CARD** | Product / media representation (image, price, chips, stock) |
| **SELECTABLE CARD** | Branch / warehouse / payment / plan option selection |
| **STATUS CARD** | Warning / error / success / operational attention summary |
| **COMPACT CARD** | Dense business information (tighter padding / structure) |
| **FEATURED CARD** | Recommended / preferred / promoted option (uses `featured` treatment) |

---

## Canonical treatments

| Treatment | API | Notes |
|-----------|-----|--------|
| **SURFACE** | `surface` | Minimal weight; transparent border |
| **BORDERED** | `bordered` | Default; most common POS card |
| **ELEVATED** | `elevated` | Elevated surface + restrained shadow |
| **INTERACTIVE** | `interactive` | Pointer + focus ring + hover border/surface (not selection) |
| **SELECTED** | `selected` | Primary border + Primary-soft surface |
| **ACCENT** | `accent` | Neutral surface + semantic accent edge/tint |
| **FEATURED** | `featured` | Stronger Primary border/soft fill + light elevation |

Keep treatment separate from purpose:

- ENTITY CARD + INTERACTIVE  
- KPI CARD + BORDERED  
- SELECTABLE CARD + SELECTED  

---

## Layouts

| Layout | API | Use |
|--------|-----|-----|
| **VERTICAL** | `vertical` | Normal content stacking (default) |
| **HORIZONTAL** | `horizontal` | Compact lists, products, entities, mobile rows |

Layout is independent of treatment and type.

---

## Anatomy

```
Card
├── optional Header
│   ├── icon / avatar
│   ├── title
│   ├── description
│   ├── Chip / Badge
│   └── header action
├── Content
└── optional Footer
    ├── metadata
    └── actions
```

Not every Card needs every section. Prefer `CardTitle` / `CardDescription` / `CardContent` / `CardFooter` / `CardMedia` / `CardReveal` as needed.

---

## Type guidance

### BASIC

Prefer restrained **SURFACE** or **BORDERED**. Quiet grouping for forms and information.

### SUMMARY

Clear typography hierarchy for totals and summaries. Do not make every total oversized.

### KPI

Approved patterns: text KPI, icon KPI, KPI + Chip, KPI + submetric. Restrained neutral surface. Do **not** create rainbow dashboards. Normally **STATIC**; **LIFT** only when clickable.

### ACTION

One clear action. Reuse locked **ExItS Button Standard** for the CTA. Do not invent Card-specific button styling. Prefer a Button inside the Card (not necessarily a fully clickable body).

### ENTITY

Leading avatar/icon (optional), title, subtitle/metadata, chips, optional footer/actions. With and without avatar follow the same content rhythm. Keep leading visual close to content — no large empty icon columns.

### PRODUCT / MEDIA

Image or fallback, title, price, chips, stock/metadata, actions. Images support responsive sizing (`CardMedia`). Business information remains primary. Media zoom applies to the media frame only.

### SELECTABLE

Chooses an option — **not** the same as Interactive (opens / navigates / acts).

| State | Presentation |
|-------|----------------|
| **UNSELECTED** | Bordered / neutral |
| **SELECTED** | Primary border + Primary-soft surface + clear indicator |
| **DISABLED** | Opacity / not-allowed; not selectable |

Do **not** rely on color alone. Use radio semantics for single-select and checkbox semantics for multi-select. Do **not** use strong EXPAND on selectable Cards — selection clarity matters more than animation.

**Responsive:** do not squeeze cards until labels wrap word-by-word. Use sensible min widths and responsive grids (desktop multi-column when space allows; tablet fewer columns; mobile normally one column). Preserve current selectable layout fixes.

### STATUS / ACCENT

Neutral Card surface + semantic accent (not a full-card color fill).

| Tone | Token family |
|------|----------------|
| INFO | `--exits-info` |
| SUCCESS | `--exits-success` |
| WARNING | `--exits-warning` |
| DANGER | `--exits-danger` |
| PRIMARY | `--exits-primary` |

### Accent position (locked API)

| Position | API | Behavior |
|----------|-----|----------|
| **START** (canonical default) | `accentPosition="start"` | Inline-start border (`border-s` / logical start) — RTL-correct |
| **TOP** | `top` | Block-start border |
| **TINT** | `tint` | Soft semantic fill + tinted border |

Default / preferred status accent: **START**.

### COMPACT

`padding="compact"` (and concise content). Distinct from Preferences density=`compact`. Global density still applies independently.

### FEATURED / RECOMMENDED

`treatment="featured"` (optionally `motion="featured"` or ACCENT + EXPAND). Use locked Chip for labels such as Recommended / Preferred. Composition for pricing/plan Cards — not a separate design system.

---

## Motion vocabulary (locked)

| Motion | API / mechanism | Canonical use |
|--------|-----------------|---------------|
| **STATIC** | `motion="none"` | Default non-interactive information |
| **LIFT** | `motion="lift"` | Normal clickable Cards (~`-translate-y-0.5`, shadow/border) |
| **EXPAND** | `motion="expand"` | Prominent clickable Cards |
| **ACCENT** | `motion="accent"` | Primary border/glow strengthening on hover |
| **MEDIA ZOOM** | `CardMedia zoom` | Product/media image ~`scale(1.04)` inside clipped frame |
| **HOVER REVEAL** | `reveal` + `CardReveal` | SPECIAL USE secondary actions |

Also locked: `motion="featured"` (featured treatment hover: lift + ~`scale(1.02)` + Primary border).

### EXPAND rules

- Uses **transform only** (`scale` / optional small `translateY`) — **not** width, height, padding, or grid size  
- No neighboring layout shift  
- Default intensity: `expandScale="standard"` → **`hover:scale-[1.02]`** (current implementation)  
- Also available: `subtle` ≈ 1.01, `strong` ≈ 1.03 (do not exceed ~1.03 for normal business Cards)  
- On pointer leave: return smoothly to original scale/position  
- Elevated `z-index` while hovered so the Card can sit above neighbors  

### Hover rules

| Card kind | Motion |
|-----------|--------|
| Non-interactive | No pointer cursor; no lift/expand |
| Interactive | May use LIFT or EXPAND |
| Selectable | Selection feedback > strong motion |
| KPI | STATIC; LIFT only if clickable |
| Featured / recommended | ACCENT + Featured / EXPAND as appropriate |
| Product / media | MEDIA ZOOM; optional LIFT |

### Motion safety

No bounce, shake, 3D tilt, large zoom, continuous pulse, large glow, or dramatic spring. Prefer CSS `transform` / `opacity` only. **No** Framer Motion / GSAP / anime.js / react-spring.

### Reduced motion (required)

Respect `prefers-reduced-motion`. Disable decorative scale, lift, media zoom, and reveal translation. Preserve selected, border/color, and focus states.

### Touch safety

Critical information and actions must **not** depend only on hover. Hover reveal must reserve space (no height jump) and remain available via focus-within / reduced-motion visibility.

---

## Special / Featured effects

| Effect | Status |
|--------|--------|
| Featured treatment + pricing/plan composition | APPROVED composition pattern |
| Gradient accent border | **SPECIAL / FEATURED USE** — showcase / rare featured only; not default; not required on core Card API |
| Countdown / info strip | Compact horizontal composition example |
| Hover reveal | SPECIAL USE |

No neon glow. No flashy gradients filling the Card body.

---

## Actions, Chips, Tabs, Tables inside Cards

| Concern | Rule |
|---------|------|
| Actions | Locked **Button** Standard only |
| Header icon action | ICON ONLY ROUND GHOST when appropriate |
| Chips / counts | Locked **Chip** Standard (`TagChip`, `StatusChip`, `CountChip`, `CountBadge`, …) |
| Tabs | Locked **Tabs** Standard (`ExitsTabs`) — avoid excessive nesting |
| Tables | Locked **ExitsTable** — do not wrap every table in an extra Card if the table already provides structure |

---

## Loading / empty / error

| State | Guidance |
|-------|----------|
| **Loading** | Prefer skeletons matching expected structure; avoid giant centered spinners when skeleton fits; avoid layout shift |
| **Empty** | Optional icon, concise title, short explanation, optional action — keep compact |
| **Error** | Concise message, restrained Danger accent, optional Retry via locked Button — do not fill the Card with aggressive red |

---

## Density, theme, Primary, RTL

- Cards inherit Preferences **compact / balance / comfort** and **light / dark / system**  
- No independent Card density preference  
- Selected / accent / featured / Primary emphasis use semantic `--exits-primary*` tokens — **PRIMARY is not hardcoded green**  
- Future Preferences → Primary Color must recolor selected, featured, and Primary accents; SUCCESS / WARNING / DANGER / INFO stay independent  
- RTL: logical properties for accents (`border-s`), leading visuals, actions, metadata, indicators  

---

## Accessibility

- A normal Card is **not** automatically interactive — do not add `role="button"` / `tabIndex` to static Cards  
- Interactive: semantic control (usually `button`), keyboard accessible, visible focus, accessible name  
- Selectable: correct radio/checkbox semantics; selected and disabled exposed programmatically  

---

## Not everything is a Card

Use Cards when they meaningfully group related information.

Avoid: Card inside Card inside Card; every field or row in a Card; excessive dashboard boxes; containers with no grouping purpose.

Prefer spacing, dividers, sections, and table structure when sufficient.

Nested Cards are discouraged by default — prefer spacing / divider / subtle surface for internal grouping. Nest only when the inner item is a genuine independent entity.

---

## Default usage recommendations (locked)

| Need | Recommendation |
|------|----------------|
| Normal information | BASIC + SURFACE or BORDERED · STATIC |
| Business summary | SUMMARY CARD |
| Dashboard metric | KPI CARD · STATIC (LIFT if clickable) |
| Business entity | ENTITY CARD |
| Clickable entity | ENTITY CARD + INTERACTIVE + LIFT |
| Product / media | PRODUCT CARD · MEDIA ZOOM |
| Interactive product | PRODUCT CARD + LIFT + MEDIA ZOOM |
| Quick action | ACTION CARD (+ LIFT or EXPAND by prominence) |
| Selection | SELECTABLE CARD (no strong expand) |
| Warning / error summary | STATUS CARD + accent START |
| Dense business info | COMPACT CARD |
| Prominent clickable | EXPAND (`expandScale` standard) |
| Featured / recommended | FEATURED + ACCENT + EXPAND |

---

## Cursor shorthand (locked)

### Types

`BASIC CARD` · `SUMMARY CARD` · `KPI CARD` · `ACTION CARD` · `ENTITY CARD` · `PRODUCT CARD` · `SELECTABLE CARD` · `STATUS CARD` · `COMPACT CARD` · `FEATURED CARD`

### Treatments

`SURFACE` · `BORDERED` · `ELEVATED` · `INTERACTIVE` · `SELECTED` · `ACCENT`

### Layout

`VERTICAL` · `HORIZONTAL`

### Motion

`STATIC` · `LIFT` · `EXPAND` · `MEDIA ZOOM` · `HOVER REVEAL`

### Options

`WITH ICON` · `WITH CHIP` · `WITH COUNT` · `WITH IMAGE` · `WITH ACTIONS` · `WITH FOOTER`

### Examples

| Phrase | Meaning |
|--------|---------|
| Customer → `ENTITY CARD + WITH CHIP + WITH ACTIONS` | Entity composition |
| Clickable customer → `ENTITY CARD + INTERACTIVE + LIFT` | Normal clickable entity |
| Featured customer → `ENTITY CARD + ACCENT + EXPAND` | Prominent featured entity |
| Product → `PRODUCT CARD + WITH IMAGE + WITH CHIP` | Product media |
| Interactive product → `PRODUCT CARD + LIFT + MEDIA ZOOM` | Clickable product |
| Warehouse selection → `SELECTABLE CARD` | Option selection |
| Today's Sales → `KPI CARD` | Dashboard metric |
| Request Stock → `ACTION CARD + BORDERED` | Quick action |
| Low Stock → `STATUS CARD WARNING` | Status accent |
| Dense warehouse info → `COMPACT CARD` | Dense padding |
| Recommended plan → `FEATURED CARD + ACCENT + EXPAND` | Featured pricing |

---

## Explicit overrides

Priority:

1. Explicit task instruction  
2. ExItS Card Standard (this document)  
3. Existing page presentation  

Example: `ENTITY CARD + INTERACTIVE + EXPAND` overrides the normal ENTITY + LIFT recommendation.

Accessibility, usability, and domain correctness remain mandatory.

---

## UI Standards

`/ui-standards` → **Cards** is the human visual reference.

Retain approved samples: treatments, types (KPI / Entity / Product / Selectable / Status / Compact), motion, Featured / pricing, media zoom, hover reveal, loading / empty / error, real-world examples, Cursor shorthand.

Status wording: **APPROVED / LOCKED**.

---

## Migration policy

Do **not** mass-migrate existing application Cards in lock / docs-only tasks.

Prefer focused conversions when a later task asks (e.g. “Convert customer list tiles to ENTITY CARD + INTERACTIVE + LIFT”).

---

## Related standards

- [ExItS Button Standard](exits-button-standard.md) — APPROVED / LOCKED (Card actions)  
- [ExItS Chip Standard](exits-chip-standard.md) — APPROVED / LOCKED (chips / counts inside Cards)  
- [ExItS Tabs Standard](exits-tabs-standard.md) — APPROVED / LOCKED (tabs inside Cards)  
- [ExItS Table Standard](exits-table-standard.md) — APPROVED / LOCKED (tables; avoid redundant Card wrapping)
