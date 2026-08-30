# POS-I18N-LOCALE-PARITY-02

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-I18N-LOCALE-PARITY-02  
**START_SHA:** `523fe6e5f46220e58ba7f9e40b2a3003e7dd05c5`  
**FEATURE_SHA:** `dd775a97e7d72524941a40b03d40435316ecc71e`

## ROOT_CAUSE

Locale `.ts` files suffered encoding corruption during prior edits/merges: em dashes (`—`) and middle dots (`·`) were saved as ASCII `?`, and ellipsis endings on stock-count loading strings became U+FFFD replacement characters. English canonical keys for stock-count movement labels retained the same defect on two keys.

Philippine locales mirrored the corrupted separator on translated inventory movement labels (`Stock adjustment ? dagdag`), making Inventory Detail / history rows show `?` instead of a proper separator.

## SUPPORTED_LOCALES

`en`, `fil-PH`, `ceb-PH`, `hil-PH`, `ilo-PH`

## LOCALE_PARITY_MODEL

- `en.ts` is the structural key reference (`MessageKey = keyof typeof en`).
- All locales must expose identical keys (existing `message-parity.test.ts` guard).
- Encoding hygiene: no U+FFFD / common mojibake; inventory movement labels use `—`; expiry summary labels use `·`; placeholders must match English token sets on audited inventory keys.

## AUDIT RESULT (pre-fix)

| LOCALE | MISSING_KEYS | EXTRA_KEYS | MOJIBAKE_KEYS | PLACEHOLDER_MISMATCHES |
|--------|--------------|------------|---------------|------------------------|
| en | 0 | 0 | 7 inventory-related | 0 |
| fil-PH | 0 | 0 | 12 inventory-related | 0 |
| ceb-PH | 0 | 0 | 13 inventory-related | 0 |
| hil-PH | 0 | 0 | 13 inventory-related | 0 |
| ilo-PH | 0 | 0 | 12 inventory-related | 0 |

## MISSING_KEYS_BEFORE / AFTER

| | Before | After |
|--|--------|-------|
| All locales | 0 | 0 |

## MOJIBAKE_KEYS_FOUND / FIXED

| Key group | Locales affected | Fix |
|-----------|------------------|-----|
| `inventory.movementType.manualIncrease/Decrease` | fil, ceb, hil, ilo | ` ? ` → ` — ` |
| `inventory.movementType.stockCountIncrease/Decrease` | en + all PH | ` ? ` → ` — ` |
| `inventory.expiryCounts` | en + all PH | ` ? ` → ` · ` |
| `inventory.expirationTrackingOnWithWarning` | ceb, hil | ` ? ` → ` · ` |
| `inventory.addStockHint` | en + all PH | ` ? ` → ` — ` |
| `inventory.untrackedHint` | en + all PH | ` ? ` → ` — ` |
| `openingStock.unitCostHelper` | en + all PH | ` ? ` → ` — ` |
| `stockCount.loadingAll/saving/loading/completing` | all PH | U+FFFD → `?` (progress prompt) |

**MOJIBAKE_KEYS_FIXED:** 58 string values across 5 locale files (encoding defects only; translations unchanged).

## PLACEHOLDER_MISMATCHES_BEFORE / AFTER

| | Before | After |
|--|--------|-------|
| Audited inventory keys | 0 | 0 |

## INVENTORY_MOVEMENT_LABELS_FIXED

- Opening stock / Direct buy / PO receipt — unchanged (no separator defect)
- **Stock adjustment — increase/decrease** (all PH + en stock count variants)
- **Stock count — increase/decrease**
- Sale, transfer, stock use, waste/loss, production keys — no separator corruption found

## LINGUISTIC_REWRITE_PERFORMED

**NO** — only punctuation/encoding repair; Filipino/Cebuano/Hiligaynon/Ilocano wording preserved.

## BACKEND_CHANGE_REQUIRED

**NO**

## MIGRATION

**N/A**

## TARGETED_TESTS

Extended `src/i18n/message-parity.test.ts`:

- No U+FFFD / common mojibake in any locale value
- Placeholder parity on inventory encoding keys
- Em dash on movement separator keys
- Middle dot on expiry summary keys

Existing `InventoryDetailPage.cost.test.tsx` still expects `Stock adjustment — increase`.

## REACT_FULL_SUITE

| Metric | Value |
|--------|-------|
| TOTAL | 1287 |
| PASS | 1287 |
| FAIL | 0 |

**Baseline delta:** +20 tests (encoding hygiene guards).

## TYPECHECK / LINT / BUILD

| Check | Result |
|-------|--------|
| TYPECHECK | PASS |
| LINT | PASS (pre-existing warnings) |
| BUILD | PASS |

## NEXT

**POS-REPORT-EXPORT-01**
