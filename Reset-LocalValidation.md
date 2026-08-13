# Reset Local Validation (2 Platform users only)

**Local Validation only. Not Production.**  
Wipes Local Validation Docker DB volumes, then reseeds **Olivia + Rafael** (Platform Administrator).

This **one command** also clears:

- All other users / orgs / memberships / invitations
- POS product database (merchant products, sales, inventory, etc.)
- Business templates and related catalog test rows (volume wipe)

Platform SaaS catalog / plans / features / built-in roles are recreated by migrate + seed after the wipe.

## Command

From the repository root:

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Reset-LocalValidation.ps1 -ConfirmReset
```

`-ConfirmReset` is required. Without it, the script refuses to run.

## What it does

1. Stops Local Validation apps + DB containers  
2. Removes **only** these volumes:
   - `exits_local_validation_platform_db_data`
   - `exits_local_validation_pos_db_data`
3. Clears Local Validation DataProtection keys under `%LOCALAPPDATA%\ExItS\LocalValidation\DataProtectionKeys`
4. Starts Local Validation with:
   - `-SeedScope PlatformAdministratorsOnly`
   - `-PurgeTransactional`
5. Verifies seed identities = exactly **2**:
   - `olivia.mendoza@exits.local`
   - `rafael.torres@exits.local`

## After reset

Sign in with Olivia or Rafael and the shared password from `deploy/docker/.env.local-validation` (`LOCAL_VALIDATION_SHARED_PASSWORD`). Never commit that secret.

Quick login on `http://127.0.0.1:8090/admin/login` is **database-backed (MODEL A)**:

- **Canonical baseline (always after this reset):** Olivia Mendoza, Rafael Torres — labeled `Baseline ·`.
- **Owner-created accounts** (for example Mica Uy) appear only after you create them, and only in scopes they actually own.
- **Full catalog demo users** (Maria, Carlo, Ana, Daniel, Luis, Sofia) are **not** part of this baseline. They are recreated only if you later start with `-SeedScope Full`.
- Ordinary Start (`PlatformAdministratorsOnly`) **decommissions** those Full-catalog fixture accounts if they were left over from an older Full seed. It does **not** delete owner-created users.
- The fixture **catalog in code** is never deleted by reset. Reset removes **database rows**, then reseeds Olivia + Rafael.

If you need the eight-identity demo catalog again:

```powershell
.\tools\Start-LocalValidation.ps1 -SeedScope Full
```

That is an explicit opt-in. Default Start does not do this.

If Admin antiforgery cookies fail after reset: Incognito window or clear localhost site data once.

## Related

- [Start-LocalValidation.md](Start-LocalValidation.md) — start / stop apps (Tailscale PublicHost)
- [Maui-Emulator-Install.md](Maui-Emulator-Install.md) — emulator build + install
- [Maui-PhysicalDevice-Install.md](Maui-PhysicalDevice-Install.md) — physical phone + Tailscale
- Deeper workflow: [deploy/docker/README.local-validation-workflow.md](deploy/docker/README.local-validation-workflow.md)
