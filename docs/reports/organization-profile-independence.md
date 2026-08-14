# Organization profile independence

**Status:** Implemented  
**Starting SHA:** `710426a97bff97d58ca3dc5b1e8a0f386f77bc31`  
**Feature SHAs:** _(filled after push)_  
**Related:** [engineering note](../engineering/organization-profile-independence.md) · [identity boundaries](../architecture/personal-organization-identity-boundaries.md)

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
