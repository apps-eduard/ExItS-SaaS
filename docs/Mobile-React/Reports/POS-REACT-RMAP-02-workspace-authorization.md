# RMAP-02 STATUS

PASS

# BASELINE

post RMAP-01b SHA `3263d6a4`

# CONTRACT REVIEW

backend grant/context locks PROVEN; React added ensure+select Organization before bind; staff lock hides switch UI; axes kept distinct  
owner decision: NO

# IMPLEMENTATION

- `ensure-organization-profile.ts` (MAUI ensure+select parity)
- `WorkspaceProvider.bindWorkspace` Personal→Organization handoff; HomeOrganization lock check
- `AppTopBar` / `HomePage` hide switch when `organizationContextLocked`
- Sell access denied test id; product-access fail-closed at bind

# TESTS

npm test 102 passed; Playwright rmap-02 + auth 6 passed

# FLAGS

RMAP_02_PASS=YES
