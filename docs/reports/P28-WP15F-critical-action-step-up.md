# P28-WP15F — Critical Action Password Step-Up and Safe Lifecycle Controls

## Summary

This work introduces a server-authoritative **password step-up** mechanism for **critical Platform governance mutations**. Critical endpoints now require:

- ordinary authorization (who is allowed to act, in the correct organization context),
- a **reason** where the domain requires it,
- a **current password** step-up executed on the server,
- a **scoped, short-lived step-up token** consumed by the critical endpoint,
- **audit** attribution that explicitly records **PasswordStepUp** as the authentication strength.

The goal is to replace “confirm-only” destructive flows with a permission + server re-auth pattern that is resistant to replay and cross-target reuse.

## Non-goals / boundaries

This report covers **Platform governance critical mutations** implemented by P28-WP15F (not POS operational void/refund/stock adjustments).

POS operational “ledger correctness” remains enforced by the relevant domain rules; this step-up system is used for the governance surfaces listed below.

## Threat model (what step-up prevents)

The step-up token is scoped and consumed so that the following attacks are blocked:

- **Wrong password**: the step-up grant is not issued without verifying the user’s current password.
- **Replay attacks**: once consumed, the same step-up token can’t be used again (token is marked consumed).
- **Cross-org / cross-target reuse**: the grant is bound to `(user, organization, actionCode, targetType, targetId)` and expires quickly.
- **Password persistence**: the server never stores or logs the entered password; it stores only a token hash.

## Token model (server-issued step-up grant)

### Issue step-up

Endpoint:

- `POST /api/v1/platform/organizations/{organizationId}/governance/step-up`

Request includes:

- `actionCode` (what critical capability is being requested),
- `targetType` (what kind of target is affected),
- `targetId` (nullable; required for target-bound grants),
- `currentPassword` (entered by the user, verified on the server).

Response includes:

- `stepUpToken` (opaque token),
- `expiresAtUtc`,
- the `(actionCode, targetType, targetId)` scoping metadata (returned for client UX).

### Consume step-up

Each critical endpoint calls the server use case that:

1. hashes the provided `stepUpToken`,
2. loads the corresponding governance step-up grant,
3. verifies it is:
   - **not consumed**,
   - **not expired**,
   - **exactly scoped** to the calling actor + organization + requested action + target,
4. marks the grant as **consumed** and persists it.

After consumption, the endpoint rechecks normal authorization for the mutation (critical endpoints never “skip” normal authz).

## Critical actions covered (P28-WP15F baseline)

At minimum, the following governance mutations require password step-up + reason + audit:

- **Branch lifecycle controls**
  - suspend / archive / reactivate (soft lifecycle; no hard delete)
- **Organization membership lifecycle**
  - suspend
  - revoke
  - role change / escalation-revocation paths classified as critical
- **POS device revocation**
  - revoke (device lifecycle protection)

The exact `actionCode` values are centralized in `GovernanceCriticalActionCodes`.

## Reason requirements

When the domain requires a reason, the API validates:

- non-empty,
- trimmed length at least **8 characters**.

On failure, the endpoint returns a `400 BadRequest` problem response with an explicit domain error code.

## Audit semantics

Successful critical operations emit audit records that include:

- the mutation identity (target),
- the acting actor,
- the reason (when provided),
- **authentication strength = PasswordStepUp**.

The entered password is never included in audit payloads.

## Error semantics (client expectations)

Observed HTTP semantics from the step-up helper:

- **401 Unauthorized**
  - wrong current password while issuing step-up
  - credential lockout behavior (existing auth lockout policy)
- **403 Forbidden**
  - missing/invalid step-up token
  - step-up token present but invalid for the requested scope
- **410 Gone**
  - step-up token expired
- **409 Conflict**
  - step-up token already consumed (replay)

## Branch suspension behavior (soft lifecycle)

Branch suspension is implemented as a soft lifecycle transition that persists:

- `Status = Inactive`
- `SuspendedAtUtc`
- `SuspendedByUserId`
- `SuspensionReason`

The implementation preserves:

- inventory/history,
- sales/history,
- orders/history,
- staff history,
- devices/history,
- audit records.

Suspension also validates domain blockers (as implemented in the use case), including:

- Primary/Main restrictions,
- open shift/register restrictions,
- unresolved operational work/orders/transfers where required by business rules.

## Client + UI requirements

The critical-action UI uses the shared `ConfirmDialog` pattern with:

- compact destructive styling,
- optional reason input (domain controlled),
- a **current password** input:
  - `type="password"`
  - `autocomplete="current-password"`
  - password is never persisted after the dialog closes.

Client flow:

1. client issues step-up token by calling `/governance/step-up` (server verifies password),
2. client calls the critical mutation endpoint including `reason` + `stepUpToken`.

## Account-scope guard integration

Organization-scoped sessions are allowed to call these governance surfaces even though they are still under `/api/v1/platform/*`. The platform account-scope middleware was extended to permit:

- `/api/v1/platform/organizations/*` (already allowed),
- `/api/v1/platform/memberships/*`,
- `/api/v1/platform/pos-devices/*`.

These routes still require trusted organization context and server-side authorization.

## Test coverage (P28-WP15F security matrix)

Integration tests added/updated cover:

- correct password + authorized critical mutation succeeds,
- wrong password denies step-up issue,
- missing step-up denies the critical endpoint,
- consumed token cannot be replayed,
- step-up token scoped to a different target is denied,
- security audit omits password.

## Files touched (implementation anchors)

- Step-up endpoints:
  - `GovernanceStepUpEndpoints`, `GovernanceStepUpHelper`
- Step-up grants and consumption:
  - `GovernanceStepUpUseCases`
- Critical mutation endpoints:
  - branch lifecycle endpoints (suspend/reactivate/archive)
  - membership lifecycle endpoints (suspend/revoke/role-change)
  - POS device revoke endpoint
- UI:
  - `ConfirmDialog` extended with reason + current-password support

