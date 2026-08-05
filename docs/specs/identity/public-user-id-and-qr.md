# Public User ID and QR

[Architecture scopes](../../architecture/saas-scopes-users-boundaries-navigation.md) | [Auth architecture](../../engineering/authentication-architecture.md) | [Client experience](../../architecture/client-experience-boundaries.md) | [P19 report](../../reports/P19-user-qr-public-id-linking.md)

**Status:** Implemented (Platform + MAUI) · Phase 19 **Open** · Phone scenarios **Retest** · **Not Device Verified**

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

```text
exits://user/v1/EX-4827-1936
```

**Must never encode:** access tokens, email, phone, internal UUID, roles, balances, organization data, or Personal Utang balances.

Visual QR may be regenerated; the public ID itself cannot change.

---

## 5. Platform APIs

| Method | Path | Notes |
|---|---|---|
| `GET` | `/api/v1/me/public-identity` | Assigns ID if missing; returns `{ publicUserId, qrPayload, displayName, status }` |
| `POST` | `/api/v1/users/resolve-public-id` | Body `{ publicUserIdOrQrPayload, purpose? }` · exact match only · rate limit `public-id-resolve` |

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

- creates a relationship
- adds a customer
- joins an organization
- assigns a role

Always show identity confirmation before the final action.

| Flow | After confirm |
|---|---|
| Personal Utang People | May create a **local contact**; debt linking still follows existing invitation/acceptance rules |
| Org / POS customer | Creates/links **organization-local customer** only after explicit save; no membership / POS role |
| Staff invitation | Prefills display name; **email + invite send** still required; user must accept |
| Sale customer picker | Prefills search / select existing org customer; does not invent membership |

---

## 7. MAUI surfaces

- **My QR Code:** Personal More, Profile, Organization essentials menu → `/personal/my-qr`
- **Resolve:** `/personal/resolve-user?purpose=…&return=…` (manual ID; camera deferred in this build)
- Contextual entry: People, I Lent / I Borrowed, Customers, Customer create, Sale checkout, Staff invite

Recommended More order: My QR → Invitations → Profile → Settings → Explore POS → Sign out.

---

## 8. Deferred

- Camera QR scan (manual ID is the required fallback)
- Invite-by-public-ID without email (staff invite still requires email)
- Deep user-to-user sharing beyond contact/customer/invite prefill
- Native share sheet for QR image on all platforms

---

## 9. Security release rules

- Platform owns public identity
- Release must not use Development authentication bypasses
- Log lookup purpose and actor; never log QR image bytes or sensitive profile dumps
