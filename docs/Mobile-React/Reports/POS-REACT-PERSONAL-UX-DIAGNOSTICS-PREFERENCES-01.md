# POS-REACT PERSONAL UX DIAGNOSTICS AND PREFERENCES 01

**Status:** COMPLETE  
**Start SHA:** `bbd7ae41f7bda738743f4ab2b5826472750ddf05`  
**Implementation commit:** _(filled after push)_  
**Branch:** `feat/pos-react-client`

## Delivered

Personal-scope UX polish, copyable global error diagnostics, preferences layout, and Platform personal todo reminder persistence.

### Global error diagnostics

- Pub/sub `global-error-reporter` + React Query handlers (`attach-global-query-error-handlers`)
- Overlay via `GlobalRuntimeErrorHost` / `ClientErrorPanel` with copy-paste report
- `ErrorState` accepts `error` and auto-builds diagnostic (Todo list/detail wired)
- i18n keys: `diagnostics.globalTitle`, `globalHint`, `copyableReport`, `reload`, `dismiss`

### Personal / buyer UX

- My Orders, Stores, Store link requests: Quick-action tiles (`ActionTileGrid`) instead of pill chips
- Empty-state primary CTA centered on full-width tiles
- Personal home / utang / todo / social / notifications localization (fil/ceb/hil/ilo)
- Preferences: Language 2-col (Ilongo full width); Theme/Density stacked on small screens, one row ≥480px

### Platform

- Personal contact update + todo reminder notified-at migration / delivery worker
- `Start-PlatformApiOnly.ps1` retained for local React POS against host Platform API `:8091`

## Tests / validation

| Check | Result |
|-------|--------|
| `global-error-reporter.test.ts` + `ErrorState.test.tsx` | PASS |
| `tsc --noEmit` (client) | PASS (earlier in session) |
| Full `ExItS.slnx` Release + Playwright | Not re-run for this note |

## Exclusions

- Forcing a single Platform API worktree (local conflict with `PlatformWeb-PA-COM-01` on `:8091` remains operator concern)
- MAUI / Org Web surfaces
- Production packaging

## Local runtime note

React POS expects Platform API from **this** worktree on `:8091`. If another worktree owns the port, Personal Todo returns generic **404**. Use:

```powershell
powershell -NoProfile -File tools/Start-PlatformApiOnly.ps1
```

in a window left open. Verify with `GET /api/v1/personal/todos` → **401** (not 404).

## Next

- Keep local Platform API source aligned with React POS during Personal QA
- Optional: suppress global overlay vs inline ErrorState on more personal queries
