# P19 — Personal-Scope Offline Operability Foundation

| Field | Value |
|---|---|
| Status | **Code Complete** · Physical device validation **Incomplete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Related | [P19-offline-operability-foundation](P19-offline-operability-foundation.md), [P19-offline-connectivity-capability-matrix](P19-offline-connectivity-capability-matrix.md), [P19-support-diagnostics](P19-support-diagnostics.md) |
| Architecture | [saas-scopes-users-boundaries-navigation](../architecture/saas-scopes-users-boundaries-navigation.md) |
| Date | 2026-08-09 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Keep Personal Utang (contacts / lent / borrowed / entries) usable offline on a previously authorized Personal session, **separate from Organization POS** local databases and grants. Reuse the existing `OfflineAwareNavigation` / `OnlineRequiredGuard` / `OnlineRequiredDialogHost` dialog system — do not invent a second one.

## 2. Capability matrix (Personal)

| Route / action | Requirement | Notes |
|---|---|---|
| `/personal`, `/personal/more`, `/personal/settings`, `/personal/profile` | OfflineCapable | Shell / settings chrome |
| `/personal/utang/people`, `/lent`, `/borrowed`, `/relationships…` | Queueable | Local-first list + create |
| `/personal/utang/invitations` | OnlineRequired | Invite accept/list |
| `/personal/explore-pos`, `/start-business` | OnlineRequired | Commercial / org upgrade |
| `/personal/resolve-user`, `/personal/my-qr` | OnlineRequired | Public id fetch / QR |
| `/personal/invitations/accept` | OnlineRequired | Token accept |
| `personal.contact.create`, `personal.lent.create`, `personal.borrowed.create`, `personal.entry.record` | Queueable | Local-first mutations |
| `personal.invite`, `personal.link_user`, `personal.start_business` | OnlineRequired | Mixed-page CTAs |

Longer Queueable prefixes override the blanket `/personal/utang` OnlineRequired classification.

## 3. Isolation model

| Concern | Rule |
|---|---|
| Path marker | `PersonalLocalScope.PathIsolationMarker` + product code `exits.personal.utang` |
| Open API | `ILocalContextManager.OpenPersonalAsync(userId)` — `OpenAsync` rejects the marker |
| Row ownership | Personal tables use `user_id` — **no** `organization_id` columns on personal rows |
| Outbox org slot | `offline_operations.organization_id` stores the isolation marker (NOT NULL constraint) |
| Payload | Enqueue payload includes `ScopeKind=Personal`; dispatchers call Platform Personal APIs only |
| Cross-use | Personal grant must not open org POS DB; org grant must not open personal DB |

## 4. Offline operating grant v2

- `OfflineGrantScopeKind` = `Organization | Personal`
- SchemaVersion **2** (`Guid? OrganizationId`, `ScopeKind`)
- SchemaVersion **1** still accepted (treated as Organization)
- Establish:
  - Org + `HasPosAccess` → Organization grant
  - `AccountClass == Personal` and `OrganizationId == null` → Personal grant
  - Staff / org-locked / org-bound sessions never get a Personal grant
- PIN unlock restores Personal session with `HasPosAccess=false` and opens Personal local context

## 5. Sync

- Operation types: `personal.contact.upsert`, `personal.relationship.create`, `personal.entry.record`
- `ILocalPersonalUtangStore` persists rows + outbox in one SQLite transaction (idempotent by `operation_id`)
- Local-first create: contact / lent / borrowed always write local SQLite + enqueue (online or offline)
- `IPersonalOfflineSyncService` / shared `OfflineQueueProcessor` + Personal dispatchers flush when online
- `IOfflineReconnectAutoSync` (`OfflineReconnectAutoSyncService`): best-effort flush on Offline→Online, login-while-online, and startup catch-up (debounced, single-flight)
- Shell **Retry sync** uses `RetryIncludingFailedAsync` (reclaim Permanent/Conflict/BlockedByAccess, reset `attempt_count`, then process) — Personal and Organization
- Relationship dispatch remaps local contact PK → Platform `server_id` before `CreatePersonalDebtRelationship` (fixes People synced / Lent stuck Recovery)
- Pending count is queryable per personal context for UI badges (includes permanent/conflict for recovery visibility)

### Contact email uniqueness

- Email optional; when present → trim + upper-case; unique per owner among **Active** contacts
- Local reject: `LocalPersonalStoreErrors.EmailConflict` → UI `Personal_PeopleEmailConflict`
- Platform: `ApplicationErrorCodes.PersonalContactEmailConflict` (409) + filtered unique index migration `20260809120000_AddPersonalContactOwnerActiveEmailUnique`

### Known residuals (polish later)

- Auto reconnect may attempt sync without always clearing Recovery; **manual Retry sync** is the supported recovery path for now
- Full bidirectional Personal conflict UX remains deferred
- Physical Android A–S / Device Verified not claimed

## 6. Device checklist (incomplete)

| Step | Status |
|---|---|
| Online Personal login establishes grant + opens personal DB | Code complete · device incomplete |
| Airplane mode: People / Lent / Borrowed list + create | Code complete · device incomplete |
| Record entry offline → pending badge → reconnect sync | Code complete · device incomplete |
| OnlineRequired dialog on invitations / explore-pos / resolve | Code complete · device incomplete |
| Org POS offline grant cannot read personal rows (and reverse) | Code complete · device incomplete |
| Cold-start PIN unlock for Personal grant | Code complete · device incomplete |
| Mandatory offline PIN enrollment after Personal online login (same as Org POS) | Code complete · device incomplete |
| Sign out keeps grant + PIN; offline Sign In offers limited PIN unlock | Code complete · device incomplete |
| Change/Set Offline PIN from Personal Settings | Code complete · device incomplete |
| Manual Retry clears stuck Personal Recovery after contact-id remap | Code complete · device incomplete |
| Unique email when provided (local + server) | Code complete · device incomplete |

Do **not** mark Device Verified until physical Android confirmation is recorded.

### Offline-capable Personal surfaces (reminder)

Without PIN enrollment, cold-start cannot unlock the Personal grant — the app is forced online. These surfaces are classified for offline / queueable use once PIN + grant exist:

- OfflineCapable: `/personal`, `/personal/more`, `/personal/settings`, `/personal/profile`, support diagnostics
- Queueable: `/personal/utang/people`, `/lent`, `/borrowed`, `/relationships…` (+ local-first create actions)
- OnlineRequired: invitations, explore-pos, start-business, resolve-user, my-qr

## 7. Explicit exclusions

- Personal invite / link / start-business remain online-only
- No second dialog stack
- No PHI / org POS data in Personal DB
- Full bidirectional server conflict UX for Personal is deferred
- Auto-sync polish (always clear Recovery without manual Retry) deferred

## 8. Git evidence

| Commit | Summary |
|---|---|
| `f3d87be` | Personal offline sync recovery, contact-id remap, email uniqueness, PIN settings UX |
| `4e09005` | This report + portfolio / phase hash record |
