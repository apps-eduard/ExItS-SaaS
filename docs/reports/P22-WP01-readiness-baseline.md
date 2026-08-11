# P22-WP01 — Production Readiness Baseline

[Phase 22](../phases/phase-22-production-readiness-release-and-operational-hardening.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** |
| Date | 2026-08-10 |

## Audit summary

| Area | Current state | Phase 22 gap |
|---|---|---|
| Organization model | `PlatformOrganization` (name/slug/profile/branding); no business type | Add `PrimaryBusinessTypeId` |
| Plans / subscriptions | `Plan.MaxBranches`, `MaxActiveStaff`; entitlement snapshots | Add `MaxActivePosDevices`; enforce branch/device usage |
| Business Types | Dynamic `BusinessType` + template/category links (P20) | Bind org primary type; filter templates |
| Catalog / Templates | Global catalog + templates + merchant import | Optional onboarding; filter by org type |
| Staff / roles | Org membership + product-local POS roles | Capacity via MaxActiveStaff (exists) |
| POS device identity | Local `pos.device.id` GUID in secure store | Server `PosDevice` registry |
| Offline grant / PIN | Device-bound grant; PIN unlock; NavigationGate | Bind to registered Active device; reject≠unreachable |
| POS transactions | Org + commercial + role auth; no device gate | Require registered device for money ops |
| Onboarding | Start Business → plan → PIN → optional template → setup | Insert Business Type + Main Branch + device register |
| Auth / session | `AuthSession` (no BranchId) | Persist current BranchId in session/device context |
| Production config | `PlatformSecurityPipeline` prod guards; health/ready | Extend for device/branch ops; document External Setup |
| Migrations / backup | EF history; LocalValidation migrate; no prod auto-Migrate | Keep history; reset scrap data only |
| Email | Invite/reset providers; prod forbids bootstrap | External Setup Required for vendors |
| Logging / health | Standard logging; `/health`, `/health/ready` | Correlation + safe device/branch diagnostics |
| Release / deploy | Compose/P14 path; Android signed APK process | Document order; do not claim deployed |

## Dependency map

```text
BusinessType (catalog) ──PrimaryBusinessTypeId──► Organization
Organization ──owns──► Branch (exactly one IsPrimary="Main Branch")
Plan entitlement ──limits──► MaxBranches / MaxActivePosDevices / MaxActiveStaff
Organization + Branch ──owns──► PosDevice (InstallationDeviceId = local DeviceId)
PosDevice Active + Branch + entitlements ──authorize──► Cashier transactions
Offline grant ──binds──► User + Org + DeviceId (+ Branch when required)
Template (optional) ──filtered by──► Organization.PrimaryBusinessTypeId
```

## Preserved vs disposable (WP09)

**Keep:** 2 Platform admin identities (Local Validation: `olivia.mendoza@exits.local`, `rafael.torres@exits.local`), commercial products/plans/features, Business Type definitions, Catalog Template definitions (rows), system permissions/roles, EF migration history.

**Remove:** scrap Personal/org/staff users, orgs, memberships, invitations, org subscriptions/trials, branches, POS devices, offline grants, merchant ops data; **all disposable Global Catalog merchandise** (products, categories, product↔business-type mappings, template↔product compositions, catalog import jobs/items). Template definition rows are kept; compositions are cleared so product deletes leave no orphans.

## Related reports

Subsequent WP reports and the Phase 22 closeout record implementation evidence, migrations, tests, and push SHAs.
