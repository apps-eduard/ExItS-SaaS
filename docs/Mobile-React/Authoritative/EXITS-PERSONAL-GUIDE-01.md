# EXITS-PERSONAL-GUIDE-01 — Personal Explore / User Guide

**Package:** EXITS-PERSONAL-GUIDE-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Route:** `/personal/guide`  
**Title:** Explore ExItS

```
GUIDE_TOGGLES_ARE_NOT_FEATURE_FLAGS=YES
```

## Purpose

Give Personal users a categorized, expandable catalog of **currently implemented** Personal features, with local learning progress, Try it links to real screens, and an optional Home discovery card.

The checks and switches on this page are **user-guide / learning progress only**. They must never enable or disable a real feature, entitlement, authorization, business capability, or Organization setting.

## Route and entry

| Surface | Behavior |
| --- | --- |
| `/personal/guide` | Explore ExItS page inside the Personal shell |
| Personal More | **Explore ExItS** tile (`more-open-guide`) |
| Personal Home | Optional discovery card (dismissible) |

No new bottom-navigation tab.

Subtitle: Discover what you can do with your Personal account.

## Categories and feature codes

Logical order:

1. Account
2. People
3. Money & Utang
4. Productivity
5. Shopping
6. Activity
7. Business

| Code | Category | Try it destination |
| --- | --- | --- |
| `profile` | Account | `/personal/profile` |
| `personal-qr` | Account | `/personal/my-qr` |
| `account-security` | Account | `/settings/preferences` |
| `install-pwa` | Account | `/personal/more` (install offer also on the card when the browser supports it) |
| `people` | People | `/personal/people` |
| `invitations` | People | `/personal/invitations` |
| `customer-links` | People | `/personal/customer-links` |
| `utang` | Money & Utang | `/personal/utang` |
| `utang-settlement` | Money & Utang | `/personal/utang` |
| `todo` | Productivity | `/personal/todo` |
| `todo-reminders` | Productivity | `/personal/todo` |
| `stores` | Shopping | `/personal/linked-merchants` |
| `business-qr` | Shopping | `/personal/linked-merchants` |
| `shopping-cart` | Shopping | `/personal/linked-merchants` |
| `checkout` | Shopping | `/personal/linked-merchants` |
| `my-orders` | Shopping | `/personal/orders` |
| `notifications` | Activity | `/personal/notifications` |
| `start-business` | Business | `/personal/explore-pos` |
| `business-switching` | Business | `/personal/more` |
| `ownership-transfer` | Business | `/personal/ownership-transfers` |

Guide definitions are code-driven in `personal-guide-features.ts`. Future features (BNPL, Pawn, Loans) can be added as new definitions without changing the storage architecture. Existing learned codes remain valid.

## Progress semantics

Persistent states:

- `NOT_EXPLORED` (`learned = false`)
- `LEARNED` (`learned = true`)

`Opened` may appear in the current session after a card is expanded. It is not persisted.

Progress summary: **X of Y features explored** and a percent bar. Unknown or future codes in storage are ignored for the count. Unavailable/conditional items are **not** auto-completed.

Filters: All / Not explored / Completed.

## Conditional wording

Some cards say:

- “Available when applicable.”
- “Your actual access is determined when you open this feature.”

The guide does **not** duplicate authorization, entitlements, ordering readiness, ownership rules, or customer-link rules. Try it navigates to the real screen; that screen remains authoritative.

Business QR ≠ ownership transfer. A participating business QR opens a public store (phone camera / deep link). Scanning it does not transfer ownership.

## Storage schema

Account-namespaced `localStorage` only. No backend entity, table, or migration.

Key: `exits.personal.guide.v1:{platformUserId}`

```json
{
  "version": 1,
  "learned": ["profile", "people", "utang"],
  "homeCardDismissed": false
}
```

Validation: invalid JSON, wrong version, missing fields, and wrong types fail safe to empty progress. Duplicate codes are normalized. Unknown feature codes are ignored for UI/progress and do not crash the app.

User A progress must never show for User B on the same browser.

## Home card

Optional card on Personal Home:

- Title: Explore ExItS
- Progress: X of Y explored
- Continue guide → `/personal/guide`
- Dismiss / Hide (persisted per account via `homeCardDismissed`)

Not a login modal. After dismiss, Explore ExItS remains in More. The Guide page can restore the Home card (**Show guide card on Home**).

## Online / offline

Guide progress is local browser state and can be read from the loaded PWA shell.

Try it destinations keep their existing authoritative online policy.

```
NEW_PERSONAL_WEB_OUTBOX_ENQUEUE=NO
```

## Non-goals

- Backend guide DB / migration / admin editor / CMS / analytics
- Feature flags, entitlements, permissions, subscription or business configuration changes
- Unfinished BNPL / PLM / PPM Personal features or “Coming Soon” cards
- Redesign of all Personal pages
- Full-app audit
- Offline checkout, offline financial action, offline ownership action, offline business mutation

## Tests

- `personal-guide-features.test.ts` — unique codes, valid categories, non-empty copy, Try it routes exist
- `personal-guide-storage.test.ts` — empty/valid restore, mark/unmark, malformed JSON, version, unknown codes, duplicates, isolation, home card
- `PersonalGuidePage.test.tsx` — render, expand/collapse, mark learned, filters, Try it, remount restore, Home card
- `e2e/exits-personal-guide-01.spec.ts` — Home card → guide; Stores learned + refresh; cross-account isolation; Try it → Stores; Home dismiss + More entry
