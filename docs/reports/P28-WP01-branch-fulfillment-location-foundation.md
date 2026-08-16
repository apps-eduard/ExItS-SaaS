# P28-WP01 — Branch & Fulfillment Location Foundation

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | Phase 28 — Open |
| Starting SHA | `f73f237f3cc409677e31aa7167cc32c8d9d5a616` |
| Feature commit(s) | `8d0be5eb` platform · `c01c2e1b` maui · `7bc63852` org-web · `f7b7c88d` tests · `6feb518f` docs |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Delivered capability

- Platform-owned branch coordinates, pickup/delivery flags, delivery policy persistence, and fee preview API.
- Shared product client contracts for branch fulfillment and delivery-price preview.
- MAUI branch list, capacity summary, create action, progressive branch editor, fulfillment controls, and draft fee examples.
- Responsive Organization Web branch management with structured address, status, fulfillment, coordinates, and delivery-policy fields.
- English and Filipino MAUI resources.
- Engineering guidance for fulfillment locations and V1 Haversine delivery pricing.

## Persistence and API

The Platform migration adds branch fulfillment fields and branch delivery policies. Migration apply, rollback, and re-apply evidence remains pending. Production startup must not apply migrations automatically.

## Security and boundaries

Organization context controls branch access. Platform owns organization data; product operational databases do not gain cross-database access or foreign keys. This development-stage capability is not evidence of production security.

## Explicit exclusions

Customer storefront, cart, checkout, order lifecycle, payment, dispatch, courier integration, route optimization, tracking, and WP02–WP10 are excluded.

## Validation evidence

- Restore: succeeded.
- Release solution build: all projects reached before MAUI Android, including Organization Web, compiled; final result blocked at the MAUI target by missing local Android SDK (`XA5300`).
- Platform unit tests: **963 passed, 0 failed, 0 skipped**.
- Organization Web tests: **44 passed, 1 failed**; the failure is an existing inventory stock-adjustment wording assertion unrelated to branch changes.
- Migration apply/rollback/re-apply, device, browser, and runtime validation: pending.

## Portfolio independence

Root a nested foreign product tree is absent, Git tracking shows no nested foreign product tree is empty, and the solution project list contains no legacy product project.

## Risks and open decisions

- Haversine is straight-line distance and may understate road travel.
- Coordinate capture is manual in Stage A.
- Fee examples are client-side guidance; server preview remains authoritative.
- Owner device/browser validation is outstanding.

## Files and documentation

Stage A hashes: `8d0be5eb`, `c01c2e1b`, `7bc63852`, `f7b7c88d`, `6feb518f` (plus this hash-record commit).

## Exact next

Master task Stage B begins at P28-WP02 (CustomerOrder domain). Phase 27 remains Open.
