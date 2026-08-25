# PERSONAL-HOME-UTANG-UX-01

| Field | Value |
|---|---|
| Status | Complete |
| Branch | `feat/pos-react-client` |
| Worktree | `ExItS-SaaS-pos-react-client` |
| Migration | **NONE** |
| Backend change | **NONE** (dashboard + lent/borrowed summaries reused) |

## Responsibility split

| Surface | Job |
|---|---|
| **Home** | At-a-glance: confirmed Utang snapshot, needs-attention (pending confirmations / overdue / due soon), primary quick actions, compact to-do preview |
| **Utang** | Workspace: confirmed totals, pending banner, segmented account list (All / Owed to me / I owe), search, navigate into relationship ledger |

Duplication reduced: Home no longer uses a 4-tile Utang metric grid for People/Active; Utang is no longer summary+actions only.

## Confirmed-only totals

`GET /api/v1/personal/dashboard` `totalLentBalance` / `totalBorrowedBalance` remain authoritative on both pages (pending shared-ledger entries excluded server-side).

## Attention

Home attention uses:

- `pendingConfirmationCount` from dashboard
- overdue / due-soon derived from lent+borrowed summary `dueDateUtc` via existing `formatDueLabel`

## Files

- `PersonalHomePage.tsx`, `PersonalHubPages.tsx`
- `utang-workspace.ts` (+ tests), `UtangAccountCard.tsx`
- locales + `globals.css` segment/balance styles
- package report (this file)
