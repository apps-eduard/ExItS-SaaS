# Platform Extraction Risk Matrix

[Extraction sequence](../reuse/extraction-sequence.md) | [Rollback plan](extraction-rollback-plan.md)

| Risk | Stage | Trigger | Impact | Prevention | Detection | Rollback | Owner | Blocking? |
|---|---|---|---|---|---|---|---|---|
| Identifier mismatch | 6 | Map User/Org incorrectly | Broken FK history / wrong actor | Preserve legacy product IDs; explicit mapping; no early FK rewrite | Mapping checksums; sample joins | L3 / L5 | Platform + legacy product | Yes for cutover |
| Duplicate identity | 2–6 | Email collision / remapped twice | Lockout / wrong person | Unique email rules; dry-run collision report | Duplicate report | L2 | Platform | Yes if unresolved |
| Organization mapping error | 3–6 | Wrong Platform↔legacy product org | Cross-tenant leakage | 1:N clinic mapping tests; server scope | Tenant denial tests | L3 | Platform + legacy product | **Critical** |
| Privilege escalation | 6–7 | Platform role → clinical powers | Unauthorized PHI access | Separate catalogs; no auto clinical for Platform Admin | Authz regression | L2/L3 | Security | **Critical** |
| Lost permission | 6 | Incomplete role map | Clinicians blocked | Permission matrix parity tests | Diff report vs baseline | L2 | foreign product | Yes for cutover |
| Session invalidation | 2–6 | Issuer change | Mass logout | Controlled rollout; refresh strategy | Auth failure metrics | L2 | Platform | Medium |
| Entitlement mismatch | 4–6 | Bad snapshot / override | Free features or total block | Contract tests; fail-closed | Projection status alerts | L4 | Platform | Yes if grants unpaid |
| Stale projection | 4–7 | Platform outage | Wrong commercial state | State matrix; R-022 durations later | Refresh-due metrics | L4 | Platform + product | No for docs |
| Migration partial failure | 6 | Mid-batch abort | Inconsistent maps | Idempotent batches; dry run | Failed-record report | L5 | Platform | **Critical** |
| Database restore failure | 5–6 | Bad backup | Cannot rollback | Restore rehearsal mandatory | Restore smoke | Block cutover | Ops | **Critical** |
| Contract incompatibility | 4–7 | Major version skew | Rejected events | Compatibility windows; quarantine | Unsupported-version alerts | L4 | Platform | Yes until fixed |
| legacy product regression | 6 | Any legacy product change/cutover | Product outage | Freeze early; regression gate | 1102 + Integration/E2E | L2–L5 | foreign product | **Critical** |
| Accidental legacy product root tracking | 1+ | `git add legacy product` | Nested secrets/history leak | Ignore + pre-commit checks | `git ls-files legacy product` | L0 revert | Repo lead | **Critical** |
| Clinical data leakage | 4–6 | PHI in Platform events/logs | Privacy breach | Contract prohibitions; log redaction | Payload scanners / review | L4 + purge process | Security | **Critical** |
| POS delayed by Platform overengineering | 1–5 | Waiting for full legacy product migration | Missed POS MVP | POS readiness gate without full legacy product reconnection | Schedule review | De-scope Platform extras | Portfolio lead | No for P1-WP03 |
| Password-hash copy without plan | 6 | Unsafe Identity import | Security failure | Prohibit until approved plan | Migration checklist | L2 | Security | Yes |
| Manual GCash recording errors | 7+ | Dup refs / wrong amount | Financial disputes | Warn on dup (OD-11); UX confirm | Payment audit | Product correction flow | POS | No for extraction |
| Feature flag misconfiguration | 6 | Integration on in prod early | Premature cutover | Disabled by default; allowlist | Config audit | Disable flag (L2–L4) | Platform | Yes if prod |
