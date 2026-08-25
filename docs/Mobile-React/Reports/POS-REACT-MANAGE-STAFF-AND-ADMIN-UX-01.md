# POS-REACT MANAGE-STAFF AND ADMIN UX 01

**Status:** COMPLETE  
**Start SHA:** `b8bb7ad7f8ad6d110de301eebc87a36ddef62476`  
**Implementation commit:** `efd18d135ec440acdf0a418080e5f3797bba8b39`  
**Docs commit:** `e8f1a13f9de53ca795f6c9cdf0f08105415d933f`  
**Branch:** `feat/pos-react-client`

## Delivered

Owner Manage business + sell/checkout UX wave, including MAUI-parity **Manage staff** and product-local POS role assign/revoke.

### Manage staff (primary)

| Route | Page | Capability |
|-------|------|------------|
| `/org/staff` | `OrgStaffPage` | List members + active product-local roles; invite CTA; suspend/remove; revoke role |
| `/org/staff/invite` | `OrgStaffInvitePage` | Create staff invitation (back → staff list) |
| `/org/staff/assign?userId=` | `OrgStaffAssignPage` | Assign Owner / Manager / Cashier (Owner confirm) |

- API: `product-local-roles-client.ts` → Platform `…/product-local-roles` (+ revoke)
- Members: extend `organization-members-client.ts` (list without status filter; suspend; revoke)
- Gate: `RequireInviteStaff` (Organization Owner), same as invite
- Nav: Manage business + More → **Manage staff** (`open-staff-manage` / `org-more-staff`)
- Motion: shared `exits-animate-toolbar` / `exits-list` / `exits-animate-panel` + staff-page CSS

### Related UX in this commit

- **Manage business home** (`OrgEssentialsPage`): today overview KPIs (real management/overview only), Insights / Administration / Operations / Workspace
- **Cashier home**: Open new sale + Switch workspace side-by-side when both present
- **Checkout**: collapsible payment/discount/customer; Exact cash; GCash ref under method; shift line under header; sale preview Total/Discount/Amount to Pay
- **Customer create**: Walk-in vs ExItS Personal ID choice cards
- **Sell floor**: floating View cart bar polish
- E2E helpers for checkout/customer create; i18n parity (`en`, `fil-PH`, `ceb-PH`, `hil-PH`, `ilo-PH`)

## Role management decision

| Capability | Backend | React action |
|------------|---------|--------------|
| Product-local Owner/Manager/Cashier | Ready | **Implemented** (staff assign) |
| POS DB `/permissions` hub | Ready | **Deferred** (Org Web / separate MAUI surface; not mobile essentials) |
| Custom org role definitions | Admin/Web | **Out of scope** |
| Change membership org role (Owner/Admin/Member) | Ready | **Skipped** (not on MAUI OrgStaff) |

## Tests / validation

| Check | Result |
|-------|--------|
| `npm run typecheck` | PASS |
| `message-parity.test.ts` | PASS |
| Full Playwright suite | Not re-run for this note |
| `ExItS.slnx` Release build | Not run (client-only change) |

## Exclusions

- Membership branch-assignment sheet (MAUI OrgStaff has it; not in this wave)
- React `/permissions` hub parity
- Platform Admin / Org Web custom RBAC UIs
- Backend / Platform API contract changes
- MAUI Blazor surfaces

## Identity / security note

Staff invites still create **organization staff** principals (`local@ORG######`). Contact email is recovery only. POS access requires an explicit **product-local** role grant for `pinoy-business-pos`.

## Next

- Optional: membership branch assignments on Manage staff
- Optional: Permissions hub only if product owner wants POS-DB assignment parity beyond product-local roles
