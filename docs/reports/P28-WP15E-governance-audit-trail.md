# P28-WP15E — Organization/Branch Governance Audit Trail

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [WP15A capability baseline](P28-WP15A-capability-client-boundary-baseline.md) | [WP15D operational actors](P28-WP15D-operational-actor-traceability.md) | [Capability matrix](../engineering/organization-branch-capability-matrix.md)

| Field | Value |
| --- | --- |
| Status | **Code Complete / Validation Pending** |
| Depends on | WP15A–D on `origin/main` |
| Closes | Platform governance audit emission gaps + org-scoped investigation UI |

## Goal

Append-only **Platform governance** audit for organization and branch administration mutations — separate from POS operational actor-on-record (WP15D).

Reuse existing `platform.audit_records` infrastructure; no parallel audit system.

## Migration

**None.** WP15E uses the existing Platform audit table and `AddPlatformAuthorizationAndAudit` migration.

## Event coverage

Success audits added or confirmed for governance mutations including:

| Area | Action codes (examples) |
| --- | --- |
| Organization profile | `platform.organization.updated`, branding |
| Branches | created, updated, archived, reactivated, hours, fulfillment, delivery policy, online orders pause |
| Staff / membership | invitation, role change, suspend/reactivate/remove, branch assignments |
| Devices | register, rename, revoke, registration token create |
| Access denials | unchanged — denied paths still audited without success claims |

Summaries use sanitized changed-field metadata; passwords, tokens, PINs, and invitee email are not logged in summaries.

## API

| Route | Purpose |
| --- | --- |
| `GET /api/v1/platform/organizations/{organizationId}/audit` | Server-paged org-scoped query (Owner/Manager or Platform `ViewAuditRecords`) |
| `GET /api/v1/platform/organizations/{organizationId}/audit/{auditId}` | Single record; cross-org returns 404 |

Query filters: `fromUtc`, `toUtc`, `actor`, `action`, `outcome`, `branchId` (maps to branch target), `page`, `pageSize`.

## UI surfaces

| Surface | Behavior |
| --- | --- |
| **MAUI Manage business** | Up to 15 recent succeeded governance rows; dense text; “View full audit on Web” (EN + fil-PH) |
| **Organization Web** | `/organization/audit` — dense table, filter bar, `OrgPager`; Owner/Manager only (`Shell.CanSee("audit")`) |

Cashiers and organization staff without Owner/Manager authority cannot access org audit read APIs or Web nav.

## Security / privacy

- Append-only — no edit/delete through organization administration
- Failed mutations do not emit success audits
- Cross-organization audit access denied
- Sensitive values excluded from audit summaries (invitation email pattern preserved)

## Relationship to WP15D

| Concern | Owner | Mechanism |
| --- | --- | --- |
| Governance (profile, branch, staff, device, settings) | Platform | `platform.audit_records` |
| Operational transactions (sales, stock, orders, payments) | POS | Actor fields on authoritative records |

No cross-database FKs or direct DB access between Platform and POS audit views.

## Tests

| Suite | Scope |
| --- | --- |
| `ExItS.Platform.IntegrationTests` | `ApiOrganizationGovernanceAuditTests` — emit once, actor/org, failed mutation, cross-org denied, staff denied, paging, sensitive summary |
| `ExItS.PinoyBusinessPOS.UnitTests` | `GovernanceAuditDisplayTests` |
| `ExItS.PinoyBusinessPOS.Web.Tests` | Audit page + Manage business activity guard tests |

## Explicit exclusions

- MAUI full audit explorer
- POS generic audit log
- Audit record edit/delete APIs
- Password step-up on audit read

## Readiness

Governance audit emission and org-scoped read paths are implemented with Mobile summary and Web investigation UI. Production readiness requires Release build/test sign-off on `ExItS.slnx` and PostgreSQL integration test pass.

## Next work package

Per phase plan after WP15E sign-off (WP15F+ or adjacent phase items as authorized).
