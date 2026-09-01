# POS-MULTI-BRANCH-V2 MB2-04-H1 — Party Access Closure

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-04-H1  
**Branch:** `feat/organization`  
**Status:** IMPLEMENTED (pending parent validation)  
**Depends on:** MB2-04 COMPLETE_VALIDATED

---

## Delivered

### Multi-source grant provenance

- Migration `20260901185015_SyncMb2PartyAccessAndSetupProgress` — PK now `(organization_id, branch_id, customer_id|supplier_id, grant_source)`.
- `GrantAsync` uses `INSERT ... ON CONFLICT DO NOTHING` per source.
- `RevokeGrantAsync` deletes a single source row.
- `HasAccessAsync` remains ANY-row semantics for `(org, branch, party)`.
- `PartyBranchAccessService` adds explicit assign/revoke helpers and optional `persistChanges` for in-transaction grants.

### Runtime supplier grants

- `CreateDirectPurchaseReceipt` grants `Transaction` supplier access at receiving branch (same UoW).
- `ReceivePurchaseOrder` grants `Transaction` supplier access when PO has supplier (same receive transaction).

### PRIVACY-04 history scoping

- `PartyBranchHistoryScopeService` — org governance sees all branches; branch staff see acting branch only.
- Credit list/summary, utang ledger, and related reads filter credits via sale branch join.
- Branch-scoped staff ledger hides repayments/write-offs (conservative).

### Integration proofs

`PartyBranchAccess04H1IntegrationTests` — PARTY_H1_SUP_01…03, PRIVACY_04_01…07.

---

## Explicit exclusions (unchanged)

- Inventory architecture (MB2-02D frozen)
- Pricing model changes
- Per-branch customer/supplier duplication

---

## Unit baseline at db1b40a5 (pre-existing)

| Test | Result |
|------|--------|
| `OperationalActorEndpointGuardTests.Stock_count_create_endpoint_requires_server_actor` | FAIL (pre-existing) |
| `InventoryTransferUseCaseTests.Same_org_transfer_full_receive_updates_ledger_and_not_destination_before_receive` | FAIL (pre-existing) |
| `PaymentOfflineStoreTests.OfflineOperationTypes_includes_payment_ops_but_not_statement_or_receipt` | FAIL (pre-existing) |

Not caused by MB2-04-H1 changes.

---

## Next

**MB2-05** guided branch setup (same branch).
