# ADR-017 — Scope-Bound Sessions

[Decisions](README.md) | [Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-02 |
| Related | Architecture v1.5 §17, Phase 16 WP02, ADR-016, ADR-018 |

## Context

Today `PlatformAuthSession` authenticates a `PlatformUser` and may carry optional `SelectedOrganizationId`, without Account Class or Allowed Scope. Architecture v1.5 requires every session to resolve a fixed security domain so API families can deny cross-class calls before domain execution. Support Sessions are a separate context (ADR-018), not ordinary scope switching.

## Decision

1. Every authenticated session includes or server-resolves at least:

```text
UserIdentityId
AccountProfileId
AccountClass
AllowedScope
SessionId
SecurityStamp
```

2. Organization sessions additionally require server-validated:

```text
ActiveOrganizationId
ValidatedMembershipId
```

Browser-supplied organization IDs are never trusted alone.

3. Account class and allowed scope are **not** client-mutable. Changing class requires ending the session and issuing a new one for the selected profile.

4. API route families enforce the bound class/scope:

| Route family | Required session |
|---|---|
| `/platform/*` | Platform Account / Platform Scope |
| `/personal/*` | Personal Account / Personal Scope |
| `/organizations/{organizationId}/*` | Organization Account / Organization Scope (membership validated) |
| `/products/{productCode}/*` | Organization Account + entitlement + product-local authorization |

5. Product authorization must not rely solely on client claims; entitlement and product-local roles remain server-resolved (ADR-011).

6. During transition, additive session fields and dual-tolerant validation are preferred over breaking all existing Live Preview sessions in one cut (see impact matrix). Production remains blocked for Live Preview; the app is not production-ready.

## Consequences

### Positive

- Deterministic denial of cross-class API calls.
- Clear revocation story via SessionId + SecurityStamp.
- Aligns caches and audit with profile/org keys.

### Negative / Follow-on

- WP02 must extend session issuance, Admin shell context, and API guards.
- Legacy sessions without AccountClass need a defined migration or re-login path.

## Rejected alternatives

- Encoding all scopes in one JWT and trusting the client to pick.
- Using `SelectedOrganizationId` alone as proof of Organization Scope for Platform staff.
- Merging Personal and Organization API families under a single authenticated principal without class checks.
