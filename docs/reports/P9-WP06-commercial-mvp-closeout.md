# P9-WP06 — Commercial MVP Closeout

Phase marker: `P9-WP06-commercial-mvp-closeout`

## Status

**Complete with documented risks. Phase 9 closed.** Reconciled P9-WP01 through P9-WP05 plus prior Platform and PinoyBusinessPOS MVP capabilities as one Commercial MVP release-candidate evidence package. **No new business functionality.** HealthCare remains frozen.

Final environment decisions:

| Environment | Decision |
|---|---|
| Development | **Ready with documented non-blocking risks** |
| Testing/CI | **Ready with documented non-blocking risks** |
| Controlled internal technical pilot | **Ready with documented non-blocking risks** |
| Restricted external pilot | **Blocked** |
| Production | **Blocked** |

Feature commit: _(recorded after feature commit)_

Docs commit: _(recorded after docs hash-record)_

## Phase 9 closeout decision

Mark Phase 9 **complete with documented risks** when:

- P9-WP01–P9-WP05 controls remain intact and evidenced
- Commercial MVP capability inventory matches implementation
- Platform commercial model and database ownership remain correct
- Risk register honestly classifies blockers vs limitations
- Full `ExItS.slnx` Release tests pass
- Android Release packaging remains buildable; R-109 retained if no device
- Documentation matches implementation
- HealthCare remains frozen
- Git is clean; `main` matches `origin/main` after push

**Do not claim Production or restricted external pilot readiness** while mandatory release blockers remain open.

## Environment readiness board

Source of truth: `CommercialMvpReadinessBoard` / `dotnet run --project tools/ExItS.Deployment.Cli -- closeout-board`.

| Environment | State | Notes |
|---|---|---|
| Development | Ready with documented non-blocking risks | Dev/Testing headers allowed only here |
| Testing/CI | Ready with documented non-blocking risks | Disposable evidence; budgets ≠ SLAs |
| Controlled internal technical pilot | Ready with documented non-blocking risks | Non-production; blockers disclosed |
| Restricted external pilot | Blocked | Requires R-091 at minimum |
| Production | Blocked | R-091, R-109, R-129, TLS-PROD, MAUI-HTTPS, POS-ROLES |

## Risk and limitation register

| Id | Classification | Mitigation / next |
|---|---|---|
| R-091 | Release blocker | Implement approved production identity |
| R-109 | Release blocker | Interactive Android validation |
| R-129 / NU1903 | Release blocker | Approved local encryption remediation |
| TLS-PROD | Release blocker | Real Production cert + endpoint test |
| MAUI-HTTPS | Release blocker | HTTPS-only Production network policy |
| POS-ROLES | Release blocker | Authorize POS operational roles |
| Manual GCash | Accepted commercial limitation | Operator-confirmed, unverified |
| Online-only Basic Store | Accepted commercial limitation | Catalog/sales/inventory/expenses/reports |
| Report export | Deferred enhancement | In-app projections only |
| Category-label caveat | Accepted commercial limitation | Documented in reports |
| MVP-scale performance | Accepted commercial limitation | Provisional budgets, not SLAs |
| PITR | Deferred enhancement | Logical dump/restore MVP path |
| Local unsynced ops | Accepted commercial limitation | Outside server backups |
| Tax/refund/accounting/supplier/purchasing | Deferred enhancement | Phase 10+ |

Owner placeholders, evidence, and next actions are encoded in `CommercialMvpRiskRegister`.

## Phase 9 reconciliation

### P9-WP01 Security and Privacy

Confirmed preserved: Production rejects Dev/Testing headers outside approved envs; `/dev/*` unavailable outside approved envs; Production startup fails on insecure config / known-dev password / `AllowedHosts=*`; CORS deny-by-default; partitioned rate limits; safe ProblemDetails; empty base connection strings; no later weakening (architecture + integration guards).

### P9-WP02 Performance and Reliability

Confirmed preserved: `/health` vs `/health/ready`; justified performance indexes; sale/expense idempotency; offline BlockedByAccess reclaim; provisional budgets not SLAs; no `Migrate()` at API startup; no unsafe broad mutation retries introduced in closeout.

### P9-WP03 Backup and Restore

Confirmed preserved: independent Platform/POS backups; manifests + SHA-256; AES-GCM helper; `DESTROY_AND_RESTORE`; retention protects latest complete; PITR deferred; local unsynced device work outside server backups.

### P9-WP04 Accessibility, Localization and Theme QA

Confirmed preserved: EN/`fil-PH` parity guards; culture fallback; System/Light/Dark; skip links and dialog a11y; money/status labels; no WCAG certification claim; R-109 remains open without interactive device evidence.

### P9-WP05 Pilot and Deployment

Confirmed preserved: non-production deploy packaging; config validation fails safely; backup-before-migration gate; Plan/Migrate/Health/Smoke/Rehearsal tooling; compose labeled NON-PRODUCTION; no Dev bypass in StagingPilot; internal pilot ready with risks; Production/external not misrepresented.

## Commercial MVP capability inventory

### Platform (delivered)

Organizations, memberships, product catalog, plans, subscriptions, SaaS payments, entitlements, product access, feature grants, commercial-state enforcement, Platform Admin, audit/operational safeguards.

Authoritative DB: **ExItS_Platform**. SaaS payments remain separate from store Cash / Manual GCash / Utang. Product access does **not** grant POS operational roles.

### PinoyBusinessPOS (delivered)

Customers; Utang credits/repayments; due dates/aging; statements/receipts; catalog/SKU/barcode; Cash sales; Manual GCash sales; Product-Based Utang; basic inventory; expenses; dashboard/reports; offline foundation (supported ops); a11y/localization/themes.

Authoritative DB: **ExItS_PinoyBusinessPOS**. Catalog/sales/inventory/expenses/reports remain online-only. Manual GCash unverified.

Machine-readable inventory: `CommercialMvpCapabilityInventory`.

## Commercial model review

- Products may have separate plans and subscriptions
- Platform SaaS payments ≠ store tender types
- Product operational data stays outside Platform DB
- Continuity/read behavior for PastDue/Cancelled/Expired remains as previously delivered; Suspended/missing/stale/unknown fail closed
- Production authentication (R-091) and POS operational roles remain blockers

## Database ownership and isolation

```text
ExItS_Platform
└── organizations, users/memberships, products, plans,
    subscriptions, SaaS payments, entitlements, access, audit

ExItS_PinoyBusinessPOS
└── customers, Utang, catalog, sales, inventory,
    expenses, idempotency, operational projections
```

Rules: no cross-database FKs; no HealthCare coupling; least-privilege accounts; independently backupable/migratable.

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet test ExItS.slnx -c Release` | **1001 passed / 0 failed / 0 skipped** (baseline 987) |
| Closeout board CLI | Production + restricted external **Blocked**; internal pilot Ready with documented risks |
| Android Release | Buildable (prior P9-WP05 Signed APK path); R-109 retained without interactive device |
| HealthCare freeze | Ignored / untracked / outside `ExItS.slnx` |

## Explicit exclusions

No new tax/VAT/refunds/returns/accounting/purchasing/suppliers/payroll; no payment gateway; no PITR; no fake production auth; no Phase 10 work; no HealthCare changes.

## Exact next phase

**Phase 10 — Full POS** (do **not** begin until explicitly authorized).
