# MOBILE-REACT-IMPL-02A — Product shell visual correction

**Package:** MOBILE-REACT-IMPL-02A  
**Date:** 2026-08-19  
**Branch:** `feat/mobile-react-client`  
**Required starting SHA (package text):** `6b24b18c7efbf191b2a4e2bd5326f669163ad4ef`  
**Actual starting HEAD:** `648541952be8c66abbd9156a951bf6afbb0ed49f` (MOBILE-REACT-IMPL-03A already on the branch; no reset/rebase)  
**Starting `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Does **not** rewrite DOC-08, AMEND-01/02/03, APPROVAL, MERGE-01, IMPL-01, IMPL-02, or IMPL-03A reports.

---

## Status

| Item | Status |
|---|---|
| IMPL-02A | **COMPLETE** after validation |
| Foundation/demo content | **REMOVED FROM PRODUCT ROUTES** |
| Development implementation documentation | **DOCS ONLY** |
| IMPL-03 | **NOT COMPLETE** (workspace UI / PIN / selling not in this package) |
| IMPL-03A browser session auth | **UNCHANGED** (cookie proxy and Sign In kept; product copy only) |
| MOBILE-D-060 | **OPEN** |
| Capacitor / selling / offline / PIN | **NOT IN THIS PACKAGE** |

---

## What changed

Product routes no longer show Preview, Foundation, sample totals, simulated errors, package notices, fake workspace, or fake Online/Synced.

`AppTopBar` is a single compact row: ExItS mark, **ExItS Mobile**, Settings. Appearance is opened from that Settings action. Bottom navigation and the desktop sidebar are omitted until real destinations exist.

`/` is a restrained welcome surface. `/appearance` is a grouped preference sheet (Language + Theme segmented controls, Back on phone, Sign out when a session exists). Shared primitives (Button, Card, EmptyState, ErrorState, Copy Diagnostics, PWA update notice) remain in the codebase and are not demonstrated on product routes.

PWA IMPL-02 behavior is unchanged: manifest, icons, static shell cache, NetworkOnly APIs, explicit update notice, no financial cache, no Background Sync.

---

## Screenshots

`docs/Mobile-React/Reports/impl-02a-ui/`

- `01-home-375x812-en-light.png`
- `02-home-375x812-en-dark.png`
- `03-home-375x812-fil-PH.png`
- `04-appearance-375x812-en-light.png`
- `05-appearance-375x812-en-dark.png`
- `06-home-768x1024.png`
- `07-home-1280x800.png`

---

## Explicitly not delivered

- Workspace resolver / branch chooser
- PIN / trusted device
- Offline LocalStore / outbox / sync
- Cart / checkout / selling
- Capacitor / Android native / MAUI changes
- Cookie architecture / CORS / Platform API behavior
- Production PWA rollout
