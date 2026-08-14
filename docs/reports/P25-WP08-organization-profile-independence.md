# P25-WP08 — Organization profile independence + multi-org ownership

| Field | Value |
|---|---|
| Phase | **P25** |
| Work package | **P25-WP08** |
| Status | **Code Complete / Owner Validation Pending** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Starting SHA | `710426a97bff97d58ca3dc5b1e8a0f386f77bc31` |
| Feature SHAs | `a3dfda282b43b6a443bee4bf6477e435b0ddc3dc`, `5fd997e058b4b2722ef5801cbc977703857e5cef` |
| Alias | [organization-profile-independence.md](organization-profile-independence.md) |
| Related | [engineering note](../engineering/organization-profile-independence.md) · [identity boundaries](../architecture/personal-organization-identity-boundaries.md) |

## Delivered

- `StartBusinessRequest` carries `UseMyContactDetails` plus optional Contact*/Address* fields (Platform Application, Admin DTO, POS client DTO).
- Start a Business seeds `OrganizationProfile` once after org create (copy Personal email/phone when requested; explicit fields win; no live link).
- MAUI and Personal.Web Start a Business UI: contact section with one-time prefill checkbox.
- MAUI Org profile edits full business contact via existing update organization API.
- Unit tests: copy-not-link isolation, multi-org owner, one-owner-per-org, Org A≠Org B profile, public identity contact leak prevention.
- `PersonalProfileDto` exposes optional `Phone` for Start a Business prefill only.

## Explicit exclusions

- No DB migration
- No live Personal↔Organization sync
- No multi-owner per organization (MVP)
- Receipt header/footer still from POS operational setup, not Platform org profile
- Public QR remains DisplayName + PublicOrganizationId only

## Gates

| Gate | Result |
|------|--------|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Privacy Impact

| Field | Value |
|---|---|
| Personal data changed? | **Yes** (organization contact/profile; optional one-time copy from Personal) |
| Data subjects | Organization Owners; Personal users starting a business |
| Purpose | Independent organization business identity / contact profile; multi-org ownership |
| Data categories | ORGANIZATION INTERNAL (profile contact); PUBLIC BUSINESS IDENTITY remains DisplayName + PublicOrganizationId only |
| New exposure/access | Org profile APIs/UI; Start a Business one-time prefill — **no live Personal sync** |
| Retention impact | Organization lifetime; **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION** |
| Offline/local impact | None (Platform profile; POS receipts still from operational setup) |
| Third-party/vendor impact | None new |
| Security controls | Copy-not-link tests; public identity contact leak prevention |
| PIA/ROPA update required? | **Yes** (P21-WP11) |
| Legal/DPO review required? | **Yes** |

See [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md). **NPC compliance NOT CLAIMED.**
