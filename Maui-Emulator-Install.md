# MAUI POS — Android emulator build + install

**Local Validation / Debug only. Not Production.**  
Default emulator profile talks to host APIs via `http://10.0.2.2:8091` (Platform) and `http://10.0.2.2:8092` (POS).

Start Local Validation first: [Start-LocalValidation.md](Start-LocalValidation.md).

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
  -p:PosLocalValidationTarget=Emulator `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

`adb devices` should list an emulator (`emulator-5554` or similar) as `device` before Install.

## Notes

- **Emulator** profile = `10.0.2.2` → PC loopback. Do **not** use this APK on a physical phone.
- For a **physical phone** + Tailscale PublicHost, see [Maui-PhysicalDevice-Install.md](Maui-PhysicalDevice-Install.md).
- Shared Local Validation password is in `deploy/docker/.env.local-validation` (never commit).
- If sign-in says server unreachable: confirm Local Validation is running and health returns 200 on `http://127.0.0.1:8091/health`.

## Related

- [Start-LocalValidation.md](Start-LocalValidation.md)
- [Reset-LocalValidation.md](Reset-LocalValidation.md)
