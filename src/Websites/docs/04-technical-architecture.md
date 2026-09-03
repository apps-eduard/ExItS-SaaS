# 04 — Technical Architecture

## Stack (Frozen)

| Layer | Choice | Notes |
|---|---|---|
| Framework | **Next.js** | App Router preferred; confirm version during WEB-01 |
| Language | **TypeScript** | Strict mode |
| UI library | **React** | Via Next.js |
| Styling | **Tailwind CSS** | Design tokens defined in [05-design-system.md](05-design-system.md) |
| Component foundation | **shadcn/ui primitives** + custom ExItS components | shadcn is a foundation only — site must not look like a default shadcn template |
| Content | **MDX** | For legal pages and content-heavy sections |
| Animation | **Motion** (formerly Framer Motion) | Selective use; respect `prefers-reduced-motion` |
| Icons | **Lucide** | |
| Forms | **React Hook Form** | |
| Validation | **Zod** | |
| Unit/component tests | **Vitest + React Testing Library** | |
| E2E | **Playwright** | |
| SEO | Next.js Metadata API, Open Graph, JSON-LD, canonical, sitemap, robots | See [06-seo-and-discoverability.md](06-seo-and-discoverability.md) |
| Images | **Next.js Image** | |
| Deployment | **Docker-compatible** | Specific host TBD — WEB-D-03 |

---

## Authority Boundaries

### What Next.js owns (public website)

- Page rendering and routing
- Marketing content and copy
- SEO metadata
- Contact / waitlist / inquiry form submission (forwarded to backend or email service)
- Visual design and brand presentation

### What Next.js does NOT own

The following authorities remain exclusively in the existing ASP.NET Core ExItS Platform and Product APIs. **Do not reimplement these in Next.js:**

| Authority | Owner |
|---|---|
| Authentication | ExItS Platform API |
| Organization / account management | ExItS Platform API |
| Staff memberships | ExItS Platform API |
| Subscriptions and entitlements | ExItS Platform API |
| POS selling, inventory, purchasing | ExItS PinoyBusinessPOS API |
| Customer credit (Utang) | ExItS PinoyBusinessPOS API |
| All other product operational logic | Respective product APIs |
| Payments / billing | ExItS Platform API |

---

## Project Structure (Target — not created yet)

```
src/Websites/ExItS.Web/
├── app/                    Next.js App Router pages
│   ├── (marketing)/        Public marketing routes
│   │   ├── page.tsx        /
│   │   ├── products/
│   │   ├── pos/
│   │   ├── service-pro/
│   │   ├── pricing/
│   │   ├── about/
│   │   └── contact/
│   ├── privacy/
│   └── terms/
├── components/
│   ├── exits/              ExItS custom components (see components.md)
│   └── ui/                 shadcn primitives
├── content/                MDX content files
├── lib/                    Utilities, constants
├── public/                 Static assets, robots.txt, sitemap (or generated)
├── styles/
│   └── globals.css         Tailwind base + custom tokens
├── next.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── vitest.config.ts
├── playwright.config.ts
└── package.json
```

> **This directory does not exist yet.** Create it only when WEB-01 is authorized.

---

## Rendering Strategy

| Route | Strategy | Rationale |
|---|---|---|
| `/` | Static (SSG) | Marketing content; no user-specific data |
| `/products` | Static (SSG) | Product listing |
| `/pos` | Static (SSG) | Product detail |
| `/service-pro` | Static (SSG) | Coming-soon page |
| `/pricing` | Static (SSG) | Pricing display; no live API for prices initially |
| `/about` | Static (SSG) | Company page |
| `/contact` | Static + client form | Form submit hits API or email service |
| `/privacy` `/terms` | Static (MDX) | Legal text |

Prefer Server Components and static rendering everywhere possible. Minimize client-side JavaScript bundle.

---

## Backend API Integration

The public website calls the existing ExItS Platform API only for:
- Verifying public organization/store landing pages (already implemented: `GET /api/v1/organizations/public/store/{publicId}`)
- Contact/waitlist form submissions (endpoint TBD — WEB-D-08)

No new backend endpoints should be created solely for website marketing copy.

---

## No Marketing Database

No separate database is required initially. All persistent data (signups, inquiries) flow through existing Platform API or a managed email/CRM service (TBD WEB-D-08).

---

## Deployment

- Docker-compatible container image.
- Specific host, CDN, and domain configuration: **TBD — WEB-D-03**.
- `exits.ph` domain ownership assumed; DNS not yet configured.
- Candidate architecture: `exits.ph` → Next.js static/SSR; `app.exits.ph` → ExItS SaaS app (TBD WEB-D-04).
