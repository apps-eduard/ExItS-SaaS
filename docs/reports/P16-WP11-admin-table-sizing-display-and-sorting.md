# P16-WP11 — Admin table sizing, display, and sorting

> **Status:** In Progress (validation)  
> **Phase:** Phase 16 — Implementation Complete, Under Validation  
> **Work package:** P16-WP11  
> **Related:** Commercial → Entitlements (primary), other Platform Admin list tables

---

## Defect

Platform Admin tables (starting with Entitlements) showed technical IDs as primary values, stretched to full page width, and had broken or unwired column sorting on server-paged lists.

---

## Correction

### Entitlements columns

| Column | Display |
|---|---|
| Product | Product display name (e.g. Pinoy Business POS) |
| Organization | Organization display name (e.g. ABC Sari-Sari Store) |
| Status | Subscription status (e.g. Trialing) |
| Generated | Local time `dd MMM yyyy, h:mm tt` (UTC in tooltip) |
| Revision | Snapshot version (renamed from Version) |
| History | Link (not sortable) |
| Actions | Open (not sortable) |

API/DB remain UTC. UI converts via browser IANA timezone (`UserTimeZoneState` + `LocalTimestamp`).

### Table width (authoritative — 2026-08-03)

Earlier “content-fit / remove ScrollX” CSS **crushed** columns: `width: 100% !important` on `.ant-table table` overrode Ant Design `ScrollX` min-widths, so headers/cells overlapped (Organization Staff: “Organization role” over “Product role”, dates into Actions).

**Fix:**

1. `wwwroot/app.css` — use `min-width: 100%` (never force `width: 100% !important`); allow wrap by default; keep nowrap on ellipsis / fixed-width cells.
2. All Admin Ant Design list and nested tables — set `ScrollX` ≈ sum of practical column mins; fixed `Width` on status / dates / money / actions; one flexible descriptive column; ellipsis + tooltip for optional long text.
3. `docs/ui/ant-design-admin-ui-standards.md` §12 — ScrollX / fixed vs flexible / no hide-columns-for-width rules aligned.

Horizontal scroll appears when the viewport is narrower than the combined minimum widths. Do not cardify Ant Design tables on mobile for this pass.

### Sorting

Server-side `Filter → OrderBy/ThenBy → Skip → Take` with safe sort keys. Entitlements default: Generated DESC, then Organization ASC. Wired OnChange handlers for Entitlements, Organizations, Products, Plans, Subscriptions (Users already worked).

---

## Tests

Unit + integration coverage for friendly names, Revision label contract, local date format, asc/desc sorts, numeric Revision, sort-before-page, Actions/History not sortable, no GUID/ProductKey as primary display.

---

## Implementation SHA

Prior sizing/sort pass: `d8bacd4673c5e5eb75641714193b37b76f9a31e8`  
Overlap / ScrollX pass: `8855da8376e02129e2f415aaa318f117c0b8a1b2`

---

## Status

- Phase 16 — Implementation Complete, Under Validation  
- **P16-WP11 — In Progress**  
- P16-WP12 — Not Started  
