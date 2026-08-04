# P19-WP02 — Mobile Registers UI

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | ee2ffb6 |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Complete Mobile Registers list, Main Register visibility, detail, create/activate/deactivate, and shift availability visibility with View vs Manage gating.

## 2. Existing reuse

Phase 10 register APIs (`IPosRegisterClient`), activate/deactivate, available-for-shift listing, activity summary DTO.

## 3. Delivered

- Registers list with search, status filter, paging, offline/error/retry
- Main Register badge (name `Main Register` or code `REG-000001`) on list and detail
- Detail: lifecycle activate/deactivate for ManageRegisters; activity summary via `GetActivityAsync`
- Create/edit gated to ManageRegisters; Cashier ViewRegisters can browse/select eligible registers without admin actions
- MoreHub Registers nav gated by ViewRegisters (with WP07 nav hardening)

## 4. Residuals

- No drawer hardware / advanced till management (out of scope)
- Main Register identity inferred from operational-setup default name/code (no dedicated IsMain flag on DTO)

## 5. Tests

`RegistersPageGuardTests` — routes, View/Manage gates, activity, Main badge, MoreHub gating.

## 6. Authorization

API + commercial grants authoritative. Client mirrors ViewRegisters vs ManageRegisters.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
