# Platform Organization Public Landing, Social Links, and Organization-Scoped Login

**Status:** FUTURE DESIGN  
**Package:** RMAP-B05 — Public Organization Landing, Social Links & Organization-Scoped Login  
**Authorization:** NOT STARTED · NOT AUTHORIZED FOR IMPLEMENTATION  

`RMAP_B05_STATUS=NOT_STARTED`  
`RMAP_B05_AUTHORIZED=NO`

This document records Product Owner decisions only. It is design documentation. Do not implement RMAP-B05 until explicitly authorized.

---

## 1. Canonical public URL

Production example:

| Surface | Example |
|---------|---------|
| Platform | `https://exitsapp.com` |
| Organization | Kizy Store |
| Slug | `kizy-store` |
| Canonical public landing | `https://exitsapp.com/store/kizy-store` |
| Organization workforce login | `https://exitsapp.com/store/kizy-store/login` |

- Slug is a friendly routing alias.
- `OrganizationId` remains the database identity.
- `PublicOrganizationId` (`ORG######`) remains the stable public organization identity.
- **Slug MUST NOT become authorization identity.**

---

## 2. Public landing sections

Owner/admin may configure visibility for:

- About
- Branches
- Products
- Store location / map
- Contact details
- Opening hours
- Order online
- Pickup
- Delivery
- QR code
- Social media

Each section: **Visible** / **Hidden**.

Master control:

- Public business page: **Published** / **Hidden**

Future UI should provide **Preview before Publish**.

---

## 3. Social links

Organization admin can configure supported social links.

Initial conceptual providers:

- Facebook
- Instagram
- TikTok
- YouTube
- Messenger
- WhatsApp

Each record should conceptually contain:

| Field | Notes |
|-------|--------|
| Platform/type | Provider enum |
| URL | Validated safe external URL only |
| Display label | Optional |
| IsVisible | May remain stored while false |
| SortOrder | Display order |

Never allow:

- arbitrary HTML
- JavaScript
- script tags
- iframe embeds
- custom executable content

---

## 4. Organization-scoped workforce login

Organization staff access must use the organization landing/login context.

Example: `/store/kizy-store/login`

- The resolved organization becomes the expected authentication organization.
- A staff principal whose `HomeOrganizationId` does not match that organization must not authenticate into that workforce context.
- Slug selection must not override the staff principal’s permanent organization scope.
- Wrong-organization login fails with generic safe wording (no membership leak across organizations).
- After successful staff login: organization context is **locked**; staff cannot switch to another organization.
- `LinkedPersonalUserId` never grants staff cross-org or Personal workspace access.

---

## 5. Personal / Owner login

Preserve global Personal ExItS login for:

- Personal account
- subscriptions
- Personal products/workspace
- organization ownership entry

A Personal/Owner principal with legitimate authority for an organization (e.g. Kizy Store) may enter that organization experience.

Do not merge Personal and staff credentials.

---

## 6. Public page ≠ authorization

Public page visibility configuration grants **no**:

- membership
- role
- POS capability
- customer data access
- staff access
- transaction access

Public page is anonymous/public presentation only. Authentication and authorization remain server-authoritative.

---

## 7. Slug change / redirect

When public landing URLs are implemented, changing:

`kizy-store` → `kizy-mini-mart`

must preserve old public links through an immutable slug-history/alias mechanism.

- Old: `/store/kizy-store` redirects permanently/canonically to `/store/kizy-mini-mart`
- Never reuse an old organization slug for another organization once publicly assigned, unless a future security-reviewed policy explicitly allows it.

---

## 8. QR code

Organization QR code points **only** to the canonical public landing URL.

Example: `https://exitsapp.com/store/kizy-store`

No password, access token, session token, or secret organization credential.

QR may be downloaded/printed/shared in a later authorized package.

---

## 9. Public products

Internal catalog ≠ automatically public catalog.

- Product must be explicitly eligible/published for public viewing.
- Never expose publicly by default: supplier cost, margin/profit, private inventory notes, supplier relationships, internal SKU metadata not intended for customers, audit data.
- Future availability may show Available / Out of stock.
- Exact internal inventory quantity requires an explicit future policy.

---

## 10. Branch public data

Each branch may eventually configure:

- public name
- public address
- map location
- phone/contact
- opening hours
- pickup availability
- delivery availability
- public visibility

Private/internal branch metadata remains hidden.

---

## 11. Order / Pickup / Delivery

Do not invent a parallel order system.

Public landing Order Online / Pickup / Delivery must integrate with authorized customer-ordering / fulfillment contracts, including **RMAP-19**.

If ordering capability is unavailable: section hidden or unavailable state.

---

## 12. Custom domain — future

Future subscription capability may map:

`https://kizystore.com`

to the same Platform Organization as:

`https://exitsapp.com/store/kizy-store`

- Custom domain never changes `OrganizationId` / `PublicOrganizationId`.
- SSL/domain verification required before activation.
- Default ExItS URL remains available unless future policy says otherwise.

---

## 13. Public page safety

Plan for:

- Published / Hidden
- Platform suspended
- Organization suspended/closed
- ordering temporarily unavailable
- contact/location privacy
- safe URL validation
- rate limiting
- abuse prevention
- no cross-tenant data exposure

Platform Admin may suspend public-page availability for abuse/security without transferring organization ownership.

Platform administration authority does not automatically grant access to private organization transaction/customer data.

---

## 14. SEO / sharing — future

Future public landing may support:

- page title
- organization description
- logo
- OpenGraph/social preview
- canonical URL
- search-engine visibility toggle

Do not expose private data to SEO metadata.

---

## 15. Future package

**RMAP-B05** — Public Organization Landing, Social Links & Organization-Scoped Login

Possible dependencies:

- Platform Organization slug
- Organization branding/profile
- branch/location data
- customer-ordering/fulfillment contract for online ordering
- subscription entitlement for premium/custom-domain features

**This documentation is DESIGN ONLY. Do not implement RMAP-B05 during I18N-01 or any adjacent unauthorized package.**
