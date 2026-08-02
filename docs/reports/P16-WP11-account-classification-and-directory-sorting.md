# P16-WP11 Defect Log — Account classification and directory sorting

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-07-29

## Title

Validation identities were assigned incorrect account classes and account directory filters were incomplete

## Root cause

`EnsureAccountProfilesForUser` always created a Personal account profile for every identity, then additionally created Platform and/or Organization profiles from roles and memberships. The Admin account directory therefore showed multi-class badges (for example Platform + Personal) even when Local Validation intended a single authoritative account class.

Directory filters for Platform / Organization / Needs Review were not aligned to active account-profile records. Personal Accounts remained a disabled “Coming soon” menu item despite Personal profiles existing. Table column sorting was client/page-local while the users list used server paging.

Obsolete Phase 16 `.exits.test` seed identities could remain in Local Validation databases that had previously run Development seed.

## Incorrect profile-creation or mapping behavior

- Automatic Personal companion profile on every ensure call
- Account-type badges derived from multi-profile state rather than exclusive Local Validation assignment
- Organization membership and POS roles confused with account class in catalog/menu expectations
- Personal Accounts submenu marked unimplemented

## Old seed identities removed

Local Validation initialization now decommissions known obsolete Phase 16 seed identities and related seed-owned data (profiles, memberships, product grants, sessions via user deactivate, Personal Utang contacts/relationships, and closes `phase16-seed-org`):

- `platform.admin1@exits.test`
- `platform.admin2@exits.test`
- `org.seed.owner@exits.test`
- `personal.user1@exits.test`
- `personal.user2@exits.test`

Cleanup is keyed by normalized email/username and the known seed org slug (not broad deletion).

## Final classification for the eight approved identities

| Identity | Account class | Notes |
|---|---|---|
| Olivia Mendoza | Platform only | Platform Administrator |
| Rafael Torres | Platform only | Platform Support |
| Maria Santos | Organization only | Sampaguita Owner / POS Owner |
| Carlo Reyes | Organization only | Sampaguita Staff / POS Cashier |
| Ana Cruz | Organization only | Mabuhay Owner / POS Owner |
| Daniel Garcia | Organization only | Mabuhay Staff / POS Cashier |
| Luis Navarro | Personal only | — |
| Sofia Ramos | Personal only | — |

## Personal Accounts menu correction

Platform Accounts → Personal Accounts is enabled and routes to `/admin/users/personal` (directory filter `Personal`).

## Table sorting implementation

- Server-side `sortBy` / `sortDesc` on `GET /api/v1/platform/users`
- Whitelist: `displayName`, `username`, `email`, `accountType`, `organization`, `status`, `updatedUtc`
- Sorting applied before pagination; secondary sort by user ID
- Unsupported sort fields ignored (fallback username ascending)
- Organization null/empty sorts first on ascending
- Admin Users table uses Ant Design `OnChange` + `RemoteDataSource` (Open column not sortable)
- Reset clears search, status, and sort

## Tests

- Catalog single-scope classification for all eight identities
- Exclusive preferred-class profile ensure (no unintended Personal)
- Directory filter contract counts (2 / 4 / 2)
- Personal Accounts nav enabled + route
- Sort whitelist / Open not sortable contract
- Production LocalValidation disabled does not activate seed options

## Manual validation evidence

Run Local Validation after this fix and confirm:

- All Accounts shows eight identities with one correct badge each
- Platform Accounts = 2, Organization Accounts = 4, Personal Accounts = 2
- Needs Review excludes approved identities
- No `.exits.test` identities remain
- Personal Accounts submenu enabled
- Sortable headers ascend/descend; Open not sortable; Reset works

## Remaining issues

- P16-WP11 remains In Progress
- P16-WP12 Not Started
- Phase 16 not closed
- Full end-to-end Local Validation UI pass should be re-run on a reseeded volume after deploy
