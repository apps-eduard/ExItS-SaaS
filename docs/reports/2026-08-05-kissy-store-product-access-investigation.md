# Investigation: Kissy Store missing POS access (2026-08-05)

## Verdict

Kissy’s `ProductAccessAssignment` was **not missing**. Start a Business created it and it remained **Active**. The blocker was a **stale entitlement snapshot** (`RefreshByUtc` elapsed with no automatic refresh). That condition was **systemic** across Local Validation orgs, not isolated Kissy data.

This was **not** an onboarding assignment defect and was **not** repaired by granting POS from Organization Owner membership alone.

## Evidence (Local Validation Platform DB)

| Check | Kissy Store result |
|---|---|
| Organization | `390ba35e-775b-4c24-893d-cf0aeecca257` Active (created 2026-08-03) |
| Owner membership | `kissy@gmail.com` / OrganizationOwner Active |
| Product access assignment | Active; reason `Start a Business product access grant.`; granted 2026-08-03 17:06:43Z |
| Assignment revoke/delete/expire | None found |
| Subscription | Active paid period through 2026-09-03 |
| Entitlement snapshot v1 | Stale before remediation (`refresh_by_utc` passed) |
| Other orgs | Same stale pattern on latest snapshots (Peter + all P18 trial orgs) |
| POS local role (kissy) | Owner revoked 2026-08-04 19:23:57Z; Active **Cashier** assigned from Mobile Org Owner essentials |

## Misleading mobile reason

Bind failures returned a generic “Product access is not allowed…” detail. Mobile `InferDeniedReasonCode` then defaulted to `product_assignment_missing`, which did not match Platform data.

## Remediation performed

1. Authorized Platform Admin (`olivia.mendoza`) called entitlement **reconcile** for Kissy Store and Peter Store.
2. Kissy evaluate returned `allowed=true`, `reasonCode=allowed` after reconcile.
3. Code fix: `EvaluateEffectiveProductAccess` lazily regenerates a fresh snapshot when past `RefreshByUtc` (does not create assignments or roles).
4. Bind denial messages now embed the effective reason code; mobile inference maps those codes correctly.

## Explicit non-changes

- No ProductAccessAssignment bootstrap from Organization Owner membership.
- No automatic POS role restore for Kissy (Active Cashier left as recorded test history).
- New-grant path still fails closed on stale snapshot until Admin reconcile or evaluate refresh has produced a current snapshot.
