# P17-WP07 — Void, Refund, and Audit

| Field | Value |
|---|---|
| Status | **Complete** (reconciled; no rebuild) |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

Minimum safe MVP controls for completed-sale void/return with audit and stock restoration.

## Existing functionality reused

- Sale void with mandatory reason (`VoidSale`); historical sale retained (status Voided).
- Sale returns / refunds (P10-WP05) with stock restore service; Manager/Owner capabilities (`VoidSale`, `ProcessReturn`).
- Cashier capability set excludes void; return processing limited by role matrix (Cashier has ProcessReturn historically — verify product policy: Phase 17 requires Manager/Owner for completed-sale void/refund).

## Implementation summary

- Reconciled: pre-completion cart line removal is client-only and is not a refund.
- Completed sales are not hard-deleted.
- **Phase 17 hardening:** Cashier role no longer includes `ProcessReturn` (void was already denied). Manager/Owner retain void and return.
- No expansion into complex partial-return approval engines.

## Authorization note

Cashier may view returns but cannot process completed-sale void/refund. Manager (`StoreManager`) and Owner/Admin retain `VoidSale` and `ProcessReturn`.

## Files / components changed

- `PosRoleMatrix.cs` — remove Cashier `ProcessReturn`
- `PosRoleMatrixTests.cs` — assert Cashier denied ProcessReturn

## Authorization and isolation behavior

- Unauthorized void/return denied by capability/role middleware.
- Audit fields: actor, timestamps, reason on void/return records.

## Tests executed and results

- Role matrix unit tests; existing void/return/stock restoration integration tests.

## Deferred items

- Complex approval workflows; partial-return engines.

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
