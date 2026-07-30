# Android pilot package (P9-WP05)

## Build (Release)

```powershell
dotnet publish src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj `
  -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

Typical APK output (Signed):

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/publish/com.exits.pinoybusinesspos-Signed.apk`

Record version/build from project versioning and Git commit used for the package (`ExItS.Deployment.Cli package-version`).

## Environment

- Intended API base URL must be set for the pilot package (HTTPS for StagingPilot).
- Do not ship Development endpoint fallback for pilot/staging.
- Do not enable arbitrary cleartext Production traffic (`network_security_config` remains localhost/emulator-limited; Production HTTPS-only replacement remains open).
- No debug identity bypass in Release; no embedded secrets.

## Device validation

When no Android device/emulator is available: retain **R-109**. Do not claim interactive installation, upgrade, TalkBack, network, or workflow validation.

## Update / install notes

- Sideload only for internal technical pilot unless store publish is separately authorized.
- Uninstall removes local SQLite; unsynced offline operations may be lost (not in server backups).
- Disclose R-129 / NU1903 local encryption package risk to pilot users.
