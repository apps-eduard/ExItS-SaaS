# Next.js Route Map

> Target project: `src/Websites/ExItS.Web/` (not yet created)
> App Router structure

---

## App Router File Map

```
app/
├── layout.tsx                  Root layout (ExItsHeader + ExItsFooter + drawer)
├── page.tsx                    / — Homepage
├── not-found.tsx               404 page
├── sitemap.ts                  /sitemap.xml (generated)
├── robots.ts                   /robots.txt (generated)
│
├── products/
│   └── page.tsx                /products
│
├── pos/
│   └── page.tsx                /pos
│
├── service-pro/
│   └── page.tsx                /service-pro
│
├── pricing/
│   └── page.tsx                /pricing
│
├── about/
│   └── page.tsx                /about
│
├── contact/
│   └── page.tsx                /contact
│
├── privacy/
│   └── page.tsx                /privacy (MDX content)
│
└── terms/
    └── page.tsx                /terms (MDX content)
```

---

## Rendering Mode per Route

| Route | Mode | Revalidation |
|---|---|---|
| `/` | Static (SSG) | On deploy |
| `/products` | Static (SSG) | On deploy |
| `/pos` | Static (SSG) | On deploy |
| `/service-pro` | Static (SSG) | On deploy |
| `/pricing` | Static (SSG) | On deploy |
| `/about` | Static (SSG) | On deploy |
| `/contact` | Static + client form | On deploy |
| `/privacy` | Static (MDX) | On deploy |
| `/terms` | Static (MDX) | On deploy |
| `/sitemap.xml` | Generated | On deploy |
| `/robots.txt` | Generated or static | On deploy |

---

## Future Routes (TBD — do not create yet)

| Potential Route | Product | Status |
|---|---|---|
| `/loan-manager` | Pinoy Loan Manager | TBD — marketing name and route TBD |
| `/bnpl` or similar | Pinoy Buy Now Pay Later | TBD — marketing name TBD |
| `/pawn-manager` | Pinoy Pawn Manager | TBD — marketing name TBD |

Do not create these routes until product marketing names are confirmed (WEB-D-05).

---

## Redirects

When DNS is configured:
- `www.exits.ph` → `exits.ph` (or vice versa, per WEB-D-03 decision)

No other redirects required initially.

---

## API Routes

The Next.js website does not expose API routes for core SaaS functionality.

Optional `app/api/` routes may be added for:
- Contact form proxying (if backend endpoint is not directly accessible from client — TBD WEB-D-08)
- Waitlist submission

If added, these must NOT implement auth, account management, or product-operational logic.
