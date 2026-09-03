# 05 — Design System

## Design Reference

Primary visual inspiration: **https://onegiantleap.com/**

Use as structural and visual principle reference only. Do NOT copy LEAP branding, copy, illustrations, logos, or event-specific widgets.

Study and adapt:
- Premium dark visual system
- Near-black backgrounds with atmospheric gradients
- Oversized display typography
- Outline + solid headline combination technique
- Generous vertical section spacing
- Thin border treatment
- Minimal conventional shadows
- Dark elevated surfaces
- Strong CTA visual hierarchy
- Gradient CTA treatment
- Sticky header pattern
- Right-side full-height drawer with dimmed backdrop
- Segmented tabs
- Premium pricing cards with recommended-plan emphasis
- Feature/trust strips
- FAQ accordions
- Editorial / full-bleed product heroes

---

## ExItS Visual Identity

ExItS uses **green** as its primary brand color. It is NOT a purple LEAP clone.

### Color Tokens (Tailwind CSS — to be finalized in WEB-01)

| Token | Description | Approximate Value |
|---|---|---|
| `bg-base` | Page background | Near-black / very dark green (`#080e0a` approx) |
| `bg-surface` | Card / section surfaces | Charcoal / deep forest (`#0f1a12` approx) |
| `bg-elevated` | Elevated panels | Slightly lighter dark green surface |
| `text-primary` | Primary body text | White / off-white (`#f0f4f1`) |
| `text-muted` | Secondary / muted text | Gray-green (`#8aa690`) |
| `color-primary` | Brand primary | Emerald green (`#10b981` approx) |
| `color-primary-bright` | Accent / hover | Fresh bright green (`#34d399` approx) |
| `color-secondary` | Secondary accent | Cyan (`#06b6d4` approx) |
| `border-default` | Thin borders | White/green at low opacity (`rgba(255,255,255,0.08)`) |
| `border-active` | Active/focus borders | Emerald at medium opacity |

> Exact hex values and CSS custom property names are finalized during WEB-01. These are direction tokens only.

### Gradient Usage Rules

**Use gradients for:**
- Primary CTA buttons: deep emerald → bright green
- Active segmented control indicator
- Ambient background glow (section-level)
- Featured product promo hero
- Recommended pricing card emphasis border/background

**Do NOT use gradients on:**
- Every card
- Generic body text
- Decorative backgrounds that compete with content
- Excessive glassmorphism layers

### Shadows

Minimal. Dark-on-dark design relies on borders and surface elevation, not conventional box shadows.

### Border Radius

Medium to moderately large. Not bubble-style. Target: `rounded-lg` to `rounded-xl` in Tailwind terms.

---

## Typography

| Role | Style |
|---|---|
| Display hero | Oversized (64–96px+); optionally OUTLINE weight for keyword, SOLID for context |
| Section heading | 32–48px; bold |
| Product heading | 24–32px; semi-bold |
| Body | 15–17px; regular weight; good line-height for readability |
| Muted / caption | 13–14px; muted color |
| CTA | 15–16px; semi-bold; button-contained |

Font family: TBD in WEB-01. Prefer a clean, slightly geometric sans-serif that reads well in English and Filipino. Should not appear "Western-corporate."

---

## Design Principles

- **70% premium visual impact / 30% product clarity.** Not 100% spectacle.
- Real product screenshots are more valuable than abstract illustrations.
- Content must be readable first, beautiful second.
- Animations must not block content; all animations must respect `prefers-reduced-motion`.
- No background autoplay video on hero sections by default.

---

## Component Library Philosophy

`shadcn/ui` provides accessible primitives (Button, Dialog, Accordion, Tabs, etc.).

All marketing-facing components must be **custom ExItS components** that use shadcn primitives internally but carry ExItS visual identity. The website must NOT look like a default shadcn/ui template.

See full component inventory: [implementation/components.md](implementation/components.md)

---

## Responsive Breakpoints

Standard Tailwind breakpoints:
- `sm` — 640px
- `md` — 768px
- `lg` — 1024px
- `xl` — 1280px
- `2xl` — 1536px

Mobile-first approach. Do not simply shrink the desktop layout on mobile — design distinct compositions for mobile where needed.

---

## Accessibility Requirements

- WCAG AA contrast ratios on all text over backgrounds.
- All interactive elements keyboard navigable.
- Visible focus indicators (emerald ring, not default browser style).
- Drawer menu: focus trapped when open, restored on close; `aria-modal`, `aria-expanded`.
- Accordions: `aria-expanded`, `aria-controls`.
- Forms: `aria-label` / `aria-describedby` on all inputs; error states associated with inputs.
- Images: meaningful `alt` text; decorative images use `alt=""`.
- Heading hierarchy: one `<h1>` per page; logical `h2`, `h3` nesting.
- `prefers-reduced-motion`: disable or reduce all Motion animations.
- Semantic HTML throughout (no `<div>` for interactive elements).
