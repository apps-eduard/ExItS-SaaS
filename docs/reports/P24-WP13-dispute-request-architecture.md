# P24-WP13 — Dispute/Request Architecture

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP12](P24-WP12-regression-security-and-edge-case-tests.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (architecture contract; **implementation deferred**) |
| Date | 2026-08-12 |
| Starting SHA | `28203abc9938de8d5c1f87441d8d98a5928deadd` on `main` |
| Implementation commit | **None** (architecture / deferral only) |
| Docs commit | `6a10dbf503c35e086e72c00f8c503bb005facfae` |
| Docs/hash-stamp commit | _(pending stamp)_ |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status legend

WP13 locks the Phase-24 dispute/request architecture so Personal linked-customer statements remain read projections. Full workflow implementation is **explicitly deferred** because it expands into merchant inbox, notification delivery, and POS correction paths that would divert the approved WP16–WP24 mobile stream. **Not Device Verified. Not Production Ready.**

## Canonical WP13 scope (Phase-24)

```text
WP13 | Dispute/request architecture (optional) | Architecture-first; defer implementation if it expands
```

## Decision: architecture now, implementation later

Implementing disputes in this WP would require at minimum:

1. New Platform and/or POS persistence for dispute tickets
2. Personal create/list APIs + merchant staff inbox APIs
3. Authorization across Personal caller and organization staff
4. Notification/email or in-app delivery (vendor still open)
5. UI on Admin/POS/Personal mobile
6. Audit and non-mutating linkage to credit/repayment/sale IDs

That exceeds “architecture-first” and competes with the mandated mobile delivery stream. Therefore:

| Choice | Result |
|---|---|
| Architecture contract | **Delivered in this report + phase/ADR notes** |
| Domain / API / UI / migration | **Deferred** — not started in Phase 24 WP13 |
| Recommended follow-on | Post–Phase-24 mobile closeout, or a dedicated later phase WP after owner prioritization |

## Ownership and non-negotiable rules

```text
POS Business Utang ledger     = authoritative (CreditEntry / Repayment / Sale)
Personal linked statement     = authorized read projection only
Personal dispute/request      = customer-raised ticket / message (future)
Dispute resolution action     = merchant/staff via existing POS correction paths
```

Hard rules:

1. **Personal must never mutate** POS `CreditEntry`, `Repayment`, sale totals, or outstanding balance.
2. **Disputes do not change balances** by themselves.
3. Merchants resolve through existing payment / adjustment / void / repayment / staff correction flows.
4. Never copy POS Business Utang into Personal Utang as a “fix.”
5. Never grant Organization membership or POS staff roles so a customer can “fix” via staff APIs.
6. Authorization remains the WP03 linked-customer contract for any Personal-facing dispute create tied to a merchant ledger row.

## Proposed future request kinds

Stable machine codes (hyphenated; FeatureCode-style lowercase segments):

| Code | Intent |
|---|---|
| `unrecognized-charge` | Customer does not recognize the credit/sale |
| `amount-incorrect` | Stated amount differs from customer expectation |
| `payment-missing` | Customer believes a repayment was not recorded |
| `item-incorrect` | Receipt line / item mismatch |
| `other` | Free-text residual category |

Each request references at most one of: linked activity entry id, sale/receipt id, or statement summary context (organization + platform business customer). Guessed foreign IDs fail closed with privacy-safe **404**.

## Proposed future lifecycle (non-implemented)

```text
Draft/Submit (Personal)
  → Open (merchant-visible)
  → UnderReview (staff)
  → ResolvedAccepted | ResolvedRejected | Withdrawn
```

- Status transitions are staff/merchant-owned except Personal `Withdrawn` while `Open`.
- Resolution notes may be customer-visible; internal staff notes remain private (same privacy denylist as statements).
- Closing a dispute **never** auto-posts ledger mutations.

## Boundary sketch (deferred APIs)

Illustrative only — **not shipped**:

```text
# Personal (Platform or POS host TBD at implementation time)
POST   .../linked-customer/.../disputes
GET    .../linked-customer/.../disputes
GET    .../linked-customer/.../disputes/{id}

# Merchant staff (POS)
GET    .../customers/{id}/disputes
POST   .../disputes/{id}/status
```

Hosting decision reserved: prefer **POS-owned** tickets if they attach to ledger ids; Platform may store only notification routing metadata. No cross-database FKs.

## Explicit exclusions (WP13)

- No domain entities, repositories, migrations, or endpoints
- No Personal / POS / Admin UI for disputes
- No email/SMS/push delivery
- No auto-adjustment of credit or repayment
- No reward-point credit for filing disputes
- No Device Verified / Production Ready claim
- Does not close Phase 24 (mobile stream follows)

## Tests / builds

| Suite | Result |
|---|---|
| New automated tests | **None** (architecture / deferral package) |
| Regression re-run | Not required for docs-only architecture; WP12 suites remain the hardening baseline |

## Known limitations

- Customers currently have no in-product dispute channel for linked Business Utang
- Merchant support remains out-of-band until a later implementation WP
- Notification vendor remains an open Phase 14 / ops dependency

## Exact next WP

**P24-WP14 — Documentation / backend closeout preparation**

- Reconcile docs; no Device Verified from tests alone; do **not** close Phase 24 (mobile follows)

## Checks performed

- Starting HEAD = `origin/main` = `28203abc9938de8d5c1f87441d8d98a5928deadd`
- Migration: None
- No application code changed
- Portfolio independence preserved
