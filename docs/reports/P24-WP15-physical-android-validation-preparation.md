# P24-WP15 — Physical Android Validation Preparation

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP14](P24-WP14-documentation-backend-closeout-preparation.md) | [Maui PhysicalDevice](../../Maui-PhysicalDevice-Install.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (preparation only — **not** Device Verified) |
| Date | 2026-08-12 |
| Starting SHA | `78d1e21b309a6853812d71e84f2e9aeead1f991a` on `main` |
| Implementation commit | **None** (preparation documentation) |
| Docs commit | `b1200f314c02c7573224899c6f1f516d5d9a32b9` |
| Docs/hash-stamp commit | `0e933704f200a26b1e7f3d42c39399ddd087f1ab` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status legend

WP15 prepares reproducible Android Local Validation prerequisites and a Phase-24 Personal validation checklist. Successful preparation does **not** equal Device Verified. Phase 24 remains Open; mobile UI implementation starts at WP16.

## Canonical WP15 scope

```text
WP15 | Physical Android validation preparation | Bridge into mobile stream; preparation ≠ Device Verified
```

## Environment prerequisites

| Item | Requirement |
|---|---|
| Host OS | Windows with Android SDK (`%LOCALAPPDATA%\Android\Sdk`) |
| Device | Physical Android phone preferred (see [Maui-PhysicalDevice-Install.md](../../Maui-PhysicalDevice-Install.md)) |
| Network | Phone + PC on same Tailscale/LAN; phone can open Platform `/health` |
| APIs | Local Validation Platform `:8091`, POS `:8092` |
| App host | `ExItS.PinoyBusinessPOS.Maui` (`net10.0-android`, package `com.exits.pinoybusinesspos`) |
| Profile | `PosLocalValidationTarget=PhysicalDevice` |

## Exact start commands

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Start-LocalValidation.ps1 -PublicHost <TAILSCALE_OR_LAN_IP>

$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:PATH"
adb devices

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -p:PosLocalValidationPublicHost=<TAILSCALE_OR_LAN_IP> `
  -t:Install
```

Emulator fallback (not preferred): [Maui-Emulator-Install.md](../../Maui-Emulator-Install.md) with `10.0.2.2:8091/8092`.

Automated mobile unit tests (non-device):

```powershell
dotnet test "tests/ExItS.PinoyBusinessPOS.Maui.Tests/ExItS.PinoyBusinessPOS.Maui.Tests.csproj" -c Release
```

## Phase-24 Personal validation checklist (for WP21 / WP24)

Use after WP16–WP20 land. Record **Pass / Fail / Blocked** per row. Do **not** mark Device Verified until owner confirms.

| # | Check |
|---|---|
| 1 | App launches on physical Android |
| 2 | Login / session restore |
| 3 | Personal home / shell navigation |
| 4 | Linked merchant / customer visibility |
| 5 | Current outstanding Business Utang |
| 6 | Recent activity pagination |
| 7 | Open-debt activity while outstanding > 0 |
| 8 | Receipt list / summary |
| 9 | Lazy receipt detail |
| 10 | Older settled history locked without entitlement |
| 11 | Redeem `personal-digital-records-extended` |
| 12 | Older history unlock after entitlement |
| 13 | Reward points balance |
| 14 | Reward activity |
| 15 | Redeem `personal-ad-free` |
| 16 | Ad-Free state reflected |
| 17 | Ad eligibility unavailable / blocked when Ad-Free |
| 18 | Back navigation / tab behavior |
| 19 | Loading / empty / error states |
| 20 | Small-screen readability / touch targets |
| 21 | Slow / no-network recoverable errors |
| 22 | Background / resume |
| 23 | Logout / login |
| 24 | Cross-account privacy sanity (no other user’s data) |

## Explicit non-claims

- This WP did **not** run or pass physical-device validation of Phase-24 Personal statement UX (UI not built until WP16+).
- **Device Verified: No**
- **Production Ready: No**
- Phase 24 **not** closed

## Prep evidence (this environment)

| Check | Result |
|---|---|
| Android SDK path `%LOCALAPPDATA%\Android\Sdk` | **Present** |
| `adb` on default PATH | **Not** on PATH without SDK platform-tools (documented setup required) |
| `ExItS.PinoyBusinessPOS.Maui.Tests` Release | **Passed 346**, failed 0, skipped 0 |
| Physical phone attached to Cursor | **Not assumed** |
| Phase-24 Personal mobile UI | **Not present** — WP16+ |

## Exact next WP

**P24-WP16 — Personal mobile linked-customer statement experience**

## Checks performed

- Starting HEAD = `origin/main` = `78d1e21b309a6853812d71e84f2e9aeead1f991a`
- Migration: None
- Preparation docs only; no Device Verified claim
