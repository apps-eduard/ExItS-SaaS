# P15-WP03 — Organization Lifecycle (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `1dbb2d5c75b59469a016c325129e77879fa69cc2` (= `origin/main` at start). Feature tip `81d19733864c4f0756d061120b156f0390d458f0`. P15-WP04 not started. Settings sidebar entries **Organization Settings** and **Branding** preserved (Branding enabled for permitted actors).

## Field decision (audit-first)

Existing domain had `DisplayName`, `Slug`, `Status` (Active / Suspended / Closed) only. Justified additions (nullable; no product-role coupling):

| Area | Fields | Notes |
|---|---|---|
| Profile | LegalName, ContactEmail, ContactPhone, AddressLine1/2, City, Region, PostalCode, CountryCode, TimeZoneId, Locale, CurrencyCode | Contact/locale metadata for Admin self-service |
| Branding | BrandDisplayName, LogoUrl (https only), PrimaryColor, AccentColor (#RRGGBB) | No binary upload; no arbitrary CSS/scripts |
| Identifiers | DisplayName, Slug | Slug remains Platform-controlled |

Unset branding falls back to organization display name and default Admin theme colors in the preview.

## Routes

| Route | Audience |
|---|---|
| `/admin/organizations` | Platform Admin list/create/filter |
| `/admin/organizations/{id}` | Detail tabs (overview, profile/settings, members link, subscriptions/payments, product access, audit) |
| `/admin/organizations/{id}/branding` | Branding editor (Platform Admin or trusted Org Admin) |
| `/admin/organizations/{id}/members` | Existing WP02 surface (unchanged) |

## Endpoints

| Method | Path | Authz |
|---|---|---|
| GET | `/api/v1/platform/organizations` | ViewPortfolio **or** ManageOrganizations; query: `page`, `pageSize`, `status`, `search`, `sortBy`, `sortDesc` |
| POST | `/api/v1/platform/organizations` | ManageOrganizations |
| GET | `/api/v1/platform/organizations/{id}` | ViewPortfolio / ManageOrganizations **or** trusted active membership |
| PUT | `/api/v1/platform/organizations/{id}` | ManageOrganizations (full incl. slug) **or** trusted Owner/Admin (profile; **no slug**) |
| PUT | `/api/v1/platform/organizations/{id}/branding` | Same as profile edit |
| POST | `…/{id}/suspend` | ManageOrganizations |
| POST | `…/{id}/reactivate` | ManageOrganizations |
| POST | `…/{id}/close` | ManageOrganizations |

Commercial summary GET uses the same view rules as organization GET (trusted Org Admin may load their org summary).

## Platform Admin capabilities

- List/search/filter/page/sort organizations
- Create organization
- Edit display name, slug, profile, branding
- Suspend / reactivate / close (no hard delete)
- View members, invitations, subscriptions, payments, entitlements, audit summaries via tabs/links

## Organization Admin capabilities

- View only trusted selected organization (active membership)
- Edit permitted profile fields and branding when org is **Active**
- Cannot change slug, lifecycle status, create/delete organizations, or edit another organization
- Cannot grant product-local roles via profile/branding edits

## Lifecycle rules

- Active → Suspended | Closed
- Suspended → Active | Closed
- Closed → (terminal; no reactivate/update)
- Suspend/close clear selected-org session binding and access-token org binding; historical memberships/subscriptions/audit retained
- No hard delete endpoint

## Branding implementation

- Domain validation: https logo URL (no markup/fragments), `#RRGGBB` colors, optional brand display name
- Preview in Admin; fallback when unset
- Binary upload deferred (residual)

## Authorization / isolation

- `PlatformOrganizationAuthz` mirrors membership authz patterns
- Trusted `OrganizationId` must match target for Org Admin path
- Cross-org profile/lifecycle mutations → 403
- Org Admin slug/lifecycle/create → 403
- Concurrency via `expectedUpdatedAtUtc` → 409
- Slug uniqueness → 409
- Profile/branding never alter platform or product-local roles

## Migration

`20260801050905_AddOrganizationProfileAndBranding` — nullable profile/branding columns on `organizations`.

## Tests

- Unit: profile/branding validation; lifecycle transitions
- Integration: list search/filter/sort; profile/branding/lifecycle/concurrency/audit; Org Admin self-service + escalation denial
- Admin/architecture guards updated as needed
- Full Release suite: **1283 passed / 0 failed / 0 skipped** (`dotnet test ExItS.slnx -c Release`, `ASPNETCORE_ENVIRONMENT=Testing`)

## Residual gaps

- Binary logo upload / virus scanning not implemented (HTTPS URL only)
- Org Admin email delivery for branding changes N/A
- Closed-org archival/export UX deferred
- P15-WP04 Products and Plans not started
- Global Admin chrome does not yet consume org branding tokens beyond the branding preview page

## Safety

- No hard delete with dependencies
- Suspension does not erase history
- Platform Admin ≠ POS access
- No cross-database access
