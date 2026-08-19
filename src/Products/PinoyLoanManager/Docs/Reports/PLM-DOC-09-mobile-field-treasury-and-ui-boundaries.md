# PLM-DOC-09 — Mobile Field, Treasury, and UI Boundaries

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19

Runtime / browser / device / database / production validation: **Not Applicable**.

> **Historical note:** Open dependencies below reflect PLM-DOC-09 package completion. **PLM-D-00-03 Closed for approved layout.** **PLM-D-00-07 Closed for MVP Product financial model** (persistence/GL are implementation work). **R-091 Closed for Phase 13 scope.** Final status: [../Decisions/PLM-decision-status-summary.md](../Decisions/PLM-decision-status-summary.md).

---

## Scope

Finalize Pinoy Loan Manager MVP planning for MAUI purpose and offline posture, collector routes and optional GPS, branch treasury and float acknowledgment, Web/MAUI component sharing, and future collector device security requirements.

**Out of scope:** code, LocalStore, SQLite, offline financial posting implementation, MDM vendor selection, legal compliance claims, client scaffold on mainline.

---

## Delivered

| Area | Canonical doc |
|---|---|
| Mobile / offline operating model | [../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md) |
| Collector route / location | [../Product/collector-route-and-location-policy.md](../Product/collector-route-and-location-policy.md) |
| Branch treasury / float acknowledgment | [../Product/branch-treasury-and-float-acknowledgment-policy.md](../Product/branch-treasury-and-float-acknowledgment-policy.md) |
| Web / MAUI sharing | [../Architecture/web-maui-component-sharing-policy.md](../Architecture/web-maui-component-sharing-policy.md) |
| Collector device security (future) | [../Security/collector-device-security-policy.md](../Security/collector-device-security-policy.md) |
| ADR-017 | [../Decisions/ADR-017-mobile-offline-route-and-device-policy.md](../Decisions/ADR-017-mobile-offline-route-and-device-policy.md) |
| ADR-018 | [../Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md](../Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md) |

---

## Key decisions

### Mobile and offline

- MAUI = limited field/collector client
- MVP = **online / server-authoritative** for final financial posting
- MVP allows **read-only cache** and **offline drafts** only
- **Offline final posting deferred** (not in MVP authorized list)
- LocalStore not justified for MVP

### Routes and location

- Assignment-based collection routes
- Manual ordering; **no auto route optimization** in MVP
- Optional **event-based GPS** only with organization policy, permission, and disclosure
- **No continuous tracking**

### Branch cash

- **Branch Treasury** funds Cashier Session opening cash
- Collector float requires **two-step acknowledgment** (Pending Receipt → Received)

### UI boundaries

- **PLM-D-00-09 Closed** — shared Domain/Application/Api; separate Web and MAUI UI; conditional future RCL
- No client project authorized without PLM-D-00-03 and owner authorization

### Device security

- Future requirements documented
- **No implemented security claim**

---

## Decisions closed / resolved in this package

| ID / topic | Resolution |
|---|---|
| PLM-D-00-09 | **Closed** — Web/MAUI sharing policy |
| Online MVP authority | Resolved — server authoritative |
| Offline cache / draft | Resolved — allowed in planning; not posting |
| Offline final posting | Resolved — deferred |
| Route model | Resolved — assignment-based; no auto optimization |
| GPS | Resolved — optional event-based; no continuous tracking |
| Device security requirements | Resolved as **future requirements** (not implemented) |
| Float acknowledgment | Resolved — Pending Receipt two-step |
| Branch Treasury | Resolved — conceptual model approved |

---

## Open dependencies

| ID | Status |
|---|---|
| PLM-D-00-03 | **Closed for approved layout** (historical: Open at package completion) |
| PLM-D-00-07 | **Closed for MVP Product financial model** (historical: Open / Partially Resolved — persistence/GL implementation deferred) |
| PLM-D-00-11 | Open — legal/compliance including location disclosure |
| R-091 | **Closed for Phase 13 scope** (historical: Open at package completion) |
| Offline financial posting implementation | Deferred — future explicit WP |
| LocalStore / SQLite | Not authorized |

---

## No-code statement

Documentation only. Implementation paused. Parked scaffold unmerged.

---

## Exact next documentation package

**PLM-DOC-08 — Documents, Receipts, Reporting, Notifications, Privacy & Retention**
