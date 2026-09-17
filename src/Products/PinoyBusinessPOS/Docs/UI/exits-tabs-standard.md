# ExItS Tabs Standard

**Status:** APPROVED / LOCKED  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Canonical foundation:** `ExItS.PinoyBusinessPOS.React/src/components/exits/ExitsTabs.tsx`  
**Shared variants (CVA):** `ExItS.PinoyBusinessPOS.React/src/components/exits/tab-variants.ts`  
**Count reuse:** `CountBadge` from `components/exits/CountChip.tsx` (ExItS Chip Standard — APPROVED / LOCKED)

**Visual authority:** `/ui-standards` → **Tabs**  
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsTabsPanel.tsx`)

The shared ExItS Tabs foundation is the **canonical tab / navigation presentation system** for Pinoy Business POS React.

Pages **MUST** reuse it whenever the required navigation / tab behavior can be expressed by the shared system.

Pages **MUST NOT** create page-local visual tab systems.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component paths and `/ui-standards` → Tabs instead.

Tabs are **selectors / navigation**, not action Buttons. Do not give them Button-style elevated hover. Tab shapes and active treatments are **independent** from the locked ExItS Button Standard and Chip Standard (except CountBadge reuse).

---

## Architecture

```
React Tabs foundation (`ExitsTabs`)
    ↓
CVA / shared variants (`tab-variants.ts`)
    ↓
Tailwind utility classes
    ↓
ExItS semantic CSS tokens (`--exits-*`, density, theme, motion)
```

| Layer | Role |
|-------|------|
| **`ExitsTabs`** | Presentation + local keyboard / ARIA (`tablist` / `tab` / `tabpanel`) |
| **CVA / `tab-variants`** | Variant, layout, active-treatment, scrollable, panel classes |
| **Tailwind** | Applies styles |
| **ExItS tokens** | Visual identity (Primary, surfaces, density, motion, reduced-motion) |

ExItS owns the visual design. No third-party visual Tabs library defines the appearance.

Accessibility primitives may be used internally where already present; the current foundation implements native roles and roving tabindex.

---

## Canonical API (current)

| Piece | Location / API |
|-------|----------------|
| Foundation | `ExitsTabs` |
| Tab items | `ExitsTabItem` (`key`, `label`, optional `icon`, `count`, `countTone`, `disabled`, `title`, …) |
| List / container | `exitsTabsListVariants` inside `ExitsTabs` (`role="tablist"`) |
| Trigger | Button with `role="tab"` + `exitsTabTriggerVariants` |
| Panel | Optional `panels` / `children` via `exitsTabsPanelVariants` (`role="tabpanel"`) |
| Variant | `variant`: `underline` \| `soft` \| `pill` \| `pillBar` \| `segmented` \| `enclosed` \| `vertical` |
| Layout | `layout`: `equal` \| `content` (esp. Pill Bar) |
| Active treatment | `activeTreatment`: `solid` \| `accent` (Pill Bar; default **solid**) |
| Overflow | `scrollable` |
| Icon-only | `iconOnly` (SPECIAL USE) |

**Independent dimensions:** `variant` + icon presence + count presence (+ `layout` / `activeTreatment` where applicable). Do **not** invent exploded variants such as `pillWithIcon` or `underlineWithCount`.

---

## Locked variants

All seven current variants are **APPROVED / LOCKED**. Do not remove or merge them.

| Variant | API | Canonical use | Default recommendation |
|---------|-----|---------------|------------------------|
| **UNDERLINE** | `underline` | Page / module navigation; low visual weight | **PAGE / MODULE NAVIGATION → UNDERLINE** |
| **SOFT** | `soft` | Normal content switching; subsections | **NORMAL CONTENT TABS → SOFT** |
| **PILL** | `pill` | Compact category / status; **separate** pills with gaps | **COMPACT CATEGORY / STATUS → PILL** |
| **PILL BAR** | `pillBar` | Prominent compact bar; continuous outer pill + inner active pill | **PROMINENT COMPACT TAB BAR → PILL BAR** |
| **SEGMENTED** | `segmented` | View / mode switcher; connected segments | **VIEW SWITCHER → SEGMENTED** |
| **ENCLOSED** | `enclosed` | Detail / record panels; selected connects to content | **DETAIL / RECORD PANEL → ENCLOSED** |
| **VERTICAL** | `vertical` | Settings / Preferences / Admin | **SETTINGS / ADMIN → VERTICAL** |

### UNDERLINE

- Active bottom indicator (~2px) using semantic Primary
- Stronger active label; quiet inactive tabs
- Prefer for Products / Inventory / Orders–style module navigation

### SOFT

- Selected: soft Primary surface + Primary text (not a Primary Button)
- Restrained rounded surface inside a quiet container

### PILL

- **Independent** fully-rounded items with **visible gaps**
- Example: `[ All ]  [ Active ]  [ Archived ]`
- **Not** a continuous bar; **not** Segmented

### PILL BAR

- One continuous fully-rounded **outer** bar (Primary-soft / muted surface)
- Tabs live inside the bar; little / no separation between inactive items
- Selected tab is its own filled **inner** rounded pill
- **PILL BAR ≠ PILL** (no independent gaps)
- **PILL BAR ≠ SEGMENTED** (not connected segment-control chrome)

**Layouts (locked):**

| Layout | API | Behavior | Good for |
|--------|-----|----------|----------|
| EQUAL WIDTH | `layout="equal"` | Tabs share available width | All / Pending / Completed / Cancelled |
| CONTENT WIDTH | `layout="content"` | Width follows label / content | Variable category labels |

Do **not** force one layout globally.

**Active treatment (locked API; default `solid`):**

| Treatment | API | Visual |
|-----------|-----|--------|
| SOLID PRIMARY | `activeTreatment="solid"` | Strong Primary fill + contrast text |
| PRIMARY ACCENT | `activeTreatment="accent"` | Primary-soft fill + stronger Primary text / border |

### SEGMENTED

- Shared structural container; connected segments; selected segment surface
- Examples: List \| Grid · Day \| Week \| Month · Retail \| Warehouse

### ENCLOSED

- Selected tab connects visually to the content panel border
- Detail / record section switching

### VERTICAL

- Side indicator + soft selected surface
- **Do not** use Vertical Tabs as a replacement for the application sidenav

---

## Icons

Icons are an **independent option** (Lucide).

Supported compositions:

- TEXT ONLY  
- ICON + TEXT  
- TEXT + COUNT  
- ICON + TEXT + COUNT  

Canonical order: **`[ICON] Label [COUNT]`**

Examples: `LayoutDashboard` Overview · `Package` Products · `Boxes` Inventory · `ClipboardList` Orders · `Users` Customers · `Settings` Settings · `History` Activity · `BarChart3` Reports · `TriangleAlert` Low stock.

Icons are optional. Do **not** require an icon on every tab.

### ICON ONLY (SPECIAL USE)

Allowed for compact tool switchers (e.g. List / Grid / Chart).

Required: `aria-label`, tooltip / `title`, visible active state, keyboard support.

Do **not** use icon-only for major business navigation where text meaning matters.

---

## Counts

Tabs may use **WITH COUNT**. Count appears **after** the label.

**Reuse** the locked ExItS **`CountBadge`** foundation.

Do **not** invent visually unrelated `TabBadge` / `TabCountBadge` / `TabCounter` components (thin adapters around `CountBadge` only if required).

### Count color policy (locked)

| Case | Tone |
|------|------|
| Normal quantity (e.g. Orders `[24]`) | **NEUTRAL** |
| Active-tab count (when desired) | May use **PRIMARY**-aware treatment per current `ExitsTabs` / caller `countTone` |
| Semantic attention (Low stock, Overdue) | **WARNING** / **DANGER** (etc.) only when the number itself communicates status |
| Completed `[16]` | Normally **NEUTRAL** — not bright Success green |

Do **not** make every count colorful.

---

## Primary color rule

Selected Tabs **MUST** use semantic ExItS Primary tokens (`--exits-primary`, soft mixes, `--exits-primary-contrast`, ring).

**PRIMARY does not mean hardcoded green.**

Current default Primary palette may be green. Preferences → Primary Color may change Primary (Blue, Purple, Teal, Orange, …). Then these follow automatically:

- active underline  
- selected Soft / Pill / Pill Bar / Segmented / Vertical treatments  

Semantic **WARNING / DANGER / SUCCESS / INFO** counts remain independent of Primary.

---

## Motion

Lock the **current** approved micro-interactions:

- Fast, subtle, professional, immediate-feeling  
- No layout shift  
- CSS transitions via `--exits-motion-fast` / `--exits-ease-standard`  

Typical per variant: underline indicator / Soft surface / Pill color-border / Pill Bar active-pill / Segmented selected surface / Vertical indicator-background.

Panel content may use the existing subtle panel enter animation.

**Forbidden:** bounce, spring, shake, large scale, dramatic sliding.

### Reduced motion

**REQUIRED:** respect `prefers-reduced-motion` (`motion-reduce:*` on the foundation). Remove unnecessary transform / scale / sliding while keeping clear selected / unselected state.

---

## Hover / press / disabled

- Inactive: subtle surface / stronger foreground on hover  
- Active: remains clearly selected  
- Press: very subtle (if any); not Button lift  
- Disabled: reduced emphasis; no misleading hover; skipped in keyboard navigation; correct disabled semantics  

---

## Density & theme

Tabs inherit global **compact / balance / comfort** and **light / dark / system**.

No separate Tabs density preference. Height, padding, gaps, and icon size use shared ExItS control / text tokens (`--exits-control-height`, `--exits-control-padding-x`, `--exits-text-sm`, …).

No page-local dark-mode tab system.

---

## Mobile / many tabs

When horizontal tabs do not fit: **horizontal scrolling** (`scrollable`) rather than excessive label shrink or multi-row wrap by default.

Requirements: touch-friendly, no broken overflow, selected tab reachable / scroll-into-view, readable labels. Pill Bar may scroll as a continuous bar.

---

## RTL

Tabs MUST support the existing RTL / i18n system. Prefer logical properties (`border-e`, `pe-*`, gap). Verify order, icon spacing, count placement, scroll, indicators, and keyboard (ArrowLeft / ArrowRight reverse under RTL).

---

## Accessibility

Current foundation provides:

- `role="tablist"` / `role="tab"` / `role="tabpanel"`  
- `aria-selected`, `aria-controls`, labelled panels  
- Roving `tabIndex`  
- Horizontal: ArrowLeft / ArrowRight (RTL-aware)  
- Vertical: ArrowUp / ArrowDown  
- Home / End  
- Disabled tabs skipped  

The active pill / underline is **visual presentation only** — do not sacrifice tab semantics.

---

## Local vs route tabs

| Kind | Meaning | Who owns behavior |
|------|---------|-------------------|
| **LOCAL TABS** | Switch content on the current page (Details / History / Notes) | Page state |
| **ROUTE TABS** | Real application navigation (Products / Inventory / Orders) | Page routing |

Visual presentation may be shared. The Tabs foundation **MUST NOT** own routing, APIs, permissions, or business rules.

### Responsibility boundary

**Foundation owns:** presentation, variants, states, orientation, icon / count layout, theme, density, responsive scroll, accessibility, local keyboard.

**Page owns:** routing, React Query / APIs, permissions, business rules, count **source**, which tabs exist, lazy loading, mutations.

---

## Default recommendations (locked)

| Intent | Variant |
|--------|---------|
| PAGE / MODULE NAVIGATION | UNDERLINE |
| NORMAL CONTENT TABS | SOFT |
| COMPACT CATEGORY / STATUS | PILL |
| PROMINENT COMPACT TAB BAR | PILL BAR |
| VIEW SWITCHER | SEGMENTED |
| DETAIL / RECORD PANEL | ENCLOSED |
| SETTINGS / ADMIN | VERTICAL |

These are **defaults**. Explicit task instructions may override them.

---

## Cursor shorthand (locked)

### Variants

`UNDERLINE TABS` · `SOFT TABS` · `PILL TABS` · `PILL BAR TABS` · `SEGMENTED TABS` · `ENCLOSED TABS` · `VERTICAL TABS`

### Options

`WITH ICON` · `NO ICON` · `WITH COUNT` · `NO COUNT` · `ICON ONLY` · `EQUAL WIDTH` · `CONTENT WIDTH` · `SCROLLABLE` · `DISABLED`

### Count options

`NEUTRAL COUNT` · `PRIMARY COUNT` · `WARNING COUNT` · `DANGER COUNT` · `SEMANTIC COUNT`

### Examples

| Phrase | Meaning |
|--------|---------|
| Products / Inventory / Orders → `UNDERLINE TABS` | Module navigation |
| Order status → `PILL TABS + WITH COUNT` | Compact status pills |
| Prominent order-status bar → `PILL BAR TABS + EQUAL WIDTH + WITH COUNT` | Continuous bar |
| Product categories → `PILL BAR TABS + CONTENT WIDTH` | Content-sized bar |
| Products → `UNDERLINE TABS + WITH ICON + WITH COUNT` | Icon + CountBadge |
| List / Grid → `SEGMENTED TABS + WITH ICON` | View switcher |
| Settings → `VERTICAL TABS + WITH ICON` | Admin / prefs |
| Customer detail → `ENCLOSED TABS` | Record panel |
| Low stock → `WITH COUNT WARNING` | Semantic count |
| Mobile module navigation → `UNDERLINE TABS + SCROLLABLE` | Overflow scroll |
| Mobile pill bar → `PILL BAR TABS + SCROLLABLE` | Scrollable bar |

### Shorthand defaults

- `PILL BAR TABS` → current locked Pill Bar defaults (`layout` content unless specified; `activeTreatment` **solid**)  
- `PILL BAR TABS + WITH COUNT` → reuse locked **CountBadge**  
- `PILL BAR TABS + EQUAL WIDTH` → equal distribution  
- `UNDERLINE TABS + WITH ICON` → canonical `[ICON] Label` layout  

Do not require restating colors, radius, or motion in every task.

---

## Explicit overrides

Priority:

1. Explicit task instruction  
2. ExItS Tabs Standard (this document)  
3. Existing page presentation  

Example: `Use PILL BAR TABS + CONTENT WIDTH + NO ICON` overrides defaults.

Accessibility and domain correctness remain mandatory.

---

## UI Standards

`/ui-standards` → **Tabs** is the human visual reference.

Retain approved samples: all variants, Pill vs Pill Bar, equal / content width, icons, counts, icon + count, semantic counts, disabled / states, mobile overflow, real-world examples, Cursor shorthand.

Status wording: **APPROVED / LOCKED**.

---

## Migration policy

Do **not** mass-migrate existing application tabs in lock / docs-only tasks.

Prefer focused conversions when a later task asks (e.g. “Convert order status to PILL BAR TABS + EQUAL WIDTH + WITH COUNT”).

---

## Related standards

- [ExItS Button Standard](exits-button-standard.md) — APPROVED / LOCKED (do not redesign; Tabs are not Buttons)  
- [ExItS Chip Standard](exits-chip-standard.md) — APPROVED / LOCKED (`CountBadge` reuse only)  
- [ExItS Table Standard](exits-table-standard.md)  
