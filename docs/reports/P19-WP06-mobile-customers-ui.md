# P19-WP06 — Mobile Customers UI

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 7361d2c |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Harden Mobile Customers list/detail/credit flows with correct view vs create capability gating.

## 2. Existing reuse

Customer/credit APIs, offline customer credit sync paths from earlier phases.

## 3. Delivered

- CustomersList requires ViewCustomersAndHistory
- Removed incorrect Access Restricted banner for view-only users without CreateCustomer
- CreateCustomer still gates Add button
- CreditCreate requires CreateCredit on entry and SaveAsync

## 4. Residuals

- Cashier role matrix does not include ViewCustomersAndHistory — cashiers sell Utang via checkout customer search, not full customers hub
- Broader customer admin remains capability-gated as before

## 5. Tests

`CustomersReportsNavPageGuardTests` — customers view gate and credit create gate.

## 6. Authorization

API + commercial grants authoritative; client mirrors ViewCustomersAndHistory / CreateCustomer / CreateCredit.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
