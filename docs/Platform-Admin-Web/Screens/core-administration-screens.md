# Platform Admin Web — Core Administration Screen Specifications

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-06  
**Branch:** `docs/platform-admin-web-v2`

---

## 0. Scope and ownership boundaries

This DOC specifies the **core Platform Administration** screens for the future ExItS Platform SaaS Control Center (Platform Admin Web).

Rules:
- The Platform Admin Web covers Platform-owned control-plane administration: identities (Platform users), organizations, membership, product access/entitlements/commercial configuration, and Platform audit.
- Do **not** duplicate product operational workflows (POS/PLM operational tasks). Screen specs must avoid any product operational “do the work” behavior.
- UI must not create authority beyond server grants. All mutations are permission-gated and audited server-side.

---

## 1. Screen specification template (required fields)

Every screen below documents:
- purpose
- route concept
- primary personas
- access/authorization expectation
- data displayed
- primary actions
- secondary actions
- search
- filtering
- sorting
- pagination/infinite-loading policy
- table/card behavior
- loading state
- empty state / zero-result state
- partial/error state
- forbidden state
- destructive actions
- audit implications
- responsive behavior
- accessibility considerations
- required backend capabilities (stable capability requirement IDs)
- explicit non-goals

Stable capability IDs are defined in §6 and used below.

---

## 2. Stable capability requirement IDs (DOC-06)

These are **capability requirements** (not endpoint mappings). Backend existence is not claimed until DOC-09.

Required minimum IDs (per DOC-06):
- `PWEB-CAP-ORG-LIST`
- `PWEB-CAP-ORG-GET`
- `PWEB-CAP-ORG-CREATE`
- `PWEB-CAP-BRANCH-LIST`
- `PWEB-CAP-IDENTITY-LIST`
- `PWEB-CAP-MEMBERSHIP-LIST`

Additional practical IDs used by the screens:
- `PWEB-CAP-ORG-OVERVIEW-DATA`
- `PWEB-CAP-ORG-ACTIVITY-AUDIT`
- `PWEB-CAP-ORG-PRODUCT-ACCESS`
- `PWEB-CAP-ORG-SUBSCRIPTION-COMMERCIAL`
- `PWEB-CAP-ORG-ENTITLEMENTS`
- `PWEB-CAP-ORG-BILLING-RECORDS`
- `PWEB-CAP-IDENTITY-GET`
- `PWEB-CAP-ORGANIZATION-INVITATIONS-LIST`
- `PWEB-CAP-MEMBERSHIP-INVITE`
- `PWEB-CAP-MEMBERSHIP-REVOKE`
- `PWEB-CAP-IDENTITY-ROLE-ASSIGNMENTS`

---

## 3. A) Platform Overview / Dashboard

### Purpose
High-level operational/control-plane overview for Platform Admin users.

### Route concept
Top-level “Overview” page in the Platform shell (dashboard landing for the Admin experience).

### Primary personas
- Platform Administrator
- Operations/Support Operator (view-focused)

### Access / authorization expectation
- Requires authenticated Platform/Admin shell authorization.
- Specific data blocks must be permission-aware (server enforces; UI hides unauthorized widgets).

### Data displayed (no fake KPIs)
- Organization counts and status distribution (only where backed by Platform capability)
- Recent audit highlights (permission filtered)
- Operational “needs attention” items expressed as status summaries (not product operations)
- Recent Platform events (time-window selection)

### Primary actions
- “Open Organizations list” (navigation only)

### Secondary actions
- Refresh/reload dashboard data
- Open Audit timeline with pre-filled filters (if supported later)

### Search
No global search execution from dashboard. Dashboard widgets are navigation-only.

### Filtering / sorting
Within dashboard widgets, any filtering/sorting is internal to that widget and must map to backed server capability.

### Pagination / loading policy
- Loading uses skeleton placeholders.
- Widgets that return lists must use server paging or limited window (no infinite loading).

### Table / card behavior
- Summary widgets use cards/stat blocks.
- Audit highlights use a compact table/list.

### Loading state
- Skeleton for each widget area.
- Disable primary navigation actions only when absolutely necessary.

### Empty state / zero-result
- Empty widget shows “No data yet” (not error).

### Partial / error state
- If a widget fails, show inline widget error with “Retry” for that widget; do not blank the entire dashboard.

### Forbidden state
- If user lacks permission for a widget, widget is hidden; no partial unauthorized disclosure.

### Destructive actions
None on dashboard.

### Audit implications
- No mutations; no additional audit write requirements.

### Responsive behavior
- Cards stack into single column on smaller widths.
- Avoid horizontal overflow; keep list widgets compact.

### Accessibility considerations
- Clear headings for each widget.
- Status badges always include text labels (no color-only meaning).

### Required backend capabilities
- `PWEB-CAP-ORG-OVERVIEW-DATA`
- `PWEB-CAP-ORG-ACTIVITY-AUDIT`

### Explicit non-goals
- Do not invent or display KPIs not backed by Platform capabilities.
- Do not include POS/PLM operational workflow data.

---

## 4. B) Organizations List

Wireframe (documentation-only):
[Page header: Organizations] [Primary: Create/Open (if available)]
[Toolbar: Search] [Filters] [Reset] [Export (if permitted)]
[Summary: counts by status]
[Data table or cards]
[Pagination]

### Purpose
Provide an overview and management entry point for all organizations relevant to Platform Admin scope.

### Route concept
Organizations collection list page in the Platform shell.

### Primary personas
- Platform Administrator
- Operations/Support Operator
- Commercial/Billing Operator (if present later; view/mutation as permitted)

### Access / authorization expectation
- Server enforces:
  - view permission for listing and sorting
  - optional create permission for organization creation

### Data displayed
- Organization identity summary:
  - Organization name/identifier (and other safe fields)
  - Status (active/suspended/etc as represented by Platform model)
  - Subscription/commercial summary (only what is authorized to display)
  - Entitlement summary (only if permitted)
  - Branch count or branch availability summary (only if backed)

### Primary actions
- Primary: open organization workspace (navigation)
- Optional primary action: “Create organization” only if/when capability exists.

### Secondary actions
- Export (if permitted)
- Open organization detail in drawer/same page (if design supports)

### Search
- Search by organization name (and any additional server-supported searchable fields).

Capability requirement: search must be server-side and permission-filtered.

### Filtering
Common filters (permission-aware):
- status filter
- trial/plan filter (if backed)
- subscription state filter (if backed)
- date filters (e.g., created/updated) only if backed

### Sorting
- Sort by created/updated date, status, and (if backed) plan/trial state.

### Pagination / infinite-loading policy
- Server pagination is required (no infinite scroll) to keep audit/state predictable.

### Table / card behavior
- Desktop: data table with fixed column headers.
- Mobile/tablet: card list with key fields and a “View” button.

### Loading state
- Skeleton table/cards.

### Empty state / zero-result
- Empty state: “No organizations” (initially).
- Zero-result: “No matches for your search/filter” with a reset action.

### Partial / error state
- Inline error in the table region with “Retry”.

### Forbidden state
- If the user has no list access: show forbidden state (not a blank page).
- If only some columns are permitted: hide those columns without revealing unauthorized data.

### Destructive actions
- None directly from the list unless explicitly permitted later.
- Any destructive action must be routed to a confirm-first workflow.

### Audit implications
- No mutations on open/navigation.

### Responsive behavior
- Avoid horizontal overflow; table scroll allowed only within the table container.

### Accessibility considerations
- Table headers use semantic `<th>` scope when implemented later.
- Filters and search are keyboard accessible.

### Required backend capabilities
- `PWEB-CAP-ORG-LIST`
- `PWEB-CAP-ORG-GET` (for detail navigation bootstrap data)

Optional:
- `PWEB-CAP-ORG-CREATE` (only if creation is supported in DOC-09)

### Explicit non-goals
- Do not expose product operational workflow states (POS/PLM).

---

## 5. C) Organization Workspace / Detail

Wireframe (documentation-only):
[Breadcrumbs: Organizations > {OrgName}]
[Workspace header: Org name] [Primary: Edit (if permitted)] [Secondary: Actions dropdown]
[Tabs: Overview | Branches | People/Memberships | Products/Access | Subscription | Entitlements | Billing | Activity/Audit]
[Tab content]

### Purpose
Provide an organization-centric workspace with Platform-owned detail tabs.

### Route concept
Organization detail workspace route with selected organization context derived from the URL/deep link, and tab selection in URL-driven manner.

### Primary personas
- Platform Administrator
- Operations/Support Operator
- Security/Governance Operator (audit/privacy review via permitted tabs)

### Access / authorization expectation
- Server validates the user’s permission to view the selected organization.
- Client must not trust a client-provided organization identifier for authorization.

### Data displayed (tabs)
Canonical tabs should reconcile with DOC-02 IA:
1. Overview: profile and status summary (`PWEB-CAP-ORG-OVERVIEW-DATA`)
2. Branches: branch list (`PWEB-CAP-BRANCH-LIST`)
3. People / Memberships: membership and invites (`PWEB-CAP-MEMBERSHIP-LIST`)
4. Products: product access/entitlements summary (`PWEB-CAP-ORG-PRODUCT-ACCESS`, `PWEB-CAP-ORG-ENTITLEMENTS`)
5. Subscription: commercial state (`PWEB-CAP-ORG-SUBSCRIPTION-COMMERCIAL`)
6. Entitlements: entitlement details (`PWEB-CAP-ORG-ENTITLEMENTS`)
7. Billing: billing records (`PWEB-CAP-ORG-BILLING-RECORDS`)
8. Activity / Audit: audit events (`PWEB-CAP-ORG-ACTIVITY-AUDIT`)

### Primary actions
- Tab-dependent, server permission-aware:
  - “Edit” in Overview (if supported)
  - “Manage memberships” in People/Memberships (invite/revoke only if authorized)
  - “Adjust entitlements” only if supported and confirmed

### Secondary actions
- Export for audit/billing where permitted
- Retry data reload for failed tabs

### Search
Tab-specific search where needed:
- People/Memberships: search members/invites by identity fields (server-side)
- Activity/Audit: filter controls (time/entity/action)

### Filtering / sorting
Tab-specific filters (server-side).

### Pagination / infinite-loading
- Tables in tabs use server pagination.
- Audit timeline uses server window paging with a “Load more” policy only if explicitly backed later.

### Table / card behavior
- Overview uses cards.
- People/Memberships uses table on desktop and card list on mobile.
- Activity/Audit uses timeline/list.

### Loading state
- Skeleton per tab, not global blanking.

### Empty state / zero-result
- Empty tab shows tab-specific “No records” and optional help text.

### Partial / error state
- If one tab fails, show error just for that tab and keep other tabs navigable.

### Forbidden state
- Forbidden per tab if permission varies (server authoritative).
- If organization is forbidden entirely: workspace shows forbidden state.

### Destructive actions
Possible in People/Memberships:
- Revoke membership/invite cancellation if supported

All destructive operations:
- confirmation required
- audit required server-side

### Audit implications
- Membership changes, entitlement changes, and billing adjustments must record audit events.

### Responsive behavior
- Tabs stack and/or become a drawer/segmented control on narrow screens.
- Avoid long horizontal overflow.

### Accessibility considerations
- Tabs use accessible roles and keyboard navigation semantics (to be implemented later).
- Focus management when switching tabs/drawers.

### Required backend capabilities
Minimum:
- `PWEB-CAP-ORG-GET`
Plus tab-specific:
- `PWEB-CAP-BRANCH-LIST`
- `PWEB-CAP-MEMBERSHIP-LIST`
- `PWEB-CAP-ORG-PRODUCT-ACCESS`
- `PWEB-CAP-ORG-SUBSCRIPTION-COMMERCIAL`
- `PWEB-CAP-ORG-ENTITLEMENTS`
- `PWEB-CAP-ORG-BILLING-RECORDS`
- `PWEB-CAP-ORG-ACTIVITY-AUDIT`

### Explicit non-goals
- Do not show POS checkout/inventory/cash operations.
- Do not show PLM operational workflow processing.

---

## 6. D) Branches Administration (Platform-owned branch relationships)

### Purpose
Administer Platform-owned branch/organization relationships and branch-level details that belong to Platform scope.

### Route concept
Within the Organization workspace: “Branches” tab.

### Primary personas
- Platform Administrator
- Operations/Support Operator

### Access / authorization expectation
- Server enforces view/manage rights for branch data in the selected organization.

### Data displayed
- List of branches for the organization
- Branch identity and capacity/operating details as available in Platform scope
- Optional online/offline or availability indicators (if backed)

### Primary actions
- Navigate to Branch detail (drawer or inline expansion)

### Secondary actions
- Filter/sort branches
- Retry branch list load

### Search
- Search by branch name/identifier if backed by `PWEB-CAP-BRANCH-LIST`

### Filtering / sorting
- status/availability filters only if backed

### Pagination / infinite-loading
- Server pagination for long branch lists.

### Table / card behavior
- Desktop: table
- Mobile: card list

### Loading / empty / zero-result
- skeleton → empty/zero-result with recovery actions

### Partial/error/forbidden
- inline errors per region
- forbidden state if no organization/branch access

### Destructive actions
- If branch updates include destructive operations, they require confirm-first flows.

### Audit implications
- Branch create/update/archive actions must write audit records if/when those mutations are supported.

### Required backend capabilities
- `PWEB-CAP-BRANCH-LIST`

Optional:
- `PWEB-CAP-ORG-GET` (workspace bootstrap may already include related context)

### Explicit non-goals
- No POS device checkout operations.

---

## 7. E) Platform Users / Identity Administration

### Purpose
Administer Platform identity administration for Platform users (global identity; do not conflate with organization membership).

### Route concept
Platform shell identity administration page (e.g., Platform users list with sub-filters).

### Primary personas
- Platform Administrator
- Operations/Support Operator
- Security/Governance Operator (view/audit where permitted)

### Access / authorization expectation
- Server authorization based on Platform user administration permissions.
- Identity pages must not implicitly allow organization membership operations.

### Data displayed
- Platform user list:
  - username/display name
  - status (active/suspended/etc)
  - account class summary (as applicable)
  - last updated or last login summary only if backed
- Identity detail view:
  - core identity profile
  - linked organization membership summary (as separate, permission-filtered view)

### Primary actions
- Open identity detail
- Optional: create/invite Platform users only if capability exists

### Secondary actions
- Suspend/reactivate identity only if supported and confirmed

### Search / filtering / sorting
- Search by username/display name/email (based on server backing).
- Filters by status/account class.
- Sorting by update timestamp.

### Pagination / infinite-loading
- Server pagination.

### Table / card behavior
- Desktop: table
- Mobile: card list

### Loading / empty / zero-result
- skeleton, then empty/zero-result states

### Partial/error/forbidden
- forbidden state when no identity view access.

### Destructive actions
- identity deactivation/suspension requires confirmation and audit.

### Audit implications
- identity credential/password changes, activation/suspension, and role assignments must write audit records.

### Required backend capabilities
- `PWEB-CAP-IDENTITY-LIST`
- `PWEB-CAP-IDENTITY-GET`

Optional:
- `PWEB-CAP-IDENTITY-ROLE-ASSIGNMENTS`

### Explicit non-goals
- Do not allow attaching org staff invitations to an existing Personal identity on the UI. Membership repair flows must follow server-side identity rules (explicitly validated later in DOC-09).

---

## 8. F) Membership / Access Management

Wireframe (documentation-only):
[Breadcrumbs within org]
[Tab: People/Memberships] [Primary: Invite / Manage]
[Search] [Filters: role, status]
[Members table]
[Row actions with confirm dialogs]

### Purpose
Manage organization membership and product access facts within Platform scope.

### Route concept
Within the Organization workspace, primary tab:
- People / Memberships (membership facts, invites)
And supporting tab:
- Products / Access (product access/grants summary)

### Primary personas
- Platform Administrator
- Operations/Support Operator

### Access / authorization expectation
- UI must not create authority beyond server grants.
- Server must validate:
  - user permission to manage memberships for the selected organization
  - product access changes based on subscription/entitlements and role grants

### Data displayed
- Membership list:
  - member identity summary
  - organization role (Owner/Staff style labels per server model)
  - status (active/pending)
  - audit-relevant metadata (time, reason if required)
- Invitations list (if supported)
- Product access summary per member (only if backed by `PWEB-CAP-ORG-PRODUCT-ACCESS`)

### Primary actions
- Invite member (if supported)
- Revoke membership (if supported)
- Cancel invitation (if supported)

### Secondary actions
- View membership audit trail (if supported)
- Export membership list (if permitted)

### Search / filtering / sorting
- Search by identity fields (as backed)
- Filters by role/status
- Sorting by joined date or last updated

### Pagination / infinite-loading
- Server pagination.

### Table / card behavior
- Desktop: table with row actions
- Mobile: card list with actions in an overflow menu

### Loading / empty / zero-result
- skeleton + empty/zero-result with guided actions

### Partial/error/forbidden
- forbidden state when no membership management access

### Destructive actions
Revoke/cancel operations:
- must require explicit confirmation
- default focus on cancel
- destructive confirm must be explicit (no one-keystroke execution)

### Audit implications
- Every membership change must write audit records and must be visible in Activity/Audit tab.

### Required backend capabilities
- `PWEB-CAP-MEMBERSHIP-LIST`

Optional for management actions:
- `PWEB-CAP-MEMBERSHIP-INVITE`
- `PWEB-CAP-MEMBERSHIP-REVOKE`
- `PWEB-CAP-ORGANIZATION-INVITATIONS-LIST`
- `PWEB-CAP-ORG-PRODUCT-ACCESS`

### Explicit non-goals
- Do not provide any POS/PLM operational actions.
- Do not grant product operational permission beyond server-defined entitlement/access rules.

