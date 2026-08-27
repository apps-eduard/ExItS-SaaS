# Pinoy Pawn Manager — Architecture Folder

> Parent overview: [../architecture.md](../architecture.md)  
> Product definition: [../product-definition.md](../product-definition.md)

| Field | Value |
|---|---|
| Status | PPM-00 documentation foundation |
| Implementation | None |
| Last updated | 2026-08-27 |

## Purpose

This folder holds **boundary and runtime** architecture for Pinoy Pawn Manager (PPM): how PPM relates to Platform, PLM, BNPL, and POS/Commerce; persistence and API contract rules; idempotency; and initial Web/PWA online-only policy.

PPM is a **first-class ExItS product**. Architecture here is planning intent only—no API, DbContext, or UI projects in PPM-00.

## Documents

| Doc | Focus |
|---|---|
| [platform-integration.md](platform-integration.md) | Identity, org/branch facts, catalog, entitlements |
| [plm-boundary.md](plm-boundary.md) | PPM ≠ PLM + photo; no PLM loan-entity reuse |
| [bnpl-boundary.md](bnpl-boundary.md) | Goods direction opposite (custody in vs goods out) |
| [pos-commerce-boundary.md](pos-commerce-boundary.md) | Pledged ≠ retail stock; disposition handoff contract |
| [persistence-boundary.md](persistence-boundary.md) | Separate DB; no cross-product FKs or table reads |
| [api-contract-boundary.md](api-contract-boundary.md) | Guids/contracts only; no shared EF models |
| [idempotency-and-reconciliation.md](idempotency-and-reconciliation.md) | Duplicate prevention; release safeguards |
| [web-pwa-runtime-policy.md](web-pwa-runtime-policy.md) | Initial **ONLINE_ONLY** mutation policy |

## Hard principles (architecture)

| Principle | Value |
|---|---|
| `PPM_FIRST_CLASS_PRODUCT` | YES |
| `DIRECT_POS_DB_ACCESS` | NO |
| `DIRECT_PLM_DB_ACCESS` | NO |
| `DIRECT_BNPL_DB_ACCESS` | NO |
| `PAWN_ITEM_IS_NORMAL_POS_INVENTORY_WHILE_PLEDGED` | NO |
| `PHYSICAL_RELEASE_SEPARATE_FROM_PAYMENT` | YES |
| Web/PWA financial & custody mutations | **ONLINE_ONLY** (initial) |
| `LEGAL_AUTHORIZATION_CLAIMED` | NO |

## Open decisions that affect architecture

See [../risks-and-decisions.md](../risks-and-decisions.md):

- **PPM-D-00-02/03/04** — product code, folder, DB name
- **PPM-D-00-15** — POS/Commerce disposition handoff
- **PPM-D-00-16** — cross-branch custody transfer
- **PPM-D-00-17** — cash-management integration
- Portfolio **D-P12-03** — commercial-state transport to products

## Non-goals

- Implementing scaffold or domain code in this package
- Collapsing payment and physical release into one transition
- Offline mutation queues for initial Web
- Claiming regulatory or licensing authorization
