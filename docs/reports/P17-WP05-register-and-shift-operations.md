# P17-WP05 — Register and Shift Operations

| Field | Value |
|---|---|
| Status | **Complete** (reconciled; default register from setup) |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

MVP register and cashier shift flow for the first sale journey.

## Existing functionality reused

- Registers (P10-WP07): create/activate/deactivate, available-for-shift.
- Cashier shifts (P10-WP04): open with opening cash, close with closing cash / expected / variance, one open shift rules, cancel.
- Sale checkout requires open shift + register (domain enforced).

## Implementation summary

- Operational setup completion creates **Main Register** when the organization has no registers and stores `DefaultRegisterId`.
- No redesign of shift domain rules.

## Files / components changed

- Operational setup complete path (WP02) provisions default register.
- Existing shift/register APIs and Maui pages unchanged except navigation to setup.

## Authorization and isolation behavior

- View/Manage registers and shifts via role matrix.
- Cross-org register access concealed (existing tests).

## Tests executed and results

- Existing `PosRegisterApiTests`, cashier shift integration tests.
- `PosOperationalSetupApiTests` asserts default register on complete.

## Deferred items

- Multi-branch drawer hardware; advanced till management.

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
