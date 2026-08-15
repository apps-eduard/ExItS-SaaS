# Connected supplier connection-request lifecycle visibility

Date: 2026-08-15  
Starting SHA: `fe2bcc5d79300462b19938a8c1f84edc2080ce2c`  
Feature SHA: `1933fc1ffbe9b0d79c31874efb9f73688f72c907`  
Phase markers: Phase 21 / 25 / 26 remain **OPEN** — Owner Validation Pending.

## Bug

Connected ExItS supplier connection request appeared to succeed for buyer Organization A, but:

- Organization A saw no Pending supplier relationship
- Organization B saw no incoming request
- Organization bell showed nothing related

## Exact root cause

1. **Buyer visibility:** `RequestConnection` persisted `ConnectedSupplierRelationship` (`Pending`) but never created/attached a buyer `Supplier` master (`AttachConnectedRelationship` unused). `SuppliersList` lists `Supplier` masters only, so the Pending relationship was invisible.
2. **Supplier visibility:** List API `view=supplier` already returned incoming Pending relationships, but no MAUI/Org Web UI called it or exposed Accept/Decline for **connection** requests (only PO inbox at `/connected-suppliers/incoming`).
3. **Bell:** Organization in-app notifications exist for customer-link only; no connected-supplier notification write path from POS → Platform. Not fabricated; deferred.

Backend buyer/supplier query direction was already correct (`BuyerOrganizationId` / `SupplierOrganizationId`).

## Fix summary

- On request: create buyer `Supplier`, attach relationship, store public counterparty name/ORG###### snapshots (migration `AddConnectedSupplierCounterpartySnapshots`)
- MAUI: `/suppliers/connected/requests` incoming Accept/Decline; list badge + Pending/Declined help text; catalog/PO actions only when Active
- Org Web: `/suppliers`, `/suppliers/requests`, `/suppliers/connect` + nav
- Plain-language duplicate/self/unavailable errors
- Focused unit + UI guard tests

## Flow (authoritative)

Send request → Pending visible to buyer → Incoming visible to supplier → Accept/Decline → Active/Declined visible to buyer → Active enables exposed catalog/PO → Goods Receipt only increases stock

## Notification / bell

**Deferred** — reuse Platform `OrganizationInAppNotification` later; surface requests in Suppliers UI now.

## Privacy impact

Only public business display name and public organization id (`ORG######`) are snapshotted on the relationship for the counterparty inbox. No owner Personal details, payment data, or private profiles.

## Migration

`20260815120000_AddConnectedSupplierCounterpartySnapshots` — four nullable varchar snapshot columns on `pos.connected_supplier_relationships`.

LocalStore schema version: unchanged (v9).

## Verification

Browser Verified: **NO**  
Device Verified: **NO**  
Production Ready: **NO**
