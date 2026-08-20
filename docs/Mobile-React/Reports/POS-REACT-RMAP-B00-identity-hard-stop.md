# RMAP-B00 — Staff Identity Reconciliation — HARD STOP (historical)

**Status:** SUPERSEDED by PASS — see [POS-REACT-RMAP-B00-identity-reconciliation.md](POS-REACT-RMAP-B00-identity-reconciliation.md)

**Original stop code:** `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED` (resolved by Product Owner Option C + separate staff passwords)

**Baseline after RMAP-00:** `c4b82ace89a1d87d14ae4dfdd31c6c2d4e8e02ae`  
**Branch:** `feat/pos-react-client`

This document records the Master Run 01 Repair 01 hard stop. It is retained as evidence. Implementation completed in Repair 02.

## Objective (unchanged)

Reconcile CURRENT separate staff `PlatformUser` employment with owner outcome: one physical human is not duplicated merely for employment; Personal may accept staff invite; org-scoped alias remains; multi-org isolation; removal preserves Personal/other orgs.

## CURRENT confirmation (re-audited)

| Fact | Evidence |
|------|----------|
| Credential 1:1 with `PlatformUser` | `PlatformUserCredential` PK = `UserId` |
| Login lookup | Identifier with `@` → `NormalizedEmail` only (not contact email) |
| No LoginAlias table | Grep / DbContext — none |
| Staff accept | Always `CreateOrganizationStaff` + **new** password on **new** user |
| Owner multi-profile | Personal `PlatformUser` may have Personal + Organization profiles (Start a Business) |
| Staff membership on Personal | Forbidden (`HomeOrganizationRequired`) |
| Soft correlator only | `NormalizedContactEmail` string; no FK person-link |

## Why implementation did not start

Owner outcome requires same human + org-scoped login alias. That forces a credential design choice that is **not** decided in owner docs or CURRENT code:

| Decision | Inferable? |
|----------|------------|
| Same password for Personal email and org alias | **No** — only follows if forced onto single 1:1 credential |
| Separate password per alias | Inferable as **CURRENT** staff accept UX — **not** confirmed as desired post-B00 |
| Shared lockout/security stamp across email + all aliases | Not decided |
| Multi-org staff independently lockable | Conflicts with single-user 1:1 credential unless new model |

Master Run instructions: *If a crucial user-visible credential decision cannot be safely inferred… STOP BEFORE IMPLEMENTATION. Report `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED`. Do not guess.*

No silent email merge was considered — matching contact email alone is **not** proof of same human.

## Architecture options considered (not selected)

### Option A — Single `PlatformUser` + LoginAlias table, one shared credential
- Pros: True one-human; simplest password UX; keeps 1:1 credential.
- Cons: Shared lockout/stamp; must redesign `HomeOrganizationId` / staff lock for multi-org; large P19 rewrite.

### Option B — One human principal + multiple credentials (per alias/membership)
- Pros: Separate staff passwords (matches CURRENT accept UX); independent lockout.
- Cons: Breaks current credential PK/`GetByUserId` 1:1; complex stamp/session rules.

### Option C — Keep separate staff `PlatformUser` + formal person-link FK
- Pros: Minimal auth change; MAUI staff login stays independent; multi-org isolation preserved.
- Cons: Still duplicate principals; must carefully satisfy OD-ID-01 “not duplicated merely for employment” without reintroducing forbidden Personal-email-as-staff attach.

**No option was chosen.** Selection requires Product Owner (+ ChatGPT) credential-policy answers.

## Required Product Owner decisions before RMAP-B00 can resume

1. When Personal accepts Org A staff invite, is the org-scoped alias unlocked by:
   - **(A)** the same Personal password, or  
   - **(B)** a new password set at accept (CURRENT-like), or  
   - **(C)** something else?
2. If multi-org staff (Org A + Org B), are lockouts/password resets:
   - shared across all aliases, or  
   - independent per organization alias?
3. Is Option C (formal person-link with separate staff principals) acceptable as “one human” for OD-ID-01, or must there be a single `PlatformUser`?

Until answered: **do not implement schema/migrations**.

## Security standing

- No silent person merge by contact email.
- Invitation-token anti-enumeration preserved (no code change).
- Fail-closed authorization unchanged.

## Marker status

`ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` — **still open** (not resolved).

New marker for this stop: `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED`.

## Master Run impact

| Package | Status |
|---------|--------|
| RMAP-00 | **PASS** (pushed) |
| RMAP-B00 | **BLOCKED** |
| RMAP-01 … RMAP-07 | **NOT STARTED** (hard-stop) |

## Next

Product Owner sends this stop report + Master Run 01 partial report to ChatGPT.  
Issue credential-policy decision, then a repair/resume command for RMAP-B00 before any later Master Run 01 packages.
