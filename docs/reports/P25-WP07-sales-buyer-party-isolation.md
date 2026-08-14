# P25-WP07 — POS sales buyer-party / QR purpose isolation

| Field | Value |
|---|---|
| Phase | **P25** |
| Work package | **P25-WP07** |
| Status | **Code Complete / Owner Validation Pending** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Starting SHA | `66a972f84741f7b7f394c19438e0b9ef0d7367c0` |
| Feature SHAs | `95f5744b79a058bd9afe91b54e527fc998027181`, `710426a97bff97d58ca3dc5b1e8a0f386f77bc31` |
| Alias | [sales-buyer-party-isolation.md](sales-buyer-party-isolation.md) |
| Related | [sales-buyer-party-model.md](../engineering/sales-buyer-party-model.md) |

## Architecture audit (pre-implementation)

| Area | Finding |
|------|---------|
| Sale ownership | Already `OrganizationId`; actor = `RecordedBy` |
| Customer | Org-owned `POSCustomer`; only `PlatformBusinessCustomerId` link |
| Buyer party | **Missing** — only optional `CustomerId` |
| Business Utang | Org-owned `CreditEntry` → `POSCustomer` (separate from Personal Utang) |
| Snapshots | Line snapshots existed; buyer display was live customer join |
| Offline | LocalStore v9; cash outbox org-scoped; no buyer columns |
| Supplier QR | Manual Guid only; no purpose enforcement |

## Delivered

- Sale buyer party kinds + immutable snapshots
- Customer ExItS Personal/Organization identity links
- Checkout + MAUI scan Personal/Business QR for customers
- Connected supplier Business QR required (server + UI)
- Purpose-mismatch plain-language messages
- Migration `20260814220000_AddSaleBuyerPartyAndCustomerExItsLinks`
- LocalStore **unchanged (v9)**

## Gates

| Gate | Result |
|------|--------|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Deferred

- Personal purchase history of merchant sales
- B2B invoice / buyer-org view of seller sales
- Automatic connected-supplier PO → seller sale
- Ownership transfer UI
- Payment / loyalty QR
