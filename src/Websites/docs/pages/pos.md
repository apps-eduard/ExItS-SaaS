# Page: /pos (Pinoy Business POS)

## Purpose

The strongest product page on the website. Present Pinoy Business POS in full detail. All capability claims must be traceable to the verified repository implementation. Drive sign-up conversion.

For product truth (what POS actually is), see: [products/pinoy-business-pos.md](../products/pinoy-business-pos.md)

---

## Breadcrumb

```
ExItS / Products / Pinoy Business POS
```

---

## Section Flow

```
1. ExItsHeader (sticky)
2. Breadcrumb
3. Product Hero
4. Key Benefit Strip
5. Selling Experience
6. Catalog and Inventory
7. Customer Management and Utang
8. Purchasing and Suppliers
9. Branch and Area Management
10. Staff Roles and Access
11. Customer Storefront
12. Reports and Shifts
13. Business Growth Story
14. Pricing Preview
15. FAQ
16. Final CTA
17. ExItsFooter
```

---

## Section Specifications

### Product Hero

- Full-bleed, dark surface, ~70–80vh on desktop
- `<h1>`: "Pinoy Business POS"
- Sub-headline: 2–3 sentences — what it is and who it's for
- Primary CTA: "Get Started" → Platform signup
- Secondary CTA: "See Pricing" → `/pricing`
- Visual: POS interface screenshot / device mockup (TBD — WEB-D-06)

### Key Benefit Strip

4–5 confirmed capability statements with icons:
- Sell online and offline (offline mode confirmed)
- Multi-branch and Area management (confirmed)
- Built-in Utang (customer credit) (confirmed)
- Supplier purchase orders (confirmed)
- Role-based staff access (confirmed)

### Selling Experience

**Claims must match [products/pinoy-business-pos.md](../products/pinoy-business-pos.md) CONFIRMED list.**

Highlight:
- Real-time POS selling
- Offline selling mode with local store / outbox sync
- Cart management — single merchant, multi-item
- Sale returns
- Cashier shift management
- Register management
- Payment handling (cash / other — do not specify uncertified payment methods)
- Idempotent transaction design

Screenshot/video placeholder for POS selling screen (TBD WEB-D-06).

### Catalog and Inventory

- Product catalog management (categories, variants, images)
- Branch-specific pricing (overrides)
- Stock tracking per branch
- Expiration tracking (FIFO)
- Inventory movements / adjustments
- Catalog import

Screenshot placeholder.

### Customer Management and Utang

- Customer list per branch
- Customer credit (Utang) — sell now, pay later
- Customer credit limits
- Personal customer linking (ExItS Personal platform users as customers)
- Customer orders via digital storefront

Do not describe Utang as a formal credit product or regulatory financial product.

### Purchasing and Suppliers

- Supplier management
- Purchase orders
- Connected ExItS suppliers (other ExItS platform organizations as suppliers)
- Supplier connection requests and approval workflow
- Direct purchase receipts

### Branch and Area Management

- Multi-branch organization support
- Branch-specific stock
- Area grouping of branches (for rollup reporting and staff oversight)
- Branch readiness checks
- Customer ordering enabled/paused per branch

**Do NOT describe Area as owning inventory or as an inventory authority.**
Area is an organizational grouping; stock belongs to branches.

### Staff Roles and Access

- Owner / Manager / Cashier role presets
- Explicit grant-based authorization (not role-name hard-coded)
- Staff invitation and onboarding
- Per-branch staff access scoping

### Customer Storefront

- Public store landing page for organization (confirmed: public store discovery API)
- Online ordering by linked Personal customers
- Ordering available/unavailable dynamically evaluated per branch readiness
- Order accept / reject by merchant staff
- Order status notifications to Personal buyers

### Reports and Shifts

- Cashier shift reports
- Sales reporting
- Inventory reports
- Statement / payables (supplier payables confirmed in API structure)

Note: specific report contents and formats should be verified against actual reporting implementation before detailed marketing copy is written.

### Business Growth Story

Visual progression (matching homepage section):
1. Single branch — start simple, sell today
2. Add staff, manage roles, track inventory
3. Multi-branch — expand confidently with Area management
4. Connect with other ExItS businesses as suppliers

### Pricing Preview

Short strip: "Find the right plan for your business." → CTA to `/pricing`
Do not show ₱ prices here until WEB-D-01 resolved.

### FAQ

POS-specific questions. See [content/faq.md](../content/faq.md) for POS FAQ list.

### Final CTA

- "Start using Pinoy Business POS today" → Platform signup
- "Have questions?" → `/contact`

---

## SEO

- Title: "Pinoy Business POS — Point of Sale for Filipino Businesses | ExItS"
- Description: See [06-seo-and-discoverability.md](../06-seo-and-discoverability.md)
- JSON-LD: SoftwareApplication schema
- OG image: pos-og.png

---

## Responsive

| Section | Desktop | Mobile |
|---|---|---|
| Hero | Side-by-side | Stacked |
| Benefit strip | Horizontal row | 2-col grid |
| Feature sections | Alternating text/screenshot | Text above image |
| Branch/area diagram | Horizontal flow | Vertical flow |
| Staff roles | Table or card grid | Single-col cards |
| FAQ | 1-col accordions | 1-col accordions |
