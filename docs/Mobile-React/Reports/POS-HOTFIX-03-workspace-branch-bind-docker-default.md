# POS-HOTFIX-03 — Workspace branch bind 400 + Docker default runtime closure

## Package

| Field | Value |
|-------|-------|
| Branch | `feat/pos-react-client` |
| Starting HEAD | `75c4f211c2313a41db8197c0fb3a0eb331139ee4` |
| Final HEAD | `fccfb3e9` _(hash record commit follows)_ |

## Executive summary

The owner-visible `PUT branch-context` **HTTP 400** was traced to **`application.branch.not_found`** with detail **"BranchId cannot be an empty GUID."** Root cause: the live validation harness (`live-antiforgery-validation.mjs`) read **`branch.branchId`** from `GET /branches`, but Platform `ListBranches` returns the authoritative id in field **`id`**. Sending `{ branchId: undefined }` produced empty GUID rejection.

React workspace code was already correct (`mapActiveBranches` maps `branch.id` → `branchId`). Secondary fixes: bind-failure diagnostics now report the correct Platform path for branch-context failures; Docker POS API startup under `Staging` + `LocalValidation:Enabled` no longer fails production signing-key guard.

## Pre-fix error capture (Copy Error Details contract)

| Branch | Status | ErrorCode | Detail |
|--------|--------|-----------|--------|
| Main Branch | 400 | `application.branch.not_found` | BranchId cannot be an empty GUID. |
| Kizy Store 02 | 400 | `application.branch.not_found` | BranchId cannot be an empty GUID. |

Reproduction: run old harness body `{ branchId: branch.branchId }` where list item only has `id`.

## Branch / org facts (Local Validation, Docker)

| Field | Main Branch | Kizy Store 02 |
|-------|-------------|---------------|
| BranchId | `742fb3f3-14f9-4bee-a94e-f5acccc7cbc5` | `4dbf1ed7-8936-4d77-90f6-464ca426bbab` |
| Code | MAIN | BR02 |
| Status | Active | Active |
| IsPrimary | true | false |
| Organization | `ca023f5b-925e-4aa5-a843-d48c4c06fa14` (Kizy Store) | same |
| Kizy access | YES (OrganizationOwner — all Active branches) | YES |

Kizy: `accountClass=Organization`, `membershipRole=OrganizationOwner`.

ListBranches contract: returns branches filtered to actor-accessible Active branches; Owner receives both branches; branch-context accepts the same `id` values.

## Runtime provenance

### Docker (final owner validation)

| Port | Container | Worktree | Branch | HEAD |
|------|-----------|----------|--------|------|
| 8091 | exits-local-validation-platform-api | ExItS-SaaS-pos-react-client | feat/pos-react-client | post-fix commit |
| 8092 | exits-local-validation-pos-api | ExItS-SaaS-pos-react-client | feat/pos-react-client | post-fix commit |

Launcher: `.\tools\Start-DockerLocalValidation.ps1 -Build`

### Host debug (developer only)

Used during investigation; not final acceptance. Host processes were stopped before Docker rebuild.

## Delivered changes

1. `live-antiforgery-validation.mjs` — resolve `id` from ListBranches; full chain through session grant + POS operational-branch.
2. `resolvePlatformBranchId` + tests; `bindWorkspaceWithSessionGrant` failure reasons (`organization_context` / `branch_context`).
3. `WorkspaceProvider` — Copy Error Details path/operation for branch-context failures.
4. `workspace-bind-error` — map Platform branch error codes to branch_not_accessible UX.
5. `PosProductionSecurityGuard` — allow `LocalValidation:Enabled` under non-Production (fixes Docker POS API crash on Staging).
6. `README.local-validation.md` — Docker is default owner-equivalent mode; host debug optional.

## Build / test evidence

See final report flags after push.
