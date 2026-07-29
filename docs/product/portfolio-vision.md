# ExITS SaaS Portfolio Vision

[Home](../index.md) | [Dashboard](../portfolio-progress.md)

## Vision

Create one commercial SaaS ecosystem where an organization can use one or more ExITS products under centrally managed identity, subscription, billing and entitlement controls.

## Platform responsibilities

- Global users and sign-in
- Platform organizations and membership
- Product catalog
- Plans, trials, subscriptions and add-ons
- Payments and billing status
- Product entitlements
- Platform administration and support
- Platform-wide audit events

## Product responsibilities

### HealthCare SaaS

Clinics, staff, patients, appointments, medical notes, healthcare permissions and healthcare data.

### PinoyBusinessPOS

Compact, multilingual, offline-capable retail management for Philippine SMEs — initially optimized for Sari-Sari stores and mini groceries, architected for broader small retail (convenience, generic pharmacy inventory, hardware, apparel, office supply, and similar). Owns stores, customers, CustomerCredit/Utang, products, sales, inventory, expenses, suppliers, purchasing and cashier operations.

Domain language stays generic (`Store`, `Customer`, `CustomerCredit`, …). Do not treat the product as Sari-Sari-only.

## Principles

1. Reuse proven HealthCare SaaS foundation where it is genuinely generic.
2. Do not copy healthcare-specific assumptions into the Platform or POS.
3. Keep each product’s database, migrations, API, tests and deployment independently operable.
4. Normal product operations must not synchronously depend on Platform availability.
5. Use compact, accessible, multilingual interfaces.
6. Support English and Filipino/Tagalog from the first PinoyBusinessPOS MVP.
7. Support light, dark and system theme preferences.
8. Preserve the completed HealthCare MVP during extraction with regression tests and rollback points.
