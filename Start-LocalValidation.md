# Start / stop Local Validation apps

**Local Validation only. Not Production.**  
Docker DBs + host Platform API, POS API, Platform Admin, Organization Web, and Personal Web (`dotnet watch`).

## Start (Tailscale / LAN PublicHost)

From the repository root:

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

Replace `100.120.79.81` with your current Tailscale or LAN host if it changed.

### Printed URLs (example)

- Admin: `http://100.120.79.81:8090` (canonical sign-in)
- Platform API: `http://100.120.79.81:8091`
- POS API: `http://100.120.79.81:8092`
- Org Web: `http://100.120.79.81:8093`
- Personal Web: `http://100.120.79.81:8094`

Kestrel binds `0.0.0.0:8090|8091|8092|8093|8094` (localhost still works). DB ports stay `127.0.0.1:15533` / `15534`. These local ports are **not** public production ports; production uses HTTPS :443 via reverse proxy.

### If you omit `-PublicHost`

Start resolves, in order:

1. `LOCAL_VALIDATION_PUBLIC_HOST` in `deploy/docker/.env.local-validation`
2. Last saved PublicHost from launcher state
3. An active Tailscale `100.x` address when present

## Default seed scope

Ordinary Start uses **`PlatformAdministratorsOnly`** (Olivia + Rafael). It does **not** wipe existing orgs/products.

For a full clean wipe (2 admins + empty POS + cleared templates):

→ [Reset-LocalValidation.md](Reset-LocalValidation.md)

## Stop

Apps only (DBs keep running):

```powershell
.\tools\Stop-LocalValidation.ps1
```

Apps + DB containers (volumes preserved):

```powershell
.\tools\Stop-LocalValidation.ps1 -StopDatabases
```

## Windows Firewall (physical device / Tailscale)

Allow inbound TCP **8090 / 8091 / 8092 / 8093 / 8094**. Do **not** open **15533 / 15534**.

```powershell
New-NetFirewallRule -DisplayName "ExItS Local Validation Admin 8090" -Direction Inbound -Protocol TCP -LocalPort 8090 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Platform API 8091" -Direction Inbound -Protocol TCP -LocalPort 8091 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation POS API 8092" -Direction Inbound -Protocol TCP -LocalPort 8092 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Org Web 8093" -Direction Inbound -Protocol TCP -LocalPort 8093 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Personal Web 8094" -Direction Inbound -Protocol TCP -LocalPort 8094 -Action Allow -Profile Any
```

## Related

- [Maui-Emulator-Install.md](Maui-Emulator-Install.md)
- [Maui-PhysicalDevice-Install.md](Maui-PhysicalDevice-Install.md)
- [deploy/docker/README.local-validation-workflow.md](deploy/docker/README.local-validation-workflow.md)
