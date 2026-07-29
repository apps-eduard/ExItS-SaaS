# Data Ownership

[Architecture](architecture.md) | [Security](security.md)

## Platform database

Users, platform organizations, products, plans, subscriptions, payments, entitlements and platform audit.

## HealthCare database

Clinics, staff assignments, patients, appointments, medical notes and healthcare audit.

## PinoyBusinessPOS database

Stores, customers, credit entries, payments, products, sales, inventory, expenses, suppliers and POS audit.

No product reads another product database directly. Cross-boundary access uses documented contracts.
