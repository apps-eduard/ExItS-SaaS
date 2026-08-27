# Custody State Model (State Machine B)

> Index: [README.md](README.md)  
> Ticket machine A: [../Product/pawn-transaction-model.md](../Product/pawn-transaction-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

Machine B tracks **who physically controls** a pledged item and where it sits in the release/disposition journey. State names below are planning labels; refinement is OK if semantics stay clear.

---

## Design invariants

1. Every pledged item under shop control has a **current custody state** and a **movement history**.  
2. `RELEASED` requires an explicit release event — not a payment flag alone.  
3. Disposition transfer is not a customer redemption release.  
4. States are org/branch scoped; no cross-org transitions.  
5. Initial Web/PWA custody transitions: **ONLINE-ONLY**.

---

## State catalog (analyzed / refinable)

| State | Meaning | Typical use |
|---|---|---|
| `RECEIVING` | Item accepted at counter for intake/appraisal; not yet sealed into long-term vault hold | Intake, appraisal desk |
| `IN_CUSTODY` | Item under pawnshop control as pledged collateral (vaulted) | Active / matured / unredeemed hold |
| `MOVING` | In-transit between storage nodes (short-lived) | Bin change, vault-to-counter pull |
| `RELEASE_PENDING` | Obligation payment path allows return; item pulled/verified pending handover | Post-redemption payment |
| `RELEASED` | Physically handed to customer (or authorized party per policy) | Successful redeem |
| `DISPOSITION_PENDING` | Held for disposition workflow (not customer release) | Unredeemed disposition started |
| `TRANSFERRED_FOR_DISPOSITION` | Left normal pawn vault path via authorized disposition/handoff event | Toward sale channel / Commerce handoff |

### Naming notes

| Alternative considered | Why current preferred |
|---|---|
| `HELD` vs `IN_CUSTODY` | `IN_CUSTODY` stresses legal/ops custody, not mere shelf presence |
| `IN_TRANSIT` vs `MOVING` | `MOVING` is shorter; either OK if history captures from/to |
| `AWAITING_RELEASE` vs `RELEASE_PENDING` | Equivalent; keep one |
| `DISPOSED` as terminal | Prefer disposition outcome on ticket + `TRANSFERRED_FOR_DISPOSITION` / closed ticket rather than erasing history |

Agents may rename for code clarity; do not merge `RELEASE_PENDING` into `RELEASED` or into payment state.

---

## Per-state detail

### `RECEIVING`

| Aspect | Rule |
|---|---|
| Entry | Item presented; intake started |
| Next | `IN_CUSTODY`, `MOVING`, return-to-customer cancel path (if never activated), discrepancy |
| Money | No implication of principal release |
| Notes | Photos/condition captured here feed evidence |

### `IN_CUSTODY`

| Aspect | Rule |
|---|---|
| Entry | Sealed into vault location after accept/activation path (or earlier hold policy) |
| Next | `MOVING`, `RELEASE_PENDING`, `DISPOSITION_PENDING` |
| Aligns with | Ticket `ACTIVE`, `MATURED`, `UNREDEEMED`, `RENEWAL_PENDING` |
| Notes | Default long-lived state while pledged |

### `MOVING`

| Aspect | Rule |
|---|---|
| Entry | Staff starts location change |
| Next | `IN_CUSTODY`, `RELEASE_PENDING` (counter), `DISPOSITION_PENDING` |
| Notes | Should not linger; stuck `MOVING` is an ops exception |

### `RELEASE_PENDING`

| Aspect | Rule |
|---|---|
| Entry | Redemption payment complete (or supervised dual-step that still records payment first) |
| Next | `RELEASED`, back to `IN_CUSTODY` if release aborted / mismatch |
| Notes | Wrong-item checks run here ([item-release.md](item-release.md)) |

### `RELEASED`

| Aspect | Rule |
|---|---|
| Entry | Handover acknowledged |
| Next | Terminal for this pledge cycle (history retained) |
| Aligns with | Ticket `REDEEMED` → `CLOSED` |
| Notes | Not used for disposition sales |

### `DISPOSITION_PENDING`

| Aspect | Rule |
|---|---|
| Entry | Disposition workflow started on unredeemed path |
| Next | `TRANSFERRED_FOR_DISPOSITION`, back to `IN_CUSTODY` if aborted |
| Aligns with | Ticket `DISPOSITION_PENDING` |
| Notes | Still not POS stock |

### `TRANSFERRED_FOR_DISPOSITION`

| Aspect | Rule |
|---|---|
| Entry | Authorized transfer event (internal disposition channel or future Commerce handoff) |
| Next | Terminal for pawn custody path |
| Aligns with | Ticket closing via disposition |
| Notes | [PPM-D-00-15](../risks-and-decisions.md) — explicit contract; no silent POS insert |

---

## Allowed transitions (summary)

| From → | RECEIVING | IN_CUSTODY | MOVING | RELEASE_PENDING | RELEASED | DISPOSITION_PENDING | TRANSFERRED_FOR_DISPOSITION |
|---|---|---|---|---|---|---|---|
| RECEIVING | — | ✓ | ✓ | | | | |
| IN_CUSTODY | | — | ✓ | ✓ | | ✓ | |
| MOVING | | ✓ | — | ✓ | | ✓ | |
| RELEASE_PENDING | | ✓ | ✓ | — | ✓ | | |
| RELEASED | | | | | — | | |
| DISPOSITION_PENDING | | ✓ | ✓ | | | — | ✓ |
| TRANSFERRED_FOR_DISPOSITION | | | | | | | — |

Cancel-before-activation may return item from `RECEIVING`/`IN_CUSTODY` to customer under ticket `CANCELLED` without using `RELEASED` redemption semantics — record as **intake return** movement reason to avoid polluting redemption stats (planning distinction).

---

## Prohibited transitions

| Prohibited | Why |
|---|---|
| Any → `RELEASED` because payment posted | Payment ≠ release |
| `IN_CUSTODY` → `TRANSFERRED_FOR_DISPOSITION` skipping disposition auth | Control bypass |
| `RELEASED` → `IN_CUSTODY` silently | Would hide returns; use new intake if re-pledged |
| Cross-branch state change without transfer | [PPM-D-00-16](../risks-and-decisions.md) |
| State change offline (initial PWA) | Online-only policy |

---

## Alignment with machine A (cheat sheet)

| Ticket (A) | Custody (B) expected |
|---|---|
| `DRAFT` / `APPRAISED` / `OFFERED` | `RECEIVING` or early `IN_CUSTODY` hold |
| `ACCEPTED` → `ACTIVE` | `IN_CUSTODY` |
| `RENEWAL_PENDING` | `IN_CUSTODY` (unchanged) |
| Redeem payment done | `RELEASE_PENDING` |
| `REDEEMED` | `RELEASED` |
| `UNREDEEMED` | `IN_CUSTODY` |
| `DISPOSITION_PENDING` | `DISPOSITION_PENDING` → `TRANSFERRED_FOR_DISPOSITION` |
| `CANCELLED` pre-activation | Intake return; not redemption `RELEASED` |

Inconsistencies (e.g. `ACTIVE` + `RELEASED`) are defect conditions for future integrity checks.

---

## Exclusions

- No implemented state machine  
- No IoT vault locks assumed  
- No legal “perfected pledge” claim from state names alone  
