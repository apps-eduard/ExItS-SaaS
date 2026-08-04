# Personal Mobile MVP — Utang Parity Completion

| Field | Value |
|---|---|
| Status | **Code Complete** (awaiting phone Retest) |
| Phase | Phase 18 follow-up + Phase 19 Open |
| Commit | *(tip after push)* |
| Date | 2026-08-04 |
| Device Verified | **No** |
| Phase 19 | Remains **Open** — phone scenarios **Retest** |
| PhysicalDevice APK | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Debug/net10.0-android/com.exits.pinoybusinesspos-Signed.apk` |

## 1. Web vs Mobile parity matrix

| Capability | Web Admin | MAUI |
|---|---|---|
| Dashboard totals (People / Active / Lent / Borrowed) | Working | **Working** |
| People/Contacts list | Working (list) | **Working** (list + create) |
| People detail | Limited | **Working** |
| I Lent list | Working | **Working** |
| I Lent create | API only on Web | **Working** |
| I Borrowed list | Working | **Working** |
| I Borrowed create | API only on Web | **Working** |
| Relationship view + entries/history | API only on Web | **Working** |
| Utang invitations list | Working | **Working** |
| Utang accept/decline by token | API | **Working** |
| Profile | Working | **Working** (API + session fallback) |
| Settings | Appearance + prefs GET | **Working** (appearance/logout; Platform prefs GET available) |
| Start a Business | Working | **Working** |
| Organization invitations | N/A (separate) | **Working** |
| Payments / Reminders / History nav | Coming soon | **Deferred** (disabled) |

## 2. Implemented / deferred

**Implemented:** Personal dashboard, People, I Lent, I Borrowed, relationship detail with record entry + history, Utang invitations accept/decline, profile, settings, Start Business, Personal account profile binding for `/api/v1/personal/*`, empty states for new users, POS denial while Personal.

**Deferred:** Payments/Reminders/History dedicated nav (backend exists for entries/history/reminders; UI remains coming-soon per MVP), invite creation from relationship detail UI, push delivery.

## 3. Authorization

- `EnsurePersonalAccountProfileAsync` binds Personal account class before Utang calls
- No organization required for Personal Utang
- POS shell still requires org + entitlement + product-local permission
- No hard-coded grants / Release Development bypass added

## 4. Tests

`PersonalPageGuardTests` — empty states, create/view routes, API client paths, session restore gate, direct POS denial.

## 5. Phone retest checklist (Phase 19 Open)

- [ ] New Personal user: dashboard zeros + empty People/Lent/Borrowed/Invitations (not errors)
- [ ] Create person → create I Lent / I Borrowed → view relationship → record payment
- [ ] Utang invitation accept/decline by token
- [ ] Profile + Settings + Start a Business
- [ ] App restart restores Personal
- [ ] Direct `/sales` while Personal redirects away
- [ ] Samsung layout usable
