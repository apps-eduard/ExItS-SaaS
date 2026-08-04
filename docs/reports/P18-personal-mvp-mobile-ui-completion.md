# Personal Mobile MVP — Audit and Completion

| Field | Value |
|---|---|
| Status | **Code Complete** (awaiting phone Retest) |
| Phase | Phase 18 follow-up + Phase 19 Open |
| Date | 2026-07-29 |
| Device Verified | **No** |
| Phase 19 | Remains **Open** — phone scenarios **Retest** |

## 1. Audit matrix

| Feature / surface | Classification | Notes |
|---|---|---|
| `/personal` home | **working** | Profile summary, orgs, invitations, switcher, logout, empty/error/retry |
| `/personal/profile` | **working** | Read-only account summary |
| `/personal/settings` | **working** (new) | Theme/lang/density + logout under AuthShell |
| `/personal/invitations/accept` | **working** (new) | Token accept + deep-link `?token=` |
| Pending invitation list + accept-by-id | **working** (new) | `GET/POST …/auth/organization-invitations/*` |
| Invitee decline (org staff) | **deferred** | Domain supports admin revoke only; UI explains |
| `/start-business` | **working** | Create org + continue in Mobile |
| Org list + continue / launch POS | **working** | Bind/entitlement on select; essentials when not entitled |
| Personal ↔ Organization switch | **working** | `AccountContextSwitcher` + `SwitchToPersonalAsync` |
| Session restore → `/personal` | **working** | `NavigationGate` when `OrganizationId` null |
| POS pages while Personal | **working** (denied) | `CanEnterProtectedShell` requires org + POS |
| `/settings` (POS shell) while Personal | **working** | Redirects to `/personal/settings` |
| AuthShell phone layout | **working** | `.pos-shell--auth` no bottom-nav padding |
| Personal Utang Mobile | **deferred** | Platform/Admin + `/api/v1/personal/utang/*`; not MVP Mobile nav |
| Platform Admin Personal screens | **backend-only** / Web | Out of Mobile scope |

## 2. Implemented MVP scope

- Personal dashboard/home with clear empty/loading/error/retry
- Profile + Personal settings (appearance + logout)
- Organization list + Start a Business
- Pending invitations list; accept by id; accept by token page
- Personal ↔ Organization switching; Ensure Organization profile after invite
- Continue into org / POS when entitled (server authorization unchanged)
- No POS operational pages under Personal AuthShell
- Regression guards in `PersonalPageGuardTests`

## 3. Deferred

- Invitee decline for organization staff invitations
- Personal Utang Mobile UI
- Rich profile edit / security (password change) beyond existing Platform auth surfaces

## 4. Authorization preserved

- Personal identity is not an organization role
- Product access still requires selected organization + entitlement + product-local permission (bind/evaluate)
- No new Release Development bypasses

## 5. Phone Retest checklist (Phase 19 Open)

- [ ] New user, no org → Personal home, empty orgs, Start a Business
- [ ] Multi-org user → list + switch Personal ↔ orgs
- [ ] Pending invitation → accept from list; token page
- [ ] App restart restores Personal when no org bound
- [ ] Deep-link `/sales` (or other POS) while Personal → redirected away
- [ ] Samsung phone: Personal screens usable (no phantom bottom padding)
