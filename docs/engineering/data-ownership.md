# Data Ownership

[Architecture](architecture.md) | [Security](security.md) | [Data authority matrix](data-authority-matrix.md) | [Capability boundary](platform-product-capability-boundary.md)

Authoritative field-level ownership is in the [data authority matrix](data-authority-matrix.md). Summary below remains valid.

## Platform database

Users, platform organizations, products, plans, subscriptions, payments (SaaS), entitlements and platform audit.

## HealthCare database

Clinics, staff assignments, patients, appointments, medical notes and healthcare audit.

## PinoyBusinessPOS database

Businesses, stores, customers, credit entries, retail payments, products, sales, inventory, expenses, suppliers, offline device state and POS audit.

No product reads another product database directly. Cross-boundary access uses documented contracts. Cross-database foreign keys are prohibited. Products may hold local entitlement projections; Platform remains commercial system of record (ADR-011).
