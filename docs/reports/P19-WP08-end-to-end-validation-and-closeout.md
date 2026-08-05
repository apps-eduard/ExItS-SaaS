# P19-WP08 — End-to-End Validation and User Closeout Checklist

| Field | Value |
|---|---|
| Status | **Retest** (awaiting user phone confirmation) |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 2c63530 |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Provide the end-to-end Mobile POS ops validation checklist for physical-phone confirmation. Phase 19 stays **Open** until the user explicitly confirms phone validation.

## 2. Implementation status entering WP08

| WP | Status |
|---|---|
| P19-WP01 Inventory | Code Complete |
| P19-WP02 Registers | Code Complete (**Retest** — Open Shift registers) |
| P19-WP03 Shifts | Code Complete (**Retest** — Open Shift / Start Shift) |
| P19-WP04 Cashier selling | Code Complete |
| P19-WP05 Sales/receipt | Code Complete |
| P19-WP06 Customers | Code Complete |
| P19-WP07 Reports/nav/UX | Code Complete (**Retest** — Selling Mode / grants) |
| Card / GCash simulated payments | Code Complete (**Retest** — see [P19-card-gcash-payment-ui-and-simulation](P19-card-gcash-payment-ui-and-simulation.md)) |

## 3. User phone checklist (Retest)

- [ ] Owner/Manager/Cashier Quick Login → correct home (final retest carry-forward from Phase 18)
- [ ] Products / Categories still healthy (Phase 18 phone-validated baseline)
- [ ] Inventory list/low-stock/detail; adjust only with ManageInventory
- [ ] Registers list + Main Register; Cashier cannot administer without ManageRegisters
- [ ] **Owner Selling Mode (role stays Owner) → Open Shift loads eligible registers → Start Shift enabled → sell → close shift with variance**
- [ ] Cashier sell: search/category/tile → cart → cash tender/change → receipt → next sale
- [ ] Cashier sell-floor visual system: portrait sheet + landscape three-pane; category does not clear cart; 48dp steppers (**Retest** — [mobile-production-ui-redesign](mobile-production-ui-redesign.md))
- [ ] Card / GCash simulated checkout: awaiting payment → Dev simulate success/decline → receipt (**Retest** — [checklist](P19-card-gcash-payment-ui-and-simulation.md#9-phone-validation-checklist-retest--do-not-mark-device-verified))
- [ ] Manual GCash operator-confirm path still completes immediately (legacy; unverified)
- [ ] Sales history + receipt reopen
- [ ] Customers list for ViewCustomersAndHistory roles; credit create gated
- [ ] Reports hub shows only allowed report kinds for Cashier vs Owner/Manager
- [ ] MoreHub hides unauthorized modules

### Observed defect under retest (fixed in code; confirm on phone)

| Observation | Expected after fix |
|---|---|
| Owner enters Selling Mode; role stays Owner | Unchanged (UI mode only) |
| Open Shift cannot load registers / shows `No available register` | Eligible Active register(s) listed (e.g. Main Register) |
| Start Shift disabled | Enabled once a register is selected |

### Personal MVP Mobile (Phase 18 follow-up) — Retest on phone

See [P18-personal-mvp-mobile-ui-completion](P18-personal-mvp-mobile-ui-completion.md).

- [ ] Bottom tabs: Home / People / I Lent / I Borrowed / More with active highlight
- [ ] Home = Personal Utang summary + recent activity only (no PageHeader “Personal home”; no primary feature buttons)
- [ ] Top bar contextual identity: Personal = user name/avatar; Org/POS = organization name/logo or initials; no `Development` badge; no static `ExItS POS` in authenticated shells (**Retest**)
- [ ] More hosts My QR, Utang invitations, Profile, Settings, Explore POS, Sign out
- [ ] My QR / ExItS ID resolve flows (People, Customers, Sale picker, Staff invite) — see [P19-user-qr-public-id-linking](P19-user-qr-public-id-linking.md); **Retest**
- [ ] Explore Pinoy Business POS loads Platform catalog plans; Select starts explicit onboarding
- [ ] Organization is **not** created until business details are confirmed
- [ ] People / I Lent / I Borrowed empty → create → detail flows
- [ ] Utang invitations empty → `No pending Utang invitations`; failure → error + Retry
- [ ] Existing org member switches from Settings / Organization Select (not Home)
- [ ] Android back stays correct; app restart restores Personal when no org bound
- [ ] Direct POS route while Personal is denied / redirected
- [ ] Samsung layout: PersonalShell bottom tabs without clipping
- [ ] Global safe-area: status bar / cutout clear on Sign-in, Personal, Owner/Manager/Cashier, Products, Sales, Shifts, Reports, Settings (no double top gap; bottom nav still inset)

Phase 19 remains **Open** until the user confirms phone validation.

## 4. Explicit non-claims

- **Not Device Verified**
- **Not Complete** for Phase 19
- Production readiness **unchanged** / **not production-ready**
- Do **not** start P14-WP03 under this phase

## 5. PhysicalDevice / Tailscale (preferred)

Physical-device Local Validation is preferred over the Android emulator for Phase 19 retest.

| Item | Value |
|---|---|
| Package | `com.exits.pinoybusinesspos` |
| Profile | `-p:PosLocalValidationTarget=PhysicalDevice` |
| Default Tailscale host | `100.120.79.81` |
| APIs | Platform `:8091`, POS `:8092` |

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:PATH"

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

Do **not** use historical HealthCare AVD names. Untracked `tools/p18-*.mjs` remain local.

Restart Local Validation APIs after this fix so POS bearer commercial merge is live:

```powershell
.\tools\Start-LocalValidation.ps1
```

## 6. Status

**Retest.** Phase 19 remains **Open** until explicit user phone confirmation of the Owner Selling Mode → Open Shift registers scenario.
