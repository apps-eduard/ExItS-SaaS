# Unified Organization business notifications (Connected Supplier)

Date: 2026-08-15  
Starting SHA: `22180c41b7cef74571d3804ca344fb9704519f5a`  
Phase markers: Phase 21 / 25 / 26 remain **OPEN** — Owner Validation Pending.

## Feature

Unified Organization business notification center across MAUI and Organization Web, starting with Connected Supplier connection requests. Customer-link notifications remain in the same inbox.

## Existing notification architecture

Platform `OrganizationInAppNotification` in `platform.organization_in_app_notifications` (RelatedType + RelatedId + IsRead). No second inbox table.

## Notification backend

- Types: `SupplierConnectionRequested` / `SupplierConnectionAccepted` / `SupplierConnectionDeclined` (+ existing customer-link types)
- Publish: `PublishOrganizationBusinessNotification` → Owners + Administrators of recipient org
- Resolve: `MarkRelatedOrganizationNotificationsRead`
- APIs: existing list/mark-read; new `POST .../business-notifications` and `POST .../notifications/related/read`
- Inbox auth: Owner or Administrator (Manager); Cashiers denied
- POS → Platform: `PlatformOrganizationBusinessNotificationClient` (session forward, best-effort)

## Supplier request notification

On `RequestConnection` success: publish Requested to supplier org with public buyer display name / ORG###### only.

## Bell unread count

Server list `Count(!IsRead)` for selected Organization — MAUI `ShellNotificationBell` and Org Web `UnreadNotificationCount`.

## MAUI / Org Web centers

- MAUI `/org/notifications` (compat `/org/customer-link-notifications`)
- Org Web `/notifications`
- Unread | All filters; Accept/Decline via Connected Supplier APIs when Pending

## Privacy impact

Public organization display name and public organization id only. No owner personal email/phone/payment data.

## Migration(s)

None — reused existing Platform notification schema.

## LocalStore

Unchanged.

## Verification

Browser Verified: **NO**  
Device Verified: **NO**  
Production Ready: **NO**
