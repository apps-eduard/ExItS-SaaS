# P25-WP09 — Organization Ownership Transfer

| Field | Value |
|---|---|
| Phase | **P25** — Organization Web / Identity / Organization Management |
| Work package | **P25-WP09** |
| Status | **Code Complete / Owner Validation Pending** |
| Phase status | **OPEN** — do not close Phase 25; owner device/browser validation pending |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Starting SHA (pre-feature) | `5fd997e058b4b2722ef5801cbc977703857e5cef` |
| Feature SHA | `67bd59bd010eaa13fe23a32546ca2b6774fee16a` |
| Docs SHAs | `f20b6dc3e60b90faa530816f1eef0b9561181249`, `5f51e35a0ff294d22af016b0467bba531e29dd98` |
| Alias | [organization-ownership-transfer.md](organization-ownership-transfer.md) |
| Engineering | [organization-ownership-transfer.md](../engineering/organization-ownership-transfer.md) |

## Delivered capability

- Aggregate `OrganizationOwnershipTransfer` (not staff `OrganizationInvitation`).
- Request / cancel / accept / decline / expire (7-day Pending; one Pending per org).
- Atomic accept: former Owner **removed**; recipient sole Owner; Organization identity/data preserved.
- Personal QR / EX ID targeting; Business/Device QR rejected.
- Org Web initiation + MAUI Personal accept/decline.
- Session invalidation for former owner (selected org + access-token org binding + product access revoke).
- Multi-org ownership preserved for non-transferred organizations.

## Architecture audit (summary)

| Area | Finding |
|------|---------|
| Owner model | `OrganizationMembership.Role = OrganizationOwner` on Personal identity |
| Single-owner | `EnsureSingleOrganizationOwnerSeatAsync` + accept post-condition |
| Membership | One current membership per (user, org) for Active/Suspended |
| Invitations | Staff invites **not** reused (`@ORG` staff; reject Owner) |
| Sessions | Clear selected org + access-token org binding on former-owner remove |
| Subscriptions | Org-owned entitlement stays with Organization |
| POS roles | Platform Owner ≠ POS-local cashier/Owner; not auto-granted |

## Persistence

- Migration: `AddOrganizationOwnershipTransfers` (`20260814192519`)
- LocalStore version: **unchanged**

## Explicit exclusions / deferred

- Keep-former-owner-as-admin option
- Business sale / payment / escrow / legal / marketplace / co-owners / equity
- Billing payment-method migration to new Owner
- Auto-grant POS product-local roles
- MAUI initiation UI
- Email/SMS notification infrastructure
- BIR / tax-document compliance implementation (future roadmap only)

## Compliance future-proofing (not implemented)

Ownership transfer preserves Organization as the stable subject:

- Organization tax/compliance settings remain Organization-owned when introduced
- Future compliance entitlement must be Organization-scoped
- Historical tax/compliance documents must never rewrite on Owner change
- Transaction Summary vs Tax Document boundary is **deferred** (future Sales Documents & Compliance Readiness)

## Tests (recorded at feature commit)

| Suite | Result |
|-------|--------|
| Platform.UnitTests | Passed **895** (incl. **13** ownership-transfer) |
| Platform.IntegrationTests `~OwnershipTransfer` | Passed **1** |
| PinoyBusinessPOS.UnitTests | Passed **728** |
| ApiClient.Tests | Passed **51** |
| ArchitectureTests | Failed **4** / Passed **163** — **PRE-EXISTING** |
| Maui.Tests | Failed **3** / Passed **407** — **PRE-EXISTING / cash leftovers** |

## Gates

| Gate | Result |
|------|--------|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Next

Owner validation for Phase 25 WP06–WP09. **Do not create P25-WP10 closeout** until the owner explicitly closes Phase 25 after real-world validation.

## Privacy Impact

| Field | Value |
|---|---|
| Personal data changed? | **Yes** |
| Data subjects | Current Organization Owner; recipient Personal user |
| Purpose | Organization ownership handoff and audit/security of actor history |
| Data categories | ORGANIZATION INTERNAL (org id, transfer status, actor user ids, timestamps) |
| New exposure/access | Owner initiate + Personal QR accept; former Owner access revoked on accept |
| Retention impact | Historical ActorUserId retained for audit — **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION** |
| Offline/local impact | None for transfer records (Platform DB) |
| Third-party/vendor impact | None (email/SMS notifications deferred) |
| Security controls | Owner-only initiate; Personal QR target; no Personal profile copy into org |
| PIA/ROPA update required? | **Yes** (P21-WP11) |
| Legal/DPO review required? | **Yes** |

See [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md). **NPC compliance NOT CLAIMED.**
