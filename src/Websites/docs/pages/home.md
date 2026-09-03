# Page: Homepage (/)

## Purpose

Introduce ExItS as a multi-product SaaS platform. Give Pinoy Business POS the strongest visual priority. Tease the product ecosystem. Drive two conversion actions: "Get Started" (primary) and "See All Products" (secondary).

---

## Section Flow

```
1. ExItsHeader (sticky)
2. Hero
3. Product/Benefit Proof Strip
4. Featured Product: Pinoy Business POS
5. POS Capability Storytelling
6. Who It Is For
7. Other ExItS Products
8. Business Growth Story
9. Pricing Preview
10. Trust / Reassurance Strip
11. FAQ
12. Newsletter / Updates CTA
13. Final CTA
14. ExItsFooter
```

---

## Section Specifications

### 1. ExItsHeader

- Sticky, full-width, dark surface with subtle border-bottom
- Left: ExItS logo
- Right: "Get Started" primary CTA button + menu trigger (opens drawer)
- Scrolled state: add slight background blur or elevated surface treatment
- See [implementation/components.md](../implementation/components.md) for `ExItsHeader`

### 2. Hero

**Purpose:** Establish ExItS positioning and drive primary conversion.

Layout:
- Full-bleed dark section, ~80–100vh on desktop
- Left/center: headline, sub-headline, dual CTAs
- Right: product visual composition (Pinoy Business POS screenshot or device mockup)

Content:
- `<h1>`: ExItS platform positioning headline (TBD — copywriter/Product Owner)
- Sub-headline: 1–2 sentences on what ExItS is
- Primary CTA: "Get Started" → Platform signup
- Secondary CTA: "See All Products" → `/products`
- Visual: real POS interface screenshot in device frame (TBD — WEB-D-06). Until assets are available, use placeholder space.

Typography: oversized display; consider outline/solid headline combination for emphasis word.

Do not invent statistics on hero ("Trusted by X businesses").

Responsive:
- Desktop: side-by-side text + visual
- Tablet: text above, visual below (reduced size)
- Mobile: stacked, hero text ~40–48px, visual as image below

### 3. Product/Benefit Proof Strip

**Purpose:** Rapid trust-building; anchor key platform capabilities.

Horizontal strip of 4–6 short capability statements with Lucide icons. Examples (verified in repo):

- "Sell online and offline" (offline capability confirmed in codebase)
- "Multi-branch management" (branches/areas confirmed)
- "Built-in customer credit (Utang)" (confirmed)
- "Supplier ordering" (purchasing/suppliers confirmed)
- "Staff roles and access control" (roles/permissions confirmed)
- "Customer storefront" (customer ordering confirmed)

No fake metrics. Capability statements only.

Responsive: horizontal scroll on mobile, grid on desktop.

### 4. Featured Product: Pinoy Business POS

**Purpose:** Position POS as the flagship ExItS product.

Large editorial section with:
- "Featured Product" label / badge
- Product name headline: "Pinoy Business POS"
- 2–3 sentence product summary
- Key benefits list (3–5 items, confirmed capabilities only)
- Primary CTA: "Explore Pinoy Business POS" → `/pos`
- Visual: product screenshot or feature screenshot (TBD WEB-D-06)

### 5. POS Capability Storytelling

**Purpose:** Show depth without requiring user to leave the homepage.

3–4 tabs or scrollable cards highlighting verified POS capabilities:

| Tab | Content |
|---|---|
| Selling | Real-time POS, offline-capable, cashier shifts, sale returns |
| Inventory | Catalog, branch stock, expiration tracking |
| Customers | Customer list, Utang (credit), linked customer orders |
| Suppliers | Supplier management, connected ExItS suppliers, purchase orders |

Only verified capabilities. See [products/pinoy-business-pos.md](../products/pinoy-business-pos.md).

Responsive: horizontal tabs on desktop, segmented scrolling on mobile.

### 6. Who It Is For

**Purpose:** Audience identification — let visitors self-select.

3 audience cards:

| Audience | Description |
|---|---|
| Personal sellers / solo business | Starting out, simple selling, single location |
| Small / growing businesses | Inventory control, staff management, multi-branch ready |
| Established multi-branch retailers | Area grouping, branch stock rollups, connected operations |

Each card has short description and a CTA link to the relevant section on `/pos` or direct to "Get Started."

### 7. Other ExItS Products

**Purpose:** Surface the product ecosystem honestly.

Grid of product cards for non-POS products. Each card:
- Product name
- 1-sentence description
- Readiness badge: "Coming Soon" or "In Development"
- No "Get Started" CTA for unconfirmed products — use "Learn More" or "Join Waitlist" (TBD WEB-D-08)

Products to show:
- Pinoy Service Pro — Coming Soon
- Pinoy Loan Manager — Coming Soon
- Pinoy Buy Now Pay Later — In Development
- Pinoy Pawn Manager — In Development

Do not imply these are available now.

### 8. Business Growth Story

**Purpose:** Communicate that ExItS grows with the business.

Visual timeline or step diagram:

```
Start simple  →  Multi-branch  →  Connected platform
(single loc)     (areas, staff)   (suppliers, customers, more products)
```

Short copy for each stage. Only describe capabilities that are confirmed in the codebase.

Do not describe Area as owning stock or being inventory authority.

### 9. Pricing Preview

**Purpose:** Give pricing confidence without a full pricing table.

Short teaser strip:
- "Simple, transparent pricing for every stage."
- 1–2 sentence description of the approach
- CTA: "See Pricing" → `/pricing`

Do NOT show ₱ prices on the homepage without WEB-D-01 resolution.

### 10. Trust / Reassurance Strip

**Purpose:** Address common purchase objections.

4–6 short trust statements with icons. Examples:

- "Works offline — keep selling when internet drops"
- "Your data, your business — no lock-in"
- "Filipino-built for Filipino businesses"
- "Secure platform with role-based access"

These are verified in repository capabilities. Do not add claims without repo evidence.

### 11. FAQ

**Purpose:** Answer common top-of-funnel questions.

See [content/faq.md](../content/faq.md) for approved questions.

Accordion pattern (`ExItsFaq` component). 5–8 questions max on homepage.

### 12. Newsletter / Updates CTA

**Purpose:** Capture interest from visitors not yet ready to sign up.

Simple email capture:
- Headline: "Stay updated on ExItS"
- Sub-headline: product updates, launch announcements
- Email input + submit button
- Submission endpoint: TBD WEB-D-08

### 13. Final CTA

**Purpose:** Last conversion push before footer.

Full-width dark section with:
- Short punchy headline
- "Get Started" primary CTA
- "Talk to Us" secondary CTA → `/contact`

### 14. ExItsFooter

- Logo + tagline
- 4-column link grid: Products | Solutions | Company | Legal
- Social links (TBD — WEB-D-TBD)
- Copyright line
- Language: English only initially

---

## SEO

- `<h1>`: homepage hero headline
- `og:image`: exits-og-home.png (1200×630)
- JSON-LD: Organization schema

---

## Responsive Summary

| Element | Desktop | Tablet | Mobile |
|---|---|---|---|
| Hero | Side-by-side | Text above visual | Stacked, smaller type |
| Proof strip | Horizontal row | Horizontal row | Horizontal scroll |
| POS feature tabs | Horizontal tabs | Horizontal tabs | Segmented scroll |
| Who it's for | 3-col grid | 3-col grid | Stacked cards |
| Product ecosystem | 2-col grid | 2-col grid | Single col |
| FAQ | 2-col or 1-col accordions | 1-col | 1-col |
| Footer | 4-col | 2-col | 1-col stacked |
