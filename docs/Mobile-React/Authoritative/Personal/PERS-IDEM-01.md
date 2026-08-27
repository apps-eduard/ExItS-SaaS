# PERS-IDEM-01 — Personal Utang Idempotency + Ambiguous Outcome Safety

**Package:** PERS-IDEM-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Baseline:** `099f548264faeff76f959929ea7b6c33efea14bc`  
**Implementation SHA:** `608fd780bb5bac2deaf501ce660c93e454dfc173`  

## Goal

Close the Personal P0 financial safety gap: Utang money/create mutations previously used `serverDedupeMode=none` and parked ambiguous transport as PermanentFailure without Org-comparable server convergence + GET reconciliation.

Personal offline (encrypted outbox, PIN, Utang/Todo queue) is **preserved**. Organization Web remains ONLINE-ONLY and was not modified.

## Operations covered

| Operation | Stable identity | Online | Offline outbox | Reconciliation |
| --- | --- | --- | --- | --- |
| Contact create | `contactId` (body) | API hardened; People UI unchanged (still ONLINE_ONLY) | Engine already queued; body now includes `contactId` | `GET /api/v1/personal/utang/contacts/{contactId}` |
| Relationship create | `relationshipId` (+ optional `initialLoanEntryId`) | Sticky id + Confirming… → GET relationship | Same ids survive enqueue/replay | `GET .../relationships/{relationshipId}` |
| Loan / Payment entry | `entryId` | Sticky id + Confirming… → GET entry | Same `entryId` in body | `GET /api/v1/personal/utang/entries/{entryId}` |
| Adjustment | `entryId` (online only) | Same entry path; still ONLINE_ONLY offline | Not queued | Same GET entry |

## Server behavior

- Optional client entity ids on create/record request DTOs.
- Domain already accepted optional ids; application now passes them.
- Same id + compatible payload → return existing DTO (no duplicate row).
- Same id + conflicting payload → `application.personal.utang.idempotency_conflict` (HTTP 409).
- PK race on client id → re-fetch and converge when payload matches.
- Authorization isolation preserved on GET-by-id (owner / relationship viewer only).

## Offline replay

- Outbox continues to store `operationId` / `entityLocalId` / `idempotencyKey` from the client-stable GUID.
- Encrypted payload now includes that GUID in the HTTP body so the Platform API sees it.
- `serverDedupeMode` for Personal Utang create/record → **`idempotency-key`** (auto-retry after ambiguous transport is safe).
- Do **not** mint a new id on outbox retry.

## Online ambiguous outcome (Utang UI)

Submitting… → network loss → Confirming transaction… → GET-by-id:

| Outcome | UX |
| --- | --- |
| Confirmed | Success + refresh |
| Confirmed not created (404) | Safe resubmit with same sticky id |
| Still unknown | Status-unknown lock; no duplicate invite |

Never fabricates success. Never enqueues offline as a workaround for an ambiguous online POST.

## Conflicting payload

Reusing a client entity id with different display name / participants / amount → 409 idempotency conflict.

## Intentionally unchanged

- People offline UI (PERS-PEOPLE-OFFLINE-01 later)
- Personal offline/online policy matrix
- Todo create still `serverDedupeMode=none` (server mints todo id)
- Org Web online-only + Org idempotency

## Remaining gaps

- People UI still ONLINE_ONLY for contact create (engine ready).
- Todo create still lacks client-stable entity id.
- Shared-ledger confirm/dispute/cancel remain online target-state (domain-partial); not part of this money-create package.
- Activation / password-reset completion pages still missing (separate P0).

## Evidence

- Platform integration: `ApiPersonalUtangTests.Personal_utang_client_ids_are_idempotent_across_replay`
- React: `personal-utang-offline.test.ts`, `outbox-processor.test.ts` (ambiguous Personal → RetryableFailure)
