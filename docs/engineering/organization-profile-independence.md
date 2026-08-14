# Organization profile independence

## Status

Implemented for MVP (no schema migration).

## Rules

| Rule | Behavior |
|------|----------|
| Independent profiles | `OrganizationProfile` is owned by the organization aggregate. It is not a live view of Personal profile. |
| One-time copy | Start a Business may copy Personal email/phone when `UseMyContactDetails` is true. Explicit Contact*/Address* request fields override copied values. |
| No live sync | Updating Personal never updates Organization; updating Organization never updates Personal. |
| Multi-org ownership | The same Personal identity may be `OrganizationOwner` of Org A and Org B. |
| One owner per org (MVP) | Each organization has exactly one Owner. Adding a second Owner fails with `OrganizationOwnerUniqueViolation`. Ownership transfer (`OrganizationOwnershipTransfer`) atomically replaces the sole Owner; see [organization-ownership-transfer.md](organization-ownership-transfer.md). |
| Business QR stable | Public organization identity remains `DisplayName` + `PublicOrganizationId` / QR payload only. Contact fields never appear on public identity. Ownership transfer does not change `OrganizationId` / `PublicOrganizationId`. |
| Receipts | POS receipt merchant details continue to come from `PosOperationalSetup` (product operational data), not Platform OrganizationProfile. |

## Clients

- **MAUI / Personal.Web Start a Business:** Contact details section with “Use my contact details” (default on), editable business email/phone/address fields, passed on `StartBusinessRequest`.
- **MAUI Org profile:** Full business contact edit via `UpdatePlatformOrganizationRequest` (name, legal, contact, address, locale fields). Slug remains editable for owners when the API allows.
- **Organization Web profile:** Already supports the same Platform fields.

## Persistence

No migration. `OrganizationProfile` columns already exist on Platform organizations. Seed happens once at Start a Business after org create.

## Related

- [personal-organization-identity-boundaries.md](../architecture/personal-organization-identity-boundaries.md)
- [client-experience-boundaries.md](../architecture/client-experience-boundaries.md)
- [organization-profile-independence report](../reports/P25-WP08-organization-profile-independence.md)
