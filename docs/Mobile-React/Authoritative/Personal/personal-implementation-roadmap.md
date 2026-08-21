# Personal React/PWA Implementation Roadmap (RMAP-22)

**Status:** Authoritative execution plan for Personal Master Run 01 — **APPROVED** (`RMAP_22_PERSONAL_MASTER_RUN_01=APPROVED`)
**Branch:** `feat/pos-react-client`
**Start SHA:** `584004b98bd6bc360dc0edfec89e6445cc920e43` (`PERSONAL_MASTER_RUN_01_START_SHA`)

## Review Repair 01 (shell notifications + connection)

After Master Run 01 visual review:

- Personal notification bell + unread badge (Personal-context only)
- Unread / All notifications views
- Honest **Connection** control (Online/Offline + Refresh data)
- **RMAP-21** will later upgrade Connection → **Connection & Sync** with real outbox/sync state
- Organization notification React contract: **GAP** (do not invent); basic Connection may be shared

See [POS-REACT-RMAP-22-REVIEW-REPAIR-01-shell-notifications-connection.md](../../Reports/POS-REACT-RMAP-22-REVIEW-REPAIR-01-shell-notifications-connection.md).

## Product Owner execution-order decision

**RMAP-22 Personal is executed before RMAP-21 Offline.**

Reason: validate end-to-end Personal ↔ Organization SaaS (registration, Utang, linking, To-do, Start Business, trial/subscription, customer link, storefront ordering, My Orders) while Personal was only a thin React shell.

- Historic package numbers are **not** renumbered.
- RMAP-21 remains planned and **not started** (`RMAP_21_AUTHORIZED=NO`).
- This does **not** cancel RMAP-21.

## Package map (RMAP-22 Master Run 01)

| ID | Intent | Notes |
| --- | --- | --- |
| **RMAP-22A** | Current-state reconciliation + canonical docs | Docs only |
| **RMAP-22B** | Personal shell + Utang-first Home | React |
| **RMAP-22C** | Personal Utang React core | Reuse `/api/v1/personal/utang/*` |
| **RMAP-22D** | Invitations + reminders + notifications | Reuse existing backend |
| **RMAP-22E1** | Personal To-do backend/domain/API | **NEW DOMAIN** if still absent |
| **RMAP-22E2** | Personal To-do React + Today agenda composition | React |
| **RMAP-22F** | Customer linking + Stores + ordering + My Orders | Reuse RMAP-19 |
| **RMAP-22G** | Start Business + trial/subscription + workspace | Existing Platform contracts |
| **RMAP-22H** | Integrated Personal ↔ Business E2E | Online-first |

Blueprint PERS-* labels map to RMAP-22A…22H above (PERS-B04-RECON / PERS-06 / PERS-07 / PERS-08 are **out of this run**).

## Hard gates (unchanged)

```text
RMAP_21_AUTHORIZED=NO
RMAP_B04_AUTHORIZED=NO
RMAP_B05_AUTHORIZED=NO
RMAP_TAX_AUTHORIZED=NO
PRODUCTION_CUTOVER=NO
```

Do not start: RMAP-21 Offline, RMAP-23, RMAP-B04, RMAP-B05, RMAP-TAX, RMAP-24, production cutover, real ad/reward/payment providers, Loan SaaS.

## Backend reuse vs new work

| Area | Action |
| --- | --- |
| Personal Utang | **Reuse** Platform domain/API |
| Invitations / reminders / in-app notifications | **Reuse** |
| Customer link / linked merchants / ordering | **Reuse** Platform + POS RMAP-19 |
| Start Business / catalog / trial / entitlement | **Reuse** |
| Public identity / QR | **Reuse** `/api/v1/me/public-identity` |
| Personal To-do | **Create** additive domain (RMAP-22E1) |
| Linked Business Utang / buyer purchase projection | Document Phase-24 overlap; **do not implement RMAP-B04** |

## Offline posture

Online-first for entire Master Run. Design mutations with stable IDs + concurrency suitable for future outbox, but implement **no** LocalStore/outbox here.

## Locale / responsive

All new user-facing keys: `en`, `fil-PH`, `ceb-PH`, `ilo-PH`, `hil-PH`. Native-speaker certification: **PENDING**.

Viewports: 375×812, 768×1024, 1024×768, 1440×900.
