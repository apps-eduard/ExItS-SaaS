# Personal / Organization identity isolation

**Status:** Implemented  
**Starting SHA:** `d8e43575d103fc19cf42c5cb33d2c28264a0d4b6`  
**Feature SHAs:** `3e1515b972e4fe2a99c2baeb26994c465bdd0edd` (feat), `8f3875f5` (docs; hash-record commit follows)  
**Related:** [architecture boundaries](../architecture/personal-organization-identity-boundaries.md) · [QR spec](../specs/identity/public-user-id-and-qr.md)

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

- Full ownership-transfer UI
- Payment / marketplace QR
- Automatic Personal↔Customer merges
- LocalStore schema bump (not required)
