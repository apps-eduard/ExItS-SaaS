# P9-WP05 — Pilot and Deployment

Phase marker: `P9-WP05-pilot-and-deployment`

## Status

**Complete with documented risks.** Delivered repeatable non-production deployment packaging, environment/config validation, backup-before-migrate gates, migration order, smoke contracts, pilot criteria/runbooks, Android Release packaging instructions, rehearsal automation, and an honest release-readiness decision. **No new business features.** HealthCare remains frozen. **Not Production-ready.**

Final readiness state: **Ready for controlled internal technical pilot** (StagingPilot). **Blocked** for restricted external pilot and Production while R-091, R-109, R-129, Production TLS, POS operational roles, Manual GCash verification, online-only limits, and PITR deferral remain open.

Feature commit: 9c1bbd0557e252758a772b985c907233da3f5214

Docs commit: 58b3c7413d5ccb29f0e0ee62007671217f7ff7f5

## Delivered

| Area | Delivered |
|---|---|
| Library | `ExItS.Deployment` — env validation, secret redaction, backup gate, migration order, readiness evaluator, rollback advisor, smoke catalog, package versioning |
| CLI | `tools/ExItS.Deployment.Cli` |
| Packaging | Dockerfiles (Platform API, POS API, Admin), `docker-compose.pilot.yml` (NON-PRODUCTION), nginx pilot conf |
| Ops | `ops/deploy/*` scripts + env templates + secret checklist |
| Smoke | Health-only (pilot-safe); Full identity-header probes only in Development/Testing |
| Runbooks | `docs/operations/pilot-and-deployment/` |
| Tests | `ExItS.Deployment.Tests` + architecture guards |
| Android | Release packaging documented; R-109 retained without interactive device claim |

## Explicit exclusions

- Production authentication (R-091 open)
- Closing R-109 / R-129 without evidence
- Production TLS completion claim without real cert + tested endpoint
- New POS business workflows; tax/VAT/refunds/accounting/purchasing/payroll
- Payment gateway / verified GCash; report export; new offline business ops; PITR
- HealthCare deployment; Kubernetes introduction; P9-WP06

## Deployment architecture

See [deployment-architecture.md](../operations/pilot-and-deployment/deployment-architecture.md). Separate Platform/POS databases and APIs; reverse-proxy TLS termination for pilot; backup storage and health probes documented.

## Environment matrix

See [environment-matrix.md](../operations/pilot-and-deployment/environment-matrix.md). Development/Testing headers remain unavailable on StagingPilot/Production.

## Configuration and secrets

Templates under `ops/deploy/templates/` (placeholders only). Production rejects Development password marker and `AllowedHosts=*`. Operator checklist: `SECRET-PROVISIONING-CHECKLIST.md`.

## TLS / network

Pilot proxy HTTPS template provided. Production TLS **not** claimed complete. Android cleartext remains limited to localhost/emulator; arbitrary Production cleartext rejected by validators.

## Migration and backup-before-deploy

Order: verify env → backup Platform+POS → verify manifests/SHA-256 → Platform migrate → validate → POS migrate → validate → start → health → smoke → evidence. Failures stop deployment. Database restore is **not** ordinary application rollback.

## Pilot

Internal technical / staging pilot only. Entry/exit criteria: [pilot-criteria.md](../operations/pilot-and-deployment/pilot-criteria.md). No public Production with Dev/Testing identity headers.

## Android package status

Release APK build executed during WP validation. Interactive install/TalkBack/network/workflow validation **not** claimed (R-109 open). Local SQLite / unsynced-data limitations disclosed. Not published to a public app store.

## Monitoring / support

Health/readiness, backup age/set IDs, migration/deploy version ownership documented in runbooks. No secrets/tokens/connection strings/customer remarks/full financial payloads in logs.

## Rehearsal evidence

Non-production:

1. Solution Release restore/build/test (987 / 0 / 0)
2. Android Release APK publish (Signed APK produced; no interactive install — R-109)
3. `Invoke-ExItsDeploy.ps1 -Environment StagingPilot -Action Plan -ConfirmPhrase DEPLOY_PILOT_CONFIRMED` (migration order + smoke catalog)
4. Dirty-tree refusal proven for StagingPilot Rehearsal before commit
5. Unit/CLI gates: Production config remains invalid while blockers open; BackupGate blocks when unverified
6. HealthCare untouched

Disposable DB migrate+backup+live health smoke remains operator-run against provisioned pilot/Testing hosts using `ops/backup` + `WaitHealth`/`SmokeHealth` after secrets and TLS material are provisioned. No Production deployment performed.

## Release readiness assessment

| Area | Assessment |
|---|---|
| Authentication | Open (R-091) |
| Authorization / roles | Org isolation preserved; POS operational roles still open |
| TLS | Pilot template only; Production open |
| Android validation | Build only; R-109 open |
| Local encryption | R-129 / NU1903 open |
| Backups / restore | Tooling present (P9-WP03); required pre-deploy |
| Monitoring / support | Runbooks + health endpoints |
| Performance | Prior P9-WP02 evidence; not full Production SLA |
| A11y / localization | Prior P9-WP04; interactive TalkBack open via R-109 |
| Business limits | Online-only catalog/sales/etc.; Manual GCash unverified; PITR deferred |

**Decision: Ready for controlled internal technical pilot. Not Ready for Production. Not Ready for restricted external pilot while R-091 is open.**

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet restore/build ExItS.slnx -c Release` | Succeeded (known NU1903 / NU1510 warnings retained as R-129) |
| `dotnet test ExItS.slnx -c Release` | **987 passed / 0 failed / 0 skipped** (baseline 950) |
| Android Release APK | `com.exits.pinoybusinesspos-Signed.apk` published under `artifacts/android-pilot/` (gitignored); R-109 interactive device validation not claimed |
| Deploy Plan | `Invoke-ExItsDeploy.ps1 -Action Plan` succeeded (migration order + smoke catalog + phase marker) |
| Dirty-tree gate | StagingPilot Rehearsal correctly refused unclean working tree |
| Production readiness | Evaluator + Production validate-config keep R-091/R-109/R-129 blockers open (unit-tested) |
| Backup gate | Unverified backups block migrate (unit-tested + CLI) |

## HealthCare freeze

`git ls-files -- HealthCare/` empty; `git check-ignore -v HealthCare/` shows ignored; Deployment projects in `ExItS.slnx`; HealthCare absent from solution and compose.

## Risks / open decisions

R-091, R-109, R-129, Production TLS/MAUI HTTPS-only, POS operational roles, Manual GCash unverified, online-only limitations, PITR deferred — **remain open**.

## Exact next work package

**P9-WP06 — Commercial MVP Closeout** (do **not** begin until explicitly authorized).
