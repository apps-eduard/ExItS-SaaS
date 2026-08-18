# Branch Edit UX densification

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Starting SHA | `3027cb5f206f35916e915d589dbdc9f117bd3063` |
| Feature commit | `c198de7078b18e8728ba7d729dc69986dfa1ac1b` |
| Hours editor follow-up | `ab608c36` |
| Migration | **No** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Densify MAUI and Organization Web Branch Edit (and the MAUI branch list) for 360–430px scan/operate speed. WP11/WP12 readiness, entitlements, inventory ownership, and fulfillment APIs are unchanged.

## What changed

- Compact Branch setup rows with chips and text actions (Manage / Set up / View). Inventory opening-stock and transfer live under Set up, not two large summary buttons.
- Details, Address & location, Operating hours, and Fulfillment use expandable sections. Address opens when incomplete. Delivery pricing is shown when configuring or when policy reasons are missing.
- Operating hours use compact weekday summary rows (day + effective hours + chevron). Editing is progressive: MAUI bottom sheet and Organization Web modal. Closed / 24 hours hide time controls; Hours shows Open/Close. Copy hours applies Monday’s UI values to selected days only. Page sticky Save remains authoritative.

## Follow-up (compact weekday rows)

UI-only. Same `UpsertBranchOperatingHours` model. TimeOnly binding is unchanged. Device Verified: **No**. Browser Verified: **No**.
- Fulfillment status is label + chip + gated action. Checklist uses complete/missing semantics plus screen-reader text. No concatenated “Online ordersSetup required”.
- Sticky Cancel / Save above the MAUI bottom nav (`safe-area-inset-bottom`). Sticky Save on Organization Web.
- Timezone empty state: inherits organization timezone helper. Free delivery blank remains **None** (not coerced to 0).

## Explicit non-changes

No API, authorization, readiness evaluator, stock-copy, or auto-enable behavior. Entitled ≠ Enabled ≠ Ready ≠ Operational.

## Verification

Focused MAUI, Organization Web, and Platform branch-fulfillment unit tests. Android Device Verified: **No**. Browser Verified: **No**.
