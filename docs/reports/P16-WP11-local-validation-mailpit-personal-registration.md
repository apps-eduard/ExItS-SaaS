# P16-WP11 Defect Log — Local Validation Mailpit + Personal Account registration

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-08-02

## Title

Local Validation had no email catcher path for Personal Account public registration and activation

## Gaps closed

### Mailpit (Local Validation only)

- `mailpit` service in `compose.local-validation.yaml` (UI host port 8025, SMTP 1025)
- `Start-LocalValidation.ps1` starts Mailpit and sets `PlatformEmail__*` on Platform API
- SMTP sink delivers auth outbound messages when `PlatformEmail` is configured; otherwise null sink (tokens still issued)

### Public Personal Account registration

| Step | Behavior |
|---|---|
| Login → Register | Public Admin page `/admin/register` |
| Submit | Creates User Identity + exclusive Personal profile + `PendingVerification` |
| Email | Verification token emailed (Mailpit in Local Validation) |
| Activate | `/admin/activate-account?token=…` → set password → email verified → `Active` |
| Sign in | Normal password login (Pending Verification blocked until activation) |

### APIs

- `POST /api/v1/platform/auth/register` (anonymous, rate-limited)
- `POST /api/v1/platform/auth/activate-account` (anonymous, rate-limited)

### Domain / audit

- `AccountStatus.PendingVerification`
- Transitions: Pending Verification → Active | Deactivated
- Audit: `platform.personal.registration_started`, `platform.personal.registration_activated`

## Out of scope (unchanged)

- Phase 17 / P16-WP12
- Production SMTP vendor selection beyond configurable `PlatformEmail`
- Unrelated POS, Personal Utang, Organization customer features

## Validation checklist

- [ ] Register from login creates Pending Verification Personal Account
- [ ] Message appears in Mailpit at http://localhost:8025
- [ ] Activation link sets password and activates
- [ ] Sign-in works after activation; blocked before activation
