# PERS-OWNERSHIP-01 — Personal Ownership Transfer Recipient UI

**Package:** PERS-OWNERSHIP-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Date:** 2026-08-27  
**Baseline:** `8ae5ed513271f58103e0e822422db7d4bf680410`  
**Implementation SHA:** `f4509b7fd1eece682b84b53192e1f0e41ea9f303`

## Gap closed

Platform ownership-transfer APIs and MAUI recipient page already existed. React Personal Web had **no** recipient UI for pending Organization ownership transfers.

## Scope

React Personal **recipient** only:

- List pending transfers for the signed-in Personal user
- Accept / Decline with confirmations
- Success handoff to business workspace when orgs are available

## Explicit non-goals

| Flag / exclusion | Value |
| --- | --- |
| `REACT_ORG_OWNER_INITIATION_PRESENT` | **NO** — no React Org Owner “request transfer” UI |
| Ownership-transfer backend / domain semantics | Unchanged |
| Migrations | None |
| Permanent bottom-nav item | Not added (More tile only) |
| Cross-product / PHI | Out of scope |

## API (authoritative)

```
GET  /api/v1/platform/ownership-transfers/my-pending
POST /api/v1/platform/ownership-transfers/{id}/accept
POST /api/v1/platform/ownership-transfers/{id}/decline
```

DTO (camelCase): `id`, `organizationId`, `organizationDisplayName`, `publicOrganizationId`, `fromOwnerUserId`, `toUserId`, `toDisplayName`, `toPublicUserId`, `status`, `createdAtUtc`, `expiresAtUtc`, `acceptedAtUtc`, `declinedAtUtc`, `cancelledAtUtc`, `completedAtUtc`, `updatedAtUtc`.

## UI

- Route: `/personal/ownership-transfers` under `RequirePersonalSession`
- More tile: `more-open-ownership-transfers` (optional pending count in label)
- Online-only accept/decline (`ONLINE_REQUIRED_CODES.PersonalOwnershipTransfer`)
- Accept confirmation discloses Owner role, former owner leaves, business data stays, Personal/Utang/payment methods/POS-local roles/devices are **not** transferred
- Expired (`status === Expired` or `expiresAtUtc < now`) hides Accept/Decline
- Accept success: success panel first; **Go to business** / **Stay in Personal** then `refreshWorkspaces()` + invalidate pending (avoids workspace `loading` remount wiping success state)
- `REACT_ORG_OWNER_INITIATION_PRESENT=NO`

## Notifications

Platform currently has **no** ownership-transfer in-app notification `relatedType` (grep found none).  
`resolveNotificationDeepLink` defensively maps strings containing `ownershiptransfer` / `ownership_transfer` / `OrganizationOwnershipTransfer` to `/personal/ownership-transfers` for future backend emission. **Documented gap:** no live relatedType today.

## Files

- `src/api/platform/ownership-transfer-client.ts` (+ unit test)
- `src/features/personal/ownership/PersonalOwnershipTransfersPage.tsx` (+ unit test)
- `e2e/pers-ownership-01.spec.ts`
- Router, Personal More, online-required, i18n (en + fil/ceb/hil/ilo)
- This package doc; PERSONAL-BASELINE sync update

## Test evidence

| Gate | Result |
| --- | --- |
| Vitest `ownership-transfer` + `PersonalOwnershipTransfers` | PASS |
| Playwright `pers-ownership-01` | PASS (accept / decline / privacy / account-class) |

## Stories covered (e2e)

- **A** Accept → Accepted + Go to business when orgs mocked post-accept  
- **B** Decline → Declined + empty list + no org membership  
- **C** Privacy — User C empty list for B’s transfer (separate BrowserContexts)  
- **D** Org staff → `account-class-denied`  
- **E** Multi-org story asserted via shared state (`userARemainingOrgs` retains Org B after Org A transfer)
