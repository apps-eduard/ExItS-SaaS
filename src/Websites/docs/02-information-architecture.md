# 02 — Information Architecture

## Sitemap

```
/                           Homepage — ExItS ecosystem overview
├── /products               All products listing
│   ├── /pos                Pinoy Business POS (primary product page)
│   ├── /service-pro        Pinoy Service Pro (planned — coming soon)
│   └── /[future-products]  Routes TBD when products confirmed
├── /pricing                Pricing (POS-first; other products TBD)
├── /about                  About ExItS
├── /contact                Contact / sales inquiry / partnership
├── /privacy                Privacy policy (legal — TBD WEB-D-07)
└── /terms                  Terms of service (legal — TBD WEB-D-07)
```

> **Note on future product routes:** `/service-pro` is the confirmed route for PinoyServicePro based on product doc naming. Routes for PinoyLoanManager, PinoyBuyNowPayLater, and PinoyPawnManager are **TBD** pending product marketing name decisions. Do not freeze them until confirmed.

---

## Navigation Structure

### Desktop Sticky Header

```
[ExItS Logo]                               [Get Started →]  [☰]
```

- Logo links to `/`
- "Get Started" is the primary CTA (links to existing Platform signup or waitlist — TBD WEB-D-04)
- Hamburger / menu trigger opens the right-side drawer

### Right-Side Drawer

Groups (confirmed by product/audience scope):

```
Products
  ├── Pinoy Business POS           /pos
  ├── Pinoy Service Pro            /service-pro  [Coming Soon]
  └── [Other products TBD]

Solutions
  ├── Personal Sellers             (section on /pos or /products)
  ├── Small Businesses             (section on /pos)
  └── Multi-Branch Businesses      (section on /pos)

Pricing                            /pricing

Company
  ├── About                        /about
  └── Contact                      /contact

Resources
  └── [TBD — blog, guides, etc.]
```

> Solutions entries are anchor-links or page sections, not necessarily separate routes. Confirm during WEB-02.

### Drawer Sizing

| Breakpoint | Width |
|---|---|
| Desktop (≥1024px) | ~32–36vw |
| Tablet (640–1023px) | ~45–55vw |
| Mobile (<640px) | 100vw |

When drawer is open: page background dims with a semi-transparent overlay; scroll is locked.

### Breadcrumbs

Present on secondary pages (`/products`, `/pos`, `/service-pro`, `/pricing`, `/about`, `/contact`).
Not present on homepage.

---

## Page-Level Summary

| Route | Purpose | Notes |
|---|---|---|
| `/` | ExItS platform overview; primary POS promotion; ecosystem teaser | See [pages/home.md](pages/home.md) |
| `/products` | All products listing with readiness badges | See [pages/products.md](pages/products.md) |
| `/pos` | Full Pinoy Business POS detail | See [pages/pos.md](pages/pos.md) |
| `/service-pro` | Pinoy Service Pro coming-soon | See [pages/service-pro.md](pages/service-pro.md) |
| `/pricing` | Pricing cards; POS-first; others TBD | See [pages/pricing.md](pages/pricing.md) |
| `/about` | Company story, mission, team TBD | See [pages/about.md](pages/about.md) |
| `/contact` | Contact form, sales, partnership | See [pages/contact.md](pages/contact.md) |
| `/privacy` | Privacy policy — legal TBD | See [pages/privacy.md](pages/privacy.md) |
| `/terms` | Terms of service — legal TBD | See [pages/terms.md](pages/terms.md) |

---

## Internal Linking Strategy

- Homepage → `/pos` (primary CTA)
- Homepage → `/products` (ecosystem CTA)
- Homepage → `/pricing` (pricing preview strip → full page)
- `/pos` → `/pricing` (CTA at end of page)
- `/products` → each product page
- Footer → all primary routes
- Every page → `/contact` and sign-up

---

## 404 Handling

- Custom 404 page with ExItS branding and links to homepage and products.
- Implement during WEB-02.
