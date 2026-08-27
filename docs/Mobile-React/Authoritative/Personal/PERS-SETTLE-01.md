# PERS-SETTLE-01 — Personal Utang Settlement + Close Flow

**Package:** PERS-SETTLE-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Date:** 2026-08-27  
**Baseline:** `11f4c75d8a0848c559f1e67b81712d21132907ef`

## Gap closed

Personal Utang already supported Loan / Payment / Adjustment with shared confirmation and pay-to-zero manually, but had **no named Settle → final Payment → Close** workflow. Domain enumerated `Closed` without a product close operation.

## Settlement semantics

**Settle** = pay the **exact remaining confirmed balance** as a canonical `PersonalUtangEntryType.Payment`, then close the relationship when that payment is confirmed.

| Ordinary Payment | Settle |
| --- | --- |
| May be partial | Exact remaining confirmed balance only |
| Relationship stays Active | Closes when final payment Confirmed |
| Intent = Regular | Intent = Settlement + balance snapshot |

**Forbidden:** writing `CurrentBalance = 0` without a ledger Payment. History always shows how the balance reached zero.

## Private (unlinked) settlement

1. User confirms Settle (amount read-only = confirmed balance)  
2. `POST …/relationships/{id}/settle` with sticky `settlementEntryId`  
3. Payment **Confirmed** immediately  
4. Balance → 0 → `CloseAsSettled`  
5. Outcome: `Completed`  
6. Scheduled reminders for the relationship cancelled; delivery history retained  

## Linked (shared) settlement

1. User A settles → Payment **Pending**, Intent=Settlement, `SettlementBalanceSnapshot` = confirmed balance at proposal  
2. Confirmed balance unchanged; relationship stays **Active**  
3. A cannot self-confirm  
4. User B Confirm → Payment Confirmed → if balance matches snapshot and becomes 0 → **Closed**  
5. User B Dispute → Disputed; balance unchanged; Active  
6. Outcome after propose: `AwaitingCounterpartyConfirmation`

## Stale settlement + pending blocks

- Any unresolved **Pending** financial entry blocks new settle/close.  
- Confirming settlement when `CurrentBalance != SettlementBalanceSnapshot` → **409** (`settlement.stale`); no apply, no close.  
- User cancels/recreates against current balance.

## Zero-balance close

`POST …/relationships/{id}/close` when Active, balance == 0, no Pending → `Closed` (UI: **Mark as settled**).  
Non-zero close denied. Already Closed → idempotent `AlreadySettled`.  
Archived / Transferred → settle/close denied.

## Closed behavior

No new Loan / Payment / Adjustment / Settle. History readable. UI shows **Settled**. Active totals / overdue exclude Closed (dashboard already filters Active).

## Idempotency (PERS-IDEM-01)

Sticky `settlementEntryId` as Payment `EntryId`. Same id + compatible payload converges; conflicting payload → idempotency conflict. Confirm retry converges (balance applied once; Closed idempotent).

## Online-only

Personal Web/PWA: Settle / Close **disabled offline** (`PersonalUtangSettle` / `PersonalUtangClose`). No Web outbox enqueue.

## API

```
POST /api/v1/personal/utang/relationships/{relationshipId}/settle
POST /api/v1/personal/utang/relationships/{relationshipId}/close
```

## Migration

`AddPersonalUtangSettlementIntent` (`20260827112344_AddPersonalUtangSettlementIntent`)

- `intent` (default `Regular`, backfill)  
- `settlement_balance_snapshot` (nullable)

No second settlement ledger table.

## UI

Relationship detail: Settle panel / Mark as settled / Settled chip / awaiting banner; Closed hides mutation controls; settlement rows labeled in history.

## Test evidence

| Gate | Result |
| --- | --- |
| Platform PersonalUtang unit (+ settlement) | PASS |
| ApiPersonalUtangSettlement integration | PASS |
| React Utang unit (detail/client/workspace) | PASS |
| Playwright `pers-settle-01` | PASS (private + linked multi-context) |

## Explicit non-goals

Forgiveness, interest, fees, partial-settlement discounts, payment gateways, BNPL/PLM/PPM/PSP, ownership-transfer UI.
