# P19 — User QR / Public ExItS ID Linking

| Field | Value |
|---|---|
| Status | **Code Complete** · Phone **Retest** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Spec | [public-user-id-and-qr.md](../specs/identity/public-user-id-and-qr.md) |
| Commits | `076512e` (Platform) · `7354fba` (MAUI) · `9ef5a53` (docs) · `994a36a` (My QR PlatformSession fix — §8) |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-05 |

## 1. Objective

Ship a secure universal ExItS User QR / Public ID for Personal Utang, organization customers, and staff invitation — with confirmation before any linking action.

## 2. Design / root cause addressed

Users needed a shareable identifier that is safe to display and scan without leaking UUID/email/phone/tokens, and without auto-creating memberships or roles. Platform now owns an immutable `public_user_id` (`EX-####-####`) with exact-match resolve only.

## 3. Schema / API

- Migration `20260804204601_AddPlatformUserPublicUserId` (+ PostgreSQL backfill)
- Unique filtered index on `platform.platform_users.public_user_id`
- `GET /api/v1/me/public-identity` — assigns missing IDs on demand via `GetOrAssignPublicIdentity`
- `POST /api/v1/users/resolve-public-id` (rate-limited, audited)
- Scope guard allows these paths for Personal and Organization sessions
- Platform auth scheme is **PlatformSession** (not Bearer access tokens)

## 4. MAUI

- My QR (`/personal/my-qr`) — local QRCoder PNG; Copy ID; Share when available; refresh visual only
- Resolve (`/personal/resolve-user`) — manual entry + confirm (camera deferred)
- Wired into More, Profile, People, Lent/Borrowed, Customers, Sale checkout, Staff invite, Org essentials
- Client base URL: Platform `:8091` (`PosApi:BaseUrl`), not POS `:8092`

## 5. Tests

- Unit: ID normalize/QR payload, assign immutability, exact resolve / not-found / self
- Maui guards: More → My QR; resolve confirm; API client paths; session-handler coverage for public-identity
- ApiClient: `PlatformSessionHeaderHandler` attaches PlatformSession for `/me/public-identity` and resolve

## 6. Phone checklist (Retest — do not mark Device Verified)

- [ ] More → My QR shows loading then display name, QR, ExItS ID; Copy ID works
- [ ] QR payload decodes to exactly `exits://user/v1/{PublicUserId}` matching on-screen ID
- [ ] Refresh QR visual does **not** change ExItS ID
- [ ] People → Add by ExItS ID → confirm → contact created (not silent debt link)
- [ ] Customers → Add by ExItS ID → confirm → save local customer (no membership/role)
- [ ] Sale checkout → Add by ExItS ID → confirm → search/select only
- [ ] Staff invite → resolve prefill → email still required → accept still required
- [ ] Self-ID blocked on confirm
- [ ] Unknown / malformed ID shows generic failure
- [ ] Offline / session restore still reaches My QR after re-auth
- [ ] Camera unavailable → manual ID fallback message shown
- [ ] Distinct errors for session expired vs network vs render failure

## 7. Explicit non-claims

- Phase 19 remains **Open**
- **Not Device Verified** until physical-phone confirmation
- Camera scan deferred
- Staff invite-by-ID-without-email deferred

## 8. My QR load failure fix (2026-08-05)

### Root cause

`GET /api/v1/me/public-identity` requires **PlatformSession** authentication. MAUI `PlatformSessionHeaderHandler` previously attached PlatformSession only for `/api/v1/personal/*`, `/organizations/*`, `/commercial/*`, and most `/platform/*` routes — **not** for `/api/v1/me/public-identity`.

The page therefore sent `Authorization: Bearer <accessToken>`. Platform rejected Bearer for that endpoint (**401**), and My QR collapsed the failure into a generic “Could not load QR” message.

Local Validation evidence (Personal user `kath`):

| Auth | Result |
|---|---|
| `PlatformSession` | **200** — `EX-2519-6181` / `exits://user/v1/EX-2519-6181` |
| `Bearer` only | **401** |

Migration `AddPlatformUserPublicUserId` is present (IDs returned; on-demand assign remains available).

### Fix

- Attach PlatformSession for `/api/v1/me/public-identity` and `/api/v1/users/resolve-public-id`
- Differentiate 401 / 403 / 404 / network / unsafe payload / QR-render failures in `PersonalMyQr.razor`
- Safer local renderer (`TryToPngDataUrl`) + share action when supported
