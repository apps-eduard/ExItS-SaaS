# Phase 22 — Production Readiness, Release & Operational Hardening

[Portfolio](../portfolio-progress.md) | [Phases](README.md) | [Phase 21](phase-21-privacy-compliance-and-regulatory-readiness.md) | [Baseline](../reports/P22-WP01-readiness-baseline.md)

| Field | Value |
|---|---|
| Status | **Open** |
| Overall | **In Progress** |
| Device Verified | **No** |
| Production Ready | **No — not claimed** |
| Physical validation | **Pending** |
| Disposable DB reset | **Authorized** (scrap/dev customer data) |

## Objective

Harden the production-readiness path for PinoyBusinessPOS onboarding and operations:

```text
Platform → Personal User → Organization → Primary Business Type
  → Subscription → Branch(es) → Registered POS Device(s)
  → Staff/Cashier → Transactions
```

Template remains **optional** onboarding assistance. Business Type is **not** a Template.
Branch / POS device / cashier capacity come from **subscription entitlement** (server-enforced).

## Work packages

| WP | Name | Target |
|---|---|---|
| P22-WP01 | Readiness baseline | Docs + dependency map |
| P22-WP02 | Organization Business Type + setup flow | Org.PrimaryBusinessTypeId; Start Business type picker |
| P22-WP03 | Organization Branches | Main Branch auto-create; CRUD; isolation |
| P22-WP04 | Plan capacity entitlements | MaxBranches, MaxActivePosDevices, MaxActiveStaff |
| P22-WP05 | Registered POS devices | Server registry bound to installation DeviceId |
| P22-WP06 | Owner device management + lost device | List/rename/revoke/replace |
| P22-WP07 | Cashier / transaction device authorization | Registered Active device required for money ops |
| P22-WP08 | Offline grant/PIN integration | Bind grant to registered device; server reject ≠ unreachable |
| P22-WP09 | Clean database baseline | Preserve 2 Platform admins + catalog/plans/types/templates |
| P22-WP10 | Security / production hardening | Config guards, secrets, health |
| P22-WP11 | Observability / operations | Correlation, safe diagnostics |
| P22-WP12 | Release / deployment foundation | Docs + Release builds |
| P22-WP13 | Full automated regression | Focused suites green |
| P22-WP14 | Physical device validation preparation | Path ready; not falsely Device Verified |
| P22-WP15 | Closeout | Phase + portfolio docs |

## Architecture reuse (do not reinvent)

| Concern | Reuse |
|---|---|
| Device identity | Existing `IDeviceIdentityProvider` / `pos.device.id` |
| Offline operate | Existing offline grant + PIN verifier |
| Plans / entitlements | Existing `Plan`, snapshots, feature codes |
| Business Types | Existing dynamic `BusinessType` catalog (P20) |
| Templates | Existing `CatalogTemplate` + merchant import |
| Auth / session | Existing Platform session + POS `AuthSession` |
| Registers | Logical sales stations — **not** branches or devices |

## Explicit exclusions

- No IMEI / serial / advertising ID collection
- No second PIN/device-identity system
- No backward-compat shims for scrap org/customer data
- No claim of production deployment or Device Verified without evidence
- No fabricated third-party credentials

## Privacy impact (standing)

Device registry stores minimized operational metadata (installation id, friendly name, platform/model when safely available, app version, branch, status, timestamps). Purpose: capacity enforcement and cashier transaction authorization. Access: org owners/managers via authenticated Platform/POS APIs. Retention: row retained after revoke for audit; no hard-delete of referenced devices. See WP15 closeout privacy notes.

## Status language

- **Implementation Complete** only with code + automated tests
- **Validation Pending** when physical validation incomplete
- **Not Device Verified** unless physical gate truly run
- Never **Production Ready** merely because code compiles
