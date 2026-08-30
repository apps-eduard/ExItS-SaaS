# POS-INVENTORY-PERMISSION-I18N-POLISH-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-INVENTORY-PERMISSION-I18N-POLISH-01  
**START_SHA:** `713aab028b0f0b217f4833d8f52aa3ee7d60d82c`
**FEATURE_SHA:** `60fd94c6209a388696b6fe5f18643d1e695e5745`

## Decision (primary question)

**Should ManageInventory automatically allow write-off, stock use, and production?**

**YES — for practical micro/small-business roles.**

Do **not** invent separate capabilities for Stock Use, Waste/Loss, expired write-off, Production, Stock Count, Transfers, or manual adjust.

| Capability | Meaning |
|------------|---------|
| `ViewInventory` | Read on-hand, lots, movements, lists (use/waste/production/counts/transfers) |
| `ManageInventory` | Any quantity or tracking mutation: adjust, stock use, waste/loss (incl. expired write-off), production, stock count, transfers, enable/disable tracking, opening stock, expiration setup, direct receive |

| Role | View | Manage |
|------|------|--------|
| Owner / Admin / StoreManager | yes | yes |
| InventoryStaff | yes | yes |
| ReportingUser | yes | **no** |
| Cashier | no | no |

Expired-stock write-off is Waste/Loss with reason Expired — **not** Business Utang `WriteOff`.

## ROOT_CAUSE (React polish)

`canViewInventory` incorrectly aliased `canManageInventory`, so ReportingUser (backend view-only) was denied inventory UI. Mutate forms on Inventory Detail were not consistently gated by ManageInventory (safe only while view≡manage). More hub required `ManageCatalog && canViewInventory`, hiding Inventory from InventoryStaff/ReportingUser.

## Changes

| Area | Change |
|------|--------|
| `pos-capabilities.ts` | Split View vs Manage; document SMB ManageInventory scope |
| `org-nav-config.ts` | More → Inventory when `canViewInventory` |
| `InventoryDetailPage.tsx` | Gate enable/opening/adjust/disable/expiration CTAs on Manage |
| `InventoryListPage.tsx` | View-only vs manage-scope hints |
| Denial copy (`en.ts` + locale keys) | Clarify actions need inventory management permission |
| Test fixtures | Replace stale `capabilities: ["Inventory.*"]` mocks with Owner grants |

**PRODUCTION_BEHAVIOR_CHANGE:** UI gating aligned to existing server `PosRoleMatrix` (no new capabilities; no backend matrix change).

**PRODUCTION_GUARDS_WEAKENED:** NO

## I18N (secondary)

| Check | Result |
|-------|--------|
| Suite-blocking Unicode keys (from prior harness package) | Already fixed |
| Intentional copy updates | manageDenied + manageScopeHint / viewOnlyHint |
| Broad remaining `?` encoding debt in unrelated strings | Out of scope |
| **I18N_CHANGE_REQUIRED** | **NO** (no further suite-blocking encoding work) |

## Validation

| Check | Result |
|-------|--------|
| React full suite | TOTAL=1256 PASS=1256 FAIL=0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; pre-existing warnings) |
| BUILD | PASS |
| NEW_TEST_SKIPS / ONLY / EXCLUSIONS | 0 |

## NEXT

Reassess Organization gaps roadmap (Expenses CRUD / B2B identity / other IMPORTANT items).
