# P16-WP07 — Organization Staff and Customer Separation

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `1889161d2f51fc28b8581f5afdd61b2689d2c335` (after P16-WP06 tip-hash) |
| Feature commit | *(recorded in tip-hash docs commit)* |
| Date | 2026-08-02 |

## Scope completed

- Organization Staff remains `OrganizationMembership` + built-in/custom org roles; never conflated with customers.
- Organization Staff Invitation type clarified on existing `OrganizationInvitation` (`InvitationType = OrganizationStaffInvitation`); acceptance still creates membership + staff role only.
- Staff invitation alias routes under `/api/v1/organizations/{orgId}/staff-invitations`.
- Business Customers as organization/product-owned records (`owningProductCode` optional; not Platform Users by default).
- Credit Customers as separate org-owned credit relationship records.
- Customer Link Requests with explicit accept/decline/resend/revoke lifecycle.
- Linked Customer App Users created only after explicit Customer Link acceptance.
- Product-owned customer operation APIs under Organization/product routes (not Personal Utang).
- Hard guards: no customer→staff conversion; Customer Link acceptance creates no staff membership; staff cannot expose unrelated personal utang; product-local customer routes remain isolated from Personal Scope.
- Authorization + privacy regression tests.
- EF migration `AddOrganizationStaffCustomerSeparation`.

## Files changed (high level)

- Domain: BusinessCustomer, CreditCustomer, CustomerLinkRequest, LinkedCustomerAppUser, InvitationKinds, CustomerStaffSeparationGuard; staff invitation type constant
- Application: customer/credit/link use cases + repository contracts; staff invitation DTO `invitationType`
- Infrastructure: records, repositories, DbContext, migration `AddOrganizationStaffCustomerSeparation`
- API: `BusinessCustomerEndpoints`; scope-guard exempt accept/decline; DI; ProblemDetails mappings
- Tests: `BusinessCustomerSeparationTests`, `ApiOrganizationStaffCustomerSeparationTests`

## Schema and migration changes

Migration `AddOrganizationStaffCustomerSeparation`:

| Table | Purpose |
|---|---|
| `platform.business_customers` | Org/product-owned commercial customers |
| `platform.credit_customers` | Credit relationship for one business customer |
| `platform.customer_link_requests` | Explicit customer link lifecycle + token hash |
| `platform.linked_customer_app_users` | Post-accept Linked Customer App User |

WP03–WP06 personal/org tables remain intact. No Personal Utang schema changes.

## API routes added

| Method | Route | Notes |
|---|---|---|
| GET/POST | `/api/v1/organizations/{orgId}/customers` | Business customers |
| GET/PUT | `/api/v1/organizations/{orgId}/customers/{id}` | Get / update |
| POST | `/api/v1/organizations/{orgId}/customers/{id}/archive` | Archive |
| POST | `/api/v1/organizations/{orgId}/customers/{id}/credit` | Enable Credit Customer |
| POST | `/api/v1/organizations/{orgId}/customers/{id}/promote-to-staff` | Always denied (403) |
| GET/POST | `/api/v1/organizations/{orgId}/products/{productCode}/customers` | Product-owned customer ops |
| GET | `/api/v1/organizations/{orgId}/credit-customers` | List credit customers |
| POST | `/api/v1/organizations/{orgId}/credit-customers/{id}/close` | Close credit customer |
| GET | `/api/v1/organizations/{orgId}/customer-link-requests` | List link requests |
| POST | `/api/v1/organizations/{orgId}/customers/{id}/link-requests` | Create (token once) |
| POST | `.../customer-link-requests/{id}/resend\|revoke` | Lifecycle |
| POST | `/api/v1/organizations/customer-link-requests/accept` | Explicit accept (no staff) |
| POST | `/api/v1/organizations/customer-link-requests/decline` | Explicit decline |
| GET | `/api/v1/organizations/{orgId}/linked-customer-app-users` | Linked app users |
| GET/POST | `/api/v1/organizations/{orgId}/staff-invitations` | Staff invitation alias |

Existing `/api/v1/platform/organizations/{orgId}/invitations*` and `/api/v1/platform/invitations/accept` remain (now typed as Organization Staff Invitation).

## Exit criteria

| Criterion | Evidence |
|---|---|
| Business Customer never treated as Organization Staff | DTO `isOrganizationStaff: false`; promote-to-staff → 403; domain guard |
| Customer Link acceptance creates no staff membership | Accept DTO flags false; membership list excludes linked user |
| Staff roles cannot expose unrelated personal records | Org session → Personal Utang → `AccountScopeDenied` |
| Product-local roles / routes remain isolated | Product customer route does not open Personal APIs |
| Regression suite passes | Unit 333 / Integration 166 |

## Audit coverage

- `platform.business_customer.created|updated|archived`
- `platform.credit_customer.enabled|closed`
- `platform.customer_link_request.created|resent|revoked|accepted|declined`
- Existing staff invitation audits unchanged

## Seed-data changes

None.

## Tests added

- `BusinessCustomerSeparationTests` — domain separation + invitation type discriminators
- `ApiOrganizationStaffCustomerSeparationTests` — link accept, promote denied, personal privacy, staff invite vs credit customer, product route isolation

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **333 passed**, 0 failed, 0 skipped
- Platform integration: **166 passed**, 0 failed, 0 skipped
- Build: Platform API Release — 0 errors (warning cleaned before commit)

## Explicit exclusions

- Start a Business / Utang migration (P16-WP08)
- Product-local POS role assignment UI (P16-WP09)
- Admin Ant Design Customers pages (API-first for WP07)
- External email delivery for customer link tokens
- Phase 14 production closeout
- WP02–WP06 SHAs unchanged

## Explicit next work package

**P16-WP08** — Start a Business and Utang Migration.

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**.
