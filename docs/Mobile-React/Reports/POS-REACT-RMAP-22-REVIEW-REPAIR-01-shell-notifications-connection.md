# POS-REACT RMAP-22 REVIEW REPAIR 01 — Shell notifications + Connection foundation

**Status:** `RMAP_22_REVIEW_REPAIR_01=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Start HEAD:** `89470397193382393fded5a8b8096e1e64c17632`  
**Branch:** `feat/pos-react-client`

## Approved baselines preserved

- `RMAP_22_PERSONAL_MASTER_RUN_01=APPROVED`
- `POS_OPERATIONS_UX_REPAIR_01=APPROVED`
- Device capacity: finite server-authoritative (`10000` is finite; unlimited client sentinel removed)

## Ops E2E encoding hygiene

- Removed UTF-8 BOM from `e2e/ops-ux-repair-01.spec.ts`
- Replaced mojibake arrow sequences with ASCII `->` in test titles
- `OPS_UX_ENCODING_HYGIENE=PASS`

## Delivered

### Personal shell

- Top bar: identity · Connection · Notifications bell · account menu
- Bell unread badge from Personal notification list (`!isRead`): blank / `1–9` / `9+`
- Navigates to `/personal/notifications`
- Personal-context only (no Organization operational notifications)

### Notifications page

- Tabs: **Unread** (default) / **All**
- Empty unread copy: caught up → switch to All
- Mark read invalidates shared `["personal","notifications"]` query (badge updates)

### Connection

- Title: **Connection** (not Connection & Sync)
- States: Online / Offline via `useBrowserOnline`
- **Refresh data** = `queryClient.invalidateQueries()` only
- No “All changes synced”, pending count, or last-synced claims

### Organization

- Shared basic Connection control on `AppTopBar`
- Organization notification React contract: **GAP** (`ORGANIZATION_NOTIFICATION_CONTRACT_GAP=YES`)
- Organization bell: not implemented

## RMAP-21 Connection & Sync contract (documented only)

Future upgrade when LocalStore/outbox exist:

| Section | Meaning |
| --- | --- |
| CONNECTION | Online / Offline (access / locally available data) |
| SYNCHRONIZATION | All synced / N waiting / needs attention — **only from real outbox** |
| Actions | Refresh from server / Retry sync / Review issue |

Personal sync scope: Personal Utang, Personal To-do, safe Personal caches.  
POS sync scope: authorized org/branch/device, catalog, allowed offline sales, outbox, reconciliation.  
Do not merge Personal and Organization local authority for one human.

`RMAP_21_AUTHORIZED=NO` — implementation not started.

## Flags

```text
OPS_UX_ENCODING_HYGIENE=PASS
RMAP22_NOTIFICATION_BELL=PASS
RMAP22_UNREAD_BADGE=PASS
RMAP22_NOTIFICATION_UNREAD_ALL=PASS
RMAP22_NOTIFICATION_PRIVACY=PASS
RMAP22_CONNECTION_STATUS=PASS
RMAP22_REFRESH_DATA=PASS
RMAP22_FAKE_SYNC_CLAIMS=NO
RMAP21_CONNECTION_SYNC_CONTRACT=DOCUMENTED
ORGANIZATION_NOTIFICATION_CONTRACT=GAP
```

## Explicit exclusions

- RMAP-21 offline/outbox/sync UI claims
- Fake sync wording
- Invented Organization notification backend
- Backend notification schema changes
