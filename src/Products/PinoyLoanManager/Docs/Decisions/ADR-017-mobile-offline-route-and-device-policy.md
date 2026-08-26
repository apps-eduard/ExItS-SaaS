# ADR-017 — Mobile, offline, route, and device policy

**Status:** Accepted product policy (PLM-DOC-09); not implemented
**Date:** 2026-08-19

---

## Context

PLM MAUI field capabilities, offline behavior, collector routes, optional GPS, and device security were listed as open in PLM-00 baselines. Prior docs stated server authority and limited MAUI scope but did not close route, GPS, offline cache/draft, or device requirement decisions.

---

## Decision

1. **MAUI purpose** — limited field/collector client; not duplicate Organization Web ([../Architecture/application-surface-model.md](../Architecture/application-surface-model.md)).
2. **MVP authority** — online / server-authoritative for all final financial posting ([../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md)).
3. **MVP offline** — read-only cache and offline **drafts** allowed in planning; **offline final financial posting not authorized**.
4. **Future offline posting** — deferred; requires explicit future package with idempotency, conflict handling, cash continuity, and device security.
5. **Routes** — assignment-based collection routes; manual ordering; **no auto route optimization** in MVP ([../Product/collector-route-and-location-policy.md](../Product/collector-route-and-location-policy.md)).
6. **Location** — optional event-based GPS only when organization enables; requires policy, permission, and disclosure; **no continuous tracking**.
7. **Device security** — future requirements documented only; **no implemented security claim** ([../Security/collector-device-security-policy.md](../Security/collector-device-security-policy.md)).

---

## Consequences

Mobile field operating boundaries are approved for planning. PLM-13 remains **not started** until scaffold and owner authorization. LocalStore remains unjustified for MVP.

**Still open:** implementation, LocalStore, offline posting package, MDM/crypto details, legal sufficiency for location/disclosure (PLM-D-00-11), PLM-D-00-03 scaffold.

---

## Canonical documents

- [../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md)
- [../Product/collector-route-and-location-policy.md](../Product/collector-route-and-location-policy.md)
- [../Security/collector-device-security-policy.md](../Security/collector-device-security-policy.md)
- [../Architecture/mobile-offline-boundary.md](../Architecture/mobile-offline-boundary.md)
