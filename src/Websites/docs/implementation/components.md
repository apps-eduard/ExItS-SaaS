# Custom Component Inventory

> All components listed are to be created during WEB-01 and WEB-02 through WEB-07.
> shadcn/ui provides accessible primitives; ExItS components wrap/compose them with ExItS visual identity.
> The website must NOT look like a default shadcn/ui template.

---

## Layout / Shell Components

### `ExItsHeader`

- Sticky full-width header
- Logo (left), primary CTA, drawer trigger (right)
- Scrolled state: background blur or elevated surface
- Props: `transparent?: boolean` (for hero overlap if desired)
- shadcn primitives: none required (custom)

### `ExItsDrawerMenu`

- Right-side full-height drawer
- Triggered from header
- Groups: Products, Solutions, Pricing, Company, Resources
- Responsive width: ~33vw (desktop) / ~50vw (tablet) / 100vw (mobile)
- Backdrop: dim overlay with click-to-close
- Focus trap when open; ESC closes; returns focus on close
- shadcn primitives: `Sheet` or custom dialog/portal

### `ExItsFooter`

- 4-column link grid (desktop) → 2-col (tablet) → 1-col (mobile)
- Columns: Products | Solutions | Company | Legal
- Social links row (TBD — handles not yet confirmed)
- Copyright line

### `ExItsBreadcrumbs`

- Present on secondary pages only
- Separator: `/` or `›`
- Current page is non-linked
- shadcn primitives: none (simple nav element)

---

## Content / Marketing Components

### `ExItsHero`

- Full-bleed dark section
- Supports: headline (with outline/solid combination), sub-headline, dual CTAs, visual slot
- Props: `headline`, `subHeadline`, `primaryCta`, `secondaryCta`, `visual`

### `ExItsOutlineHeading`

- Renders a headline with mixed outline and solid text spans
- Used in hero and major section headers for visual impact

### `ExItsProductShowcase`

- Featured product editorial block (text + screenshot side by side)
- Alternating layout support (text-left / text-right)
- Props: `label`, `headline`, `body`, `cta`, `visual`, `reversed?`

### `ExItsFeatureGrid`

- Grid of feature cards (icon + headline + body)
- Layouts: 2-col, 3-col, 4-col
- Props: `items: FeatureItem[]`, `columns?: 2 | 3 | 4`

### `ExItsStatsStrip`

- Horizontal strip of benefit/proof statements
- Icon + short statement per item
- Horizontal scroll on mobile
- Props: `items: StatItem[]`

### `ExItsTrustStrip`

- Similar to StatsStrip but focused on trust/reassurance messaging
- May include subtle icon + bold short phrase format

### `ExItsSegmentedTabs`

- Segmented control with active indicator (emerald gradient)
- Tab panels with content (feature screenshots, capability descriptions)
- shadcn primitives: `Tabs`
- Responsive: horizontal tabs (desktop), scrollable tabs or accordion (mobile)

### `ExItsPricingCard`

- Pricing tier card
- Props: `planName`, `price`, `features: string[]`, `recommended?: boolean`, `cta`
- Recommended variant: emerald gradient border or elevated visual emphasis
- "Recommended" badge on the `recommended` card

### `ExItsFaq`

- Accordion FAQ section
- shadcn primitives: `Accordion`
- `aria-expanded`, `aria-controls` on triggers
- Props: `items: FaqItem[]`

### `ExItsNewsletter`

- Email capture block
- Headline + sub-headline + email input + submit button
- Success / error inline states

### `ExItsCtaSection`

- Full-width final CTA section
- Gradient or elevated dark surface
- Props: `headline`, `primaryCta`, `secondaryCta?`

---

## Form Components

### `ExItsContactForm`

- General contact / sales / partnership form
- React Hook Form + Zod validation
- 2-column (desktop) / 1-column (mobile)
- Accessible error messages
- Submit → backend endpoint (TBD WEB-D-08)

### `ExItsFormField`

- Wrapper for a labelled, validated input field
- Props: `label`, `name`, `type`, `placeholder`, `error?`
- Dark field background, white label, emerald focus border

---

## Utility / Primitive Extensions

### `ExItsBadge`

- Small badge for product readiness: "Available" / "Coming Soon" / "In Development"
- Variants: `available` (emerald), `coming-soon` (amber/muted), `in-development` (blue/muted)

### `ExItsButton`

- Primary: deep emerald → bright green gradient, white text
- Secondary: outlined emerald border, emerald text
- Ghost: text only, arrow icon
- All variants: `:focus-visible` ring, disabled state, loading state

---

## Component Design Rules

1. shadcn/ui is the accessible primitive layer — use its ARIA patterns, keyboard handling, and portal logic.
2. All visual styling is applied via ExItS Tailwind tokens. Do not use shadcn default color names.
3. Every interactive component must support keyboard navigation.
4. All components must respect `prefers-reduced-motion`.
5. Components do not contain business logic, API calls (except form submit), or auth.
