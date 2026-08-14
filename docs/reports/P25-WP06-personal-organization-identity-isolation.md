# P25-WP06 — Personal / Organization identity isolation + typed QR

| Field | Value |
|---|---|
| Phase | **P25** — Organization Web / Identity / Organization Management |
| Work package | **P25-WP06** |
| Status | **Code Complete / Owner Validation Pending** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Starting SHA | `d8e43575d103fc19cf42c5cb33d2c28264a0d4b6` |
| Feature SHAs | `3e1515b972e4fe2a99c2baeb26994c465bdd0edd`, `8f3875f5ae7ec38bfd1703d7e44a78e7fed7d533`, `66a972f84741f7b7f394c19438e0b9ef0d7367c0` |
| Alias | [personal-organization-identity-isolation.md](personal-organization-identity-isolation.md) |
| Related | [architecture boundaries](../architecture/personal-organization-identity-boundaries.md) · [QR spec](../specs/identity/public-user-id-and-qr.md) |

## Architecture audit (pre-implementation)

| Area | Finding |
|------|---------|
| User identity | `PlatformUser` / `PlatformUserId` — one human |
| Personal identity | Same `PlatformUserId` when `HomeOrganizationId` is null; no separate PersonalAccountId table |
| Organization | `PlatformOrganization` + `PublicOrganizationId` (`ORG######`) |
| Membership/owner | `OrganizationMembership` role (`OrganizationOwner`…) — not a column on org |
| QR (before) | Only `exits://user/v1/{EX-…}` |
| Device registration (before) | Authenticated API + installation GUID; no opaque QR token |
| LocalStore | v9; **file per (user, org, product)** — already isolates orgs |

## Delivered

- Typed QR envelope: Personal / Organization / PosDeviceRegistration
- Business QR + resolve APIs
- Opaque short-lived one-time POS device registration tokens
- MAUI My QR / Business QR / device show+scan
- Org-switch clears SellingMode, BranchId/PosDeviceId; SaleCart clears on org change
- Leak-prevention unit tests

## Gates

| Gate | Result |
|------|--------|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Deferred

- Payment / marketplace QR
- Automatic Personal↔Customer merges
- LocalStore schema bump (not required)

Ownership transfer UI was delivered in [P25-WP09](P25-WP09-organization-ownership-transfer.md).
