# Personal Mobile MVP — Personal-First Home + Utang Parity

| Field | Value |
|---|---|
| Status | **Code Complete** (awaiting phone Retest) |
| Phase | Phase 18 follow-up + Phase 19 Open |
| Commit | `6a79550` |
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
| Settings | Appearance + prefs GET | **Working** (appearance/logout; org/product switcher) |
| Explore Pinoy Business POS (catalog plans) | Plan cards on Start Business | **Working** (`/personal/explore-pos`) |
| Explicit business onboarding (confirm before create) | Working | **Working** (`/start-business?planKey=…`) |
| Organizations list on Personal home | N/A | **Removed** (personal-first) |
| Account context on Personal home | N/A | **Removed** (available in Settings / Org select) |
| Payments / Reminders / History nav | Coming soon | **Deferred** (disabled) |

## 2. Implemented / deferred

**Implemented:** Personal-first home (People / I Lent / I Borrowed / Utang invitations / Profile / Settings), Explore POS from Platform commercial catalog, plan selection → explicit Start Business confirmation (no silent org create), Utang empty success `No pending Utang invitations` + API error Retry, org/product switching via Settings and Organization Select, POS denial while Personal.

**Deferred:** Payments/Reminders/History dedicated nav, invite creation from relationship detail UI, push delivery.

## 3. Authorization

- `EnsurePersonalAccountProfileAsync` binds Personal account class before Utang calls
- No organization required for Personal Utang
- Commercial plans loaded from `GET /api/v1/commercial/plans?productCode=pinoy-business-pos` (no hard-coded plan list)
- Organization created only after user confirms business details on `/start-business`
- POS shell still requires org + entitlement + product-local permission
- No hard-coded grants / Release Development bypass added

## 4. Tests

`PersonalPageGuardTests` — personal-first home (no orgs/switcher/Start Business CTA), Explore POS catalog + deferred create, switcher outside home, Utang invitation empty copy, session restore gate, direct POS denial.

## 5. Phone retest checklist (Phase 19 Open)

- [ ] New Personal user: no Organizations list / Account context / Start a Business on home
- [ ] Personal features primary: People, I Lent, I Borrowed, Utang invitations, Profile, Settings
- [ ] Explore Pinoy Business POS loads catalog plans (name, price/period, features, trial/status, Select)
- [ ] Plan selection opens business details; org created only after Confirm
- [ ] Existing org member can switch via Settings / Organization Select (not Personal home)
- [ ] Utang invitations empty → `No pending Utang invitations`; API failure → error + Retry
- [ ] App restart restores Personal
- [ ] Direct `/sales` while Personal redirects away
- [ ] Samsung layout usable
