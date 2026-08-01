# ADR-016 — Account Profile Isolation

[Decisions](README.md) | [Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-02 |
| Related | Architecture v1.5, Phase 16, ADR-017, ADR-018 |

## Context

The current Platform model uses a flat `PlatformUser` plus `PlatformAuthSession` with optional `SelectedOrganizationId`. Architecture v1.5 replaces unrestricted multi-scope use with **isolated account profiles**: one verified person may hold Platform, Personal, and Organization profiles, but each session is bound to exactly one account class. Without this ADR, implementation risks treating directory filters or org selection as permission inheritance across scopes.

## Decision

1. A **User Identity** represents the verified person (authentication and recovery). It is not a role, organization, or product entitlement.
2. Every operational login uses an **Account Profile** with exactly one **Account Class**:
   - Platform Account → Platform Scope only
   - Personal Account → Personal Scope only
   - Organization Account → Organization Scope only
3. One person may own multiple account profiles; profiles do not inherit permissions from one another.
4. One authenticated session is bound to exactly one account profile and one allowed scope. Cross-account-class access is prohibited.
5. Account-class selection is a distinct step from organization switching (organization switching occurs only inside Organization Scope among active memberships).
6. Persistence may retain `PlatformUser` as the initial table/aggregate name while mapping conceptually to User Identity (and later Account Profiles). Terminology in APIs and Admin must follow architecture v1.5; see [P16-WP01 impact matrix](../architecture/p16-wp01-entity-api-impact-matrix.md).

```text
One verified person (User Identity)
├── Platform Account Profile
├── Personal Account Profile
└── Organization Account Profile

One session → one AccountClass → one AllowedScope
```

## Consequences

### Positive

- Compromise of one session class does not grant APIs of another class.
- Clear Admin directory views: Platform / Organization / Personal Accounts (not “Personal Product Users”).
- Aligns Phase 16 WP02+ implementation with architecture v1.5.

### Negative / Follow-on

- Existing flat sessions must be evolved (ADR-017); transitional compatibility is required.
- Admin and API surface will need profile-aware language without a big-bang rename of persistence.

## Rejected alternatives

- Single session with multi-scope claims and client-side switching.
- Treating Platform Admin as an organization membership in the org switcher.
- Inferring Personal or Platform access from Organization membership alone.
