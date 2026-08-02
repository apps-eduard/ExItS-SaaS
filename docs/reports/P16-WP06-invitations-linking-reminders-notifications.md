# P16-WP06 — Invitations, Linking, Reminders, and Notifications

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `42e7bca260bb8e00f57af65f7e2fb9313d3f3ecb` (after P16-WP05 tip-hash) |
| Feature commit | *(recorded after commit)* |
| Date | 2026-08-02 |

## Scope completed

- Personal Utang invitation lifecycle: Pending, Accepted, Declined, Revoked, Expired.
- Explicit participant acceptance via one-time token (SHA-256 hash stored; plaintext shown once).
- Anti-enumeration: no user lookup by name/email/phone on invite; invalid/revoked tokens return the same generic `404 application.personal.utang_invitation.not_found`.
- On accept: link contact → user, authorize shared relationship view; **no** Organization membership; **no** product role.
- One-time and scheduled reminder foundation (`OneTime`, `OnDueDate`, `BeforeDueDate`, `RecurringOverdue`) with due-list endpoint.
- In-app notifications + pluggable/null push sink (`NullPersonalPushNotificationSink`).
- Notification preferences reuse WP04 `PersonalAccountSettings` (email/push/in-app/reminder flags gate delivery).
- Reminder rate limits: max 3 deliveries / relationship / 24h; min 1 hour between deliveries → `429`.
- Delivery audit records with minimized preview text (no balances/amounts).
- EF migration `AddPersonalUtangInvitationsAndNotifications`.
- Unit + integration regression coverage.

## Files changed (high level)

- Domain: invitation, reminder, in-app notification, delivery audit; `PersonalContact.LinkUser`; `PersonalDebtRelationship.AuthorizeLinkedParticipant`
- Application: invitation/reminder/notification use cases; repository contracts; null push sink
- Infrastructure: records, repositories, DbContext, migration `AddPersonalUtangInvitationsAndNotifications`
- API: Personal endpoints under `/api/v1/personal/...`; DI; `PlatformApiResults` 404/409/429 mappings
- Tests: domain unit tests; `ApiPersonalUtangInvitationTests`

## Schema and migration changes

Migration `AddPersonalUtangInvitationsAndNotifications`:

| Table | Purpose |
|---|---|
| `platform.personal_utang_invitations` | Invitation lifecycle + token hash |
| `platform.personal_reminders` | One-time / scheduled reminders |
| `platform.personal_in_app_notifications` | In-app notification inbox |
| `platform.personal_notification_deliveries` | Delivery audit (InApp/Push/Email) |

WP03 org preferences, WP04 settings, WP05 utang tables remain intact.

## API routes added

| Method | Route | Notes |
|---|---|---|
| POST | `/api/v1/personal/utang/relationships/{id}/invitations` | Create (returns `acceptToken` once) |
| GET | `/api/v1/personal/utang/invitations` | Sent + inbox (by authenticated email) |
| POST | `/api/v1/personal/utang/invitations/accept` | Explicit accept + link |
| POST | `/api/v1/personal/utang/invitations/decline` | Explicit decline by token |
| POST | `/api/v1/personal/utang/invitations/{id}/resend` | Rotate token |
| POST | `/api/v1/personal/utang/invitations/{id}/revoke` | Inviter revoke |
| POST | `/api/v1/personal/utang/relationships/{id}/reminders` | Create reminder |
| GET | `/api/v1/personal/utang/relationships/{id}/reminders` | List reminders |
| GET | `/api/v1/personal/utang/reminders/due` | Scheduler foundation |
| POST | `/api/v1/personal/utang/reminders/{id}/deliver` | Deliver + audit (rate-limited) |
| POST | `/api/v1/personal/utang/reminders/{id}/cancel` | Cancel reminder |
| GET | `/api/v1/personal/utang/delivery-audit` | Delivery audit |
| GET | `/api/v1/personal/notifications` | In-app inbox |
| POST | `/api/v1/personal/notifications/{id}/read` | Mark read |

## Exit criteria

| Criterion | Evidence |
|---|---|
| No silent matching by name/email/phone | Invite create never looks up users; link only after token accept |
| Accept creates no Organization membership | Accept DTO `createdOrganizationMembership: false`; no membership APIs called |
| Accept grants no product role | Accept DTO `grantedProductRole: false` |
| Sensitive values minimized in previews | `PersonalReminder.BuildMinimizedPreview`; integration asserts no balance amounts |
| Repeated reminders rate-limited | Domain + API `429 application.personal.reminder.rate_limited` |
| Regression suite passes | Unit 327 / Integration 162 |

## Audit coverage

- `platform.personal.utang_invitation.created|resent|revoked|accepted|declined|expired`
- `platform.personal.contact.linked`
- `platform.personal.utang_participant.authorized`
- `platform.personal.reminder.created|delivered|cancelled`

## Seed-data changes

None. Existing Phase 16 personal utang seed unchanged (non-Production only).

## Tests added

- `PersonalUtangDomainTests` — invite accept/link, decline/revoke, rate limit, minimized preview
- `ApiPersonalUtangInvitationTests` — accept + shared view, rate limit + preview, revoke anti-enumeration

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **327 passed**, 0 failed, 0 skipped
- Platform integration: **162 passed**, 0 failed, 0 skipped
- Build: Platform API Release — 0 warnings, 0 errors

## Explicit exclusions

- Organization Staff Invitations / Customer Link Requests (P16-WP07)
- External email/SMS/push vendor delivery
- Mobile navigation UI
- Business Utang migration (P16-WP08)
- WP03/WP04/WP05 SHAs unchanged

## Explicit next work package

**P16-WP07** — Organization Staff and Customer Separation.

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**. Push sink is null (no vendor).
