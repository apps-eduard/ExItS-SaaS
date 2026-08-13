# Phase 25 — Organization Web Admin Management Center

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md)

| Field | Value |
|---|---|
| Status | **Open** — WP01 Code Complete / Owner Validation Pending |
| Device Verified | **No** |
| Production Ready | **No** |
| Boundary | **Organization Web Admin is not a POS checkout client.** |

## Purpose

Deliver a professional Organization Web Admin for **management, control, and reporting**. Operational selling stays in the POS/MAUI experience.

## Web vs POS

| Surface | Allowed |
|---|---|
| Organization Web | Profile, branches, staff/roles, catalog, inventory management, customers, devices/registers, shift inspection, reports, settings, subscription (read), notifications |
| Organization Web sales | **Read-only** history, receipt detail, aggregates |
| POS / MAUI | Checkout, cart, barcode selling, payment-taking, cashier sale creation, open/close shift as cashier work |

## Navigation

Overview · Products (catalog / categories / global catalog) · Inventory (stock / transfers / lots) · Customers · Branches · Staff · Reports · Operations · Settings · Subscription · Notifications

Unauthorized sections are hidden in navigation and still denied by server APIs.

## Performance / bandwidth

- Dashboard uses one POS SQL aggregate (`GET /api/v1/pos/management/overview`) plus a few bounded Platform counts.
- Lists are paged (default 20).
- Reports use server-side aggregation, not browser-side full-history loads.
- No Redis added for this phase.

## Owner acceptance

Browser checklist is in [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md). Do not mark Device Verified or Production Ready until the owner validates.
