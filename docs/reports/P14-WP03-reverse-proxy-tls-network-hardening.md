# P14-WP03 — Reverse Proxy, TLS, and Network Hardening

[Phase 14](../phases/phase-14-production-deployment-and-operations.md) | [Portfolio](../portfolio-progress.md) | [Production architecture](../engineering/production-deployment-architecture.md)

## Status

**Complete as topology/template baseline.** Starting tip after preflight `b5ff8c2830286d098145ad77e03dbde8802d2189`. Feature tip `a015d0afad0ab20c4a2a9f019615970c82b3f3d6` (forwarded headers `2994ed2b5602bec30a4553252df30aa2caf81f79`). **Not Production-ready.** P14-WP04+ not started.

## Decision

**nginx** remains the approved reverse-proxy technology (pilot already used nginx). Production uses a **separate** Compose project and `nginx/production.conf` — pilot conf stays NON-PRODUCTION.

## Topology

```text
Internet
  → reverse-proxy :80  → 301 HTTPS
  → reverse-proxy :443
        ├── /admin/*      → platform-admin:8080  (Blazor WS on /_blazor)
        ├── /platform/*   → platform-api:8080
        └── /pos/*        → pos-api:8080
Internal network only:
  platform-db, pos-db, platform-api, pos-api, platform-admin
```

## Public / internal ports

| Publish | Service |
|---|---|
| 80, 443 (configurable) | reverse-proxy only |
| none by default | APIs, Admin, PostgreSQL |

Live Preview unchanged: Admin **8090**, Platform API **8091**, POS API **8092**, DBs **15533/15534**.

## Proxy / TLS behavior

- HTTP → HTTPS redirect at nginx
- TLS 1.2/1.3; certs from `PRODUCTION_TLS_CERT_DIR` (`fullchain.pem`, `privkey.pem`)
- HSTS + security headers on HTTPS listener
- Body limit 1m; documented proxy timeouts; Blazor circuit long timeouts + WebSocket upgrade
- No automatic ACME/public issuance claimed
- No real certificates or private keys committed

## Forwarded headers / CORS

- Apps enable `ForwardedHeaders` only when configured (`Enabled=true`)
- KnownNetworks / KnownProxies cleared then loaded from config — untrusted spoofed headers ignored
- Production CORS remains explicit `Cors:AllowedOrigins` (no wildcard + credentials)
- Secure cookies (`CookieSecurePolicy.Always` outside Dev/Testing/Live Preview) rely on forwarded HTTPS scheme

## Network hardening

- `exits-production-internal` marked `internal: true`
- Edge network attaches only reverse-proxy
- App containers `read_only` + tmpfs where practical
- `restart: unless-stopped`; DB healthchecks; no startup `Migrate()`
- Firewall: allow 80/443 to host; deny direct DB/API ports

## MAUI / client HTTPS

- ApiClient rejects clear-text BaseUrls when Production / `Security:RequireHttpsApiUrls`
- Emulator HTTP URLs remain for Development
- **MAUI-HTTPS** residual: interactive device/emulator certificate trust validation against customer CA/public TLS is **not** evidenced in this WP

## Validation evidence

- Architecture tests: production Compose port isolation, nginx routes/TLS/headers, Live Preview port regression, env template without secrets
- Integration tests: ForwardedHeaders KnownNetworks application / CIDR parse
- `docker compose … config` operator validation documented
- Full Release suite: **1306 passed / 0 failed / 0 skipped** (`ASPNETCORE_ENVIRONMENT=Testing`, `dotnet test ExItS.slnx -c Release`). Baseline was 1301.

## Residual blockers (explicit)

TLS-PROD end-to-end operator cutover evidence; MAUI-HTTPS device validation; R-109; R-129 / NU1903; auth email vendor; MFA deferred; D-P12-03; D-P12-04; EVAL-DRIFT; backup/ops (P14-WP04); monitoring (P14-WP05).

## Production-readiness conclusion

**Not Production-ready.** P14-WP03 delivers the reverse-proxy/TLS/network **baseline templates and app trust configuration**. External certificate issuance, customer cutover, and remaining Phase 14 WPs remain open.
