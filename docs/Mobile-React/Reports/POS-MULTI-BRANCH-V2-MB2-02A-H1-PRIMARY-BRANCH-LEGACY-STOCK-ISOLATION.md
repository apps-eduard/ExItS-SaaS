# POS-MULTI-BRANCH-V2 MB2-02A-H1 — Primary Branch Legacy Stock Isolation

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02A-H1  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Parent:** [MB2-02A Branch Inventory Read Authority](POS-MULTI-BRANCH-V2-MB2-02A-BRANCH-INVENTORY-READ-AUTHORITY.md)

---

## Problem

Before H1, `PosOrganizationBranchDirectory.GetPrimaryBranchIdAsync` derived primary from the **caller-filtered** Platform `ListBranches` response. Remote-only staff did not see Main in that list, so primary resolved to `null`. `BranchStockResolver` treated `primaryBranchId == null` as eligible for unallocated organization stock, allowing Remote to display Main's legacy 100 as its own.

## Fix

1. **Platform:** `GET /api/v1/platform/organizations/{organizationId}/primary-branch` returns `{ branchId }` using `IOrganizationBranchRepository.GetPrimaryAsync`. Auth: active org member (`EnsureCanViewOrganizationAsync`). No branch assignment required. Minimum disclosure.

2. **POS:** `GetPrimaryBranchIdAsync` calls structural primary endpoint (not filtered branch list).

3. **Fail closed:** `BranchStockResolver`, `BranchInventoryQueryRepository`, reorder fallback, null-branch lot/movement reads: `primaryBranchId == null` never grants unallocated stock to an arbitrary branch.

## Preserved

- Operational branch selection still uses ACL-filtered existence/active checks.
- Remote-only staff cannot select Main (`CanAccessBranchAsync` / filtered list).
- No inventory quantity or balance mutations.

## Corrected MB2-02A implementation SHA

`a711019c284aeffd1d8bdfb692dc0abbd98d42c3`

## Migration

None.

## NEXT

MB2-02B — physical inventory write-path authority (not started).
