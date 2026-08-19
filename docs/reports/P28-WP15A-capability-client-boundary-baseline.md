# P28-WP15A — Organization/Branch Capability + Client Boundary Baseline

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [Capability matrix](../engineering/organization-branch-capability-matrix.md) | [WP14 workspace selection](P28-WP14-unified-organization-branch-workspace-selection.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Docs Complete** |
| Starting SHA | `5a3df78af17c3f1b97703e84ebb0b649e58b2279` |
| Commit | `1e20781f` |
| Code changed | **No** — docs-only baseline |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Before adding more authorization, establish canonical rules for organization governance, branch configuration, branch operations, Mobile vs Web exposure, actor attribution, critical-action step-up, and audit requirements.

## Delivered

### Canonical capability matrix

[docs/engineering/organization-branch-capability-matrix.md](../engineering/organization-branch-capability-matrix.md)

Covers at minimum:

- create / edit / primary change / archive branch
- organization profile, subscription, settings
- staff invite/remove/role (+ future branch assignment)
- device registration/revocation
- catalog, hours, fulfillment, online-order pause
- sale, void/refund, shift/register
- stock count/adjustment, transfer dispatch/receive
- customer-order transitions
- reporting/audit read paths
- workspace selection

Each row records: scope class, data owner, role, branch access, Mobile/Web exposure, exact branch, device match, shift rules, actor, audit, reason, step-up target, lifecycle.

### Locked principles (12)

Documented in matrix § Locked product principles — including:

- Workspace ≠ POS permission
- Owner authority ≠ selling permission
- Mobile Primary/Main governance gateway (not a superuser identity)
- Web full governance without fake Main selection
- UI hiding ≠ authorization
- Immutable financial history; archive for master records

### Honest current-state gaps

Documented explicitly (not hidden):

| Gap | Notes |
|---|---|
| MAUI Owner `/org/*` nav without Primary gate | Target policy in matrix; **current UI** still shows org governance from any workspace branch |
| Staff↔branch ACL | Abstraction exists (WP14); resolver not implemented |
| POS password step-up | Platform lifecycle step-up exists; not generalized for void/adjustment |
| Shifts org-scoped | WP13 documented limitation retained |

## Audit of WP11–WP14 implementation

| Area | Finding |
|---|---|
| WP14 workspace | Confirmed at `f5c4b2fb` — `/workspace-select`, `SelectWorkspaceAsync`, burger Switch workspace |
| WP13 branch ops | Device binding, shift guard, `SelectedBranchId` vs device branch preserved |
| WP12 branch ownership | Org master vs branch overlay documented and aligned |
| WP11 fulfillment | Branch readiness evaluator; reason optional on online pause API |
| POS actor attribution | `PosOrganizationScope.TryGetActorId` on mutation endpoints |
| Platform audit | Platform lifecycle + org actions audited; POS uses actor-on-record |
| Org Web | Full management; no checkout; workspace accordion (WP14) |
| MAUI OrgSummary | Owner governance links; no `IsPrimary` filter yet |

## Docs updated (conflicts corrected)

| Document | Change |
|---|---|
| [organization-branch-capability-matrix.md](../engineering/organization-branch-capability-matrix.md) | **Created** — authoritative matrix |
| [client-experience-boundaries.md](../architecture/client-experience-boundaries.md) | WP15A cross-ref; workspace in nav; deferred multi-branch updated |
| [organization-web-role-and-workflow-matrix.md](../engineering/organization-web-role-and-workflow-matrix.md) | Web vs Mobile Primary distinction |
| [organization-branches-and-fulfillment-locations.md](../engineering/organization-branches-and-fulfillment-locations.md) | Matrix cross-link |
| [authorization-matrix.md](../engineering/authorization-matrix.md) | Matrix link in header |
| [phase-28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | WP15A row |
| [P28-WP14 report](P28-WP14-unified-organization-branch-workspace-selection.md) | WP15A cross-link |

Historical reports retaining `/organization-select` or topbar branch dropdown references were left unchanged where they record accurate past evidence.

## Validation

| Check | Result |
|---|---|
| WP14 present on origin/main | **Yes** (`f5c4b2fb`) |
| Code build | **Not run** — docs-only package |
| Tests | **Not run** — no code changes |
| Link/doc coherence | Matrix + cross-links reviewed |

## Explicit exclusions

- No new ACL subsystem
- No POS step-up implementation
- No MAUI Primary-gate UI refactor (WP15B+)
- No Device / Browser / Production Ready claims

## Next work package

**WP15B** (or adjacent auth WP): enforce Mobile Primary governance exposure + API alignment where matrix marks **Target** vs **Current gap**.
