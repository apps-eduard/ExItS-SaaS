# Organization ownership transfer — completion report

## Status

Implemented (MVP).

## Starting SHA

`5fd997e058b4b2722ef5801cbc977703857e5cef`

## Feature / documentation hashes

Recorded after push (see Git / push evidence below).

## Delivered capability

- New aggregate `OrganizationOwnershipTransfer` (not `OrganizationInvitation` / staff invites).
- Request / cancel / accept / decline / expire lifecycle (7-day Pending; one Pending per org via unique filtered index).
- Atomic accept under org advisory lock: former Owner removed; recipient becomes sole Owner; org identity and business data preserved.
- Personal QR / EX ID target resolution; Business/Device QR rejected with WP wording.
- Platform API endpoints + Org Web initiation (typed confirmation) + MAUI Personal accept/decline.
- Session invalidation for former owner (selected org + access-token org binding + product access revoke).
- Multi-org ownership preserved for non-transferred organizations.

## Explicit exclusions / deferred

- Keep-former-owner-as-admin option (MVP removes former owner).
- Business sale / payment / escrow / legal / marketplace / co-owners / equity.
- Billing payment-method migration to new Owner (org entitlement retained; Personal payment methods do not transfer).
- Auto-grant POS product-local Owner/cashier roles on accept.
- MAUI initiation UI (Web-only for request).
- Email/SMS notification infrastructure.

## Persistence / migrations

- Table `platform.organization_ownership_transfers`
- Migration: `AddOrganizationOwnershipTransfers` (`20260814192519_AddOrganizationOwnershipTransfers`) — idempotent SQL
- Unique filtered index: one Pending per `organization_id`
- LocalStore version: **unchanged** (no offline schema change)

## Architecture audit (summary)

| Area | Finding |
|------|---------|
| Owner representation | `OrganizationMembership.Role = OrganizationOwner` on Personal identity |
| Single-owner enforcement | `EnsureSingleOrganizationOwnerSeatAsync` + transfer post-condition; no DB unique on Owner role |
| Membership model | One current membership per (user, org) for Active/Suspended |
| Invitation infrastructure | Staff `OrganizationInvitation` **not** reused (creates `@ORG` staff identities; rejects Owner) |
| Sessions | Membership revoke pattern: clear selected org + access-token org binding |
| Subscriptions | Org-owned entitlement remains with Organization |
| POS roles | Platform Owner ≠ POS-local roles; transfer does not invent cashier/device access |

## Build / test evidence

| Suite | Result |
|-------|--------|
| Platform.UnitTests (full Release) | Passed 895 / Failed 0 / Skipped 0 |
| Platform.UnitTests `~OwnershipTransfer` | Passed 13 / Failed 0 / Skipped 0 |
| Platform.IntegrationTests `~OwnershipTransfer` | Passed 1 / Failed 0 / Skipped 0 |
| PinoyBusinessPOS.UnitTests | Passed 728 / Failed 0 / Skipped 0 |
| PinoyBusinessPOS.ApiClient.Tests | Passed 51 / Failed 0 / Skipped 0 |
| ArchitectureTests | Failed 4 / Passed 163 — **PRE-EXISTING** (SaaS payment prefix, Android cleartext wording, Admin page header hard-code, etc.) |
| Maui.Tests | Failed 3 / Passed 407 — **PRE-EXISTING / cash leftovers** (`ShiftsPageGuardTests`, `OperationalSetupUiGuardTests`, `MauiFoundationGuardTests` Cashier assert) — not introduced by ownership transfer commits |
| Full `ExItS.slnx` Release | Maui Android **XA5300** (Android SDK missing locally) — environment, not feature defect |

Transfer scenarios covered: request+accept sole owner; multi-org preservation; existing-member promote; decline/cancel; admin cannot initiate; wrong recipient; self-transfer; QR purpose reject; expiry; API resolve/request/accept; Business QR reject.

## Security limitations

- Development-stage Platform auth patterns unchanged.
- Accept/decline require authenticated recipient matching `ToUserId`.
- Org Web cancel requires Organization account-class session (scope guard).

## Portfolio independence

No HealthCare nesting; Platform/Product DB boundaries preserved; POS has no PHI.

## Risks / open decisions

- Product-local POS Owner role is not automatically reassigned on ownership transfer.
- Concurrent accept races rely on org advisory lock + pending unique index + post-condition rollback.
- Billing payer transition deferred.

## Exact next work package

Optional: auto-grant POS Owner product-local role on accept; MAUI initiation UI; billing payer update workflow; keep-as-admin transfer option.

## Git / push evidence

(Filled after push.)
