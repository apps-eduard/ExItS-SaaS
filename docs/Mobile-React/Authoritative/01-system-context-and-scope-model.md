# System Context and Scope Model

## Top-level model

```text
EXITS PLATFORM          shared SaaS control plane
PERSONAL                independent personal account surface
ORGANIZATION            independent business identity surface
PinoyBusinessPOS        organization-owned operational product (own DB)
```

| Surface | Owns | Does not own |
|---------|------|--------------|
| **Platform** | Identity, credentials, account profiles, sessions, organizations, memberships, product catalog definitions, plans, subscriptions, entitlements, commercial state, Platform Admin, platform audit, org compliance capability administration, branches/devices registration | POS operational sales/inventory ledgers |
| **Personal** | Personal profile/dashboard, Personal QR, Personal Utang, contacts/relationships, invitations, linked-merchant views, Explore POS / Start a Business entry | Organization operational data; POS product DB rows |
| **Organization** | Business profile/QR, ownership, memberships, staff invites, org-scoped staff identities, switching (owners), subscriptions/product access, business types, branches, devices, notifications, customer/supplier relationship *authority where Platform/POS split applies* | Cross-product DB FKs; Personal ledger copy |
| **PinoyBusinessPOS** | Org-scoped catalog, inventory, sales, purchasing, connected-supplier commerce, customers/Business Utang, shifts/registers, expenses, reports, customer orders (product DB `pos`) | Platform identity tables; other products’ databases |

## Hard portfolio rules (current)

Evidence: `docs/engineering/final-portfolio-boundaries.md`, workspace AGENTS / architecture guards.

- Product operational data stays in the product database (`pos` schema for PinoyBusinessPOS).
- No cross-product database access or foreign keys.
- POS must not contain PHI.
- Domain remains persistence-independent; Application must not reference Infrastructure.
- UI projects must not reference Infrastructure / EF / Npgsql.

## API family isolation

| Account class | Primary API family | Guard |
|---------------|-------------------|-------|
| `Personal` | `/api/v1/personal*` | `AccountScopeGuardMiddleware` |
| `Organization` | `/api/v1/organizations*`, `/api/v1/pos/*` (with org/product session) | same |
| `Platform` | `/api/v1/platform*` | same |

Evidence: `src/Platform/ExItS.Platform.Api` middleware + ADR-017 (`docs/decisions/ADR-017-scope-bound-sessions.md`).

## Product instance model

1. Platform defines product (PinoyBusinessPOS).
2. Organization obtains access via subscription/entitlement.
3. Platform may grant `ProductLocalRoleGrant` (Owner/Manager/Cashier/Viewer).
4. POS maps those codes to product-local role codes (`Owner`, `StoreManager`, `Cashier`, `ReportingUser`).
5. POS authorization for operational actions uses product roles + device/branch/session context — not org membership alone.

## Client surfaces (current)

| Client | Role | Status |
|--------|------|--------|
| MAUI Blazor Hybrid (`ExItS.PinoyBusinessPOS.Maui`) | Full Personal + Org Owner + POS operations host | `PROVEN_CURRENT` |
| Organization Web / Personal Web (Blazor) | Parallel web surfaces for org/personal | `PROVEN_CURRENT` (out of React scope except contracts) |
| React PWA (`ExItS.PinoyBusinessPOS.Client`) | Browser session + workspace + sell-floor shell | `PROVEN_PARTIAL` |
| Platform Admin | Platform control plane UI | `PROVEN_CURRENT` |

## Authority map (selected)

| Concern | Authority |
|---------|-----------|
| Login / password / session | Platform |
| Account profile selection | Platform |
| Organization create / ownership | Platform |
| Staff login alias | Platform (`StaffLoginNameRules`) |
| Branch address / hours / delivery policy | Platform |
| POS device registration | Platform |
| Catalog product / inventory / sale | POS |
| Connected supplier relationship + PO | POS (org↔org commerce) |
| Personal Utang ledger | Platform Personal |
| Business Utang ledger | POS |
| Linked Business Utang read for Personal | POS personal linked-customer projection + Platform link |

## Scope rule for React

React must treat Platform contracts and POS contracts as separate systems of record. Workspace binding (org + branch + product access + role) is a prerequisite to any POS operational screen.
