# Personal Mobile MVP — Personal-First Bottom Tabs + Utang Parity

| Field | Value |
|---|---|
| Status | **Code Complete** (awaiting phone Retest) |
| Phase | Phase 18 follow-up + Phase 19 Open |
| Commit | `b6585eb` |
| Date | 2026-08-04 |
| Device Verified | **No** |
| Phase 19 | Remains **Open** — phone scenarios **Retest** |
| PhysicalDevice APK | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Debug/net10.0-android/com.exits.pinoybusinesspos-Signed.apk` |

## 1. Web vs Mobile parity matrix

| Capability | Web Admin | MAUI |
|---|---|---|
| Dashboard totals (People / Active / Lent / Borrowed) | Working | **Working** (Home tab) |
| Recent activity | Limited | **Working** (Home tab) |
| People/Contacts list | Working (list) | **Working** (People tab) |
| People detail | Limited | **Working** |
| I Lent list / create | Working / API | **Working** (I Lent tab) |
| I Borrowed list / create | Working / API | **Working** (I Borrowed tab) |
| Relationship view + entries/history | API only on Web | **Working** |
| Utang invitations | Working | **Working** (More) |
| Profile / Settings | Working | **Working** (More) |
| Explore Pinoy Business POS | Plan cards | **Working** (More → catalog) |
| Explicit business onboarding | Working | **Working** (`/start-business?planKey=…`, AuthShell) |
| Bottom tabs (Home / People / I Lent / I Borrowed / More) | N/A | **Working** (`PersonalShell`) |
| Organizations / Account context on Home | N/A | **Absent** (Settings / Org select) |
| Payments / Reminders / History | Coming soon | **Hidden** until implemented |

## 2. Implemented / deferred

**Implemented:** `PersonalShell` bottom tabs with active highlight and tab `replace` navigation (Android back stays sane), Home summary + recent activity only, People / I Lent / I Borrowed tabs, More hub (invitations, profile, settings, Explore POS, sign out), Explore POS catalog → explicit Start Business confirmation, POS isolation preserved.

**Deferred:** Payments/Reminders/History primary UI, invite creation from relationship detail UI, push delivery.

## 3. Authorization

- `EnsurePersonalAccountProfileAsync` binds Personal account class before Utang calls
- No organization required for Personal Utang
- Commercial plans from Platform catalog (no hard-coded plan list)
- Organization created only after confirm on `/start-business`
- POS shell still requires org + entitlement + product-local permission

## 4. Tests

`PersonalPageGuardTests` — PersonalShell tabs (no POS chrome), Home summary-only, More hub secondary actions, Explore deferred create, switcher outside home, Utang empty copy, AuthShell for onboarding, direct POS denial.

## 5. Phone retest checklist (Phase 19 Open)

- [ ] Bottom tabs persist on Personal routes with active-tab highlight
- [ ] Home shows totals + recent activity only (no large People/Lent/Borrowed/Invitations/Settings/Sign out/coming-soon buttons)
- [ ] People / I Lent / I Borrowed tabs open list/create/detail flows
- [ ] More → invitations, profile, settings, Explore POS, sign out
- [ ] Explore POS plans → confirm business details before org create
- [ ] No Organizations / Account context on Home
- [ ] Android back behaves correctly across tabs and detail pages
- [ ] Direct `/sales` while Personal redirects away
- [ ] Samsung layout: PersonalShell bottom inset usable
