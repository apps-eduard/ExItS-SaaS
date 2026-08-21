# POS-REACT MASTER RUN 02 — REVIEW REPAIR 02

## Status

**COMPLETE** — sale return/void concurrency boundary closed with shared `OrganizationId+SaleId` advisory lock. RMAP-14 React UI remains **not started**.

## Baseline

| Item | Value |
|------|-------|
| Starting HEAD | `2364727c` (clean, pushed) |
| Branch | `feat/pos-react-client` |
| Preflight | PASS |

## Findings repaired

### Shared sale mutation lock

- Application: `ISaleMutationLock.AcquireAsync(org, saleId)`
- Infrastructure: `PosSaleMutationLock` → `SELECT pg_advisory_xact_lock(...)`
- Key: FNV-1a 64-bit over org Guid[16] + sale Guid[16], XOR namespace `0x53414C45524554` (`SALERET`) — distinct from sale seq, return seq (`bytes[19]=0x52`), inventory `0x1A7E7E5E11EED`, shift `0x5348494654`
- DI: scoped

### ProcessSaleReturn

- Core mutation wrapped in `ExecuteInSerializableTransactionAsync`
- After lock: recheck ReturnId idempotency → reload sale → cash shift → **fresh** `GetPriorTotalsBySaleLineAsync` → `CreateAsync` (ambient txn) → restock/Utang/SaveChanges
- Retries `PersistenceConflictException` so SSI waiters that began before the lock holder committed get a fresh snapshot
- Early empty-lines / actor validation remain outside the txn

### VoidSale

- First statement inside existing serializable txn: `AcquireAsync`
- Then reload sale, `HasReturnsForSaleAsync`, void

### Utang Amount persistence

- `CreditEntryEntityMapper.ApplyToRecord` now persists `Amount` so `ReduceForSaleReturn` survives SaveChanges

### PostgreSQL concurrency proofs

`PosSaleReturnConcurrencyTests` (Barrier + dual HttpClients on Testcontainers):

| Case | Coverage |
|------|----------|
| A | Concurrent 6+6 on qty 10 — never both accept 6; returned ≤ 10 |
| B | Concurrent 6+4 — both succeed; qty 10; refund = LineTotal |
| C | Discounted LineTotal=80 concurrent 6+6 — cumulative refund ≤ 80 |
| D | Expiry lots — never over-restore past original lot received qty |
| E | Branch org/branch on-hand reconcile after concurrent restock returns |
| F | Utang concurrent — debt never over-reduced; Amount tracks refunds |
| G | Return vs Void — exclusive outcomes only |
| H | Same ReturnId idempotent under concurrency |
| I | Different SaleIds independent |

## Commits

| # | Message |
|---|---------|
| 1 | `fix(pos): serialize sale return and void mutations` |
| 2 | `docs(pos-react): record sale return concurrency repair` |

(Docs commit SHA is not self-recorded here.)

## Flags

| Flag | Value |
|------|-------|
| `RMAP14_RETURN_CONCURRENCY_GAP` | **CLOSED** |
| `RMAP14_RETURN_VOID_RACE_GAP` | **CLOSED** |
| `RMAP14_BACKEND_READY_FOR_REACT_RESTART` / `BACKEND_READY` | **YES** |
| `REACT_UI_STARTED` / `RMAP14_REACT_UI_NOT_STARTED` | **NO** / YES |
| RMAP-14 package PASS | **NO** (React UI not started) |

## Exclusions

- RMAP-14 React UI
- RMAP-15
- Migrations
- SHA-sync / final-sync commits

## Exact next

Start **RMAP-14 React returns UI only** against the concurrency-safe backend. Do not start RMAP-15 until RMAP-14 PASS.
