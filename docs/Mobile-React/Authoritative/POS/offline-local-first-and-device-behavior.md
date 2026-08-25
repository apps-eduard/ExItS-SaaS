# Offline, Local-First, and Device Behavior

## Policy source of truth

`PosOfflineCapabilityPolicy` + `docs/reports/P19-offline-connectivity-capability-matrix.md`

Unknown routes = **OnlineRequired** (fail-closed).

## Capability classes

| Class | Meaning | Examples |
|-------|---------|----------|
| OfflineCapable | Usable offline without queue mutation | Shell hubs, offline PIN, some linked-product reads |
| Queueable | Local write + outbox sync | Cash checkout, customer create/edit/credit/repay, catalog product create, some purchasing draft |
| OnlineRequired | Must be online | Catalog admin/import, inventory admin, expenses, registers/shifts admin, reports, non-cash payments, org switch, permissions, customer ordering residual |

## LocalStore (MAUI)

| Concern | Status | Evidence |
|---------|--------|----------|
| SQLite LocalStore | PROVEN_CURRENT | `ExItS.PinoyBusinessPOS.LocalStore` |
| Encryption | PROVEN_CURRENT | encrypted customer/credit + outbox patterns |
| Offline grant / PIN / device binding | PROVEN_CURRENT | OfflinePin pages + operating grant |
| Outbox + idempotency | PROVEN_CURRENT | cash sale dispatcher |
| Sell catalog + sell units cache | PROVEN_CURRENT | schema v9 |
| Sale snapshot fidelity | PROVEN_CURRENT | `OfflineSaleSnapshotFidelityTests` |
| Connected linked-product selective projection | PROVEN_CURRENT | LocalConnectedSupplierStore |
| Personal utang local store | PROVEN_CURRENT | local personal utang paths |
| Device verified production claim | PROVEN_MISSING | docs: physical Device Verified = No |

## React

PWA service worker exists for static shell caching. This is **not** MAUI LocalStore parity.

| React offline aspect | Status |
|----------------------|--------|
| PWA/SW (prod) | PROVEN_CURRENT for static assets |
| Encrypted operational LocalStore | MISSING |
| Outbox cash sales | MISSING |
| Offline PIN/grant | MISSING |

## React migration rule

Do not schedule full offline parity before online sales/checkout parity and device/session contracts are stable. Offline is a late hardening track relative to foundational domain WPs — see roadmap.
