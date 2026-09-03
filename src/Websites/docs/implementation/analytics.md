# Analytics

## Vendor Decision

Analytics vendor is **TBD — WEB-D-02**.

Options include:
- Self-hosted (Plausible, Umami) — preferred for privacy and cost
- Managed (Google Analytics 4, PostHog, Mixpanel)

Architecture must remain vendor-neutral at event definition level. Events below should be implementable in any of the above.

---

## Event Inventory

| Event Name | Trigger | Properties |
|---|---|---|
| `page_view` | Every page load | `page_path`, `page_title` |
| `product_view` | Visiting /pos, /service-pro, /products, or any product page | `product_name` |
| `cta_click` | Any CTA button click | `cta_label`, `cta_location`, `destination` |
| `pricing_view` | Visiting /pricing | — |
| `signup_click` | "Get Started" → Platform signup link clicked | `source_page`, `cta_label` |
| `login_click` | "Sign In" → Platform login link clicked | `source_page` |
| `contact_submit` | Contact form successfully submitted | `form_type` (general/sales/partnership) |
| `waitlist_submit` | Waitlist form successfully submitted | `product_name` |
| `drawer_open` | Navigation drawer opened | — |
| `faq_expand` | FAQ accordion item expanded | `question` (text or ID) |
| `pricing_plan_click` | Pricing card CTA clicked | `plan_name` (TBD) |

---

## Privacy Rules

- Do not send PII (email, name, phone) in analytics event properties.
- Do not track content typed in form fields.
- Comply with applicable Philippine data privacy requirements (RA 10173).
- Cookie consent approach: TBD WEB-D-07 / WEB-D-02. Do not use tracking cookies without consent mechanism if required.
- Prefer cookieless analytics (Plausible, Umami) to minimize compliance surface.

---

## Implementation Notes

- Implement during WEB-11 only — do not add analytics to individual page PRs.
- Use a centralized analytics abstraction layer so the vendor can be swapped without changing event call sites.
- Verify that "Get Started" click events fire correctly before launch.

---

## Conversion Goals

Primary conversion: `signup_click` (visitor clicks "Get Started")
Secondary conversion: `contact_submit` (visitor submits inquiry)
Awareness: `product_view` (visitor reaches a product page)
