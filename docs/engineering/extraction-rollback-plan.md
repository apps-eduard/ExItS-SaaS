# Extraction Rollback Plan

[Extraction sequence](../reuse/extraction-sequence.md) | [Risk matrix](platform-extraction-risk-matrix.md) | [Gate matrix](implementation-gate-matrix.md) | [ADR-013](../decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md)

**Work package:** P1-WP03  
**Status:** Planning only — no cutover authorized

---

## Rollback levels

| Level | Scope | Action | Affects HealthCare? |
|---|---|---|---|
| L0 | Documentation / architecture | Revert focused docs commit(s) | No |
| L1 | New Platform foundation | Revert/remove root Platform projects; keep root ignore of `HealthCare/` | No |
| L2 | Identity integration | Disable Platform auth adapter; HealthCare uses existing auth authority | HC returns to prior auth |
| L3 | Organization mapping | Disable mapping adapter; HC uses existing org behavior | Mapping off only |
| L4 | Entitlement integration | Use last trusted projection or disable new integration per fail-safe | Product ops continue within policy |
| L5 | Database cutover | Restore verified backup or reverse approved migration steps | Yes — restore path |
| L6 | UI rollout | Revert Platform Admin deployment independently | No |

## Trigger conditions (examples)

- Architecture/dependency gate failure
- Identity lockout, mass session invalidation, credential leakage
- Privilege escalation or lost clinical permissions
- Organization mapping causing cross-tenant access
- Entitlement mismatch granting unpaid features or blocking all ops incorrectly
- Contract major-version incompatibility
- Migration partial failure or restore failure
- HealthCare regression below accepted baseline
- Accidental root tracking of HealthCare
- Clinical data in Platform logs/contracts
- Inability to complete rollback rehearsal before cutover

## Required backups

Before any L2+ cutover attempt:

- HealthCare database backup verified by **restore rehearsal**
- Platform database backup (if exists)
- Mapping export / snapshot
- Configuration / feature-flag state snapshot
- Git tags for Platform and (separately) HealthCare baselines

## Authentication rollback (L2)

1. Disable Platform integration flag / issuer switch.
2. Confirm HC clients use prior authority.
3. Invalidate or tolerate transitional sessions per approved plan.
4. Verify login for staff and patient paths.
5. Audit rollback actor/reason.

**Do not** leave dual-auth in undefined state.

## Mapping rollback (L3)

1. Disable organization/user mapping adapter.
2. Leave mapping tables intact for forensics (prefer soft-disable).
3. Confirm HC operational queries use HC org IDs only.
4. Audit disablement.

## Entitlement rollback (L4)

1. Stop applying new Platform entitlement events if unsafe.
2. Retain last trusted projection within policy, or enter Never initialized / fail-closed for protected writes per [entitlement-state-matrix.md](entitlement-state-matrix.md).
3. Do not invent Active entitlements.
4. Alert operators; audit.

## Database rollback (L5)

1. Halt writes that depend on new schema if corrupted.
2. Restore from verified backup **or** execute reverse migration only if reverse steps were rehearsed.
3. Re-validate row counts, mapping integrity, sample clinical reads.
4. Extend rollback window until verification passes.
5. Prohibit cascade deletes across databases during rollback.

## Deployment rollback (L6)

1. Redeploy previous Platform Admin / API artifacts.
2. Confirm HC deployment unchanged unless HC change was part of approved WP.
3. Smoke-test independent surfaces.

## Verification after rollback

| Check | Evidence |
|---|---|
| HC auth works | Login/refresh smoke |
| No cross-tenant access | Tenant denial tests |
| Clinical permissions intact | Permission matrix sample |
| Baseline tests | 1102 safe suite (or current agreed baseline) |
| No Platform secrets in logs | Log sample review |
| `git ls-files HealthCare` empty | Root freeze check |
| Mapping disabled if required | Config audit |

## Audit requirements

Record: who authorized rollback, trigger, level, start/end UTC, backups used, verification results, residual risk, follow-up tickets.

## Who may authorize rollback later

| Level | Minimum authorizer (future policy) |
|---|---|
| L0–L1 | Engineering lead |
| L2–L4 | Engineering lead + Platform owner |
| L5 | Engineering lead + Platform owner + product owner (HC); compliance if PHI risk |
| L6 | Platform owner |

Exact names/roles finalized in Phase 2/9 runbooks — not assigned as people in this WP.

## Evidence required before any cutover (not rollback)

Backup verification · Test totals · Migration report · Mapping report · Security tests · Smoke tests · Rollback rehearsal · Written approval record

**P2-WP05:** Platform-side dry-run validators and rollback-evidence checks exist. They do **not** prove database restore rehearsal (R-027) or completed HealthCare migration.
