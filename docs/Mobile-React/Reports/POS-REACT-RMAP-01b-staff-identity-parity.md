# RMAP-01b STATUS

PASS

# BASELINE

starting SHA: `c93695e153aa3840df07b384d87b3a1dbff37618` (post RMAP-01)
branch: `feat/pos-react-client`
clean at start after RMAP-01 push

# CONTRACT REVIEW

backend: InvitationEndpoints + AuthEndpoints accept twins; AccountScopeGuard
MAUI: OrgStaffInvite, PersonalInvitationAccept
docs: B00 Option C; ORGANIZATION_STAFF_LATE_PERSONAL_LINK_FLOW_DEFERRED remains OPEN
contradictions: none material
owner decision needed: NO

# IMPLEMENTATION

- Staff invitation API client (create, anonymous accept, accept-as-personal)
- `/org/staff/invite` (Organization + workspace bound)
- `/personal/invitations/accept` (anonymous or Personal; Organization/Platform denied)
- Success UI: org, contact email, staff login (never label contact as login)
- Auto-bind no longer steals non-`/` routes after workspace bind

# SECURITY / ISOLATION

AccountClass gates; person-link correlation only; no late Personal link UI

# UI VALIDATION

Mobile-friendly forms/sheets patterns; Playwright covers flows (SW blocked)

# TESTS

```
npm test → 99 passed
npx playwright test e2e/rmap-01b-staff-identity.spec.ts → 5 passed
```

PREEXISTING_BASELINE_RED_START_BUSINESS: not exercised

# DOCS

This report + parity/roadmap/current-state updates

# GIT

implementation SHA: `52c8a82c6a2c513e36628d3735419c235256b214`
docs/report SHA: (pending)
remote SHA: (pending)

# FLAGS

RMAP_01B_PASS=YES
LOCAL_EQUALS_REMOTE=PENDING
WORKING_TREE_CLEAN=PENDING

# RMAP-02R RECONCILIATION

Invite authority was corrected under the locked Product Owner role/experience model.

- Prior Cashier-mocked invite success was a **frontend test defect** (false positive under AccountClass + bound workspace alone).
- Owner invite is now the allowed path (OrganizationOwner membership).
- Manager invite denied / CTA hidden; direct `/org/staff/invite` fails closed.
- Cashier invite denied / CTA hidden; direct `/org/staff/invite` fails closed.
- Direct-route denial fails closed for non-Owner principals.
- RMAP-01b remains **PASS** after RMAP-02R reconciliation.
- Historical package docs close SHA (post-RMAP-01b): `3263d6a4`
- RMAP-02R implementation SHA: `b209fd7a422cbcde8ae3ec47c0560f83d786f905`
- RMAP-02R reconciliation docs SHA: `77888cbfcccaf384838a50186e4b352dceee79f6`

See [POS-REACT-RMAP-02R-role-experience-reconciliation.md](POS-REACT-RMAP-02R-role-experience-reconciliation.md).
