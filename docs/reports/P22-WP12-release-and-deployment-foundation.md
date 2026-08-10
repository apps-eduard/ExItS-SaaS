# P22-WP12 — Release and deployment foundation

## Status
Implementation documentation complete. No environment has been deployed by this work package.

## Required configuration

- Platform uses `ConnectionStrings__PlatformDatabase`; POS uses `ConnectionStrings__PosDatabase`.
- Configure `AllowedHosts`, explicit `Cors__AllowedOrigins`, and HTTPS endpoints for production.
- Keep `LocalValidation__Enabled=false`, `PlatformAuthentication__Bootstrap__Enabled=false`, and
  `PlatformAuthentication__Lifecycle__ExposeDebugTokens=false` in production.
- Supply external-provider credentials only through an approved secret provider. Do not put them in
  appsettings files or Android bundles.
- POS needs an HTTPS `PlatformAuth__BaseUrl`; the mobile app needs HTTPS Platform and POS API URLs.

## Migration and release order

1. Take and verify a restorable backup of both independent PostgreSQL databases.
2. Deploy and apply Platform migrations first.
3. Deploy and apply PinoyBusinessPOS migrations second.
4. Start APIs without automatic runtime migration. Check `/health` and `/ready`.
5. Release the MAUI/Android application only after the API version and the registered-device flow
   have been validated in the target environment.

## Android notes

Build the signed release APK/AAB with release-only configuration and signing material injected by
the approved build system. Do not commit keystores, passwords, generated APKs, or device screenshots.
The installation identity is application-secure-storage generated; it is not an IMEI, serial number,
or advertising identifier.

## Rollback limits

Database migrations are forward changes and may require a deliberate, tested rollback migration or
restore from backup. Revoking a device blocks subsequent online money mutations but does not erase
already recorded transactions. An APK rollback must remain compatible with the deployed API and
database schema.

## External setup still required

TLS certificate/ingress configuration, production CORS origins, secret provider wiring, database
backup/restore evidence, Android signing/release pipeline, and physical-device validation remain
environment-owner tasks.
