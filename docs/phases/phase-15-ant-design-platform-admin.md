# Phase 15 — Ant Design Platform Administration

[Documentation Home](../index.md) | [Portfolio progress](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**P15-WP01–WP05 complete.** Prior Fluent UI Phase 15 direction is **cancelled and superseded** (never pushed; working tree discarded at `e6d0185`). See [WP01](../reports/P15-WP01-antdesign-admin-foundation.md), [WP02](../reports/P15-WP02-users-and-organization-memberships.md), [WP03](../reports/P15-WP03-organization-lifecycle.md), [WP04](../reports/P15-WP04-product-catalog-and-plan-crud.md), [WP05](../reports/P15-WP05-subscriptions-and-entitlements.md).

## Goal

Rebuild Platform Admin on **Ant Design Blazor** (`AntDesign`), using [Ant Design Pro Blazor](https://pro.antblazor.com/) as a visual/structural reference only — not a wholesale import. POS remains native/DesignSystem. Platform APIs, authz, audit, and business rules unchanged.

## Work packages

| WP | Name | Status |
|---|---|---|
| P15-WP01 | Ant Design Admin Foundation | **Complete** |
| P15-WP02 | Users and Organization Memberships | **Complete** — [report](../reports/P15-WP02-users-and-organization-memberships.md) |
| P15-WP03 | Organization Lifecycle | **Complete** — [report](../reports/P15-WP03-organization-lifecycle.md) |
| P15-WP04 | Product Catalog and Plan CRUD | **Complete** — [report](../reports/P15-WP04-product-catalog-and-plan-crud.md) |
| P15-WP05 | Subscriptions and Product Entitlements | **Complete** — [report](../reports/P15-WP05-subscriptions-and-entitlements.md) |
| P15-WP06 | Authorization, Audit, and UX Hardening | Blocked until authorized |
| P15-WP07 | Closeout | Blocked |

## Cancelled direction

Any Fluent UI Blazor Admin foundation plans, packages, ADR amendments favoring Fluent for Admin, and uncommitted Fluent code are **void**. Do not resume Fluent Admin work.

## Non-goals (P15-WP01)

- Lifecycle CRUD expansions beyond existing surfaces
- P14-WP03 TLS / reverse proxy
- POS UI changes
- Copying Ant Design Pro repository / demo apps wholesale
