# PERSONAL-BASELINE-SYNC-AND-AUDIT-01

**Package:** PERSONAL-BASELINE-SYNC-AND-AUDIT-01  
**Status:** COMPLETE (audit + baseline sync; no Personal feature implementation)  
**Branch:** `feat/personal`  
**Date:** 2026-08-27  

## Baseline identity

| Label | SHA |
| --- | --- |
| Personal remote (pre-sync) | `ed382be941a3145014abda1810f2b517afcb57c0` |
| Organization frozen | `112dc2675b0fbba0450a698662c445c02cc60a18` |
| Fast-forward safe | **YES** (`origin/feat/personal` is ancestor of `origin/feat/organization`) |
| Personal synced HEAD | `112dc2675b0fbba0450a698662c445c02cc60a18` |

Organization is **FROZEN**. This package does not intentionally change Organization behavior. Personal offline/online **policy was not changed**.

Historical reports (RMAP-22A…H, RMAP-21F/G) remain historical. This document is the **current** Personal React audit after sync onto the Organization foundation.

---

## Separation of claims

| Bucket | Meaning |
| --- | --- |
| **CURRENT IMPLEMENTATION** | Verified in React routes/components/clients/offline code on this SHA |
| **VERIFIED GAP** | Missing, partial, or unsafe relative to product expectations — evidence-based |
| **DEFERRED / FUTURE IDEA** | Explicitly not in scope; do not treat as implemented |

### Deferred / future ideas (not implemented)

- Public external-camera Organization QR acquisition  
- QR → registration continuation  
- Install ExItS after registration  
- Resume original QR intent after registration/install  
- Advanced action QR / payment QR flows  

---

## CURRENT IMPLEMENTATION (summary)

Router: `ExItS.PinoyBusinessPOS.React/src/app/router.tsx` Personal tree under `RequirePersonalSession` + `PersonalShell`.

| Surface | Class | Evidence |
| --- | --- | --- |
| Sign-in / sign-up / forgot-password | COMPLETE / PARTIAL | `/sign-in`, `/forgot-password`; activation + reset-completion pages **MISSING** |
| Offline PIN unlock / enroll | COMPLETE (Personal) | `/offline-pin`, `/offline-pin-setup`; Org Web skips enroll gate |
| Personal Home | COMPLETE | `/personal` → `PersonalHomePage` (Utang summary, people/todo/stores tiles) |
| People | FUNCTIONAL_BUT_UX_GAP | `/personal/people` list/add/detail/QR resolve; offline contact queue **not UI-wired** |
| Utang lent / owe / create / pay / history / invites | COMPLETE | `/personal/utang/*`; named settlement wizard **MISSING** |
| Todo | COMPLETE | `/personal/todo` CRUD + offline transitions |
| Stores / shop / cart / checkout / orders / receipts | COMPLETE (online) | Linked merchants + customer ordering; cart memory-only |
| My QR / public resolve-in-flow | FUNCTIONAL_BUT_UX_GAP | `/personal/my-qr`; dedicated resolve route **MISSING** |
| Notifications + invitations | PARTIAL | Inbox/archive/utang/people invites; ownership-transfer UI **MISSING** |
| Profile + preferences | FUNCTIONAL_BUT_UX_GAP | Profile + language/theme/density; no dedicated diagnostics page |
| Start a Business | COMPLETE | Explore + start → Organization **onboarding** handoff |
| Personal ↔ Org context switch | COMPLETE | `useSwitchToBusiness`, `ensurePersonalSessionProfile`, `/switching-context` |

Organization preservations verified on this SHA: online-only policy (`organization-web-runtime-policy.ts`), mutation idempotency infra, loading UX, Manage Business / bottom-nav e2e PASS.

---

## Personal offline matrix (AUDIT ONLY — policy unchanged)

| Capability | Classification |
| --- | --- |
| Auth (register/sign-in/forgot/activate) | ONLINE_ONLY |
| Personal ↔ Org switch | ONLINE_ONLY |
| Personal Home dashboard (live) | ONLINE_ONLY |
| Home todo counts when cached | OFFLINE_READ |
| People list/detail/QR/connect | ONLINE_ONLY (UI) |
| Contact create (engine) | OFFLINE_QUEUEABLE — **NOT_IMPLEMENTED in UI** |
| Utang relationship/list/detail read | OFFLINE_READ (encrypted cache) |
| Utang relationship create (contact-side) | OFFLINE_QUEUEABLE |
| Utang Loan/Payment entry | OFFLINE_QUEUEABLE |
| Utang Adjustment / invite / remind / identity link | ONLINE_ONLY |
| Todo create/update/complete/reopen/cancel | OFFLINE_QUEUEABLE |
| Todo share / push reminders | ONLINE_ONLY / NOT_IMPLEMENTED |
| Stores / cart / checkout / orders | ONLINE_ONLY |
| My QR / notifications / Start Business / profile | ONLINE_ONLY |
| Offline PIN + DEK | Present for Personal (not Org Web) |
| Personal outbox | Encrypted AES-GCM (`enqueueEncryptedOperation`) |
| Utang/Todo caches | Encrypted |
| Cart | Unencrypted React memory |

### Idempotency / ambiguous financial outcome (Personal)

| Operation family | `serverDedupeMode` | Ambiguous transport |
| --- | --- | --- |
| Personal contact / relationship create / utang entry | **`idempotency-key`** (PERS-IDEM-01) | Auto-retry safe — client entity id in body + GET-by-id reconcile |
| Todo create | `none` | No auto-retry |
| Todo update/complete/reopen/cancel | `target-state` | Auto-retry allowed |
| Org POS money | `idempotency-key` | Separate Org stack (preserved) |

**PERSONAL_AMBIGUOUS_FINANCIAL_OUTCOME (historical audit):** SAFE against blind duplicate auto-retry for money ops, but **GAP** vs Org-style sticky id + GET reconciliation.

**PERS-IDEM-01 (RESOLVED):** That P0 gap is closed on `feat/personal`. See [`PERS-IDEM-01.md`](./PERS-IDEM-01.md). Client-stable entity ids (`contactId` / `relationshipId` / `entryId`) converge on the server; online Utang UI uses Confirming… → GET reconcile; encrypted outbox persists the same ids across replay. People offline UI remains unwired (engine hardened only).

---

## UX / responsive (code + Playwright)

| Viewport | Result |
| --- | --- |
| Phone 375×812 | RMAP-22H responsive shell **PASS** (no horizontal overflow assert) |
| Tablet 768×1024 / 1024×768 | **PASS** |
| Desktop 1440×900 | **PASS** |
| Org Manage Business hang | **PASS** |
| Org bottom-nav stress (20 cycles) | **PASS** |

Manual stress of Personal↔Org switching is covered by unit tests (`personal-switch-to-business.test.tsx`) and Org e2e; integrated two-user RMAP-22H stories remain **PARTIAL** (mock debt — see below).

Loading foundation (PageSkeleton / BackgroundRefreshIndicator / shell) is preserved; no redesign in this package.

---

## Cross-context safety

| Check | Result |
| --- | --- |
| AccountClass guards | `RequirePersonalSession` / `RequireOrganizationSession` |
| Staff cannot open Personal | E2E privacy test **PASS** |
| Org online-only after sync | Policy + unit tests **PASS** |
| Org idempotency after sync | Unit tests **PASS** |
| Stale Personal data under Org | Guards + workspace bind isolate contexts |

---

## Gap classification (prioritized plan — DO NOT IMPLEMENT HERE)

### P0 — security / money / identity / data-loss

1. ~~**Personal Utang money mutations lack server idempotency keys** (`serverDedupeMode=none`)~~ — **RESOLVED by PERS-IDEM-01** (see [`PERS-IDEM-01.md`](./PERS-IDEM-01.md); implementation SHA recorded there after push).
2. **People offline contact enqueue exists but UI never uses it** — risk of divergent mental model / accidental double-create if later wired without idempotency *(backend/outbox path hardened in PERS-IDEM-01; UI still ONLINE_ONLY)*.
3. **Email activation + password-reset completion missing in React** — account recovery incomplete on Web/PWA.

### P1 — broken / incomplete primary workflows

1. RMAP-22H integrated story mocks stale vs People `/personal/connections` and Start Business → `/onboarding` seller continuation (e2e debt).  
2. No named Utang **settlement/close** flow (pay-to-zero only).  
3. Ownership-transfer Personal UI absent (backend/docs exist historically).

### P2 — important UX / completeness

1. Dedicated public-user resolve route absent (resolve embedded in People/customer-link).  
2. Diagnostics page absent (copy-diagnostics only on errors).  
3. Cart not durable across refresh/offline.  
4. Todo share stub / online-required only.

### P3 — polish

1. Unused `PersonalNotificationsPage` duplicate in social module.  
2. Home empty-state composition polish.  
3. Locale native-speaker certification still PENDING (roadmap note).

---

## Test evidence (this package)

| Gate | Result |
| --- | --- |
| Full React Vitest | **932 passed** / 168 files |
| Typecheck | **PASS** |
| Lint | **PASS** (0 errors; existing warnings) |
| Build | **PASS** |
| Org Manage Business e2e | **PASS** |
| Org bottom-nav stress e2e | **PASS** |
| RMAP-22H | **PASS** (7/7 after FF hygiene: PIN enroll wait, People `/personal/people`, connections mock, purpose field, todo create toggle, Start Business → `/onboarding` handoff). Full buyer→seller order continuation deferred as **PERS-E2E-22H-REPAIR**. |
| Org online-only regression | **PASS** |
| Org idempotency regression | **PASS** |
| Loading UX regression | **PASS** (suite includes loading-ux tests) |
| Context-switch regression | **PASS** (unit + RMAP-22H privacy/invite accept) |

### Narrow code change (not feature work)

`e2e/rmap-22h-personal-business-e2e.spec.ts` only: Personal offline PIN after sign-in; People at `/personal/people`; Start Business → `/onboarding`; mock `GET /api/v1/personal/connections` + unread-count; purpose + todo create toggle. Seller multi-user commerce continuation deferred to **PERS-E2E-22H-REPAIR**. **No Personal offline policy change. No Organization product change.**

---

## Next implementation packages (evidence-based order)

1. **PERS-IDEM-01** — Personal Utang (and contact create) server/client idempotency + ambiguous-outcome UX aligned with Org conventions (without forcing Org online-only onto Personal).  
2. **PERS-AUTH-01** — Activation + password-reset completion pages.  
3. **PERS-PEOPLE-OFFLINE-01** — Decide: wire People UI to existing contact outbox **or** remove dead enqueue path; classify policy explicitly.  
4. **PERS-E2E-22H-REPAIR** — Refresh RMAP-22H mocks for People + Start Business onboarding seller path.  
5. **PERS-SETTLE-01** / **PERS-OWNERSHIP-01** — settlement UX; ownership-transfer UI if product prioritizes.  

Do **not** change Personal offline/online policy until a dedicated authorized package.

---

## Git notes

- Fast-forward only: `feat/personal` → Organization SHA `112dc267…`  
- Do not modify `feat/organization` or `main`  
- Docs commit: `docs(personal): audit current React experience`  
- E2E hygiene (if present): separate focused commit before docs  
