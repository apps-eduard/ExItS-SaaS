# Personal / Organization identity boundaries

[Public User ID and QR](../specs/identity/public-user-id-and-qr.md) | [Client experience](client-experience-boundaries.md) | [Isolation report](../reports/personal-organization-identity-isolation.md)

## Purpose

Clarify how Personal identities, Organization identities, and POS device registration QR codes stay isolated on Platform and Mobile.

## Identity classes

| Class | Login | Home org | Public QR subject |
|---|---|---|---|
| Personal / Owner | Real email | `null` | `exits://qr/v1/personal/{EX-####-####}` |
| Organization staff | `<local>@ORG######` | Exactly one org | Same personal envelope on their staff `PlatformUser` (not the business QR) |
| Organization (business) | N/A (org aggregate) | N/A | `exits://qr/v1/organization/{ORG######}` |
| POS device registration | N/A (one-time token) | Owning org | `exits://qr/v1/pos-device-registration/{opaqueToken}` |

Personal subjects remain keyed by `PlatformUserId` / `PublicUserId`. Organization subjects are owned by `OrganizationId` / `PublicOrganizationId`. Device registration tokens are org-scoped, hash-stored, 15-minute TTL, single-use.

## Scan / resolve rules

1. Scan alone never grants membership, POS role, or Personal link.
2. Callers that know the expected purpose (e.g. device registration) must pass `expectedPurpose` to `POST /api/v1/qr/resolve` and reject mismatches with a plain mismatch message.
3. Without `expectedPurpose`, the typed dispatcher routes by envelope purpose.
4. Business QR is displayed under org essentials (`/org/business-qr`); Personal My QR stays under Personal (`/personal/my-qr`).

## Mobile org-switch isolation

On `SelectOrganizationAsync` / `SwitchToPersonalAsync`:

- Close local SQLite context (file-per-org / personal already isolates on disk)
- Clear protected-shell process validation
- Clear `SellingModeService` preferred / selling state
- Clear `BranchId` / `PosDeviceId` so Org A device binding cannot authorize Org B
- `SaleCartService` clears itself when `ICurrentUserContext.OrganizationId` changes (Maui singleton; not injected into Application auth)

LocalStore schema **v9** is unchanged for this package: file-per-organization isolation already prevents Org A→B SQLite bleed; no schema bump is required for identity QR work.

## Connected suppliers

Buyer↔supplier relationships remain **organization↔organization** commercial links. Public identity QR does not create supplier relationships. See [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md).
