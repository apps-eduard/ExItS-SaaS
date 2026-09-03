# Page: /pricing

## Status

Pricing structure and card layout are specifiable. **Actual commercial prices (₱), plan names, branch limits, and transaction limits are TBD — WEB-D-01 (Product Owner decision required).**

Do not publish ₱ amounts, fake discounts, or feature limits without Product Owner approval.

---

## Breadcrumb

```
ExItS / Pricing
```

---

## Section Flow

```
1. ExItsHeader (sticky)
2. Breadcrumb
3. Pricing Hero
4. (Optional) Audience Segmented Tabs
5. Pricing Cards
6. Feature Comparison Table
7. FAQ
8. Final CTA
9. ExItsFooter
```

---

## Pricing Hero

- `<h1>`: "Simple pricing for every stage of your business" (draft)
- Sub-headline: 1–2 sentences
- No ₱ amounts here

---

## Audience Segmented Tabs (Optional)

If pricing varies significantly by audience segment:

```
[Solo / Micro]  [Small Business]  [Multi-Branch]
```

Only implement if plan structure supports this segmentation. TBD WEB-D-01.

---

## Pricing Card Design

3-column card layout:

| Attribute | Specification |
|---|---|
| Card count | 3 (e.g., Starter / Growth / Pro — names TBD) |
| Recommended badge | Center card (or highest value) shows "Recommended" badge with emerald/gradient emphasis |
| Price display | "₱ TBD / month" placeholder until WEB-D-01 |
| Feature list | Bullet list of included features per plan |
| CTA | "Get Started" per card → Platform signup |
| Annual/monthly toggle | Optional — TBD WEB-D-01 |

Card visual:
- Dark surface background
- Recommended card: emerald gradient border or elevated treatment
- Clear visual hierarchy between plans

### Placeholder Plan Structure (to be replaced when WEB-D-01 resolved)

| Plan | Audience | Notes |
|---|---|---|
| Starter (name TBD) | Solo / single-branch | TBD features |
| Growth (name TBD) | Small multi-branch | TBD features |
| Pro (name TBD) | Larger / multi-branch | TBD features |

Do not publish this placeholder on the live site. Replace with real plan data.

---

## Feature Comparison Table

Full feature list with ✓ / — per plan.

Only list features confirmed in the repository. Do not invent features.

Starter confirmed candidates:
- POS selling
- Catalog management
- Inventory tracking
- Customer management
- Utang (customer credit)
- Basic reporting
- [Number of branches — TBD WEB-D-01]
- [Staff accounts — TBD WEB-D-01]

Growth / Pro additions (TBD WEB-D-01):
- Multiple branches
- Area grouping
- Connected ExItS suppliers
- Advanced reporting
- Customer storefront / ordering

---

## Pricing FAQ

Short FAQ section specific to pricing. See [content/faq.md](../content/faq.md) for pricing FAQ list.

---

## Final CTA

"Not sure which plan is right? Talk to us." → `/contact`

---

## SEO

- Title: "Pricing — Pinoy Business POS | ExItS"
- Description: "Flexible pricing plans for Pinoy Business POS. Find the right plan for your Filipino business."

---

## Responsive

| Element | Desktop | Mobile |
|---|---|---|
| Pricing cards | 3-col horizontal | 1-col stacked (recommended plan first) |
| Feature comparison | Full table | Scrollable horizontal table or card-per-plan |
| FAQ | 1-col accordions | 1-col |
