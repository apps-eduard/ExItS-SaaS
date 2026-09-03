# Implementation Phases

> None of these phases have been started.
> `src/Websites/ExItS.Web/` does not exist yet.
> Create it only when WEB-01 is explicitly authorized.

---

## WEB-01 — Foundation and Design System

**Goal:** Establish the Next.js project, Tailwind design tokens, and base component primitives.

Deliverables:
- `src/Websites/ExItS.Web/` Next.js project scaffold
- `tailwind.config.ts` with ExItS design tokens (colors, typography, radius, shadows)
- `globals.css` with CSS custom properties
- Base shadcn/ui primitive installation
- `ExItsButton`, `ExItsInput`, `ExItsBadge` base components
- Vitest + React Testing Library setup
- Playwright setup
- `next.config.ts` base configuration
- Docker build (`Dockerfile`)

Gates:
- `npm run build` succeeds
- `npm run test` passes
- `npm run lint` clean
- Design token review against [05-design-system.md](../05-design-system.md)

---

## WEB-02 — Shell, Header, Footer, Navigation

**Goal:** Implement persistent layout components.

Deliverables:
- `ExItsHeader` — sticky, logo, primary CTA, drawer trigger
- `ExItsDrawerMenu` — right-side full-height drawer with backdrop dim, all navigation groups, responsive sizing
- `ExItsFooter` — 4-column link grid, social, copyright
- `ExItsBreadcrumbs` — breadcrumb component for secondary pages
- 404 page (`app/not-found.tsx`)
- Responsive behavior: desktop / tablet / mobile for all shell components
- Keyboard accessibility: focus trap in drawer, ESC to close, visible focus rings

Gates:
- Drawer opens and closes correctly on all breakpoints
- Keyboard navigation through drawer with focus trap
- All nav links resolve to correct routes
- Header passes WCAG AA contrast check

---

## WEB-03 — Homepage

**Goal:** Implement the homepage per [pages/home.md](../pages/home.md) and [content/homepage-copy.md](../content/homepage-copy.md).

Deliverables:
- All homepage sections (Hero → Footer)
- `ExItsHero` component
- `ExItsProductShowcase` component
- `ExItsStatsStrip` / `ExItsTrustStrip`
- `ExItsSegmentedTabs` for capability storytelling
- `ExItsFaq` accordion component
- `ExItsNewsletter` form component (submission endpoint TBD WEB-D-08)
- `ExItsCtaSection` final CTA
- Homepage JSON-LD (Organization schema)
- OG image: exits-og-home.png

Gates:
- No fake statistics, customer counts, or ₱ prices present
- Product readiness badges correct
- Core Web Vitals: LCP < 2.5s (check in dev with Lighthouse)
- Mobile layout verified at 375px, 768px, 1280px

---

## WEB-04 — POS Product Page

**Goal:** Implement `/pos` per [pages/pos.md](../pages/pos.md) and [content/pos-copy.md](../content/pos-copy.md).

Deliverables:
- All `/pos` sections
- `ExItsFeatureGrid` component
- Product screenshots / placeholders (WEB-D-06 must be resolved before real images)
- SoftwareApplication JSON-LD
- OG image: exits-og-pos.png
- Breadcrumb

Gates:
- All capability claims match CONFIRMED list in [products/pinoy-business-pos.md](../products/pinoy-business-pos.md)
- No unconfirmed capabilities presented as available
- Area section correctly describes Area as grouping, not inventory owner
- Mobile layout verified

---

## WEB-05 — Products Listing and Remaining Product Pages

**Goal:** Implement `/products` and `/service-pro` (plus routes for other products if added).

Deliverables:
- `/products` listing page with readiness badges
- `/service-pro` coming-soon page
- Placeholder routes for other products (HTTP 200 with coming-soon content or HTTP 404 until ready)

Gates:
- No product incorrectly classified as CONFIRMED without evidence
- Waitlist form connected to endpoint (TBD WEB-D-08)

---

## WEB-06 — Pricing Page

**Goal:** Implement `/pricing` per [pages/pricing.md](../pages/pricing.md).

**Blocked on:** WEB-D-01 (commercial pricing — Product Owner decision required)

Deliverables:
- `ExItsPricingCard` component with recommended badge treatment
- Pricing card layout (3-column desktop, stacked mobile)
- Feature comparison table
- Pricing FAQ section

Gates:
- No fake ₱ prices present unless WEB-D-01 is resolved
- Recommended card visual treatment correct
- Mobile stacked layout verified

---

## WEB-07 — About, Contact, and Forms

**Goal:** Implement `/about` and `/contact` pages with all forms.

Deliverables:
- `/about` page
- `/contact` page with General / Sales / Partnership forms
- `ExItsContactForm` component
- Form submission connected to backend (TBD WEB-D-08)
- Success and error states
- Accessible form validation (Zod + React Hook Form)

Gates:
- Forms submit correctly end-to-end
- Accessible error messages for all fields
- No placeholder team profiles published

---

## WEB-08 — Legal Pages

**Goal:** Implement `/privacy` and `/terms`.

**Blocked on:** WEB-D-07 (legal review required)

Deliverables:
- MDX content pages for privacy and terms
- Placeholder warning page until WEB-D-07 resolved

Gates:
- No template/placeholder legal text presented as ExItS's actual legal documents
- Print-friendly layout
- Footer links resolve correctly

---

## WEB-09 — SEO, Structured Metadata, Social Previews

**Goal:** Finalize all SEO per [06-seo-and-discoverability.md](../06-seo-and-discoverability.md).

Deliverables:
- All page metadata via Next.js Metadata API
- Canonical URLs on all pages
- Open Graph images (1200×630) for all pages
- JSON-LD schemas (Organization, SoftwareApplication, FAQPage)
- `sitemap.xml` generation (`app/sitemap.ts`)
- `robots.txt`
- Social preview verification

Gates:
- Google Rich Results Test passes for JSON-LD
- All OG images render correctly in social share preview tool
- sitemap.xml is valid and includes all public routes

---

## WEB-10 — Responsive, Accessibility, and Performance

**Goal:** Systematic audit across all breakpoints and accessibility requirements.

Deliverables:
- Responsive layout verification at 375px, 640px, 768px, 1024px, 1280px, 1536px
- Keyboard navigation audit on all interactive components
- Screen reader review (VoiceOver / NVDA)
- WCAG AA contrast audit (all text/background combinations)
- `prefers-reduced-motion` animation audit
- Lighthouse CI integration (LCP, INP, CLS)
- Image optimization audit
- Client JS bundle size audit

Gates:
- Lighthouse score ≥ 90 on Performance, Accessibility, Best Practices
- No keyboard navigation dead-ends
- No motion playing when `prefers-reduced-motion: reduce` is active

---

## WEB-11 — Analytics and Conversion Tracking

**Goal:** Implement analytics event tracking per [implementation/analytics.md](analytics.md).

**Blocked on:** WEB-D-02 (analytics vendor decision)

Deliverables:
- Analytics provider integration
- Event tracking for all events in [implementation/analytics.md](analytics.md)
- Privacy-compliant implementation (no PII in event payloads without legal sign-off)

---

## WEB-12 — Final E2E and Launch Hardening

**Goal:** Full end-to-end Playwright test coverage and production readiness verification.

Deliverables:
- Playwright E2E tests for all primary user journeys
- All pre-launch gates from [08-launch-plan.md](../08-launch-plan.md) verified
- Production build Docker image created and tested
- DNS and hosting configured (WEB-D-03 must be resolved)
- "Get Started" and "Sign In" links verified against live Platform

Gates:
- All WEB-01 through WEB-11 deliverables complete and gated
- All pre-launch gates in [08-launch-plan.md](../08-launch-plan.md) satisfied
- Final review by Product Owner
