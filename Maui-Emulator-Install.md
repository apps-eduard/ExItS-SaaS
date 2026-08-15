# MAUI POS — Android emulator build + install

**Local Validation / Debug only. Not Production.**  
Default Debug profile (emulator **and** physical) uses Tailscale/LAN PublicHost:

- Platform: `http://100.120.79.81:8091`
- POS: `http://100.120.79.81:8092`

Start Local Validation with the same host: [Start-LocalValidation.md](Start-LocalValidation.md).

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

## 1. Android SDK on PATH (this PowerShell session)

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:PATH"
```

## 2. Start emulator

Use your AVD name (example below matches a common local AVD):

```powershell
emulator -avd ExItS_Pixel_API34
```

Wait until the emulator UI is up.

## 3. Confirm device + install Debug APK

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS

$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:PATH"

adb devices

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

`PosLocalValidationTarget=PhysicalDevice` is now the **default** when omitted. Explicit flag is fine for clarity.

`adb devices` should list an emulator (`emulator-5554` or similar) as `device` before Install.

## Notes

- Emulator and physical phone use the **same** Tailscale PublicHost URLs by default.
- Host PC Tailscale must be up; Local Validation must be started with `-PublicHost 100.120.79.81`.
- Legacy host-loopback (`10.0.2.2`) only if you pass `-p:PosLocalValidationTarget=Emulator` **and** change URLs back — not the default Owner path.
- Shared Local Validation password is in `deploy/docker/.env.local-validation` (never commit).
- If sign-in says server unreachable: from the emulator/browser open `http://100.120.79.81:8091/health` and confirm Local Validation health is 200.

## Related

- [Maui-PhysicalDevice-Install.md](Maui-PhysicalDevice-Install.md)
- [Start-LocalValidation.md](Start-LocalValidation.md)
- [Reset-LocalValidation.md](Reset-LocalValidation.md)
