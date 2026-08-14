# Organization ownership transfer

## Status

Implemented (MVP). Canonical Phase report: [P25-WP09-organization-ownership-transfer.md](../reports/P25-WP09-organization-ownership-transfer.md).

Phase **P25** remains **OPEN** (Owner Validation Pending). No Phase closeout.

## Purpose

Hand off the sole **Organization Owner** seat from one **Personal** identity to another Personal identity, without reusing staff invitations (`OrganizationInvitation`).

## Non-goals / exclusions

- Does **not** create `@ORG` staff identities.
- Does **not** keep the former owner as Admin (MVP: former owner is **removed** from the organization).
- Does **not** move Personal data; only organization membership/ownership changes.
- Does **not** change `OrganizationId`, `PublicOrganizationId`, Business QR, profile, branding, or business data.
- Transferring Org A must not affect Org B for multi-org owners.

## Aggregate

`OrganizationOwnershipTransfer`

| Field | Notes |
|-------|--------|
| Id | Strong id |
| OrganizationId | Target org (unchanged through accept) |
| FromOwnerUserId | Current Owner (Personal) |
| ToUserId | Recipient Personal user |
| Status | Pending, Accepted, Declined, Cancelled, Expired |
| CreatedAtUtc / ExpiresAtUtc | Default lifetime **7 days** |
| AcceptedAtUtc / DeclinedAtUtc / CancelledAtUtc / CompletedAtUtc / UpdatedAtUtc | Lifecycle stamps |

One **Pending** transfer per organization (unique filtered index `ux_organization_ownership_transfers_pending_org`).

## Target resolution

- Accept **Personal QR** or **PublicUserId** (`EX-####-####`) only.
- Reject **Business QR** and **Device QR** with friendly messages.
- Reject organization-scoped staff identities as transfer targets.

## Accept (atomic)

Under organization advisory lock + DB transaction:

1. Validate Pending, not expired, actor = `ToUserId`.
2. Confirm `FromOwner` is still the sole active Owner.
3. Create or promote recipient membership to `OrganizationOwner`.
4. Soft-remove former owner (bypass last-owner guard because the new Owner is staged in the same transaction).
5. Revoke product access assignments, clear selected org + access-token org binding for the former owner (same pattern as membership revoke).
6. Mark transfer Accepted/Completed.
7. Post-condition: exactly one active Owner (failure rolls back the transaction).

## API

| Method | Path |
|--------|------|
| POST | `/api/v1/platform/organizations/{organizationId}/ownership-transfer/resolve-target` |
| POST | `/api/v1/platform/organizations/{organizationId}/ownership-transfer/request` |
| GET | `/api/v1/platform/organizations/{organizationId}/ownership-transfer/pending` |
| POST | `/api/v1/platform/ownership-transfers/{id}/cancel` |
| POST | `/api/v1/platform/ownership-transfers/{id}/accept` |
| POST | `/api/v1/platform/ownership-transfers/{id}/decline` |
| GET | `/api/v1/platform/ownership-transfers/my-pending` |

Owner initiates/cancels; recipient accepts/declines.

## UI

- **Organization Web:** `/organization/ownership-transfer` (Owner only) — resolve, request, cancel.
- **MAUI Personal:** `/personal/ownership-transfers` — list pending for recipient; accept/decline.
- Initiation stays Web-first; MAUI initiation deferred.

## Migration

`AddOrganizationOwnershipTransfers` (idempotent `CREATE TABLE IF NOT EXISTS`).

## Audit

- `platform.ownership_transfer.requested`
- `platform.ownership_transfer.cancelled`
- `platform.ownership_transfer.declined`
- `platform.ownership_transfer.accepted`
- `platform.organization.owner_changed`

## Former-owner membership policy (MVP)

After successful accept, the former Owner is **removed** from the organization (no silent Admin retention). Personal login remains; only that Organization membership/authorization is revoked (selected org + access-token org binding cleared).

## Subscription / billing

Organization-owned subscription/entitlement identity stays with the Organization. Personal payment methods / billing credentials do **not** transfer. Billing payer/payment-method migration is deferred; an already-paid entitlement period is not broken solely because Owner changed.

## Offline / LocalStore

LocalStore remains file-per-(user, org, product). No file transfer. Former-owner outbox keeps original `OrganizationId` / `ActorUserId`; replay follows server authorization and must fail after membership removal. New Owner uses their own LocalStore context.

## POS roles

Platform Organization Owner ≠ POS product-local Owner/cashier. Ownership transfer does not auto-grant POS-local roles or reset devices.

## Related

- [organization-ownership-transfer report](../reports/P25-WP09-organization-ownership-transfer.md)
- [personal-organization-identity-boundaries.md](../architecture/personal-organization-identity-boundaries.md)
- [client-experience-boundaries.md](../architecture/client-experience-boundaries.md)
