# MAUI POS — physical device build + install (Tailscale)

**Local Validation / Debug only. Not Production.**  
PhysicalDevice profile uses Tailscale/LAN URLs (example: `http://100.120.79.81:8091` / `:8092`).

## Prerequisites

1. Start Local Validation with the same PublicHost:

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

2. Phone on the same Tailscale network as the PC.
3. From the phone browser, confirm: `http://100.120.79.81:8091/health` → OK.
4. USB debugging enabled; device authorized for adb.

## Android SDK on PATH

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:PATH"
```

## Build + install

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS

adb devices

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

If your Tailscale IP changed, also pass:

```powershell
  -p:PosLocalValidationPublicHost=100.x.y.z
```

(and update `wwwroot/appsettings.LocalValidation.PhysicalDevice.json` / cleartext domain if needed).

## Related

- [Maui-Emulator-Install.md](Maui-Emulator-Install.md) — emulator (`10.0.2.2`)
- [Start-LocalValidation.md](Start-LocalValidation.md)
- [Reset-LocalValidation.md](Reset-LocalValidation.md)
