# ExItS Module Subnav Standard

**Status:** PILOT / CANDIDATE (not locked)  
**Visual reference:** `/ui-standards` → Module Subnav  
**Component:** `ExItS.PinoyBusinessPOS.React/src/components/exits/ModuleSubnav.tsx`

---

## Purpose

**MODULE SUBNAV** navigates between **related routes/pages inside one module**.

It is **not**:

| Primitive | Use for |
|-----------|---------|
| **Button** | Action or explicit action/navigation command (New PO, Receive stock) |
| **Tabs** | Switch content/panel/state **inside the same view** |
| **Filter / Segmented** | Change filter/view of the **same dataset** |
| **Sidenav** | Top-level application modules (Catalog, Inventory, Purchasing) |

---

## When to use

- Purchasing: Purchase orders · Incoming · Ready to receive · Direct · Suppliers  
- Orders: All · Pending · Completed · Cancelled  
- Inventory: Stock · Movements · Adjustments · Expiry  
- Settings subsections (vertical)

## When NOT to use

- In-page Details / History / Activity → **Tabs**  
- All / Active / Inactive dataset filters → **Filter / Segmented**  
- Module-to-module jumps → **Sidenav**  
- Primary CTAs → **Button**

---

## Routing & accessibility

- Prefer `<nav aria-label="…">` + route links (`NavLink` / equivalent)  
- Current route: `aria-current="page"`  
- **Do not** use `role="tab"` / `tablist` / `aria-selected` / tabpanel  
- Keyboard: normal link Tab / Enter — **not** arrow-key Tabs semantics  
- Pages own routing, permissions, and count data — Module Subnav does **not** fetch or mount destinations  

---

## Variants (candidates)

| Variant | Notes |
|---------|--------|
| **UNDERLINE** | Understated page-header sub-nav |
| **SOFT** | General module navigation |
| **PILL** | Compact independent pills |
| **PILL BAR** | Continuous outer bar + inner active pill (prominent) |
| **COMPACT** | Dense admin footprint |
| **VERTICAL** | Settings / many destinations |

Do **not** name a route-nav variant “Segmented” — keep Segmented for filters.

---

## Active treatments

- Underline → Primary underline  
- Soft / Compact / Vertical → Primary soft surface  
- Pill / Pill Bar → **SOLID PRIMARY ACTIVE** (recommended default for Pill Bar) or **SOFT PRIMARY ACTIVE**  

No gradients for navigation active state.

---

## Composition

Independent dimensions:

`WITH ICON` · `NO ICON` · `WITH COUNT` · `NO COUNT` · `CONTENT WIDTH` · `EQUAL WIDTH` · `SCROLLABLE MOBILE`  
Counts: `NEUTRAL COUNT` (default) · `PRIMARY COUNT` · `SEMANTIC COUNT` (explicit only)

Reuse locked **CountBadge**. Module Subnav **may show 0**; sidenav activity badges hide 0.

---

## Responsive / mobile

- One row · no wrap · no unreadably small type  
- Horizontal scroll when needed (`overflow-x: auto`)  
- Equal width may fall back to content width + scroll on narrow viewports  
- Active item scroll-into-view (`nearest`, respect reduced motion)  

---

## Theme · density · RTL · motion

- Semantic Primary tokens (not hardcoded green) — Preferences Primary Color ready  
- Global compact / balance / comfort  
- Logical start/end · RTL-safe  
- Restrained transitions · `prefers-reduced-motion`  

---

## Cursor shorthand (pilot)

```
Use the approved ExItS Module Subnav pilot.
Apply: MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE.
Context: Use related route navigation with aria-current="page"; do not use tab/tabpanel semantics.
Preserve existing routing, business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.
```

Examples: `MODULE SUBNAV + UNDERLINE` · `MODULE SUBNAV + SOFT + WITH COUNT` · `MODULE SUBNAV + VERTICAL`

---

## Recommended candidates (not locked)

| Context | Candidate |
|---------|-----------|
| General module | SOFT + CONTENT WIDTH |
| Prominent module / Purchasing | PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE |
| Dense admin | UNDERLINE |
| Settings | VERTICAL |

Lock only after product-owner visual approval + a dedicated STANDARD LOCK task.
