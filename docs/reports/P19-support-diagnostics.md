# P19 — Scoped Support Diagnostics (Device-Local)

| Field | Value |
|---|---|
| Status | **Code Complete** · Physical device validation **Incomplete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Related | [P19-offline-operability-foundation](P19-offline-operability-foundation.md), [P19-offline-connectivity-capability-matrix](P19-offline-connectivity-capability-matrix.md), [P19-personal-scope-offline-operability](P19-personal-scope-offline-operability.md) |
| Date | 2026-08-09 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

One shared, read-only **Settings → Support → Diagnostics** experience for Personal and Organization scopes. Reuses auth, offline grant, SQLite context, outbox/sync, and the shared Internet-required dialog. Device-local only — not Platform Admin remote diagnostics.

## 2. Architecture

```
ISupportDiagnosticsService
├─ PersonalSupportDiagnosticsProvider
└─ OrganizationSupportDiagnosticsProvider
     └─ IOrganizationOwnerProbe (Platform owner / offline Owner grant)
```

Shared UI: `SupportDiagnosticsView.razor`  
Routes: `/personal/settings/support/diagnostics`, `/settings/support/diagnostics` (both OfflineCapable).

## 3. Access rules

| Scope | Who may view |
|---|---|
| Personal | Authenticated Personal session (`OrganizationId` null, not org-context-locked, no POS access) |
| Organization | Platform **OrganizationOwner** / **OrganizationAdministrator** for the **current** org; offline fallback: durable Organization grant with POS **Owner** role for same user/org |

Managers, cashiers, and other staff are blocked in UI (Settings link hidden) and on direct route (access denied state; no snapshot).

Staff identities must not open Personal diagnostics.

## 4. Fields

**Shared:** scope, connection, device ID (short), app version, API/server status if known, local schema version, last successful sync, pending/failed counts, offline grant status + expiry, PIN enrolled (yes/no), last server contact (grant validation time).

**Personal:** UserId, PersonalProfileId — Personal pending/failed only.  
**Organization:** UserId, OrganizationId, public org id when available from username host (`@ORG######`), current role, org display name — Organization pending/failed only.

## 5. Prohibited

No passwords, PIN hashes, tokens, encryption keys, raw payloads, full DB paths/content, queue clear/edit, force-synced, grant reset, or destructive repair tools.

## 6. Safe actions

| Action | Behavior |
|---|---|
| Retry connection | `OnlineRequiredGuard` + API health refresh |
| Retry sync | Existing Personal / Organization sync seams; requires online (shared Internet-required dialog when offline) |
| Copy diagnostic report | Safe plain text only |

Blocked OnlineRequired actions do not logout, clear local work, or redirect to `/reconnect`.

## 7. Isolation

- Personal provider opens/reads Personal context only.
- Organization provider reads ActiveContext only when it matches current org/user; never Personal marker or Org B.
- Pending/failed counts come from the active scoped outbox.

## 8. Device vs remote

This release is **device-local**. Future Platform Admin remote diagnostics are out of scope.

## 9. Validation

| Layer | Status |
|---|---|
| Unit + Maui guard tests | Covered in this delivery |
| Physical device checklist | **Incomplete** — do not mark Device Verified |
