# P19 — User QR / Public ExItS ID Linking

| Field | Value |
|---|---|
| Status | **Code Complete** · Phone **Retest** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Spec | [public-user-id-and-qr.md](../specs/identity/public-user-id-and-qr.md) |
| Commits | `076512e` (Platform) · `7354fba` (MAUI) · `9ef5a53` (docs) |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Ship a secure universal ExItS User QR / Public ID for Personal Utang, organization customers, and staff invitation — with confirmation before any linking action.

## 2. Design / root cause addressed

Users needed a shareable identifier that is safe to display and scan without leaking UUID/email/phone/tokens, and without auto-creating memberships or roles. Platform now owns an immutable `public_user_id` (`EX-####-####`) with exact-match resolve only.

## 3. Schema / API

- Migration `20260804204601_AddPlatformUserPublicUserId` (+ PostgreSQL backfill)
- Unique filtered index on `platform.platform_users.public_user_id`
- `GET /api/v1/me/public-identity`
- `POST /api/v1/users/resolve-public-id` (rate-limited, audited)
- Scope guard allows these paths for Personal and Organization sessions

## 4. MAUI

- My QR (`/personal/my-qr`) — local QRCoder PNG; Copy ID; refresh visual only
- Resolve (`/personal/resolve-user`) — manual entry + confirm (camera deferred)
- Wired into More, Profile, People, Lent/Borrowed, Customers, Sale checkout, Staff invite, Org essentials

## 5. Tests

- Unit: ID normalize/QR payload, assign immutability, exact resolve / not-found / self
- Maui guards: More → My QR; resolve confirm; API client paths
- Broader Platform / Maui / integration / security suites re-run for this delivery

## 6. Phone checklist (Retest — do not mark Device Verified)

- [ ] More → My QR shows display name, QR, ExItS ID; Copy ID works
- [ ] Refresh QR visual does **not** change ExItS ID
- [ ] People → Add by ExItS ID → confirm → contact created (not silent debt link)
- [ ] Customers → Add by ExItS ID → confirm → save local customer (no membership/role)
- [ ] Sale checkout → Add by ExItS ID → confirm → search/select only
- [ ] Staff invite → resolve prefill → email still required → accept still required
- [ ] Self-ID blocked on confirm
- [ ] Unknown / malformed ID shows generic failure
- [ ] Offline / session restore still reaches My QR after re-auth
- [ ] Camera unavailable → manual ID fallback message shown

## 7. Explicit non-claims

- Phase 19 remains **Open**
- **Not Device Verified** until physical-phone confirmation
- Camera scan deferred
- Staff invite-by-ID-without-email deferred
