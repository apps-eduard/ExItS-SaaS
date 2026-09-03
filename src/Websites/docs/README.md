# ExItS Public Website — Source of Truth

> **DO NOT IMPLEMENT FROM MEMORY.**
> Every product claim, design decision, and technical choice must be traceable to a document in this folder or to verified repository code.
> Future agents must start here and follow the links below before writing a single line of website code.

---

## Purpose

This documentation package defines everything needed to build the public ExItS marketing website at:

```
https://exits.ph
```

ExItS is a multi-product SaaS platform. The website presents ExItS as the platform/company with multiple products.

---

## Project Status

| Item | Status |
|---|---|
| Documentation | Complete (this package) |
| Next.js implementation | **Not started** |
| ExItS.Web project | **Not created** — target path `src/Websites/ExItS.Web/` |
| Design assets | Not created |
| DNS / hosting | Not configured |

---

## Primary / Featured Product

**Pinoy Business POS** — the only product with substantial working implementation.

---

## Website Goals

1. Clearly present ExItS as a product ecosystem (not a single tool).
2. Feature Pinoy Business POS as the flagship product.
3. Represent other ExItS products honestly with correct readiness status.
4. Drive qualified leads to sign up or request a demo.
5. Establish brand trust for Philippine small and medium businesses.
6. Never claim unavailable functionality is live.

---

## Target Audience

- Filipino small business owners (single-branch, sari-sari, retail)
- Growing multi-branch retail businesses
- Business owners exploring digital tools for the first time
- Filipino entrepreneurs looking for a local, trustworthy SaaS product

---

## Product Readiness Matrix

| Product | Display Name | Readiness | Marketing Classification |
|---|---|---|---|
| PinoyBusinessPOS | Pinoy Business POS | Substantial working implementation | **CONFIRMED — featured** |
| PinoyServicePro | Pinoy Service Pro | Docs only, implementation not started | **PLANNED** |
| PinoyLoanManager | Pinoy Loan Manager | Docs complete, implementation absent and paused | **PLANNED** |
| PinoyBuyNowPayLater | Pinoy Buy Now Pay Later | Scaffold only, financing domain not started | **IN DEVELOPMENT (scaffold)** |
| PinoyPawnManager | Pinoy Pawn Manager | Scaffold only, no operational domain | **IN DEVELOPMENT (scaffold)** |

> **Rule:** Do not upgrade IN DEVELOPMENT or PLANNED into CONFIRMED without verified implementation evidence.

---

## Approved Technology Stack

See full specification: [04-technical-architecture.md](04-technical-architecture.md)

| Concern | Choice |
|---|---|
| Framework | Next.js |
| Language | TypeScript |
| UI | React |
| Styling | Tailwind CSS |
| Component foundation | shadcn/ui primitives + custom ExItS components |
| Content | MDX where appropriate |
| Animation | Motion |
| Icons | Lucide |
| Forms | React Hook Form + Zod |
| Unit/component tests | Vitest + React Testing Library |
| E2E | Playwright |
| SEO | Next.js Metadata API, Open Graph, JSON-LD, sitemap, robots |
| Images | Next.js Image |
| Backend authority | Existing ASP.NET Core ExItS Platform APIs |
| Marketing DB | None initially |
| Deployment | Docker-compatible |

---

## Approved Information Architecture

See full specification: [02-information-architecture.md](02-information-architecture.md)

```
/                       Homepage — ExItS ecosystem
/products               Product listing
/pos                    Pinoy Business POS — primary product page
/service-pro            Pinoy Service Pro — planned
/pricing                Pricing
/about                  About ExItS
/contact                Contact / inquiry
/privacy                Privacy policy
/terms                  Terms of service
```

Additional product routes will be finalized when implementations are confirmed.

---

## Design Reference

Primary visual inspiration: **https://onegiantleap.com/**

- Use as structural and visual principle reference only.
- Do NOT copy LEAP branding, copy, illustrations, or proprietary identity.
- ExItS retains its own GREEN identity — not purple.

See: [05-design-system.md](05-design-system.md)

---

## Design Principles

- 70% premium high-impact visual presentation / 30% clean SaaS product clarity
- ExItS is NOT an event website — product understanding and conversion are paramount
- Prioritize real screenshots, understandable benefits, concise copy, clear CTAs
- Accessibility-first, responsive from mobile to desktop

---

## Source-of-Truth Document Index

### Root documents

| File | Contents |
|---|---|
| [01-website-vision.md](01-website-vision.md) | Goals, audience, brand positioning |
| [02-information-architecture.md](02-information-architecture.md) | Sitemap, routes, navigation structure |
| [03-brand-and-messaging.md](03-brand-and-messaging.md) | Brand voice, messaging hierarchy, copy rules |
| [04-technical-architecture.md](04-technical-architecture.md) | Stack, boundaries, deployment |
| [05-design-system.md](05-design-system.md) | Tokens, components, visual language |
| [06-seo-and-discoverability.md](06-seo-and-discoverability.md) | SEO strategy, structured data, metadata |
| [07-auth-and-app-routing.md](07-auth-and-app-routing.md) | Public website vs SaaS app boundaries |
| [08-launch-plan.md](08-launch-plan.md) | Launch readiness gates |

### Pages

| File | Contents |
|---|---|
| [pages/home.md](pages/home.md) | Homepage composition and section specs |
| [pages/products.md](pages/products.md) | /products listing page |
| [pages/pos.md](pages/pos.md) | /pos — Pinoy Business POS detail page |
| [pages/service-pro.md](pages/service-pro.md) | /service-pro — planned product page |
| [pages/pricing.md](pages/pricing.md) | /pricing page structure |
| [pages/about.md](pages/about.md) | /about page |
| [pages/contact.md](pages/contact.md) | /contact page and forms |
| [pages/privacy.md](pages/privacy.md) | /privacy legal page |
| [pages/terms.md](pages/terms.md) | /terms legal page |

### Products (what the products are)

| File | Contents |
|---|---|
| [products/pinoy-business-pos.md](products/pinoy-business-pos.md) | POS product truth — verified capabilities |
| [products/pinoy-service-pro.md](products/pinoy-service-pro.md) | Service Pro product truth |
| [products/pinoy-loan-manager.md](products/pinoy-loan-manager.md) | Loan Manager product truth |
| [products/pinoy-buy-now-pay-later.md](products/pinoy-buy-now-pay-later.md) | BNPL product truth |
| [products/pinoy-pawn-manager.md](products/pinoy-pawn-manager.md) | Pawn Manager product truth |

### Content

| File | Contents |
|---|---|
| [content/homepage-copy.md](content/homepage-copy.md) | Draft homepage copy |
| [content/pos-copy.md](content/pos-copy.md) | Draft POS page copy |
| [content/calls-to-action.md](content/calls-to-action.md) | CTA inventory |
| [content/faq.md](content/faq.md) | FAQ questions and answers |

### Implementation

| File | Contents |
|---|---|
| [implementation/phases.md](implementation/phases.md) | WEB-01 through WEB-12 phases |
| [implementation/routes.md](implementation/routes.md) | Next.js route map |
| [implementation/components.md](implementation/components.md) | Custom component inventory |
| [implementation/analytics.md](implementation/analytics.md) | Analytics event plan |
| [implementation/deployment.md](implementation/deployment.md) | Deployment architecture |

---

## Implementation Phases

WEB-01 → WEB-12. See [implementation/phases.md](implementation/phases.md).

**None have been started.**

---

## Open Decisions

| ID | Decision | Status |
|---|---|---|
| WEB-D-01 | Commercial pricing — plan names, ₱ prices, limits | **TBD — Product Owner required** |
| WEB-D-02 | Analytics vendor (self-hosted vs. managed) | **TBD** |
| WEB-D-03 | Hosting provider / CDN | **TBD** |
| WEB-D-04 | `app.exits.ph` vs current domain routing for SaaS entry | **TBD** |
| WEB-D-05 | Product route names for future products | **TBD — confirm when products ready** |
| WEB-D-06 | Real product screenshots / video assets | **TBD — requires design/marketing assets** |
| WEB-D-07 | Privacy policy / terms — legal review | **TBD — legal required** |
| WEB-D-08 | Newsletter / CRM integration | **TBD** |

---

> **Reminder:** This is documentation only. `src/Websites/ExItS.Web/` does not exist yet.
> Do not create it until WEB-01 is explicitly authorized.
