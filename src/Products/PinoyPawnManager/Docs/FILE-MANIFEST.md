# Pinoy Pawn Manager — File Manifest / Documentation Index

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Docs root | `src/Products/PinoyPawnManager/Docs/` |
| Last updated | 2026-08-27 |
| Implementation present | Scaffold only (PPM-01) — no operational domain |

## Authoritative docs

### Root canonicals

| Path | Purpose | Status |
|---|---|---|
| `README.md` | Doc index and permanent principles | PPM-01 |
| `product-definition.md` | Purpose, ownership, boundaries | PPM-01 |
| `architecture.md` | System architecture overview | PPM-00 |
| `security.md` | Security overview | PPM-00 |
| `authorization-matrix.md` | Grants / presets planning | PPM-00 |
| `development-plan.md` | Delivery buckets | PPM-00 |
| `roadmap.md` | PPM-00 … PPM-16 packages | PPM-01 |
| `risks-and-decisions.md` | `PPM-D-00-*` / `PPM-R-00-*` | PPM-01 |
| `FILE-MANIFEST.md` | This inventory | PPM-01 |

### Product/

| Path | Purpose |
|---|---|
| `Product/README.md` | Product domain index |
| `Product/pawn-transaction-model.md` | Lifecycle + state machine A |
| `Product/customer-model.md` | Customer / identity references |
| `Product/pledged-item-model.md` | Collateral model |
| `Product/appraisal-model.md` | Appraisal |
| `Product/pawn-ticket-and-agreement.md` | Ticket snapshots |
| `Product/loan-release-model.md` | Principal vs appraisal; fund release |
| `Product/maturity-model.md` | Maturity concepts |
| `Product/renewal-model.md` | Renewal / extension |
| `Product/redemption-model.md` | Redemption payment |
| `Product/unredeemed-and-disposition-model.md` | Unredeemed + disposition |
| `Product/reporting-baseline.md` | Report families |

### Custody/

| Path | Purpose |
|---|---|
| `Custody/README.md` | Custody domain index |
| `Custody/custody-state-model.md` | State machine B |
| `Custody/storage-location-model.md` | Storage hierarchy |
| `Custody/custody-movement.md` | Movement audit |
| `Custody/item-release.md` | Physical release |
| `Custody/loss-damage-discrepancy.md` | Incidents / discrepancies |

### Architecture/

| Path | Purpose |
|---|---|
| `Architecture/README.md` | Architecture index |
| `Architecture/platform-integration.md` | Platform boundary |
| `Architecture/plm-boundary.md` | vs PLM |
| `Architecture/bnpl-boundary.md` | vs BNPL |
| `Architecture/pos-commerce-boundary.md` | vs POS / Commerce |
| `Architecture/persistence-boundary.md` | DB isolation |
| `Architecture/api-contract-boundary.md` | API contracts |
| `Architecture/idempotency-and-reconciliation.md` | Financial safety |
| `Architecture/web-pwa-runtime-policy.md` | ONLINE-ONLY |

### Security/

| Path | Purpose |
|---|---|
| `Security/README.md` | Security index |
| `Security/role-and-grant-baseline.md` | Grants baseline |
| `Security/custody-security.md` | Custody controls |
| `Security/audit-and-history.md` | Audit events |
| `Security/privacy-and-sensitive-data.md` | Privacy |

### Compliance / Decisions / Phases / Operations / Reports / Validation

| Path | Purpose |
|---|---|
| `Compliance/README.md` | Compliance index |
| `Compliance/philippines-regulatory-review.md` | Open legal/regulatory questions |
| `Decisions/README.md` | ADR index |
| `Decisions/ADR-001-product-identity.md` | Provisional implementation approval (not final marketing Closed) |
| `Phases/README.md` | Phase pointer (roadmap canonical) |
| `Operations/README.md` | Operating notes (planning) |
| `Reports/README.md` | Reports index |
| `Reports/PPM-00-foundation-closeout.md` | PPM-00 closeout |
| `Reports/PPM-01-product-scaffold-platform-registration.md` | PPM-01 closeout |
| `Validation/README.md` | Validation index |
| `Validation/PPM-00-readiness-checklist.md` | PPM-00 readiness checklist (docs foundation) |

## Source roots

| Path | Role |
|---|---|
| `src/Products/PinoyPawnManager/Docs/` | Authoritative docs |
| `src/Products/PinoyPawnManager/ExItS.PinoyPawnManager.Domain/` | Scaffold Domain (PPM-01) |
| `src/Products/PinoyPawnManager/ExItS.PinoyPawnManager.Application/` | Scaffold Application (PPM-01) |
| `src/Products/PinoyPawnManager/ExItS.PinoyPawnManager.Infrastructure/` | Scaffold Infrastructure — **no DbContext** (PPM-01) |
| `src/Products/PinoyPawnManager/ExItS.PinoyPawnManager.Api/` | Scaffold Api — health only (PPM-01) |
| `tests/ExItS.PinoyPawnManager.UnitTests/` | Scaffold / safety unit tests (PPM-01) |

## Explicitly not in this product tree (after PPM-01)

- Operational pawn domain entities
- DbContext / migrations / operational database
- Organization Web / PWA / MAUI / LocalStore
- Platform operational ownership
- POS / PLM / BNPL / PSP databases and domains
- Customer-specific forks

## Notes

- **PPM-D-00-01 / 02 / 03** = Provisionally Approved for Implementation (Product Owner, PPM-01) — **not** final marketing approval
- **PPM-D-00-04** … **PPM-D-00-20** remain **OPEN**
- Platform registration in PPM-01 is **Local Validation / Dev fixture** only (`EnsurePpmLocalValidationCatalog`, `GrantPpmProductAccess` for ABC maria/carlo; XYZ denied by default)
- **LEGAL_AUTHORIZATION_CLAIMED=NO**
- Next package: **PPM-02**
