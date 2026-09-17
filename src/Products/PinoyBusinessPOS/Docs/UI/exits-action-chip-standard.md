# ExItS Action Chip Standard

**Status:** PILOT / CANDIDATE (not locked)  
**Visual reference:** `/ui-standards` → Action Chips  
**Production baseline:** `ExitsChipBar` `variant="actions"`  
**Pilot component:** `ExItS.PinoyBusinessPOS.React/src/components/exits/ActionChipBar.tsx`

---

## Purpose

**ACTION CHIP** — lightweight quick action inside a page/module (Refresh, Export, Scan, Start count).

**NAVIGATION ACTION CHIP** — lightweight shortcut to another workflow/page (Stock Count, Stock Use, Waste / Loss, Production).

These are **not** structural module navigation. Inventory workflow shortcuts are Nav Action Chips; Purchasing sibling destinations (Purchase orders · Incoming · …) may be Module Subnav.

---

## Relation to ExitsChipBar

Production already uses `ExitsChipBar` `variant="actions"` extensively (buttons via `onSelect`, links via `href`, optional `emphasis="primary"`, icons, density tokens, wrap + narrow-viewport scroll).

This pilot **formalizes** that pattern with richer visual/grouping candidates in `ActionChipBar`.  
**Do not mass-migrate** production pages until lock (`POS-EXITS-ACTION-CHIP-STANDARD-LOCK-01`).

---

## Semantic map

| Primitive | Use for |
|-----------|---------|
| **Action Chip** | Lightweight in-page action |
| **Nav Action Chip** | Lightweight workflow/page shortcut |
| **Filter Chip** | Change/filter current dataset |
| **Tab** | Switch content/panel in the same view |
| **Module Subnav** | Sibling routes that define a module’s structure |
| **Button** | Primary CTA, form submit, destructive / strong hierarchy |

---

## Rendering semantics

- Action (`onSelect`) → native `<button type="button">`
- Navigation (`href`) → React Router `Link`
- Optional group `role="toolbar"` when it is truly an action toolbar
- **Never** `role="tab"` / `aria-selected` / tablist for Action Chips
- No persistent selected/active state (that belongs to Filter / Tab / Module Subnav)

---

## Visual variants (candidates)

| Variant | Notes |
|---------|--------|
| **SOFT** | Subtle filled/tinted — general default candidate |
| **OUTLINE** | Bordered surface — close to current production chip |
| **GHOST** | Quiet utilities / dense toolbars |
| **SOLID PRIMARY** | One emphasized action among peers |
| **TINTED PRIMARY** | Matches production `exits-chip--primary` emphasis |
| **ELEVATED** | Restrained depth — candidate only |
| **GRADIENT** | Same-family Primary gradient — **SPECIAL USE / NOT DEFAULT** |

---

## Shapes

**PILL** · **SOFT** · **SQUARE** (locked Chip shape language).  
Recommended: Soft or Pill. Square only if touch targets remain adequate.

---

## Content

`TEXT ONLY` · `WITH ICON` · `TRAILING ICON` · `ICON ONLY` · `WITH COUNT` · `DISABLED` · `LOADING`

- Workflow shortcuts → prefer **WITH ICON**
- Icon-only → `aria-label` + `title` + focus ring
- Counts → locked **CountBadge**; tone explicit; default **HIDE ZERO** for nav/workflow shortcuts
- Loading only for direct chip execution (e.g. Refresh), not for navigation links

---

## Grouping

| Pattern | When |
|---------|------|
| **INLINE** | 2–4 short actions |
| **WRAP** | Strong general default; excellent mobile |
| **SCROLL** | One-line toolbar / many compact actions — not universal default |
| **RESPONSIVE AUTO** | Preferred production-friendly: wrap → narrow scroll via CSS |
| **GRID** | 4–8 equal mobile launchers |
| **PRIMARY FIRST** | One tinted/solid primary + neutral peers |
| **SECTIONED** | Only when categories are justified |
| **OVERFLOW** | Many lower-priority actions — never hide critical |
| **PRIMARY BUTTON + ACTION CHIPS** | Dominant CTA + lightweight shortcuts |
| **TRAILING UTILITIES** | Start actions + end utilities (Refresh / More) |

Mobile requirements: readable labels, density touch targets, no page overflow; scroll groups scroll only themselves; primary stays visible (not buried in More).

---

## When NOT to use

- Primary form submit / large CTA → **Button**
- Irreversible destructive → **Button DANGER / DANGER STRONG**
- Dataset filter → **Filter Chip**
- In-view panels → **Tabs**
- Module structural siblings → **Module Subnav**
- Status / tags / metadata → Status / Tag chips

---

## Accessibility · RTL · Theme · Density · Performance

- Native button / link semantics; visible focus; disabled not clickable
- Logical CSS (`ms`/`me`, `ps`/`pe`, start alignment); scroll/wrap/grid remain logical
- Theme tokens (`--exits-primary*`) — future Preferences primary color updates Primary treatments
- Density from `--exits-chip-*` (compact / balance / comfort)
- No animation/layout/carousel libraries; CSS + React + Lucide + router only
- Respect `prefers-reduced-motion`

---

## Clipboard (pilot)

```
Use the approved ExItS Action Chip pilot.
Apply: <EXACT SHORTHAND>.
Context: …
Preserve existing routing, business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.
```

After lock: switch wording to locked Standard.

---

## Lock gate

Do **not** create an authoritative Cursor rule yet.  
Next: `POS-EXITS-ACTION-CHIP-STANDARD-LOCK-01` after visual approval.
