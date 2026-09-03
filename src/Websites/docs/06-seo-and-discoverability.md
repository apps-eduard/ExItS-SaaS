# 06 — SEO and Discoverability

## Title Template

```
{Page Title} | ExItS
```

Homepage exception:
```
ExItS — Business Management Platform for Filipino Businesses
```

---

## Meta Descriptions (Draft — finalize with copywriter)

| Route | Draft Description |
|---|---|
| `/` | ExItS is a multi-product SaaS platform built for Filipino businesses. Manage sales, inventory, staff, and customers — all in one place. |
| `/pos` | Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers. |
| `/products` | Discover all ExItS products for Filipino businesses — Pinoy Business POS and more coming soon. |
| `/service-pro` | Pinoy Service Pro by ExItS — coming soon. Service business management designed for Filipino service organizations. |
| `/pricing` | ExItS pricing plans — find the right plan for your business. |
| `/about` | About ExItS — the team, mission, and vision behind the ExItS platform. |
| `/contact` | Contact ExItS — sales inquiries, partnerships, and support. |

---

## Canonical URLs

- All pages must declare `<link rel="canonical" href="https://exits.ph/[path]" />`
- Use Next.js Metadata API `alternates.canonical` field.
- No duplicate content across `www` and non-`www` (redirect one to the other — TBD WEB-D-03).

---

## Open Graph

Required on every page:

```
og:type         website
og:site_name    ExItS
og:title        {page title}
og:description  {meta description}
og:url          https://exits.ph/{path}
og:image        https://exits.ph/og/{page}-og.png  (1200×630)
og:locale       en_PH
```

Social preview images (og:image) must be created during WEB-09. Use a consistent branded template with ExItS green identity.

---

## Twitter / X Cards

```
twitter:card        summary_large_image
twitter:site        @ExItS  (handle TBD — WEB-D-TBD)
twitter:title       {page title}
twitter:description {meta description}
twitter:image       {og image URL}
```

---

## JSON-LD Structured Data

### Organization (site-wide)

```json
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "ExItS",
  "url": "https://exits.ph",
  "logo": "https://exits.ph/logo.png",
  "sameAs": []
}
```

Apply on homepage and `/about`.

### SoftwareApplication (product pages)

Apply on `/pos` and `/service-pro` (when live):

```json
{
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  "name": "Pinoy Business POS",
  "applicationCategory": "BusinessApplication",
  "operatingSystem": "Web, iOS, Android",
  "offers": { "@type": "Offer", "price": "TBD", "priceCurrency": "PHP" }
}
```

> Do NOT fill in prices until WEB-D-01 is resolved.

### FAQPage (FAQ sections)

Apply only where a genuine FAQ section exists with real Q&A.

```json
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [
    {
      "@type": "Question",
      "name": "...",
      "acceptedAnswer": { "@type": "Answer", "text": "..." }
    }
  ]
}
```

---

## Sitemap

Generate `sitemap.xml` via Next.js `app/sitemap.ts`.

Include:
- All confirmed public routes
- `changefreq` and `priority` appropriate to update frequency
- Exclude `/privacy`, `/terms` from priority sitemap (still include but low priority)

---

## robots.txt

```
User-agent: *
Allow: /

Sitemap: https://exits.ph/sitemap.xml
```

Block nothing on the marketing website. Apply additional rules only if admin/preview routes exist in the same Next.js deployment.

---

## Heading Hierarchy

- One `<h1>` per page.
- Hero headline = `<h1>`.
- Section headings = `<h2>`.
- Sub-sections = `<h3>`.
- Do not skip heading levels.

---

## Internal Product Linking

- Homepage → `/pos` multiple CTAs
- `/products` → each product page
- `/pos` → `/pricing` (end CTA)
- Footer → all primary routes
- Every product page cross-links to other products (with correct readiness labels)

---

## Performance and SEO

- Core Web Vitals targets: LCP < 2.5s, INP < 200ms, CLS < 0.1.
- Images: Next.js `<Image>` with `width`, `height`, `alt`. Lazy-load below-fold.
- Fonts: self-host or use `next/font`; avoid render-blocking external font requests.
- No background video on hero (avoid LCP penalty).
- Prefer static rendering for all marketing pages.

---

## Philippine / Local SEO Targeting

- Use `<html lang="en-PH">` or `en`.
- Include Philippine business terminology in metadata naturally.
- Do not keyword-stuff. Target understandable SME owner language.
- Future: Google Business Profile when physical presence is confirmed.
