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

# GIT

implementation SHA: `916dc63988c4985a40104dfc1aa2bc853e289885`
docs/report SHA: `7bfe5d8c568d81f6fe96207777431d2441b51ee2`

# FLAGS

RMAP_02_PASS=YES

# RMAP-02R RECONCILIATION

Authority dimensions and experience eligibility were reconciled without weakening POS operational capabilities.

- AccountClass / organization membership / product-local POS role remain distinct.
- OrganizationAdministrator is explicit admin-side authority.
- StoreManager / Manager is POS operations authority (not automatic Organization administration).
- StoreManager alone no longer enters admin experience (`/org`).
- Owner may enter Admin / Operations / Selling experiences.
- Manager may enter Operations / Selling (when CreateSale permits).
- Cashier: selling experience only.
- Experience selection does **not** mutate security role.
- RMAP-02 remains **PASS** after RMAP-02R.

RMAP-02R implementation SHA: `b209fd7a422cbcde8ae3ec47c0560f83d786f905`

See [POS-REACT-RMAP-02R-role-experience-reconciliation.md](POS-REACT-RMAP-02R-role-experience-reconciliation.md).
