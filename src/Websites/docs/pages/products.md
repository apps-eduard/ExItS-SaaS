# Page: /products

## Purpose

Provide a clear overview of all ExItS products with accurate readiness status. Let visitors self-select their product of interest. Drive traffic to product detail pages.

---

## Breadcrumb

```
ExItS / Products
```

---

## Section Flow

```
1. ExItsHeader (sticky)
2. Page Hero
3. Product Grid
4. Platform Promise Strip
5. Final CTA
6. ExItsFooter
```

---

## Section Specifications

### Page Hero

- Short section, not full-bleed
- `<h1>`: "Our Products" or "The ExItS Platform" (TBD — copywriter)
- Sub-headline: 1 sentence — what ExItS offers
- No product-specific CTAs here

### Product Grid

Each product is a card:

```
[Product name]
[Readiness badge]
[1-sentence description]
[CTA button]
```

| Product | Readiness Badge | CTA |
|---|---|---|
| Pinoy Business POS | ✅ Available | "Explore" → /pos |
| Pinoy Service Pro | 🔜 Coming Soon | "Join Waitlist" (TBD WEB-D-08) or "Learn More" |
| Pinoy Loan Manager | 🔜 Coming Soon | "Learn More" |
| Pinoy Buy Now Pay Later | 🚧 In Development | "Learn More" |
| Pinoy Pawn Manager | 🚧 In Development | "Learn More" |

> "Available" on Pinoy Business POS means the platform and product implementation exist and are the featured product. Do not imply public commercial launch until confirmed.

Grid layout:
- Desktop: 3-col or 2-col
- Tablet: 2-col
- Mobile: 1-col stacked

### Platform Promise Strip

Short strip:
- "One account. Multiple business tools. Built for Filipino businesses."
- 3–4 capability icons (account, multi-branch, secure, connected)

### Final CTA

- "Start with Pinoy Business POS" → `/pos`
- "Have questions?" → `/contact`

---

## SEO

- `<h1>`: page headline
- Title: "ExItS Products | ExItS"
- Description: "Discover the ExItS product suite — business management tools built for Filipino businesses."
- No ProductListItem JSON-LD required here; use plain page metadata.
