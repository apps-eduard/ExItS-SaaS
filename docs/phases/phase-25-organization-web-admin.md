# Phase 25 — Organization Web Admin, AntDesign hosts, and unified web auth

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md) | [P25-WP02](../reports/P25-WP02-antdesign-web-standardization-and-host-separation.md) | [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) | [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) | [P25-WP05](../reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md) | [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)

| Field | Value |
|---|---|
| Status | **Open** — WP01–WP05 Code Complete / Owner Validation Pending |
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

## Work packages

| WP | Scope | Status |
|---|---|---|
| [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md) | Organization Web management center (original DesignSystem host) | Code Complete |
| [P25-WP02](../reports/P25-WP02-antdesign-web-standardization-and-host-separation.md) | AntDesign standardization; Org/Personal/Admin host split | Code Complete |
| [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) | Canonical sign-in, SSO handoff, workspace routing | Code Complete |
| [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) | Web host legacy cleanup and Local Validation identity determinism | Code Complete |
| [P25-WP05](../reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md) | Cash count policy simplification and denomination-assisted reconciliation | Code Complete |

Local ports: **8090** Admin, **8091** Platform API, **8092** POS API, **8093** Org Web, **8094** Personal Web. Production public entry is HTTPS :443 via reverse proxy.

Owner browser checklist is in [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) (SSO items also in [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md)). Cash count owner checklist is in [pos-cashier-cash-count.md](../engineering/pos-cashier-cash-count.md). Do not mark Device Verified or Production Ready until the owner validates.

## Recorded hashes (P25-WP05)

| Kind | SHA |
|---|---|
| Starting | `147b94e4d0363354a80b8c31a9f353ae1299bb80` |
| Implementation | `cbcdb8a9` |
| Tests | `8869a179` |
| Docs | `528de183` |
