# Branch Edit UX densification

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Original densify SHA | `c198de7078b18e8728ba7d729dc69986dfa1ac1b` |
| Hours editor follow-up | `ab608c36` |
| This pass starting SHA | `4a2b57017bf663fb632230af95c162f2355f2728` |
| This pass feature commit | `fddee031` |
| Migration | **No** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Densify MAUI and Organization Web Branch Edit (and the MAUI branch list) for 360–430px scan/operate speed. WP11/WP12/WP13 readiness, entitlements, inventory ownership, fulfillment APIs, branch context, device rules, and authorization are unchanged.

## What changed (original pass)

- Compact Branch setup rows. Inventory opening-stock and transfer live under the Inventory row, not two large summary buttons. Operating hours and a single Fulfillment row sit on the setup card; Online / Pickup / Delivery stay inside the Fulfillment fold.
- Details, Address & location, Operating hours, and Fulfillment use expandable sections. Details and Fulfillment start collapsed. Address opens when incomplete. Hours opens when no day is configured.
- Operating hours use compact weekday summary rows (day + effective hours + chevron). Editing is progressive: MAUI bottom sheet and Organization Web modal. Closed / 24 hours hide time controls; Hours shows Open/Close. Copy hours applies Monday’s UI values to selected days only. Page sticky Save remains authoritative.

## Follow-up (compact weekday rows)

UI-only. Same `UpsertBranchOperatingHours` model. TimeOnly binding is unchanged. Device Verified: **No**. Browser Verified: **No**.
- Sticky Cancel / Save above the MAUI bottom nav (`safe-area-inset-bottom`). Sticky Save on Organization Web.
- Timezone empty state: inherits organization timezone helper. Free delivery blank remains **None** (not coerced to 0).

## Follow-up (setup-row overflow + progressive disclosure)

UI-only. No API or readiness-evaluator change.

- Branch setup rows are entire-row tappable (`Label | status-chip | ›`). Separate right-side Manage / Set up / View / Configure text links were removed so 360–430px no longer clips actions.
- Details / Address / Hours / Fulfillment accordion headers are compact status rows. Catalog and Customers show **Organization-wide**.
- Address: line 1 primary; blank line 2 uses “Add address line 2”; city and province stack; postal + country share a row. Country is a named select that still posts the ISO code. Map coordinates stay collapsed until Map location is opened.
- Fulfillment fold defaults to three compact rows (Online / Pickup / Delivery). The long always-on checklist is gone. Delivery expands into Missing / Ready chips plus Enable / Configure. Delivery pricing stays collapsed until Configure.
- Header branch switcher (`Kizy Store` / `Main Branch ▼`) was not redesigned.
- Organization Web reuses the same hierarchy, with a two-column address grid from 768px.

## Explicit non-changes

No API, authorization, readiness evaluator, stock-copy, auto-enable, device binding, or selected-branch context behavior. Entitled ≠ Enabled ≠ Ready ≠ Operational. Staff↔branch ACL was not added in this UI pass.

## Verification

Focused Release MAUI (`BranchFulfillmentUiGuardTests`, `BranchHoursScheduleUiTests`, `BranchOperationalContextGuardTests`) and Organization Web (`OrgWebAuthErrorAndBranchesGuardTests`). Android Device Verified: **No**. Browser Verified: **No**.
