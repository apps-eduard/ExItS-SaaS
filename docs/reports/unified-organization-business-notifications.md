# Unified Organization business notifications (Connected Supplier)

Date: 2026-08-15  
Starting SHA: `2950b7d888ef2d67b28a9ce23a7581c3e2218269`  
Feature SHA: `bcc84f96` (Read-on-open + Connected buyers)  
Test SHA: `f76b7905`  
Docs SHA: `196c9171`  
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
Format: hide at 0, exact 1–99, `99+` above.

## Notification tap → Read

Opening a notification calls `MarkOrganizationNotificationRead` immediately, updates the local list (`IsRead=true`), refreshes the bell, removes the row from Unread, and keeps it under All. **Unread ≠ Pending Action** — a Requested row can be read while the relationship stays Pending with Accept/Decline available.

Deep links: buyer Accepted/Declined → Suppliers; supplier Accept success → Connected buyers; customer-link → Customers.

## Connected buyers (supplier-side)

Active relationships where current Organization is `SupplierOrganizationId`. Not Customers. Explicit “Add as customer” deferred.

## MAUI / Org Web centers

- MAUI `/org/notifications` (compat `/org/customer-link-notifications`)
- Org Web `/notifications`
- Unread | All filters; Accept/Decline via Connected Supplier APIs when Pending
- MAUI `/suppliers/connected/buyers`; Org Web `/suppliers/buyers` (+ nav under People → Suppliers)

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
