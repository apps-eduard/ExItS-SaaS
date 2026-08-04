# P19-WP03 — Mobile Shift Operations UI

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 1c86c49 |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Complete Mobile shift list/history, current open shift, open with opening cash and eligible register, close with expected cash/variance, and role-gated manage actions.

## 2. Existing reuse

Phase 10 cashier shift APIs (`IPosCashierShiftClient`), register available-for-shift, one-open-shift enforcement on server.

## 3. Delivered

- Shifts list: current open shift banner, status filter, shift-number search, paging, loading/empty/error/retry, detail links
- Open shift: eligible registers only, opening cash, ManageShifts gate (Cashier has ManageShifts per role matrix)
- Detail: summary, expected cash, movements, close with closing cash, cancel where domain allows
- MoreHub Shifts nav gated by ViewShifts

## 4. Residuals

- Cross-user shift admin beyond existing API filters remains server-enforced
- Cancel remains available only when backend accepts (client surfaces API errors)

## 5. Tests

`ShiftsPageGuardTests` — list/current/open/detail gates and close/summary surfaces.

## 6. Authorization

ViewShifts for browse; ManageShifts for open/close/movement/cancel. Cashier permitted for own-shift manage per `PosRoleMatrix`.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
