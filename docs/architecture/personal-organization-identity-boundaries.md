# Personal / Organization identity boundaries

[Public User ID and QR](../specs/identity/public-user-id-and-qr.md) | [Client experience](client-experience-boundaries.md) | [Isolation report](../reports/P25-WP06-personal-organization-identity-isolation.md)

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

## Organization profile independence

- Organization contact/profile fields live on the organization aggregate (`OrganizationProfile`).
- Start a Business may **copy** Personal email/phone once (`UseMyContactDetails`); there is **no live sync** afterward.
- The same Personal identity may own multiple organizations; MVP allows **one Owner per organization**.
- Ownership transfer is supported via `OrganizationOwnershipTransfer` (Personal QR / EX ID only). Staff invitations must not be used for Owner handoff. See [organization-ownership-transfer.md](../engineering/organization-ownership-transfer.md).
- Business QR / public identity exposes only DisplayName + PublicOrganizationId (no contact leak).
- Business regulatory / tax / compliance identity belongs to the Organization (Platform compliance profile anchor + capability), never to the Owner Personal profile. Public Business QR must not expose TIN, compliance profile, evidence, or TaxDocument capability. See [organization-compliance-profile.md](../engineering/organization-compliance-profile.md) and [bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md).
- POS receipts still use product operational setup, not Platform OrganizationProfile.

See [organization-profile-independence.md](../engineering/organization-profile-independence.md).

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

Buyer↔supplier relationships remain **organization↔organization** commercial links. Public identity QR does not create supplier relationships. Connected-supplier connect requires Business QR / `ORG######` and rejects Personal and device-registration QR on both MAUI and POS Application. See [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md).

## POS sale buyer parties

POS sales remain owned by the selling organization. Personal or Business QR at checkout identifies the **buyer counterparty** only — never Personal ownership of the sale. See [sales-buyer-party-model.md](../engineering/sales-buyer-party-model.md).

## Privacy refresh (P21-WP11)

Post–Phase-21 privacy inventory for typed QR, ownership transfer, buyer linking, and related classifications: [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md). **NPC compliance NOT CLAIMED**; Legal/DPO review pending.
