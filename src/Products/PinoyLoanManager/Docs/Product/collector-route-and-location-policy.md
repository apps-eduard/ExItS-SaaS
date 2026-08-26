# Pinoy Loan Manager — Collector Route and Location Policy

**Status:** Accepted product policy (PLM-DOC-09); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Route assignment and optional location capture for field collectors. Complements [daily-operational-workflow.md](daily-operational-workflow.md), [collections-case-and-promise-to-pay-policy.md](collections-case-and-promise-to-pay-policy.md), and [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md).

Authorization: `plm.collection-assignments.manage` and assigned-work scope ([../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md)).

---

## Route model

A **collection route** is an organization-scoped assignment container for collector field work. It is operational planning, not a navigation product.

Conceptual contents (not a schema):

- organization and branch
- assigned collector(s)
- effective date or period
- ordered or grouped list of assigned borrowers / loans / collection tasks
- optional notes or branch area label
- status (Draft, Active, Completed, Cancelled)

Routes derive from authorized assignments. Collectors must not browse all organization borrowers by default.

### Assignment sources

Routes may include work from:

- due collections for assigned borrowers
- approved field disbursement tasks
- Collection Case follow-ups
- Promise to Pay visit tasks
- manager-assigned exceptions

Eligibility for route inclusion does **not** imply payment success or loan approval changes.

---

## Ordering and optimization

| Rule | Policy |
|---|---|
| Route creation | Manager / authorized staff assigns borrowers or tasks to a route |
| Ordering | Manual ordering or stable list order; collector may reorder **within assigned work** if organization policy allows |
| Auto route optimization | **Not in MVP** — no automatic reordering, map routing, or third-party optimization engine |
| Mandatory sequence enforcement | **Not required in MVP** — system records visit order as captured, not as enforced turn-by-turn navigation |

Organizations may adopt external route planning outside PLM. PLM does not require integration with external map or fleet systems in MVP.

---

## Location capture — optional, event-based only

GPS or location capture is **optional**, **organization-configurable**, and **never continuous**.

### Allowed capture points (when enabled)

Location may be captured only at explicit operational events, for example:

- start route (optional single capture)
- visit attempt recorded (payment, missed collection, PTP note)
- field disbursement release event
- end route / remittance preparation (optional single capture)

Each capture is tied to a **business event** with audit metadata. PLM must not maintain a live location stream or background tracking loop.

### Required controls when GPS is enabled

Before any location capture is used in Production, the product design must support:

1. **Organization policy toggle** — GPS off by default unless organization explicitly enables
2. **Role permission** — only authorized collectors on assigned work
3. **Staff disclosure** — collectors informed that event location may be stored for operational audit
4. **Borrower-facing disclosure** — where required by organization policy and applicable law; PLM does not invent legal text (PLM-D-00-11)
5. **Data minimization** — store event coordinates and timestamp; not continuous track history
6. **Retention** — follow organization audit / privacy policy; exact retention schedule remains open outside PLM-DOC-09

When GPS is disabled, field workflows must remain usable without location fields.

---

## Explicit prohibitions

- **No continuous tracking** — no background geolocation polling, no live map surveillance mode, no always-on device tracking
- **No automatic route optimization** in MVP
- **No borrower location sharing to Personal** as a default feature
- **No treating GPS capture as proof of payment** — location is supplementary audit context only
- **No covert tracking** — undisclosed always-on location collection is forbidden by product policy

---

## Offline interaction

When offline, route and assignment lists may appear from read-only cache ([../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md)). Visit drafts may queue locally but are not authoritative until synced.

Location capture while offline may be stored as draft event metadata pending upload; server validation and policy checks apply on sync.

---

## Explicit non-goals

- Turn-by-turn navigation product
- Fleet management or telematics platform
- Geofence-based automatic payment posting
- Mandatory GPS for all organizations
- Legal sufficiency claims for consent notices (PLM-D-00-11)
