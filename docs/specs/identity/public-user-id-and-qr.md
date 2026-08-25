# Public User ID and QR

[Architecture scopes](../../architecture/saas-scopes-users-boundaries-navigation.md) | [Auth architecture](../../engineering/authentication-architecture.md) | [Client experience](../../architecture/client-experience-boundaries.md) | [P19 report](../../reports/P19-user-qr-public-id-linking.md)

**Status:** Implemented (Platform + MAUI + **React Personal My QR / Business QR / customer-link entry**) · Phase 19 **Open** · Phone scenarios **Retest** · React camera still-image path (not live viewfinder Device Verified)

---

## 1. Purpose

Every authenticated ExItS user has:

- an immutable **public ExItS ID** (human-readable)
- a **scannable QR** encoding only a versioned public reference
- **manual ID entry** when camera scanning is unavailable

Primary workflows: Personal Utang people/linking, organization POS customers, organization staff invitation, future user-to-user sharing.

---

## 2. Ownership boundaries

| Concern | Owner | Notes |
|---|---|---|
| Public ExItS ID generation / storage | **Platform** | Column `platform.platform_users.public_user_id` |
| Exact-match resolve API | **Platform** | Rate-limited + audited |
| Product-local customer / contact rows | **Product / Personal Utang** | Store Platform User ID as **external reference only** when needed |
| Membership / POS roles | Existing invitation / assignment flows | **Never** granted by scan alone |

No cross-product database FK. Products must not query Platform tables directly.

---

## 3. ID format

- Canonical: `EX-4827-1936` (case-insensitive; stored uppercase)
- Unique; assigned once; immutable after assignment
- Separate from database UUID, email, phone, username
- Collision handling: crypto/random generation with unique index + retry (and migration backfill)

---

## 4. QR payload contract

Canonical personal form (emit):

```text
exits://qr/v1/personal/EX-4827-1936
```

Legacy personal form (still accepted on parse):

```text
exits://user/v1/EX-4827-1936
```

Scoped envelope types (v1):

| Purpose | Payload |
|---|---|
| Personal (PlatformUser / Personal identity) | `exits://qr/v1/personal/{EX-####-####}` |
| Organization | `exits://qr/v1/organization/{ORG######}` |
| POS device registration | `exits://qr/v1/pos-device-registration/{opaqueToken}` |

**Must never encode:** access tokens (except opaque one-time device registration tokens), email, phone, internal UUID, roles, balances, or Personal Utang balances.

Visual QR may be regenerated; the public ID itself cannot change. Personal subjects remain keyed by `PlatformUserId` / `PublicUserId` (no parallel PersonalAccountId).

### Purpose matrix (do not silently reinterpret)

| Flow | Personal | Organization (Business) | Device registration |
|---|---|---|---|
| Sale customer selection | Allow (buyer) | Allow (buyer) | Reject |
| Connected supplier connect | Reject | Allow | Reject |
| POS device registration | Reject | Reject | Allow |
| Personal people / contacts | Allow | Reject / explicit business action | Reject |

POS sale buyer parties: seller organization owns the sale; scanned Personal/Business QR is counterparty only — see [sales-buyer-party-model.md](../../engineering/sales-buyer-party-model.md).

---

## 5. Platform APIs

| Method | Path | Notes |
|---|---|---|
| `GET` | `/api/v1/me/public-identity` | Assigns ID if missing; returns `{ publicUserId, qrPayload, displayName, status }` |
| `POST` | `/api/v1/users/resolve-public-id` | Body `{ publicUserIdOrQrPayload, purpose? }` · exact match only · rate limit `public-id-resolve` |
| `GET` | `/api/v1/organizations/{organizationId}/public-identity` | Member-only; `{ publicOrganizationId, qrPayload, displayName }` |
| `POST` | `/api/v1/organizations/resolve-public-id` | Body `{ publicOrganizationIdOrQrPayload, purpose? }` · exact match · rate-limited · audited · no membership grant |
| `POST` | `/api/v1/qr/resolve` | Body `{ payload, expectedPurpose? }` · typed dispatcher for scanners |
| `POST` | `/api/v1/platform/organizations/{organizationId}/pos-devices/registration-tokens` | Create opaque 15-minute device registration token + QR |
| `POST` | `/api/v1/platform/organizations/{organizationId}/pos-devices/registration-tokens/redeem` | Authenticated org member redeems token into a PosDevice |
| `GET` | `/api/v1/platform/organizations/{organizationId}/pos-devices/registration-tokens/{tokenId}` | Metadata (expires in X minutes) |

**Auth:** These routes use Platform **session** authentication (`Authorization: PlatformSession …`). MAUI must attach the Platform session token (not only a POS Bearer access token). Bearer-only calls return **401**.

Resolve response (minimal): `publicUserId`, `userIdentityId`, `displayName`, masked email (policy), `status`, `isSelf`.

Enumeration protections:

- exact ID / QR payload only (no partial search)
- generic not-found for missing / non-active
- rate limiting + audit (`purpose`, actor; never QR image or full profile dump)

Allowed for any authenticated account class (Personal and Organization sessions).

---

## 6. Confirmation and acceptance rules

Scan or enter ID **never** automatically:

- creates Organization membership
- grants staff or product roles
- activates a Personal↔merchant **LinkedCustomerAppUser**

### Existing ExItS Personal user (primary path)

1. Resolve Public ExItS ID / QR (exact match).
2. Confirm identity in UI (display name, public ID, policy-masked fields only).
3. Save organization-local customer (`BusinessCustomer` + POS correlation).
4. Create **PENDING** `CustomerLinkRequest` targeted to that Personal identity.
5. Personal in-app notification → Accept or Decline.
6. **Accept** creates/activates `LinkedCustomerAppUser` only.
7. **Decline** keeps merchant customer; no active Personal link.

Email invitation/token is **fallback**, not required for the normal existing-user flow.

See [P24 ExItS-ID customer-link consent](../../reports/P24-exits-id-customer-link-consent-flow.md).

### Organization customer create (POS)

Organization MAUI customer create may resolve ExItS ID, confirm, save local customer, and automatically create the pending Personal link request. Resolving an ID alone is **not** acceptance.

Always show identity confirmation before the final action.

| Flow | After confirm |
|---|---|
| Personal Utang People | May create a **local contact**; debt linking still follows existing invitation/acceptance rules |
| Org / POS customer | Creates organization-local customer + **pending** Personal link request after explicit save; no membership / POS role; link activates only after Personal Accept |
| Staff invitation | Prefills display name; **email + invite send** still required; user must accept |
| Sale customer picker | Prefills search / select existing org customer; does not invent membership |

---

## 7. MAUI surfaces

- **My QR:** Personal More → `/personal/my-qr` (wording: “My QR” / “Use this to connect with me on ExItS.”)
- **Business QR:** Org essentials → `/org/business-qr` (org name + QR; “Use this to identify or connect with this business.”)
- **Resolve Personal:** `/personal/resolve-user?purpose=…&return=…` (scan or manual ID; expected Personal purpose guarded locally)
- **Device registration:** Org devices “Show registration code”; `/devices/register` “Scan registration code” → `ResolveQr(expectedPurpose=PosDeviceRegistration)` → redeem
- Contextual entry: People, I Lent / I Borrowed, Customers, Customer create, Sale checkout, Staff invite
- **Customer link requests:** `/personal/customer-link-requests` (Accept/Decline pending merchant links)
- **Notifications:** `/personal/notifications`

## 7b. React surfaces (parity package)

- **My QR:** Personal More → `/personal/my-qr` — visual QR + Copy/Share; canonical Personal envelope
- **Business QR:** Org essentials → `/org/business-qr`
- **Customer create:** Scan QR / Enter ExItS ID → confirm → `POST .../customers/with-personal-link` → POS customer with `platformBusinessCustomerId`; Personal Accept still required
- **Customer-link inbox:** `/personal/customer-links` (existing)
- **Linked merchants:** `/personal/linked-merchants` (existing RMAP-22F / RMAP-19)
- **Not in React:** POS device-registration QR/code UX (intentionally removed); RMAP-B04 purchase history; RMAP-B05 public business landing

Recommended More order: My QR → Invitations → Profile → Settings → Explore POS → Sign out.

Shared client guard: `ExItsQrPurposeGuard` validates `expectedPurpose` after decode; otherwise routes by envelope type.

See also [personal-organization-identity-boundaries.md](../../architecture/personal-organization-identity-boundaries.md).

---

## 8. Deferred

- Invite-by-public-ID without email (staff invite still requires email)
- Deep user-to-user sharing beyond contact/customer/invite prefill
- Native share sheet for QR image on all platforms
- Broad organization-wide broadcast of customer-link responses (responses notify the initiating user)
- Physical device verification of camera QR flows

---

## 9. Security release rules

- Platform owns public identity
- Release must not use Development authentication bypasses
- Log lookup purpose and actor; never log QR image bytes or sensitive profile dumps
- Device registration tokens are opaque, hash-stored, single-use, org-scoped; never encode email/phone/Bearer tokens in Personal/Organization QR
