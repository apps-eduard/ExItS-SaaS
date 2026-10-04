# Cloudflare Tunnel — Local Validation preview

**LOCAL VALIDATION. NOT PRODUCTION.**

This is an optional way to open the existing FULL Docker Local Validation stack on `exitsapps.com` so a tester on another network can reach it. It is not the daily workflow. It does not deploy Production.

Daily Local Validation stays:

```powershell
.\tools\Start-DockerLocalValidation.ps1
```

Use this preview only when an outside device must reach that same stack.

## What it is

```text
Internet
   |
   v
Cloudflare + Cloudflare Access
   |
   v
Cloudflare Tunnel
   |
   v
cloudflared  (Docker network exits-local-validation)
   |
   +-- react-pos:80          React Organization, Personal, and POS client
   +-- admin-web:8080        Platform Admin (React)
   |
   private, not published:
   platform-db, pos-db, Mailpit, Docker API
```

No router port forwarding. No public PostgreSQL. No public Mailpit. No public Docker daemon. The host IP is not published.

`cloudflared` is opt-in. `deploy/docker/compose.local-validation.yaml` does not start it. The overlay is `deploy/docker/compose.cloudflare-preview.yaml`.

## Public preview hosts

Configure these in the Cloudflare Zero Trust tunnel as **Public Hostnames**. The tunnel token mode keeps ingress in the Cloudflare dashboard. Targets are Docker DNS names on `exits-local-validation`, not `localhost`.

| Public hostname | Origin service |
| --- | --- |
| `app.exitsapps.com` | `http://react-pos:80` |
| `my.exitsapps.com` | `http://react-pos:80` |
| `pos.exitsapps.com` | `http://react-pos:80` |
| `admin.exitsapps.com` | `http://admin-web:8080` |

`admin-web` is the canonical React Platform Admin. The preview publishes that service.

Do not add public hostnames for:

- `bnpl.exitsapps.com`
- `loan.exitsapps.com`
- `pawn.exitsapps.com`
- `service.exitsapps.com`
- `api.exitsapps.com`
- `platform-db` / `pos-db` / host ports `15533` and `15534`
- Mailpit UI `8025` or SMTP `1025`

Those product hostnames stay planned until a real runtime exists. The React client already proxies `/platform-api` and `/pos-api` on the same origin, so the raw API containers are not published. Personal Web calls Platform API on the Docker network. `app.exitsapps.com` and `pos.exitsapps.com` share the `react-pos` container.

## Cloudflare Access is required

Local Validation has test identities, relaxed local settings, and non-production data. Do not leave these hostnames open to the Internet.

Before anyone outside uses them, create a Cloudflare Access application for each preview hostname:

- Allow only named ExItS developer and tester identities.
- Deny everyone else.

Protect at least:

- `https://admin.exitsapps.com`
- `https://app.exitsapps.com`
- `https://my.exitsapps.com`
- `https://pos.exitsapps.com`

Cloudflare Access is an extra outer gate. Application login, cookies, CSRF, and authorization stay in place.

## Operator setup

1. In Cloudflare Zero Trust, create or select a named tunnel for local ExItS preview.
2. Copy the tunnel token into the local uncommitted file `deploy/docker/.env.local-validation`:

   ```text
   LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN=...
   ```

   Never commit that file. Never paste the token into chat, tickets, or logs.
3. Create the four Public Hostnames in the table above.
4. Configure Cloudflare Access **before** sharing the URLs.
5. From the repository root, with Docker Desktop running. The first time after this preview was added, rebuild so Personal Web includes forwarded-header support:

   ```powershell
   .\tools\Start-DockerLocalValidation.ps1 -Build
   .\tools\Start-CloudflareLocalPreview.ps1
   ```

   Later starts only need:

   ```powershell
   .\tools\Start-CloudflareLocalPreview.ps1
   ```

   The script checks Docker, the env file, and that a token value exists (it does not print the token). If the full stack is down, it runs `Start-DockerLocalValidation.ps1`. It then starts only cloudflared, reads that container's address on `exits-local-validation`, and recreates Personal Web so its public links trust that one address. React Admin and the React Organization client do not use ASP.NET forwarded headers. It does not trust the Docker subnet, `0.0.0.0/0`, or any other container.
6. Test from another network, through Cloudflare Access.
7. Stop the tunnel only:

   ```powershell
   .\tools\Stop-CloudflareLocalPreview.ps1
   ```

   That removes `exits-local-validation-cloudflared` only. Application containers and database volumes stay. It does not run `compose down` and it does not delete volumes.

After stopping, the three web containers still advertise the public `https://*.exitsapps.com` links until you start the normal stack again:

```powershell
.\tools\Start-DockerLocalValidation.ps1
```

That restores localhost cross-links. Preview mode is off when the overlay is not used.

## Auth through the tunnel

Cloudflare terminates public HTTPS. The Docker origin stays HTTP.

The preview overlay does not enable ASP.NET forwarded headers. React nginx is not configured as a Blazor host. Cookie `Secure` stays on the application session policy. Production still requires `Secure`. `SameSite` stays `Lax`. Antiforgery and application authentication are unchanged. Cloudflare Access does not replace application login.

`AllowedHosts` in preview is the existing internal list plus `app.exitsapps.com`, `my.exitsapps.com`, `admin.exitsapps.com`, and `pos.exitsapps.com`. It is not `*`. CORS is not opened to any origin.

## Manual smoke (not executed by this document)

Do not treat this checklist as a pass unless you actually ran it.

Local, before the tunnel:

- React Organization client `http://127.0.0.1:5177`
- React Personal, Organization, and POS `http://127.0.0.1:5177`
- Platform Admin `http://127.0.0.1:8095`

Then from a non-home connection, after Access is on:

- `https://app.exitsapps.com`
- `https://my.exitsapps.com`
- `https://pos.exitsapps.com`
- `https://admin.exitsapps.com`

Confirm:

- An unauthorized visitor is blocked by Cloudflare Access.
- An authorized tester can pass Access.
- Each application still requires its own login.
- Login and session work.
- Cross-surface links stay on `https://admin.exitsapps.com`, `https://app.exitsapps.com`, and `https://my.exitsapps.com` rather than `localhost:809x`.
- The APIs each UI already uses still respond.
- PostgreSQL and Mailpit are not reachable from the Internet.

## Related

- [README.local-validation.md](README.local-validation.md)
- [README.local-validation-workflow.md](README.local-validation-workflow.md)
- [Domain and subdomain strategy](../../docs/architecture/domain-and-subdomain-strategy.md)
