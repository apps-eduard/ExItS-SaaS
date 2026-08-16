# Post–Phase-21 Privacy Impact Refresh (P25 / P26 Delta)

> Engineering readiness reference only. **NPC compliance is NOT CLAIMED.**  
> No row or status in this document means legal approval. **LEGAL/DPO REVIEW REQUIRED.**

| Field | Value |
|---|---|
| Work package | [P21-WP11](../reports/P21-WP11-post-phase21-privacy-impact-refresh.md) |
| Phase | [Phase 21](../phases/phase-21-privacy-compliance-and-regulatory-readiness.md) — **OPEN** |
| Related open phases | [Phase 25](../phases/phase-25-organization-web-admin.md), [Phase 26](../phases/phase-26-sales-documents-compliance-readiness.md) |
| Catalog seed | `EnsurePrivacyComplianceCatalog` (additive; no migration) |
| Legal / NPC Compliant | **No — not claimed** |
| Production Ready | **No** |

---

## What changed (P25 / P26 delta)

After the original Phase 21 foundation inventory, these processing surfaces were introduced or materially hardened:

| Area | Change (engineering) | Catalog PIA |
|---|---|---|
| Typed public identity QR | Personal / Business / POS device-registration purposes; minimal public DTOs | `PIA_P25_TYPED_QR` |
| Ownership transfer | Owner handoff via Personal QR; former Owner access revoked; historical actor retained | `PIA_P25_OWNERSHIP_TRANSFER` |
| Buyer-party linking | Seller-owned customer records may link Personal or Organization identities | `PIA_P25_BUYER_PARTY` |
| Org profile independence | Organization contact/profile not live-synced from Personal; multi-org ownership | Covered under `DATA_INV_ORG_IDENTITY_MEMBERSHIP` |
| Sales-document education ack | Owner-only versioned acknowledgment (`transaction-summary-v1`); not legal certification | `PIA_P26_SALES_DOC_EDUCATION` |
| Compliance eligibility | Platform-controlled lifecycle; default off; cashiers see no review detail | `PIA_P26_COMPLIANCE_ELIGIBILITY` |
| Compliance profile anchor | Org-scoped profile with registered taxpayer name + masked TIN; branch profiles separate | `PIA_P26_COMPLIANCE_PROFILE` |
| BIR registration readiness (WP06) | Registration records + readiness evaluator; TIN is RESTRICTED COMPLIANCE (masked on DTOs) | `PIA_P26_COMPLIANCE_PROFILE` (extend) |
| Future evidence intake | Architecture / roadmap only — **not implemented** | `PIA_P26_FUTURE_EVIDENCE` |

Phase 21 foundation workspace (documents, systems, PIA views, PDF DRAFT export) remains readiness tooling only.

---

## Data classification

| Class | Examples (P25/P26) | Public QR? |
|---|---|---|
| **PUBLIC BUSINESS IDENTITY** | Display name, `PublicOrganizationId` (`ORG######`) | Yes (Business QR only) |
| **PERSONAL ACCOUNT DATA** | Display name, `PublicUserId`, minimal self-resolution status; masked email only where self-resolution allows | Personal QR: identity-minimal only |
| **ORGANIZATION INTERNAL** | Membership, ownership transfer status/actors/timestamps, org profile contact | No |
| **TRANSACTIONAL** | Sales, buyer-party snapshots, seller-owned customer records | No |
| **RESTRICTED COMPLIANCE** | Eligibility status, issuance capability flag, compliance profile timestamps/actors, **registered taxpayer name**, **TIN (normalized at rest; MaskedTin on DTOs)**, registration reference numbers, reviewer audit | No |
| **HIGHER-SENSITIVITY FUTURE** | Future government refs beyond current readiness types, signatures, uploaded evidence files | No — collect only when confirmed required |

---

## Purposes of processing

| Purpose | Notes |
|---|---|
| Ownership transfer | Secure Organization control-plane handoff; audit/security of actor history |
| Education acknowledgment | Record that current Owner reviewed current ExItS sales-document product behavior (not legal/BIR certification) |
| Compliance eligibility | Internal Platform review/authorization for future tax-document *eligibility* (issuance runtime still unavailable) |
| Compliance profile | Organization-scoped readiness (taxpayer name, masked TIN, branch profiles, registration records) |
| Future evidence | Private intake for eligibility verification when implemented — see [BIR roadmap](bir-compliance-activation-roadmap.md) |

Lawful basis / PIC–PIP conclusions: **LEGAL/DPO CLASSIFICATION REQUIRED** (`PIC_PIP_ROLE_CLASSIFICATION`). Do not hard-code one global legal role for ExItS.

---

## Access boundaries (summary)

| Data / action | Personal user | Org Owner | Org Staff / Cashier | Platform Admin | Public QR resolver |
|---|---|---|---|---|---|
| View own Personal QR / minimal identity | Self | — | Staff QR is staff identity, not Business QR | Support under Platform rules | Typed Personal purpose only |
| View Business QR (display + public org id) | No | Yes (essentials) | Policy-limited | Yes (org management) | Typed Business purpose only |
| Initiate / accept ownership transfer | Accept (Personal QR target) | Initiate (current Owner) | No | Support/repair only if explicitly authorized | No |
| Sales-document education acknowledge | No | Exact current Owner only | No | No impersonation | No |
| View compliance eligibility status (org) | No | Limited org status | No review details for cashiers | View / transition via `ManageOrganizations` | **Nothing** |
| Manage Privacy & Compliance workspace | No | No | No | `view_privacy_compliance` / `manage_privacy_compliance` | No |
| Future compliance evidence | N/A (not built) | Submit when built | No default access | Authorized reviewers only | No public URLs |

Authoritative permission rows: [authorization-matrix.md](../engineering/authorization-matrix.md).

---

## Risks and existing technical mitigations

Mitigations below are **engineering controls**. They are **not** legal “resolved” status without DPO/legal evidence.

| Risk scenario | Catalog | Technical mitigations (exist) | Residual |
|---|---|---|---|
| QR enumeration / over-disclosure | `INCIDENT_P25_P26_SCENARIOS` | Typed purpose guards; minimal DTOs; isolation tests (`OrganizationPublicIdentityIsolationTests`, `PostPhase21PublicIdentityPrivacyGuardTests`) | Abuse monitoring / rate limits may still need ops policy |
| Cross-org access | same | Org-scoped predicates; buyer ≠ sale owner; switch clears device/selling context | Continuous authz review |
| Former-owner stale auth after transfer | `PIA_P25_OWNERSHIP_TRANSFER` | Accept revokes former Owner membership/access | Session invalidation edge cases — validate in ops |
| Compliance / evidence exposure | `PIA_P26_*` | Capability/profile not on public QR; cashier UI has no review details | Evidence storage not implemented — design constraints only |
| Reviewer compromise | `PIA_P26_COMPLIANCE_ELIGIBILITY` | Platform RBAC + audit actions | Privileged-access procedures need DPO/ops |
| Offline device compromise | `SYS_MAUI_OFFLINE` | No compliance evidence on cashier devices; LocalStore unchanged for this refresh | Device theft still operational risk |

---

## Outstanding LEGAL / DPO decisions

All marked **LEGAL/DPO REVIEW REQUIRED** — none Approved:

- Lawful basis per processing purpose (identity QR, transfer, buyer link, ack, eligibility, profile, future evidence)
- **PIC/PIP:** activity-scoped classification (`PIC_PIP_ROLE_CLASSIFICATION`) — **LEGAL/DPO CLASSIFICATION REQUIRED**
- Retention periods for all P25/P26 delta categories (`RETENTION_P25_P26_DELTA`) — **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION**
- Privacy notice updates (`PRIVACY_NOTICE_P25_P26_DRAFT`) — draft technical description only
- DSAR procedure specifics for Personal vs Organization business records
- Breach notification duties for listed incident scenarios
- Vendor/processor registration before any production evidence storage (`VENDOR_FUTURE_EVIDENCE_STORAGE`)

**NPC compliance NOT CLAIMED.** No fabricated registration, submission date, or “Compliant” score.

---

## ROPA / PIA / system catalog codes

### PIA

`PIA_P25_TYPED_QR`, `PIA_P25_OWNERSHIP_TRANSFER`, `PIA_P25_BUYER_PARTY`, `PIA_P26_SALES_DOC_EDUCATION`, `PIA_P26_COMPLIANCE_ELIGIBILITY`, `PIA_P26_COMPLIANCE_PROFILE`, `PIA_P26_FUTURE_EVIDENCE`

### Data inventory (ROPA-style)

`DATA_INV_ORG_IDENTITY_MEMBERSHIP`, `DATA_INV_OWNERSHIP_TRANSFER`, `DATA_INV_BUYER_PARTY`, `DATA_INV_SALES_DOC_ACK`, `DATA_INV_COMPLIANCE_ELIGIBILITY`, `DATA_INV_COMPLIANCE_PROFILE`, `DATA_INV_FUTURE_COMPLIANCE_EVIDENCE`

### Retention / incident / notice / DPO / vendor

`RETENTION_P25_P26_DELTA`, `INCIDENT_P25_P26_SCENARIOS`, `PRIVACY_NOTICE_P25_P26_DRAFT`, `PIC_PIP_ROLE_CLASSIFICATION`, `VENDOR_FUTURE_EVIDENCE_STORAGE`

### Processing systems (additive)

`SYS_TYPED_QR_IDENTITY`, `SYS_OWNERSHIP_TRANSFER`, `SYS_BUYER_PARTY_LINKING`, `SYS_SALES_DOC_EDUCATION`, `SYS_ORG_COMPLIANCE_ELIGIBILITY`, `SYS_ORG_COMPLIANCE_PROFILE`, `SYS_FUTURE_COMPLIANCE_EVIDENCE`

Plus foundation systems: `SYS_PLATFORM`, `SYS_PERSONAL_UTANG`, `SYS_ORGANIZATION`, `SYS_POS`, `SYS_MAUI_OFFLINE`, `SYS_AUTH_IDENTITY`, `SYS_FUTURE_INTEGRATIONS`.

---

## Public QR privacy rules

1. Scan alone never grants membership, POS role, or Personal link.
2. Business QR exposes **PUBLIC BUSINESS IDENTITY** only (display name + `PublicOrganizationId`).
3. Must **not** expose: membership lists, TIN, compliance profile, eligibility, issuance capability, education acknowledgment, evidence, sales, or contact that is not already public-by-design.
4. Personal QR remains identity-minimal; device-registration QR is opaque, short-lived, single-use.
5. Callers with a known purpose must pass `expectedPurpose` and fail closed on mismatch.

See [personal-organization-identity-boundaries.md](../architecture/personal-organization-identity-boundaries.md).

---

## Offline / local

- Cashier / MAUI offline LocalStore holds operational POS cache (catalog, cart, shift, pending sync) — **not** Platform compliance evidence, eligibility review packets, or future uploaded documents.
- This refresh: **LocalStore unchanged**; no compliance evidence replicated to cashier devices.
- Offline sales remain Transaction Summaries; they are not per-sale compliance-checked against Platform eligibility.

---

## Future BIR evidence principles (not implemented)

When evidence intake is built (see [bir-compliance-activation-roadmap.md](bir-compliance-activation-roadmap.md)):

- Private, access-controlled storage only — **no public URLs**
- Register real storage/email/support vendors before production (`VENDOR_FUTURE_EVIDENCE_STORAGE`)
- Malware / type validation on upload
- No cashier / default staff access
- No AI processing of evidence unless explicitly authorized
- Deletion/disposal process required before production — **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION**
- Collect higher-sensitivity fields only when confirmed required

---

## DSAR — Personal vs Organization business records

| Subject request | Engineering principle |
|---|---|
| Personal account data | Handled under Personal / Platform identity procedures (to be finalized with DPO) |
| Organization business records (sales, customers, acknowledgments, capability, profile) | Belong to the **Organization**; DSAR for a leaving Personal user must **not** destroy accounting / business records solely because the individual left — **LEGAL/DPO REVIEW REQUIRED** |
| Ownership transfer history | Audit/security retention of `ActorUserId` — retention period not guessed |

---

## Retention

All P25/P26 delta retention categories: **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION.**

Do not implement destructive purge from guessed periods. Catalog item: `RETENTION_P25_P26_DELTA`.

---

## Related reports

- [P21-WP11 report](../reports/P21-WP11-post-phase21-privacy-impact-refresh.md)
- P25: WP06–WP09 reports · P26: WP01–WP05 reports
- [security.md Privacy Impact template](../engineering/security.md)
