# POS-REACT-RMAP-22B — Personal Shell + Home

## Status

**PASS**

## Scope delivered

- `PersonalShell` with Personal top bar (no POS `AppTopBar` / workspace chrome on `/personal/*`)
- Bottom navigation: Home | Utang | To-do | Orders | More
- Utang-first `PersonalHomePage` using `GET /api/v1/personal/dashboard`
- Quick actions to Utang/To-do placeholders
- Hub placeholders for Utang sections, To-do, More, Start a Business (filled in later packages)
- `RequirePersonalSession` retained — Organization staff principals denied
- Five-locale keys for new Personal copy (`en`, `fil-PH`, `ceb-PH`, `ilo-PH`, `hil-PH`)

## Explicit non-claims

- Full Utang CRUD (RMAP-22C)
- Invitations/reminders/notifications UI (RMAP-22D)
- Personal To-do backend/UI (RMAP-22E1/E2)
- Start Business subscription flow (RMAP-22G)
- Offline (RMAP-21)
- RMAP-B04 / B05 / TAX

## Tests

| Suite | Result |
| --- | --- |
| `personal-shell-home.test.tsx` | PASS (shell, home summary, staff denial) |
| `personal-dashboard-client.test.ts` | PASS |
| `message-parity.test.ts` | PASS |
| `typecheck` | PASS |
| `lint` | PASS (0 errors) |
| `format:check` | PASS |

Native-speaker certification: **PENDING**

## Files

- `src/.../Client/src/features/personal/PersonalShell.tsx`
- `src/.../Client/src/features/personal/PersonalBottomNav.tsx`
- `src/.../Client/src/features/personal/PersonalHomePage.tsx`
- `src/.../Client/src/features/personal/PersonalHubPages.tsx`
- `src/.../Client/src/api/platform/personal-dashboard-client.ts`
- `src/.../Client/src/app/RootLayout.tsx`, `router.tsx`
- Locale catalogs

## Next

**RMAP-22C — Personal Utang React core**
