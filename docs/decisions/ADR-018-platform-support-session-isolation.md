# ADR-018 — Platform Support Session Isolation

[Decisions](README.md) | [Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-02 |
| Related | Architecture v1.5 §5.4, Phase 16, ADR-016, ADR-017 |

## Context

Platform Accounts operate only in Platform Scope. Historical or UI patterns that place Platform Administration in an organization switcher would collapse vendor and tenant security domains. Architecture v1.5 requires tenant operational access only through an explicit Support Session — not ordinary navigation or membership.

## Decision

1. **Platform Administration is never part of the normal Organization switcher.** Platform users do not become tenant staff through ordinary navigation.
2. Tenant operational access from Platform staff occurs only via a **Support Session** that is:
   - permission-gated
   - organization-specific
   - time-limited
   - reason-required
   - prominently displayed in the UI
   - fully audited (start, extend, elevate, end, deny)
   - revocable
   - **read-only by default**
3. Write access requires an explicit elevated support permission and, where appropriate, approval. Support Sessions must not silently change record ownership or authorship.
4. A Support Session is a **separate audited session context**, not a change of Account Class to Organization and not a substitute for Organization membership.
5. Platform sessions continue to call `/platform/*` for vendor administration; Support Session access to tenant data is gated and audited distinctly from Organization Account sessions.

```text
Platform Account session
→ Platform APIs only

Support Session (optional overlay / separate context)
→ one organization, time-limited, reason-required, audited
→ read-only default; elevate only with explicit permission
```

## Consequences

### Positive

- Clear separation of SaaS vendor operations from tenant staff.
- Auditable break-glass path without polluting membership tables.
- Reduces accidental privilege when browsing organizations in Admin.

### Negative / Follow-on

- Support Center UX and APIs must be built (later Phase 16 WPs / hardening).
- Existing Admin patterns that treat selected org as ambient tenant identity must be narrowed.

## Rejected alternatives

- Adding Platform Admin users to Organization membership for convenience.
- Showing Platform Administration in the same switcher as tenant organizations.
- Unbounded or write-by-default support access without reason or audit.
