# P24-WP24 — Owner/User Final Validation and Acceptance

| Field | Value |
|---|---|
| Status | **Awaiting Owner Validation** |
| Device Verified | **No** (until owner confirms) |
| Production Ready | **No** |
| Phase 24 Closed | **No** |

## Cursor must not fabricate Complete

This package is a hard user gate. Automated tests, builds, and screenshots alone do **not** close WP24 or Phase 24.

## Exact Android build/run

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

See also [P24-WP15](P24-WP15-physical-android-validation-preparation.md) checklist items 1–24.

## Acceptance checklist (owner)

### Core Personal linked-merchant / rewards (prior)

1. Launch · 2. Login · 3. Personal nav · 4. Linked merchants · 5. Outstanding · 6. Recent · 7. Open debt · 8–9. Receipts · 10–12. Older history lock/unlock via rewards · 13–16. Rewards/Ad-Free · 17. Ads eligibility · 18–24. Nav/errors/small screen/network/resume/logout/privacy

### ExItS-ID customer-link consent (required)

Organization:
1. Login as organization user
2. Customers → Add
3. Enter valid Personal ExItS ID
4. Confirm identity
5. Save customer
6. Verify pending customer-link status (customer exists ≠ Personal linked)

Personal:
7. Login as that Personal user
8. Notifications / Customer link requests shows merchant request
9. Open request
10. Decline path tested (or second request)
11. Accept request
12. Linked Merchants updates
13. Open merchant
14. Verify authorized balance/activity
15. Verify receipts/history behavior

Also validate:
- Wrong Personal account does not see request
- Merchant BusinessCustomer remains after Decline
- No staff/org membership created
- Organization inviter receives Accept/Decline response notification

After explicit owner acceptance only: mark WP24 Complete, Phase 24 Closed, Device Verified Yes (if physical), and update portfolio.
